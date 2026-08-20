using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FactoryErp.Application.Abstractions.Persistence;
using FactoryErp.Application.Shipping;
using FactoryErp.Domain.Common;
using FactoryErp.Domain.Shipping;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.Shipping;

public sealed partial class LoadPlanCommandService(
    FactoryErpDbContext dbContext,
    IAuditWriter auditWriter,
    IIdempotencyStore idempotencyStore) : ILoadPlanCommandService
{
    public async Task<LoadPlanDto> CreateLoadPlanAsync(
        Guid shipmentId,
        CreateLoadPlanRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        DomainGuard.AgainstEmpty(shipmentId, "SHIPMENT_REQUIRED", "Shipment zorunludur.");
        var scope = $"load-plan:create:{actorId}:{shipmentId}";
        var payloadHash = ComputePayloadHash(new { shipmentId, request });
        var replay = await TryReplayAsync<LoadPlanDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var shipment = await LockShipmentAsync(shipmentId, cancellationToken);
        if (shipment is null)
        {
            throw new DomainException(new("SHIPMENT_NOT_FOUND", "Shipment bulunamadı."));
        }

        EnsureExpectedVersion(shipment.RowVersion, request.ExpectedShipmentRowVersion, nameof(ShipmentRecord), shipment.Id);
        var routePlan = await LockRoutePlanAsync(request.RoutePlanId, cancellationToken);
        if (routePlan is null)
        {
            throw new DomainException(new("ROUTE_PLAN_NOT_FOUND", "Route plan bulunamadı."));
        }

        if (routePlan.ShipmentId != shipmentId)
        {
            throw new DomainException(new("LOAD_PLAN_ROUTE_SHIPMENT_MISMATCH", "Route plan aynı shipment'a ait olmalıdır."));
        }

        if (routePlan.Version != request.ExpectedRoutePlanVersion)
        {
            throw new DomainException(new(
                "ROUTE_PLAN_VERSION_CONFLICT",
                "Route plan versiyonu değişti; LoadPlan yeniden oluşturulmalıdır."));
        }

        var activePlanExists = await dbContext.LoadPlans.AnyAsync(
            x => x.ShipmentId == shipmentId
                && x.RoutePlanId == routePlan.Id
                && x.RoutePlanVersion == routePlan.Version
                && x.Status != nameof(LoadPlanStatus.Superseded),
            cancellationToken);
        if (activePlanExists)
        {
            throw new DomainException(new(
                "LOAD_PLAN_ACTIVE_EXISTS",
                "Aynı shipment ve route plan versiyonu için aktif bir LoadPlan zaten vardır."));
        }

        var nextVersion = (await dbContext.LoadPlans
            .Where(x => x.ShipmentId == shipmentId)
            .Select(x => (int?)x.Version)
            .MaxAsync(cancellationToken) ?? 0) + 1;
        var loadPlan = LoadPlan.CreateDraft(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            shipmentId,
            routePlan.Id,
            routePlan.Version,
            nextVersion);

        var routeStops = await dbContext.RouteStops
            .AsNoTracking()
            .Where(x => x.RoutePlanId == routePlan.Id)
            .OrderBy(x => x.SequenceNo)
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var shipmentItems = await dbContext.ShipmentItems
            .AsNoTracking()
            .Where(x => x.ShipmentId == shipmentId)
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var packageIds = request.LoadUnits
            .SelectMany(x => x.Items)
            .Select(x => x.ShipmentPackageId)
            .Distinct()
            .ToArray();
        var packages = await LockPackagesAsync(shipmentId, packageIds, cancellationToken);
        var existingItemQuantities = await GetExistingItemQuantitiesAsync(shipmentId, cancellationToken);
        var existingPackageIds = await GetExistingPackageIdsAsync(packageIds, cancellationToken);

        var records = new List<LoadUnitRecord>();
        var requestPackageIds = new HashSet<Guid>();
        var requestItemQuantities = new Dictionary<Guid, decimal>();
        foreach (var unitRequest in request.LoadUnits)
        {
            var unit = LoadUnit.Create(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                loadPlan.Id,
                unitRequest.UnitCode,
                ParseEnum<LoadUnitType>(unitRequest.UnitType, "LOAD_UNIT_TYPE_INVALID"),
                unitRequest.PalletTypeId,
                unitRequest.IsMixed,
                unitRequest.LengthMm,
                unitRequest.WidthMm,
                unitRequest.HeightMm,
                unitRequest.TareWeightKg,
                unitRequest.GrossWeightKg,
                unitRequest.VolumeM3,
                unitRequest.MaxStackCount,
                unitRequest.PlacementZone,
                unitRequest.UnloadingPriority);

            var unitRecord = ToRecord(unit);
            foreach (var itemRequest in unitRequest.Items)
            {
                if (!shipmentItems.TryGetValue(itemRequest.ShipmentItemId, out var shipmentItem))
                {
                    throw new DomainException(new(
                        "SHIPMENT_ITEM_NOT_FOUND",
                        "LoadUnitItem shipment item aynı shipment'a ait olmalıdır."));
                }

                if (!packages.TryGetValue(itemRequest.ShipmentPackageId, out var package)
                    || package.ShipmentItemId != itemRequest.ShipmentItemId)
                {
                    throw new DomainException(new(
                        "PACKAGE_SHIPMENT_MISMATCH",
                        "ShipmentPackage ve ShipmentItem ownership zinciri geçersizdir."));
                }

                if (existingPackageIds.Contains(package.Id) || !requestPackageIds.Add(package.Id))
                {
                    throw new DomainException(new(
                        "PACKAGE_ALREADY_ASSIGNED",
                        "ShipmentPackage başka bir LoadUnit allocation’ında zaten bulunmaktadır."));
                }

                if (package.Status == nameof(ShipmentPackageStatus.Cancelled))
                {
                    throw new DomainException(new(
                        "PACKAGE_NOT_AVAILABLE",
                        "Cancelled ShipmentPackage LoadUnit’e atanamaz."));
                }

                if (!package.SplitAllowed && itemRequest.QuantityBase != package.QuantityBase)
                {
                    throw new DomainException(new(
                        "PACKAGE_SPLIT_NOT_ALLOWED",
                        "Split edilmeyen package miktarı bütünüyle atanmalıdır."));
                }

                var previousQuantity = existingItemQuantities.GetValueOrDefault(shipmentItem.Id)
                    + requestItemQuantities.GetValueOrDefault(shipmentItem.Id);
                LoadUnitItem.EnsureQuantityCeiling(
                    previousQuantity,
                    itemRequest.QuantityBase,
                    shipmentItem.QuantityBase,
                    package.SplitAllowed,
                    packageAlreadyAssigned: false);
                requestItemQuantities[shipmentItem.Id] = requestItemQuantities.GetValueOrDefault(shipmentItem.Id) + itemRequest.QuantityBase;

                var physical = ReadPhysicalValues(package.PhysicalSnapshot);
                var ratio = itemRequest.QuantityBase / package.QuantityBase;
                var item = LoadUnitItem.Create(
                    Guid.NewGuid(),
                    DateTimeOffset.UtcNow,
                    unit.Id,
                    package.Id,
                    shipmentItem.Id,
                    itemRequest.QuantityBase,
                    physical.GrossWeightKg * ratio,
                    physical.VolumeM3 * ratio,
                    JsonSerializer.Serialize(new
                    {
                        source = "ShipmentPackage",
                        packageId = package.Id,
                        packageQuantityBase = package.QuantityBase,
                        allocatedQuantityBase = itemRequest.QuantityBase,
                        physicalSnapshot = package.PhysicalSnapshot,
                    }));

                var stopAllocations = new List<LoadUnitStopAllocation>();
                foreach (var stopRequest in itemRequest.StopAllocations)
                {
                    if (!routeStops.ContainsKey(stopRequest.RouteStopId))
                    {
                        throw new DomainException(new(
                            "PACKAGE_STOP_MISMATCH",
                            "Stop allocation route stop aynı route planına ait olmalıdır."));
                    }

                    if (package.RouteStopId is not null && package.RouteStopId != stopRequest.RouteStopId)
                    {
                        throw new DomainException(new(
                            "PACKAGE_STOP_MISMATCH",
                            "Package’a atanmış route stop ile allocation route stop aynı olmalıdır."));
                    }

                    stopAllocations.Add(LoadUnitStopAllocation.Create(
                        Guid.NewGuid(),
                        DateTimeOffset.UtcNow,
                        item.Id,
                        stopRequest.RouteStopId,
                        stopRequest.QuantityBase,
                        stopRequest.SequenceNo));
                }
                item.SetStopAllocations(stopAllocations);
                unit.AddItem(item);
                unitRecord.Items.Add(ToRecord(item));
            }

            loadPlan.AddLoadUnit(unit);
            records.Add(unitRecord);
        }

        loadPlan.SetPlanningSnapshot(
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            JsonSerializer.Serialize(new
            {
                hardErrors = 0,
                warnings = 0,
                loadUnitCount = request.LoadUnits.Count,
            }),
            DateTimeOffset.UtcNow);

        var planRecord = ToRecord(loadPlan);
        foreach (var unitRecord in records)
        {
            planRecord.LoadUnits.Add(unitRecord);
        }

        dbContext.LoadPlans.Add(planRecord);
        await auditWriter.AppendAsync(new(
            "LoadPlanDraftCreated",
            nameof(LoadPlanRecord),
            planRecord.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new
            {
                planRecord.ShipmentId,
                planRecord.RoutePlanId,
                planRecord.RoutePlanVersion,
                planRecord.Version,
                loadUnitCount = planRecord.LoadUnits.Count,
            })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var result = Map(planRecord);
        await SaveIdempotencyAsync(scope, idempotencyKey, payloadHash, 201, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<LoadPlanDto> AssignVehicleAsync(
        Guid loadPlanId,
        AssignLoadPlanVehicleRequest request,
        long expectedRowVersion,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        DomainGuard.AgainstEmpty(loadPlanId, "LOAD_PLAN_REQUIRED", "LoadPlan zorunludur.");
        DomainGuard.AgainstEmpty(request.VehicleId, "VEHICLE_REQUIRED", "LoadPlan vehicle kaydına bağlı olmalıdır.");
        DomainGuard.AgainstEmpty(request.VehicleCapacityId, "CAPACITY_REQUIRED", "LoadPlan vehicle capacity kaydına bağlı olmalıdır.");
        var scope = $"load-plan:assign-vehicle:{actorId}:{loadPlanId}";
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

        var now = DateTimeOffset.UtcNow;
        var vehicle = await dbContext.Vehicles
            .FromSqlInterpolated($"SELECT * FROM vehicles WHERE id = {request.VehicleId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new DomainException(new("VEHICLE_NOT_FOUND", "Vehicle bulunamadı."));
        var capacity = await dbContext.VehicleCapacities
            .FromSqlInterpolated($"SELECT * FROM vehicle_capacities WHERE id = {request.VehicleCapacityId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new DomainException(new("VEHICLE_CAPACITY_NOT_FOUND", "Vehicle capacity bulunamadı."));
        if (!string.Equals(vehicle.Status, nameof(VehicleStatus.Available), StringComparison.OrdinalIgnoreCase)
            || (vehicle.MaintenanceUntil is not null && vehicle.MaintenanceUntil > now))
        {
            throw new DomainException(new("VEHICLE_NOT_AVAILABLE", "Vehicle Available durumda değil veya bakım süresi devam ediyor."));
        }

        if (capacity.VehicleTypeId != vehicle.VehicleTypeId)
        {
            throw new DomainException(new("CAPACITY_VEHICLE_TYPE_MISMATCH", "Vehicle ve capacity aynı vehicle type'a ait olmalıdır."));
        }

        if (capacity.EffectiveFrom > now || (capacity.EffectiveTo is not null && capacity.EffectiveTo <= now))
        {
            throw new DomainException(new("VEHICLE_CAPACITY_NOT_FOUND", "Vehicle capacity geçerlilik penceresi dışındadır."));
        }

        var zones = await dbContext.VehicleCapacityZones
            .AsNoTracking()
            .Where(x => x.VehicleCapacityId == capacity.Id)
            .OrderBy(x => x.SequenceNo)
            .ThenBy(x => x.Id)
            .ToArrayAsync(cancellationToken);
        var domainPlan = RehydrateDomainPlan(plan);
        domainPlan.SetPlanningSnapshot(
            vehicle.Id,
            capacity.Id,
            plan.AlgorithmName,
            plan.AlgorithmVersion,
            plan.ParameterSet,
            plan.InputSnapshotHash,
            JsonSerializer.Serialize(new
            {
                capacity.Id,
                capacity.VehicleTypeId,
                capacity.EffectiveFrom,
                capacity.EffectiveTo,
                capacity.MaxGrossWeight,
                capacity.TareWeight,
                capacity.MaxUsableVolume,
                capacity.MaxPalletCount,
                capacity.MaxLoadHeight,
                capacity.CapacityPolicySnapshot,
                zones = zones.Select(x => new
                {
                    x.Id,
                    x.ZoneCode,
                    x.LengthMm,
                    x.WidthMm,
                    x.MaxLoadKg,
                    x.SequenceNo,
                }).ToArray(),
            }),
            plan.UtilizationSnapshot,
            string.IsNullOrWhiteSpace(plan.ValidationSummary) ? "{}" : plan.ValidationSummary,
            now);

        plan.VehicleId = domainPlan.VehicleId;
        plan.VehicleCapacityId = domainPlan.VehicleCapacityId;
        plan.AlgorithmName = domainPlan.AlgorithmName;
        plan.AlgorithmVersion = domainPlan.AlgorithmVersion;
        plan.ParameterSet = domainPlan.ParameterSet;
        plan.InputSnapshotHash = domainPlan.InputSnapshotHash;
        plan.CapacitySnapshot = domainPlan.CapacitySnapshot;
        plan.UtilizationSnapshot = domainPlan.UtilizationSnapshot;
        plan.ValidationSummary = domainPlan.ValidationSummary;
        plan.UpdatedAt = domainPlan.UpdatedAt;
        await auditWriter.AppendAsync(new(
            "LoadPlanVehicleAssigned",
            nameof(LoadPlanRecord),
            plan.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new
            {
                vehicleId = vehicle.Id,
                capacityId = capacity.Id,
                plateNumber = vehicle.PlateNumber,
            })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var result = Map(plan);
        await SaveIdempotencyAsync(scope, idempotencyKey, payloadHash, 200, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<LoadPlanDto?> GetLoadPlanAsync(Guid loadPlanId, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.LoadPlans
            .AsNoTracking()
            .Include(x => x.LoadUnits)
                .ThenInclude(x => x.Items)
                    .ThenInclude(x => x.StopAllocations)
            .SingleOrDefaultAsync(x => x.Id == loadPlanId, cancellationToken);
        return record is null ? null : Map(record);
    }

    public async Task<IReadOnlyCollection<LoadPlanDto>> ListLoadPlansByShipmentAsync(
        Guid shipmentId,
        CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.LoadPlans
            .AsNoTracking()
            .Include(x => x.LoadUnits)
                .ThenInclude(x => x.Items)
                    .ThenInclude(x => x.StopAllocations)
            .Where(x => x.ShipmentId == shipmentId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .ToArrayAsync(cancellationToken);
        return rows.Select(Map).ToArray();
    }

    private async Task<ShipmentRecord?> LockShipmentAsync(Guid shipmentId, CancellationToken cancellationToken)
        => await dbContext.Shipments
            .FromSqlInterpolated($"SELECT * FROM shipments WHERE id = {shipmentId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<RoutePlanRecord?> LockRoutePlanAsync(Guid routePlanId, CancellationToken cancellationToken)
        => await dbContext.RoutePlans
            .FromSqlInterpolated($"SELECT * FROM route_plans WHERE id = {routePlanId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<Dictionary<Guid, ShipmentPackageRecord>> LockPackagesAsync(
        Guid shipmentId,
        IReadOnlyCollection<Guid> packageIds,
        CancellationToken cancellationToken)
    {
        if (packageIds.Count == 0)
        {
            return new Dictionary<Guid, ShipmentPackageRecord>();
        }

        var rows = await dbContext.ShipmentPackages
            .FromSqlInterpolated($"SELECT * FROM shipment_packages WHERE shipment_id = {shipmentId} ORDER BY id FOR UPDATE")
            .Where(x => packageIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(x => x.Id);
    }

    private async Task<Dictionary<Guid, decimal>> GetExistingItemQuantitiesAsync(
        Guid shipmentId,
        CancellationToken cancellationToken)
        => await dbContext.LoadUnitItems
            .Where(x => x.ShipmentItem.ShipmentId == shipmentId)
            .GroupBy(x => x.ShipmentItemId)
            .Select(x => new { x.Key, Quantity = x.Sum(y => y.QuantityBase) })
            .ToDictionaryAsync(x => x.Key, x => x.Quantity, cancellationToken);

    private async Task<HashSet<Guid>> GetExistingPackageIdsAsync(
        IReadOnlyCollection<Guid> packageIds,
        CancellationToken cancellationToken)
    {
        if (packageIds.Count == 0)
        {
            return [];
        }

        var ids = await dbContext.LoadUnitItems
            .Where(x => packageIds.Contains(x.ShipmentPackageId))
            .Select(x => x.ShipmentPackageId)
            .ToArrayAsync(cancellationToken);
        return ids.ToHashSet();
    }

    private static ShipmentPackagePhysicalValues ReadPhysicalValues(string snapshot)
    {
        using var document = JsonDocument.Parse(snapshot);
        var root = document.RootElement;
        var gross = ReadNullableDecimal(root, "grossWeightKg") ?? ReadNullableDecimal(root, "netWeightKg") ?? 0m;
        var volume = ReadNullableDecimal(root, "volumeM3") ?? 0m;
        return new ShipmentPackagePhysicalValues(gross, volume);
    }

    private static decimal? ReadNullableDecimal(JsonElement root, string propertyName)
        => root.TryGetProperty(propertyName, out var property)
            && property.ValueKind is JsonValueKind.Number
            && property.TryGetDecimal(out var value)
            ? value
            : null;

    private static LoadPlanRecord ToRecord(LoadPlan plan)
        => new()
        {
            Id = plan.Id,
            ShipmentId = plan.ShipmentId,
            RoutePlanId = plan.RoutePlanId,
            RoutePlanVersion = plan.RoutePlanVersion,
            Version = plan.Version,
            ReplannedFromId = plan.ReplannedFromId,
            VehicleId = plan.VehicleId,
            VehicleCapacityId = plan.VehicleCapacityId,
            Status = plan.Status.ToString(),
            FeasibilityStatus = plan.FeasibilityStatus.ToString(),
            AlgorithmName = plan.AlgorithmName,
            AlgorithmVersion = plan.AlgorithmVersion,
            ParameterSet = plan.ParameterSet,
            InputSnapshotHash = plan.InputSnapshotHash,
            CapacitySnapshot = plan.CapacitySnapshot,
            UtilizationSnapshot = plan.UtilizationSnapshot,
            ValidationSummary = plan.ValidationSummary,
            ApprovedBy = plan.ApprovedBy,
            ApprovedAt = plan.ApprovedAt,
            LockedBy = plan.LockedBy,
            LockedAt = plan.LockedAt,
            CreatedAt = plan.CreatedAt,
            UpdatedAt = plan.UpdatedAt,
            RowVersion = plan.RowVersion,
        };

    private static LoadUnitRecord ToRecord(LoadUnit unit)
        => new()
        {
            Id = unit.Id,
            LoadPlanId = unit.LoadPlanId,
            UnitCode = unit.UnitCode,
            UnitType = unit.UnitType.ToString(),
            PalletTypeId = unit.PalletTypeId,
            IsMixed = unit.IsMixed,
            LengthMm = unit.LengthMm,
            WidthMm = unit.WidthMm,
            HeightMm = unit.HeightMm,
            TareWeightKg = unit.TareWeightKg,
            GrossWeightKg = unit.GrossWeightKg,
            VolumeM3 = unit.VolumeM3,
            MaxStackCount = unit.MaxStackCount,
            PlacementZone = unit.PlacementZone,
            UnloadingPriority = unit.UnloadingPriority,
            Status = unit.Status.ToString(),
            CreatedAt = unit.CreatedAt,
            UpdatedAt = unit.UpdatedAt,
            RowVersion = unit.RowVersion,
        };

    private static LoadUnitItemRecord ToRecord(LoadUnitItem item)
    {
        var record = new LoadUnitItemRecord
        {
            Id = item.Id,
            LoadUnitId = item.LoadUnitId,
            ShipmentPackageId = item.ShipmentPackageId,
            ShipmentItemId = item.ShipmentItemId,
            QuantityBase = item.QuantityBase,
            GrossWeightKg = item.GrossWeightKg,
            VolumeM3 = item.VolumeM3,
            AllocationSnapshot = item.AllocationSnapshot,
            CreatedAt = item.CreatedAt,
            RowVersion = item.RowVersion,
        };
        foreach (var allocation in item.StopAllocations)
        {
            record.StopAllocations.Add(new LoadUnitStopAllocationRecord
            {
                Id = allocation.Id,
                LoadUnitItemId = allocation.LoadUnitItemId,
                RouteStopId = allocation.RouteStopId,
                QuantityBase = allocation.QuantityBase,
                SequenceNo = allocation.SequenceNo,
                CreatedAt = allocation.CreatedAt,
            });
        }

        return record;
    }

    private static LoadPlanDto Map(LoadPlanRecord record)
        => new(
            record.Id,
            record.ShipmentId,
            record.RoutePlanId,
            record.RoutePlanVersion,
            record.Version,
            record.ReplannedFromId,
            record.VehicleId,
            record.VehicleCapacityId,
            record.Status,
            record.FeasibilityStatus,
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
            record.LoadUnits.OrderBy(x => x.UnitCode, StringComparer.Ordinal).Select(Map).ToArray(),
            record.CreatedAt,
            record.UpdatedAt,
            record.RowVersion);

    private static LoadUnitDto Map(LoadUnitRecord record)
        => new(
            record.Id,
            record.LoadPlanId,
            record.UnitCode,
            record.UnitType,
            record.PalletTypeId,
            record.IsMixed,
            record.LengthMm,
            record.WidthMm,
            record.HeightMm,
            record.TareWeightKg,
            record.GrossWeightKg,
            record.VolumeM3,
            record.MaxStackCount,
            record.PlacementZone,
            record.UnloadingPriority,
            record.Status,
            record.Items.OrderBy(x => x.Id).Select(Map).ToArray(),
            record.CreatedAt,
            record.RowVersion);

    private static LoadUnitItemDto Map(LoadUnitItemRecord record)
        => new(
            record.Id,
            record.LoadUnitId,
            record.ShipmentPackageId,
            record.ShipmentItemId,
            record.QuantityBase,
            record.GrossWeightKg,
            record.VolumeM3,
            record.AllocationSnapshot,
            record.StopAllocations.OrderBy(x => x.SequenceNo).Select(Map).ToArray(),
            record.CreatedAt,
            record.RowVersion);

    private static LoadUnitStopAllocationDto Map(LoadUnitStopAllocationRecord record)
        => new(record.Id, record.LoadUnitItemId, record.RouteStopId, record.QuantityBase, record.SequenceNo, record.CreatedAt);

    private static TEnum ParseEnum<TEnum>(string value, string code)
        where TEnum : struct, Enum
        => Enum.TryParse<TEnum>(value, true, out var result)
            ? result
            : throw new DomainException(new(code, $"Geçersiz state değeri: {value}."));

    private static void EnsureExpectedVersion(long actual, long expected, string resourceType, Guid resourceId)
    {
        if (actual != expected)
        {
            throw new DomainException(new(
                "RESOURCE_VERSION_CONFLICT",
                $"{resourceType} ({resourceId}) güncel değil; beklenen row_version: {expected}, mevcut: {actual}."));
        }
    }

    private async Task<T?> TryReplayAsync<T>(string scope, string key, string payloadHash, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainException(new("MISSING_IDEMPOTENCY_KEY", "Idempotency-Key zorunludur."));
        }

        var stored = await idempotencyStore.FindAsync(scope, key, cancellationToken);
        if (stored is null)
        {
            return default;
        }

        if (!string.Equals(stored.PayloadHash, payloadHash, StringComparison.Ordinal))
        {
            throw new DomainException(new("IDEMPOTENCY_PAYLOAD_MISMATCH", "Aynı Idempotency-Key farklı payload ile kullanıldı."));
        }

        return JsonSerializer.Deserialize<T>(stored.ResponseBody)
            ?? throw new DomainException(new("IDEMPOTENCY_REPLAY_INVALID", "Idempotency replay sonucu okunamadı."));
    }

    private Task SaveIdempotencyAsync<T>(string scope, string key, string payloadHash, int statusCode, T result, CancellationToken cancellationToken)
        => idempotencyStore.SaveAsync(
            scope,
            key,
            payloadHash,
            statusCode,
            JsonSerializer.Serialize(result),
            DateTimeOffset.UtcNow.AddDays(30),
            cancellationToken);

    private static string ComputePayloadHash<T>(T payload)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload))));

    private sealed record ShipmentPackagePhysicalValues(decimal GrossWeightKg, decimal VolumeM3);
}
