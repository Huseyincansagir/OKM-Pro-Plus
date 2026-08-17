using System.Text.Json;
using FactoryErp.Application.Abstractions.Persistence;
using FactoryErp.Application.Shipping;
using FactoryErp.Domain.Common;
using FactoryErp.Domain.Shipping;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.Shipping;

public sealed partial class LoadPlanCommandService
{
    public async Task<LoadPlanValidationDto> ValidateLoadPlanAsync(
        Guid loadPlanId,
        ValidateLoadPlanRequest request,
        long expectedRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        DomainGuard.AgainstEmpty(loadPlanId, "LOAD_PLAN_REQUIRED", "LoadPlan zorunludur.");
        var scope = $"load-plan:validate:{actorId}:{loadPlanId}";
        var payloadHash = ComputePayloadHash(new { loadPlanId, request });
        var replay = await TryReplayAsync<LoadPlanValidationDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var plan = await LockLoadPlanGraphAsync(loadPlanId, cancellationToken);
        if (plan is null)
        {
            throw new DomainException(new("LOAD_PLAN_NOT_FOUND", "LoadPlan bulunamadı."));
        }

        EnsureExpectedVersion(plan.RowVersion, expectedRowVersion, nameof(LoadPlanRecord), plan.Id);
        EnsureMutablePlan(plan);
        var shipment = await LockShipmentAsync(plan.ShipmentId, cancellationToken)
            ?? throw new DomainException(new("SHIPMENT_NOT_FOUND", "LoadPlan shipment kaydı bulunamadı."));
        var routePlan = await LockRoutePlanAsync(plan.RoutePlanId, cancellationToken)
            ?? throw new DomainException(new("ROUTE_PLAN_NOT_FOUND", "LoadPlan route plan kaydı bulunamadı."));
        await LockRouteStopsAsync(routePlan.Id, cancellationToken);
        await LockShipmentPackagesAsync(plan.ShipmentId, cancellationToken);
        await LockLoadUnitsAsync(plan.Id, cancellationToken);
        await LoadLoadUnitGraphAsync(plan.Id, cancellationToken);
        await LockVehicleAndCapacityAsync(plan, cancellationToken);

        plan.Status = nameof(LoadPlanStatus.Validating);
        var candidates = await EvaluateValidationCandidatesAsync(plan, routePlan, cancellationToken);
        await UpsertValidationResultsAsync(plan, candidates, actorId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var results = await dbContext.LoadPlanValidationResults
            .Where(x => x.LoadPlanId == plan.Id)
            .OrderBy(x => x.Severity)
            .ThenBy(x => x.ValidationKey)
            .ToListAsync(cancellationToken);
        ApplyValidationSummary(plan, results);
        plan.UpdatedAt = DateTimeOffset.UtcNow;
        await auditWriter.AppendAsync(new(
            "LoadPlanValidated",
            nameof(LoadPlanRecord),
            plan.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new
            {
                plan.Status,
                plan.FeasibilityStatus,
                hardErrors = results.Count(x => x.Severity == nameof(LoadPlanValidationSeverity.HardError) && x.ResolutionStatus == nameof(LoadPlanValidationResolutionStatus.Open)),
                warnings = results.Count(x => x.Severity == nameof(LoadPlanValidationSeverity.Warning) && x.ResolutionStatus == nameof(LoadPlanValidationResolutionStatus.Open)),
            })), cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        var result = new LoadPlanValidationDto(Map(plan), results.Select(Map).ToArray());
        await SaveIdempotencyAsync(scope, idempotencyKey, payloadHash, 200, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyCollection<LoadPlanValidationResultDto>> GetValidationResultsAsync(
        Guid loadPlanId,
        CancellationToken cancellationToken = default)
        => (await dbContext.LoadPlanValidationResults
                .AsNoTracking()
                .Where(x => x.LoadPlanId == loadPlanId)
                .OrderBy(x => x.Severity)
                .ThenBy(x => x.ValidationKey)
                .ToListAsync(cancellationToken))
            .Select(Map)
            .ToArray();

    public async Task<LoadPlanValidationResultDto> ResolveValidationResultAsync(
        Guid loadPlanId,
        Guid validationResultId,
        ResolveLoadPlanValidationRequest request,
        long expectedRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        DomainGuard.AgainstEmpty(loadPlanId, "LOAD_PLAN_REQUIRED", "LoadPlan zorunludur.");
        DomainGuard.AgainstEmpty(validationResultId, "VALIDATION_RESULT_REQUIRED", "Validation result zorunludur.");
        var scope = $"load-plan:validation-resolution:{actorId}:{loadPlanId}:{validationResultId}";
        var payloadHash = ComputePayloadHash(new { loadPlanId, validationResultId, request });
        var replay = await TryReplayAsync<LoadPlanValidationResultDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var plan = await LockLoadPlanGraphAsync(loadPlanId, cancellationToken)
            ?? throw new DomainException(new("LOAD_PLAN_NOT_FOUND", "LoadPlan bulunamadı."));
        EnsureExpectedVersion(plan.RowVersion, expectedRowVersion, nameof(LoadPlanRecord), plan.Id);
        EnsureMutablePlan(plan);
        var validation = await dbContext.LoadPlanValidationResults
            .SingleOrDefaultAsync(x => x.Id == validationResultId && x.LoadPlanId == loadPlanId, cancellationToken)
            ?? throw new DomainException(new("VALIDATION_RESULT_NOT_FOUND", "Validation result bulunamadı."));

        var resolutionStatus = ParseResolutionStatus(request.ResolutionStatus);
        if (validation.Severity == nameof(LoadPlanValidationSeverity.HardError))
        {
            throw new DomainException(new(
                "LOAD_PLAN_HARD_ERROR_RESOLUTION_FORBIDDEN",
                "Hard error resolution veya override ile kapatılamaz; plan yeniden düzenlenmelidir."));
        }

        ApplyResolution(validation, resolutionStatus, actorId, request.Reason);
        plan.Status = nameof(LoadPlanStatus.NeedsReview);
        plan.UpdatedAt = DateTimeOffset.UtcNow;
        await auditWriter.AppendAsync(new(
            "LoadPlanValidationResolved",
            nameof(LoadPlanValidationResultRecord),
            validation.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { validation.ResolutionStatus, validation.ResolutionReason })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = Map(validation);
        await SaveIdempotencyAsync(scope, idempotencyKey, payloadHash, 200, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<LoadPlanDto> CreateManualChangeAsync(
        Guid loadPlanId,
        CreateLoadPlanManualChangeRequest request,
        long expectedRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        DomainGuard.AgainstEmpty(loadPlanId, "LOAD_PLAN_REQUIRED", "LoadPlan zorunludur.");
        var scope = $"load-plan:manual-change:{actorId}:{loadPlanId}";
        var payloadHash = ComputePayloadHash(new { loadPlanId, request });
        var replay = await TryReplayAsync<LoadPlanDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var plan = await LockLoadPlanGraphAsync(loadPlanId, cancellationToken)
            ?? throw new DomainException(new("LOAD_PLAN_NOT_FOUND", "LoadPlan bulunamadı."));
        EnsureExpectedVersion(plan.RowVersion, expectedRowVersion, nameof(LoadPlanRecord), plan.Id);
        EnsureMutablePlan(plan);
        await LoadLoadUnitGraphAsync(plan.Id, cancellationToken);
        var change = LoadPlanManualChange.Create(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            loadPlanId,
            actorId,
            ParseEnum<LoadPlanManualChangeType>(request.ChangeType, "MANUAL_CHANGE_TYPE_INVALID"),
            request.EntityType,
            request.EntityId,
            request.BeforeJson,
            request.AfterJson,
            request.Reason);
        dbContext.LoadPlanManualChanges.Add(new LoadPlanManualChangeRecord
        {
            Id = change.Id,
            LoadPlanId = change.LoadPlanId,
            ActorUserId = change.ActorUserId,
            ChangeType = change.ChangeType.ToString(),
            EntityType = change.EntityType,
            EntityId = change.EntityId,
            BeforeJson = change.BeforeJson,
            AfterJson = change.AfterJson,
            Reason = change.Reason,
            CreatedAt = change.CreatedAt,
        });
        plan.Status = nameof(LoadPlanStatus.NeedsReview);
        plan.UpdatedAt = DateTimeOffset.UtcNow;
        await auditWriter.AppendAsync(new(
            "LoadPlanManualChangeRecorded",
            nameof(LoadPlanManualChangeRecord),
            change.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { change.ChangeType, change.EntityType, change.EntityId, change.Reason })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = Map(plan);
        await SaveIdempotencyAsync(scope, idempotencyKey, payloadHash, 200, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<LoadPlanDto> LockLoadPlanAsync(
        Guid loadPlanId,
        LockLoadPlanRequest request,
        long expectedRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        DomainGuard.AgainstEmpty(loadPlanId, "LOAD_PLAN_REQUIRED", "LoadPlan zorunludur.");
        var scope = $"load-plan:lock:{actorId}:{loadPlanId}";
        var payloadHash = ComputePayloadHash(new { loadPlanId, request });
        var replay = await TryReplayAsync<LoadPlanDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var plan = await LockLoadPlanGraphAsync(loadPlanId, cancellationToken)
            ?? throw new DomainException(new("LOAD_PLAN_NOT_FOUND", "LoadPlan bulunamadı."));
        EnsureExpectedVersion(plan.RowVersion, expectedRowVersion, nameof(LoadPlanRecord), plan.Id);
        var shipment = await LockShipmentAsync(plan.ShipmentId, cancellationToken)
            ?? throw new DomainException(new("SHIPMENT_NOT_FOUND", "LoadPlan shipment kaydı bulunamadı."));
        var routePlan = await LockRoutePlanAsync(plan.RoutePlanId, cancellationToken)
            ?? throw new DomainException(new("ROUTE_PLAN_NOT_FOUND", "LoadPlan route plan kaydı bulunamadı."));
        await LockRouteStopsAsync(routePlan.Id, cancellationToken);
        await LockShipmentPackagesAsync(shipment.Id, cancellationToken);
        await LockLoadUnitsAsync(plan.Id, cancellationToken);
        await LoadLoadUnitGraphAsync(plan.Id, cancellationToken);
        var vehicle = await LockVehicleAndCapacityAsync(plan, cancellationToken);

        var currentResults = await dbContext.LoadPlanValidationResults
            .Where(x => x.LoadPlanId == plan.Id)
            .ToListAsync(cancellationToken);
        foreach (var resolution in request.WarningResolutions ?? Array.Empty<WarningResolutionInput>())
        {
            var validation = currentResults.SingleOrDefault(x => x.Id == resolution.ValidationResultId)
                ?? throw new DomainException(new("VALIDATION_RESULT_NOT_FOUND", "Lock warning resolution validation result bulunamadı."));
            if (validation.Severity != nameof(LoadPlanValidationSeverity.Warning))
            {
                throw new DomainException(new(
                    "LOAD_PLAN_HARD_ERROR_OVERRIDE_FORBIDDEN",
                    "Yalnızca warning validation sonuçları lock sırasında override edilebilir."));
            }

            ApplyResolution(validation, ParseLockAction(resolution.Action), actorId, resolution.Reason);
        }

        var openHardErrors = currentResults.Any(x => x.Severity == nameof(LoadPlanValidationSeverity.HardError)
            && x.ResolutionStatus == nameof(LoadPlanValidationResolutionStatus.Open));
        var openWarnings = currentResults.Any(x => x.Severity == nameof(LoadPlanValidationSeverity.Warning)
            && x.ResolutionStatus == nameof(LoadPlanValidationResolutionStatus.Open));
        var hasOverride = currentResults.Any(x => x.Severity == nameof(LoadPlanValidationSeverity.Warning)
            && x.ResolutionStatus == nameof(LoadPlanValidationResolutionStatus.Overridden));

        var domainPlan = RehydrateDomainPlan(plan);
        domainPlan.Lock(actorId, DateTimeOffset.UtcNow, request.Approval, openHardErrors, openWarnings, hasOverride);
        plan.Status = domainPlan.Status.ToString();
        plan.ApprovedBy = domainPlan.ApprovedBy;
        plan.ApprovedAt = domainPlan.ApprovedAt;
        plan.LockedBy = domainPlan.LockedBy;
        plan.LockedAt = domainPlan.LockedAt;
        foreach (var unit in plan.LoadUnits)
        {
            unit.Status = nameof(LoadUnitStatus.Locked);
            unit.UpdatedAt = DateTimeOffset.UtcNow;
        }
        plan.UpdatedAt = DateTimeOffset.UtcNow;
        await auditWriter.AppendAsync(new(
            "LoadPlanLocked",
            nameof(LoadPlanRecord),
            plan.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new
            {
                plan.Status,
                plan.ApprovedBy,
                plan.LockedBy,
                vehicleId = vehicle?.Id,
                plan.VehicleCapacityId,
                warningOverrideApproved = hasOverride,
            })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = Map(plan);
        await SaveIdempotencyAsync(scope, idempotencyKey, payloadHash, 200, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private async Task<LoadPlanRecord?> LockLoadPlanGraphAsync(Guid loadPlanId, CancellationToken cancellationToken)
    {
        var plan = await dbContext.LoadPlans
            .FromSqlInterpolated($"SELECT * FROM load_plans WHERE id = {loadPlanId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (plan is null)
        {
            return null;
        }

        return plan;
    }

    private async Task LoadLoadUnitGraphAsync(Guid loadPlanId, CancellationToken cancellationToken)
        => await dbContext.LoadUnits
            .Where(x => x.LoadPlanId == loadPlanId)
            .Include(x => x.Items)
                .ThenInclude(x => x.StopAllocations)
            .LoadAsync(cancellationToken);

    private async Task LockRouteStopsAsync(Guid routePlanId, CancellationToken cancellationToken)
        => await dbContext.RouteStops
            .FromSqlInterpolated($"SELECT * FROM route_stops WHERE route_plan_id = {routePlanId} ORDER BY sequence_no, id FOR UPDATE")
            .ToListAsync(cancellationToken);

    private async Task LockShipmentPackagesAsync(Guid shipmentId, CancellationToken cancellationToken)
        => await dbContext.ShipmentPackages
            .FromSqlInterpolated($"SELECT * FROM shipment_packages WHERE shipment_id = {shipmentId} ORDER BY id FOR UPDATE")
            .ToListAsync(cancellationToken);

    private async Task LockLoadUnitsAsync(Guid loadPlanId, CancellationToken cancellationToken)
        => await dbContext.LoadUnits
            .FromSqlInterpolated($"SELECT * FROM load_units WHERE load_plan_id = {loadPlanId} ORDER BY unit_code, id FOR UPDATE")
            .ToListAsync(cancellationToken);

    private async Task<VehicleRecord?> LockVehicleAndCapacityAsync(LoadPlanRecord plan, CancellationToken cancellationToken)
    {
        VehicleRecord? vehicle = null;
        if (plan.VehicleId is not null)
        {
            vehicle = await dbContext.Vehicles
                .FromSqlInterpolated($"SELECT * FROM vehicles WHERE id = {plan.VehicleId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (vehicle is null)
            {
                throw new DomainException(new("VEHICLE_NOT_FOUND", "LoadPlan vehicle kaydı bulunamadı."));
            }
        }

        if (plan.VehicleCapacityId is not null)
        {
            var capacity = await dbContext.VehicleCapacities
                .FromSqlInterpolated($"SELECT * FROM vehicle_capacities WHERE id = {plan.VehicleCapacityId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (capacity is null)
            {
                throw new DomainException(new("VEHICLE_CAPACITY_NOT_FOUND", "LoadPlan vehicle capacity kaydı bulunamadı."));
            }

            if (vehicle is not null && capacity.VehicleTypeId != vehicle.VehicleTypeId)
            {
                throw new DomainException(new("CAPACITY_VEHICLE_TYPE_MISMATCH", "Vehicle ve capacity aynı vehicle type'a ait olmalıdır."));
            }
        }

        return vehicle;
    }

    private async Task<IReadOnlyCollection<ValidationCandidate>> EvaluateValidationCandidatesAsync(
        LoadPlanRecord plan,
        RoutePlanRecord routePlan,
        CancellationToken cancellationToken)
    {
        var candidates = new List<ValidationCandidate>();
        var now = DateTimeOffset.UtcNow;
        var units = plan.LoadUnits.OrderBy(x => x.UnitCode, StringComparer.Ordinal).ToArray();
        if (units.Length == 0)
        {
            candidates.Add(new("plan:empty", LoadPlanValidationSeverity.HardError, "LOAD_PLAN_EMPTY", "LoadPlan en az bir LoadUnit içermelidir.", nameof(LoadPlanRecord), plan.Id));
        }

        if (plan.FeasibilityStatus == nameof(LoadPlanFeasibilityStatus.Infeasible))
        {
            candidates.Add(new("plan:ffd-infeasible", LoadPlanValidationSeverity.HardError, "LOAD_PLAN_INFEASIBLE", "FFD/vehicle fit sonucu planı uygulanabilir göstermiyor.", nameof(LoadPlanRecord), plan.Id));
        }
        if (plan.VehicleId is null)
        {
            candidates.Add(new("vehicle:required", LoadPlanValidationSeverity.Warning, "VEHICLE_REQUIRED", "Lock öncesinde bir araç seçilmelidir.", nameof(LoadPlanRecord), plan.Id));
        }
        if (plan.VehicleCapacityId is null)
        {
            candidates.Add(new("capacity:required", LoadPlanValidationSeverity.Warning, "CAPACITY_REQUIRED", "Lock öncesinde effective vehicle capacity seçilmelidir.", nameof(LoadPlanRecord), plan.Id));
        }
        if (string.IsNullOrWhiteSpace(plan.InputSnapshotHash))
        {
            candidates.Add(new("snapshot:input-required", LoadPlanValidationSeverity.Warning, "INPUT_SNAPSHOT_HASH_REQUIRED", "Plan input snapshot hash içermelidir.", nameof(LoadPlanRecord), plan.Id));
        }

        foreach (var unit in units)
        {
            if (unit.Status is nameof(LoadUnitStatus.Cancelled) or nameof(LoadUnitStatus.Loaded))
            {
                candidates.Add(new($"unit:{unit.Id}:state", LoadPlanValidationSeverity.HardError, "LOAD_UNIT_INVALID_STATE", "Loaded veya Cancelled LoadUnit lock edilemez.", nameof(LoadUnitRecord), unit.Id));
            }

            foreach (var duplicate in unit.Items.GroupBy(x => x.ShipmentPackageId).Where(x => x.Count() > 1))
            {
                candidates.Add(new($"package:{duplicate.Key}:duplicate", LoadPlanValidationSeverity.HardError, "PACKAGE_ALREADY_ASSIGNED", "Aynı ShipmentPackage aynı LoadUnit içinde birden fazla allocation ile tekrar edemez.", nameof(ShipmentPackageRecord), duplicate.Key));
            }

            foreach (var item in unit.Items)
            {
                var stopTotal = item.StopAllocations.Sum(x => x.QuantityBase);
                if (stopTotal > item.QuantityBase)
                {
                    candidates.Add(new($"item:{item.Id}:stop-quantity", LoadPlanValidationSeverity.HardError, "STOP_ALLOCATION_QUANTITY_EXCEEDED", "Stop allocation toplamı LoadUnitItem miktarını aşamaz.", nameof(LoadUnitItemRecord), item.Id));
                }

                if (item.StopAllocations.Count == 0)
                {
                    candidates.Add(new($"item:{item.Id}:stop-missing", LoadPlanValidationSeverity.Warning, "STOP_ALLOCATION_REQUIRED", "Sevkiyat planında paket için en az bir route stop allocation önerilir.", nameof(LoadUnitItemRecord), item.Id));
                }
            }
        }

        var allocatedByItem = plan.LoadUnits
            .SelectMany(x => x.Items)
            .GroupBy(x => x.ShipmentItemId)
            .ToDictionary(x => x.Key, x => x.Sum(y => y.QuantityBase));
        var shipmentItems = await dbContext.ShipmentItems
            .AsNoTracking()
            .Where(x => x.ShipmentId == plan.ShipmentId)
            .ToListAsync(cancellationToken);
        foreach (var item in shipmentItems)
        {
            var allocated = allocatedByItem.GetValueOrDefault(item.Id);
            if (allocated > item.QuantityBase)
            {
                candidates.Add(new($"shipment-item:{item.Id}:over", LoadPlanValidationSeverity.HardError, "QUANTITY_OVER_ALLOCATION", "LoadUnit allocation shipment item miktarını aşamaz.", nameof(ShipmentItemRecord), item.Id));
            }
            else if (allocated < item.QuantityBase)
            {
                candidates.Add(new($"shipment-item:{item.Id}:remainder", LoadPlanValidationSeverity.Info, "QUANTITY_REMAINDER", "Shipment item miktarının bir kısmı sonraki planlara bırakılmıştır.", nameof(ShipmentItemRecord), item.Id));
            }
        }

        if (plan.VehicleId is not null)
        {
            var hasScheduleConflict = routePlan.PlannedStartAt is not null && routePlan.PlannedEndAt is not null
                && await dbContext.RoutePlans.AnyAsync(x => x.Id != routePlan.Id
                    && x.VehicleId == plan.VehicleId
                    && x.Status != "Cancelled"
                    && x.Status != "Superseded"
                    && x.PlannedStartAt < routePlan.PlannedEndAt
                    && x.PlannedEndAt > routePlan.PlannedStartAt, cancellationToken);
            if (hasScheduleConflict)
            {
                candidates.Add(new("vehicle:schedule-overlap", LoadPlanValidationSeverity.HardError, "VEHICLE_SCHEDULE_CONFLICT", "Seçilen araç için route plan zaman aralığı çakışıyor.", nameof(VehicleRecord), plan.VehicleId));
            }
        }

        _ = now;
        return candidates;
    }

    private async Task UpsertValidationResultsAsync(
        LoadPlanRecord plan,
        IReadOnlyCollection<ValidationCandidate> candidates,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.LoadPlanValidationResults
            .Where(x => x.LoadPlanId == plan.Id)
            .ToDictionaryAsync(x => x.ValidationKey, cancellationToken);
        var activeKeys = candidates.Select(x => x.ValidationKey).ToHashSet(StringComparer.Ordinal);
        foreach (var candidate in candidates)
        {
            if (existing.TryGetValue(candidate.ValidationKey, out var record))
            {
                record.Severity = candidate.Severity.ToString();
                record.Code = candidate.Code;
                record.Message = candidate.Message;
                record.EntityType = candidate.EntityType;
                record.EntityId = candidate.EntityId;
                continue;
            }

            var validation = LoadPlanValidationResult.Create(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                plan.Id,
                candidate.ValidationKey,
                candidate.Severity,
                candidate.Code,
                candidate.Message,
                candidate.EntityType,
                candidate.EntityId);
            dbContext.LoadPlanValidationResults.Add(new LoadPlanValidationResultRecord
            {
                Id = validation.Id,
                LoadPlanId = validation.LoadPlanId,
                ValidationKey = validation.ValidationKey,
                Severity = validation.Severity.ToString(),
                Code = validation.Code,
                Message = validation.Message,
                EntityType = validation.EntityType,
                EntityId = validation.EntityId,
                ResolutionStatus = validation.ResolutionStatus.ToString(),
                CreatedAt = validation.CreatedAt,
            });
        }

        foreach (var record in existing.Values.Where(x => !activeKeys.Contains(x.ValidationKey) && x.ResolutionStatus == nameof(LoadPlanValidationResolutionStatus.Open)))
        {
            record.ResolutionStatus = nameof(LoadPlanValidationResolutionStatus.NotApplicable);
            record.ResolvedBy = actorId;
            record.ResolvedAt = DateTimeOffset.UtcNow;
            record.ResolutionReason = "Validation yeniden çalıştırıldı; kural artık aktif değil.";
        }
    }

    private static void ApplyValidationSummary(
        LoadPlanRecord plan,
        IReadOnlyCollection<LoadPlanValidationResultRecord> results)
    {
        var hardErrors = results.Count(x => x.Severity == nameof(LoadPlanValidationSeverity.HardError) && x.ResolutionStatus == nameof(LoadPlanValidationResolutionStatus.Open));
        var warnings = results.Count(x => x.Severity == nameof(LoadPlanValidationSeverity.Warning) && x.ResolutionStatus == nameof(LoadPlanValidationResolutionStatus.Open));
        plan.FeasibilityStatus = hardErrors > 0
            ? nameof(LoadPlanFeasibilityStatus.Infeasible)
            : warnings > 0
                ? nameof(LoadPlanFeasibilityStatus.FeasibleWithWarnings)
                : nameof(LoadPlanFeasibilityStatus.Feasible);
        plan.Status = hardErrors > 0 || warnings > 0
            ? nameof(LoadPlanStatus.NeedsReview)
            : nameof(LoadPlanStatus.Valid);
        plan.ValidationSummary = JsonSerializer.Serialize(new
        {
            hardErrors,
            warnings,
            infos = results.Count(x => x.Severity == nameof(LoadPlanValidationSeverity.Info)),
            validatedAt = DateTimeOffset.UtcNow,
        });
        foreach (var unit in plan.LoadUnits.Where(x => x.Status == nameof(LoadUnitStatus.Draft)))
        {
            unit.Status = nameof(LoadUnitStatus.Validated);
            unit.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    private static void ApplyResolution(
        LoadPlanValidationResultRecord record,
        LoadPlanValidationResolutionStatus resolutionStatus,
        Guid actorId,
        string reason)
    {
        DomainGuard.AgainstBlank(reason, "VALIDATION_RESOLUTION_REASON_REQUIRED", "Validation resolution reason zorunludur.");
        if (resolutionStatus == LoadPlanValidationResolutionStatus.Open)
        {
            throw new DomainException(new("VALIDATION_RESOLUTION_INVALID", "Validation result Open durumuna resolve edilemez."));
        }
        if (record.ResolutionStatus != nameof(LoadPlanValidationResolutionStatus.Open))
        {
            throw new DomainException(new("VALIDATION_ALREADY_RESOLVED", "Validation result yalnızca Open durumdayken resolve edilebilir."));
        }

        record.ResolutionStatus = resolutionStatus.ToString();
        record.ResolvedBy = actorId;
        record.ResolvedAt = DateTimeOffset.UtcNow;
        record.ResolutionReason = reason.Trim();
    }

    private static LoadPlanValidationResolutionStatus ParseResolutionStatus(string value)
        => ParseEnum<LoadPlanValidationResolutionStatus>(value, "VALIDATION_RESOLUTION_INVALID");

    private static LoadPlanValidationResolutionStatus ParseLockAction(string value)
        => value.Trim().ToLowerInvariant() switch
        {
            "resolve" or "resolved" => LoadPlanValidationResolutionStatus.Resolved,
            "override" or "overridden" => LoadPlanValidationResolutionStatus.Overridden,
            "notapplicable" or "not-applicable" => LoadPlanValidationResolutionStatus.NotApplicable,
            _ => throw new DomainException(new("VALIDATION_RESOLUTION_INVALID", $"Geçersiz warning resolution action: {value}.")),
        };

    private static void EnsureMutablePlan(LoadPlanRecord plan)
    {
        if (plan.Status is nameof(LoadPlanStatus.Locked) or nameof(LoadPlanStatus.Superseded))
        {
            throw new DomainException(new("LOAD_PLAN_IMMUTABLE", $"{plan.Status} durumundaki LoadPlan değiştirilemez."));
        }
    }

    private static LoadPlan RehydrateDomainPlan(LoadPlanRecord record)
    {
        var units = record.LoadUnits
            .OrderBy(x => x.UnitCode, StringComparer.Ordinal)
            .Select(unit => LoadUnit.Rehydrate(
                unit.Id,
                unit.CreatedAt,
                unit.LoadPlanId,
                unit.UnitCode,
                ParseEnum<LoadUnitType>(unit.UnitType, "LOAD_UNIT_TYPE_INVALID"),
                unit.PalletTypeId,
                unit.IsMixed,
                unit.LengthMm,
                unit.WidthMm,
                unit.HeightMm,
                unit.TareWeightKg,
                unit.GrossWeightKg,
                unit.VolumeM3,
                unit.MaxStackCount,
                unit.PlacementZone,
                unit.UnloadingPriority,
                ParseEnum<LoadUnitStatus>(unit.Status, "LOAD_UNIT_STATUS_INVALID"),
                unit.Items.OrderBy(x => x.Id).Select(item => LoadUnitItem.Rehydrate(
                    item.Id,
                    item.CreatedAt,
                    item.LoadUnitId,
                    item.ShipmentPackageId,
                    item.ShipmentItemId,
                    item.QuantityBase,
                    item.GrossWeightKg,
                    item.VolumeM3,
                    item.AllocationSnapshot,
                    item.StopAllocations.OrderBy(x => x.SequenceNo).Select(stop => LoadUnitStopAllocation.Create(
                        stop.Id,
                        stop.CreatedAt,
                        stop.LoadUnitItemId,
                        stop.RouteStopId,
                        stop.QuantityBase,
                        stop.SequenceNo))))));
        return LoadPlan.Rehydrate(
            record.Id,
            record.CreatedAt,
            record.UpdatedAt,
            record.ShipmentId,
            record.RoutePlanId,
            record.RoutePlanVersion,
            record.Version,
            record.ReplannedFromId,
            record.VehicleId,
            record.VehicleCapacityId,
            ParseEnum<LoadPlanStatus>(record.Status, "LOAD_PLAN_STATUS_INVALID"),
            ParseEnum<LoadPlanFeasibilityStatus>(record.FeasibilityStatus, "LOAD_PLAN_FEASIBILITY_INVALID"),
            record.AlgorithmName,
            record.AlgorithmVersion,
            record.ParameterSet,
            record.InputSnapshotHash,
            record.CapacitySnapshot,
            record.UtilizationSnapshot,
            record.ValidationSummary,
            record.ApprovedBy,
            record.ApprovedAt,
            record.LockedBy,
            record.LockedAt,
            units);
    }

    private static LoadPlanValidationResultDto Map(LoadPlanValidationResultRecord record)
        => new(
            record.Id,
            record.LoadPlanId,
            record.ValidationKey,
            record.Severity,
            record.Code,
            record.Message,
            record.EntityType,
            record.EntityId,
            record.ResolutionStatus,
            record.ResolvedBy,
            record.ResolvedAt,
            record.ResolutionReason,
            record.CreatedAt);

    private sealed record ValidationCandidate(
        string ValidationKey,
        LoadPlanValidationSeverity Severity,
        string Code,
        string Message,
        string? EntityType,
        Guid? EntityId);
}
