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

public sealed class VehicleFitCommandService(
    FactoryErpDbContext dbContext,
    IAuditWriter auditWriter,
    IIdempotencyStore idempotencyStore) : IVehicleFitCommandService
{
    private const string DefaultParameterSet = "ffd:v1:compat,keep,fragile,gross,volume,footprint,stop,item,packaging,package";

    public async Task<VehicleFitEvaluationBatchDto> EvaluateVehicleFitAsync(
        Guid shipmentId,
        EvaluateVehicleFitRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        DomainGuard.AgainstEmpty(shipmentId, "SHIPMENT_REQUIRED", "Shipment zorunludur.");
        DomainGuard.AgainstEmpty(request.LoadPlanId, "LOAD_PLAN_REQUIRED", "LoadPlan zorunludur.");
        var algorithmVersion = string.IsNullOrWhiteSpace(request.AlgorithmVersion)
            ? DeterministicFfdEngine.AlgorithmVersion
            : request.AlgorithmVersion.Trim();
        var parameterSet = string.IsNullOrWhiteSpace(request.ParameterSet)
            ? DefaultParameterSet
            : request.ParameterSet.Trim();
        if (parameterSet.Length > 120)
        {
            throw new DomainException(new("PARAMETER_SET_TOO_LONG", "FFD parameter set en fazla 120 karakter olabilir."));
        }
        var scope = $"vehicle-fit:evaluate:{actorId}:{shipmentId}:{request.LoadPlanId}";
        var payloadHash = ComputePayloadHash(new { shipmentId, request, algorithmVersion, parameterSet });
        var replay = await TryReplayAsync<VehicleFitEvaluationBatchDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var loadPlan = await dbContext.LoadPlans
            .FromSqlInterpolated($"SELECT * FROM load_plans WHERE id = {request.LoadPlanId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (loadPlan is null || loadPlan.ShipmentId != shipmentId)
        {
            throw new DomainException(new("LOAD_PLAN_NOT_FOUND", "LoadPlan shipment ile eşleşmiyor veya bulunamadı."));
        }

        if (loadPlan.Status is nameof(LoadPlanStatus.Locked) or nameof(LoadPlanStatus.Superseded))
        {
            throw new DomainException(new("LOAD_PLAN_IMMUTABLE", "Locked veya superseded LoadPlan yeniden değerlendirilemez."));
        }

        EnsureExpectedVersion(loadPlan.RowVersion, request.ExpectedLoadPlanRowVersion, nameof(LoadPlanRecord), loadPlan.Id);
        var shipment = await dbContext.Shipments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == shipmentId, cancellationToken);
        if (shipment is null)
        {
            throw new DomainException(new("SHIPMENT_NOT_FOUND", "Shipment bulunamadı."));
        }

        var routeStops = await dbContext.RouteStops
            .AsNoTracking()
            .Where(x => x.RoutePlanId == loadPlan.RoutePlanId)
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var packages = await dbContext.ShipmentPackages
            .AsNoTracking()
            .Where(x => x.ShipmentId == shipmentId && x.Status != nameof(ShipmentPackageStatus.Cancelled))
            .OrderBy(x => x.Id)
            .ToArrayAsync(cancellationToken);
        var shipmentItems = await dbContext.ShipmentItems
            .AsNoTracking()
            .Where(x => x.ShipmentId == shipmentId)
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var planningItems = NormalizePlanningItems(packages, shipmentItems, routeStops);
        var missingPhysicalProfilePackageIds = planningItems
            .Where(x => !x.PhysicalProfilePresent)
            .Select(x => x.ShipmentPackageId)
            .OrderBy(x => x)
            .ToArray();

        var vehiclesQuery = dbContext.Vehicles.AsNoTracking().AsQueryable();
        var vehicleIds = request.VehicleIds?
            .Where(x => x != Guid.Empty)
            .Distinct()
            .Order()
            .ToArray();
        if (vehicleIds is { Length: > 0 })
        {
            vehiclesQuery = vehiclesQuery.Where(x => vehicleIds.Contains(x.Id));
        }

        var vehicles = await vehiclesQuery
            .OrderBy(x => x.PlateNumber)
            .ThenBy(x => x.Id)
            .ToArrayAsync(cancellationToken);
        var evaluatedAt = DateTimeOffset.UtcNow;
        var capacityInputs = new Dictionary<Guid, (VehicleCapacityRecord? Capacity, VehicleCapacityZoneRecord[] Zones)>();
        foreach (var vehicle in vehicles)
        {
            var capacity = await dbContext.VehicleCapacities
                .AsNoTracking()
                .Where(x => x.VehicleTypeId == vehicle.VehicleTypeId
                    && x.EffectiveFrom <= evaluatedAt
                    && (x.EffectiveTo == null || x.EffectiveTo > evaluatedAt))
                .OrderByDescending(x => x.EffectiveFrom)
                .ThenBy(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);
            var zones = capacity is null
                ? []
                : await dbContext.VehicleCapacityZones
                    .AsNoTracking()
                    .Where(x => x.VehicleCapacityId == capacity.Id)
                    .OrderBy(x => x.SequenceNo)
                    .ThenBy(x => x.Id)
                    .ToArrayAsync(cancellationToken);
            capacityInputs[vehicle.Id] = (capacity, zones);
        }

        var inputHash = ComputeInputSnapshotHash(shipmentId, loadPlan, planningItems, vehicles, capacityInputs, algorithmVersion, parameterSet);
        var evaluations = new List<VehicleFitEvaluationRecord>();

        foreach (var vehicle in vehicles)
        {
            var input = capacityInputs[vehicle.Id];
            var evaluation = EvaluateCandidate(
                shipmentId,
                loadPlan,
                vehicle,
                input.Capacity,
                input.Zones,
                planningItems,
                inputHash,
                algorithmVersion,
                evaluatedAt);
            evaluations.Add(evaluation);
        }

        evaluations = evaluations
            .OrderBy(x => x.CandidateStatus == "Rejected")
            .ThenBy(x => x.FitScore ?? decimal.MaxValue)
            .ThenBy(x => x.VehicleId)
            .ToList();

        var existing = await dbContext.VehicleFitEvaluations
            .Where(x => x.LoadPlanId == loadPlan.Id && x.InputSnapshotHash == inputHash)
            .ToArrayAsync(cancellationToken);
        var existingKeys = existing
            .Select(x => EvaluationKey(x.VehicleId, x.VehicleCapacityId))
            .ToHashSet(StringComparer.Ordinal);
        foreach (var evaluation in evaluations.Where(x => !existingKeys.Contains(EvaluationKey(x.VehicleId, x.VehicleCapacityId))))
        {
            dbContext.VehicleFitEvaluations.Add(evaluation);
        }

        loadPlan.AlgorithmName = DeterministicFfdEngine.AlgorithmName;
        loadPlan.AlgorithmVersion = algorithmVersion;
        loadPlan.ParameterSet = parameterSet;
        loadPlan.InputSnapshotHash = inputHash;
        loadPlan.ValidationSummary = JsonSerializer.Serialize(new
        {
            hardErrors = evaluations.Count(x => x.CandidateStatus == "Rejected"),
            warnings = evaluations.Count(x => x.CandidateStatus == "NeedsReview"),
            candidateCount = evaluations.Count,
            missingPhysicalProfilePackageCount = missingPhysicalProfilePackageIds.Length,
        });
        loadPlan.UpdatedAt = evaluatedAt;

        await auditWriter.AppendAsync(new(
            "VehicleFitEvaluated",
            nameof(LoadPlanRecord),
            loadPlan.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new
            {
                loadPlan.ShipmentId,
                loadPlan.Id,
                algorithm = DeterministicFfdEngine.AlgorithmName,
                algorithmVersion,
                inputHash,
                candidateCount = evaluations.Count,
            })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var result = new VehicleFitEvaluationBatchDto(
            loadPlan.Id,
            shipmentId,
            DeterministicFfdEngine.AlgorithmName,
            algorithmVersion,
            parameterSet,
            inputHash,
            evaluations.Select(Map).ToArray(),
            missingPhysicalProfilePackageIds);
        await SaveIdempotencyAsync(scope, idempotencyKey, payloadHash, 200, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyCollection<VehicleFitEvaluationDto>> GetVehicleFitCandidatesAsync(
        Guid shipmentId,
        Guid loadPlanId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.VehicleFitEvaluations
            .AsNoTracking()
            .Where(x => x.LoadPlanId == loadPlanId && x.LoadPlan.ShipmentId == shipmentId)
            .OrderBy(x => x.CandidateStatus == "Rejected")
            .ThenBy(x => x.FitScore ?? decimal.MaxValue)
            .ThenBy(x => x.VehicleId)
            .Select(x => MapProjection(x))
            .ToArrayAsync(cancellationToken);
    }

    private static IReadOnlyCollection<PlanningItem> NormalizePlanningItems(
        IReadOnlyCollection<ShipmentPackageRecord> packages,
        IReadOnlyDictionary<Guid, ShipmentItemRecord> shipmentItems,
        IReadOnlyDictionary<Guid, RouteStopRecord> routeStops)
    {
        var normalized = new List<PlanningItem>(packages.Count);
        foreach (var package in packages)
        {
            shipmentItems.TryGetValue(package.ShipmentItemId, out var shipmentItem);
            var physical = ReadPhysicalSnapshot(package.PhysicalSnapshot);
            var routeSequence = package.RouteStopId is { } routeStopId && routeStops.TryGetValue(routeStopId, out var stop)
                ? stop.SequenceNo
                : int.MaxValue;
            normalized.Add(PlanningItem.Create(
                package.Id,
                package.ShipmentItemId,
                shipmentItem?.ProductId ?? package.ShipmentItem.ProductId,
                package.PackagingId,
                package.QuantityBase,
                package.PackageCount,
                physical.NetWeightKg,
                physical.TareWeightKg,
                physical.GrossWeightKg,
                physical.VolumeM3,
                physical.LengthMm,
                physical.WidthMm,
                physical.HeightMm,
                physical.CompatibilityGroup,
                physical.IncompatibleGroups,
                physical.IsFragile,
                physical.KeepUpright,
                physical.AllowedOrientations,
                package.SplitAllowed,
                routeSequence,
                physical.Present));
        }

        return normalized;
    }

    private static VehicleFitEvaluationRecord EvaluateCandidate(
        Guid shipmentId,
        LoadPlanRecord loadPlan,
        VehicleRecord vehicle,
        VehicleCapacityRecord? capacity,
        IReadOnlyCollection<VehicleCapacityZoneRecord> zones,
        IReadOnlyCollection<PlanningItem> planningItems,
        string inputHash,
        string algorithmVersion,
        DateTimeOffset evaluatedAt)
    {
        var capacitySnapshot = capacity is null
            ? null
            : JsonSerializer.Serialize(new
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
                zones = zones.Select(x => new { x.Id, x.ZoneCode, x.LengthMm, x.WidthMm, x.MaxLoadKg, x.SequenceNo }).ToArray(),
            });

        if (!string.Equals(vehicle.Status, "Available", StringComparison.OrdinalIgnoreCase))
        {
            return Rejected(loadPlan.Id, vehicle, capacity, inputHash, algorithmVersion, capacitySnapshot, evaluatedAt, "VEHICLE_NOT_AVAILABLE", "Vehicle status Available değil.");
        }

        if (capacity is null)
        {
            return Rejected(loadPlan.Id, vehicle, null, inputHash, algorithmVersion, capacitySnapshot, evaluatedAt, "VEHICLE_CAPACITY_MISSING", "Vehicle için effective capacity bulunamadı.");
        }

        var maxUnitLength = zones.Count > 0 ? zones.Max(x => x.LengthMm) : 0m;
        var maxUnitWidth = zones.Count > 0 ? zones.Max(x => x.WidthMm) : 0m;
        var maxUnitHeight = capacity.MaxLoadHeight > 0 ? capacity.MaxLoadHeight : decimal.MaxValue;
        var ffdCapacity = FfdUnitCapacity.Create(
            $"{vehicle.PlateNumber}-FFD",
            maxUnitLength > 0 ? maxUnitLength : decimal.MaxValue,
            maxUnitWidth > 0 ? maxUnitWidth : decimal.MaxValue,
            maxUnitHeight,
            capacity.MaxGrossWeight,
            capacity.MaxUsableVolume,
            true,
            capacity.MaxPalletCount,
            zones.FirstOrDefault()?.ZoneCode);
        var ffd = DeterministicFfdEngine.Execute(planningItems, [ffdCapacity]);
        var firstRejection = ffd.Rejections.FirstOrDefault();
        var totalGrossWeight = planningItems.Sum(x => x.GrossWeightKg);
        var totalVolume = planningItems.Sum(x => x.VolumeM3);
        var palletCount = loadPlan.LoadUnits.Count;
        var maxHeight = planningItems.Count == 0 ? 0m : planningItems.Max(x => x.HeightMm);
        var weightRatio = Ratio(totalGrossWeight, capacity.MaxGrossWeight);
        var volumeRatio = Ratio(totalVolume, capacity.MaxUsableVolume);
        var palletRatio = Ratio(palletCount, capacity.MaxPalletCount);
        var floorCapacity = zones.Sum(x => x.LengthMm * x.WidthMm);
        decimal? floorRatio = floorCapacity <= 0 ? null : Ratio(planningItems.Sum(x => x.FloorFootprint), floorCapacity);
        decimal? heightRatio = capacity.MaxLoadHeight <= 0 ? null : Ratio(maxHeight, capacity.MaxLoadHeight);
        var rejected = firstRejection is not null
            || weightRatio > 1
            || volumeRatio > 1
            || palletRatio > 1
            || heightRatio > 1;
        var rejectionCode = firstRejection is not null
            ? ToRejectionCode(firstRejection.Code)
            : weightRatio > 1
                ? "LOAD_UNIT_WEIGHT_EXCEEDED"
                : volumeRatio > 1
                    ? "LOAD_UNIT_VOLUME_EXCEEDED"
                    : palletRatio > 1
                        ? "PALLET_COUNT_EXCEEDED"
                        : heightRatio > 1
                            ? "LOAD_UNIT_DIMENSION_MISMATCH"
                            : null;
        var fitScore = (weightRatio + volumeRatio + palletRatio + (floorRatio ?? 0) + (heightRatio ?? 0)) / 5m;
        return new VehicleFitEvaluationRecord
        {
            Id = Guid.NewGuid(),
            LoadPlanId = loadPlan.Id,
            VehicleId = vehicle.Id,
            VehicleCapacityId = capacity.Id,
            CandidateStatus = rejected ? "Rejected" : "Candidate",
            RejectionCode = rejectionCode,
            ReasonText = firstRejection?.Reason,
            WeightRatio = weightRatio,
            VolumeRatio = volumeRatio,
            PalletRatio = palletRatio,
            FloorAreaRatio = floorRatio,
            HeightRatio = heightRatio,
            DoorCheckStatus = "NotChecked",
            DimensionCheckStatus = rejected && rejectionCode == "LOAD_UNIT_DIMENSION_MISMATCH" ? "Fail" : "Pass",
            StackingCheckStatus = "Pass",
            AxleCheckStatus = "NotChecked",
            StopAccessStatus = "Pass",
            FitScore = fitScore,
            AlgorithmVersion = algorithmVersion,
            InputSnapshotHash = inputHash,
            CapacitySnapshot = capacitySnapshot,
            EvaluatedAt = evaluatedAt,
        };
    }

    private static VehicleFitEvaluationRecord Rejected(
        Guid loadPlanId,
        VehicleRecord vehicle,
        VehicleCapacityRecord? capacity,
        string inputHash,
        string algorithmVersion,
        string? capacitySnapshot,
        DateTimeOffset evaluatedAt,
        string rejectionCode,
        string reason)
        => new()
        {
            Id = Guid.NewGuid(),
            LoadPlanId = loadPlanId,
            VehicleId = vehicle.Id,
            VehicleCapacityId = capacity?.Id,
            CandidateStatus = "Rejected",
            RejectionCode = rejectionCode,
            ReasonText = reason,
            DoorCheckStatus = "NotChecked",
            DimensionCheckStatus = "NotChecked",
            StackingCheckStatus = "NotChecked",
            AxleCheckStatus = "NotChecked",
            StopAccessStatus = "NotChecked",
            AlgorithmVersion = algorithmVersion,
            InputSnapshotHash = inputHash,
            CapacitySnapshot = capacitySnapshot,
            EvaluatedAt = evaluatedAt,
        };

    private static PhysicalSnapshot ReadPhysicalSnapshot(string snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot))
        {
            return PhysicalSnapshot.Missing;
        }

        try
        {
            using var document = JsonDocument.Parse(snapshot);
            var root = document.RootElement;
            var length = ReadDecimal(root, "lengthMm");
            var width = ReadDecimal(root, "widthMm");
            var height = ReadDecimal(root, "heightMm");
            var volume = ReadDecimal(root, "volumeM3");
            var net = ReadDecimal(root, "netWeightKg");
            var tare = ReadDecimal(root, "tareWeightKg");
            var gross = ReadDecimal(root, "grossWeightKg") ?? (net ?? 0m) + (tare ?? 0m);
            return new PhysicalSnapshot(
                length ?? 0,
                width ?? 0,
                height ?? 0,
                net ?? 0,
                tare ?? 0,
                gross,
                volume ?? 0,
                ReadString(root, "compatibilityGroup"),
                ReadStringArray(root, "incompatibleGroups"),
                ReadBool(root, "isFragile"),
                ReadBool(root, "keepUpright"),
                ReadStringArray(root, "allowedOrientations"),
                length is not null && width is not null && height is not null && volume is not null && (gross > 0 || net > 0));
        }
        catch (JsonException)
        {
            return PhysicalSnapshot.Missing;
        }
    }

    private static decimal? ReadDecimal(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var result)
            ? result
            : null;

    private static string? ReadString(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool ReadBool(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False && value.GetBoolean();

    private static string[] ReadStringArray(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).ToArray()
            : [];

    private static decimal Ratio(decimal numerator, decimal denominator)
        => denominator <= 0 ? decimal.MaxValue : numerator / denominator;

    private static string ToRejectionCode(FfdHardConstraintCode code)
        => code switch
        {
            FfdHardConstraintCode.QuantityExceeded => "QUANTITY_EXCEEDED",
            FfdHardConstraintCode.PhysicalProfileMissing => "PHYSICAL_PROFILE_MISSING",
            FfdHardConstraintCode.LoadUnitWeightExceeded => "LOAD_UNIT_WEIGHT_EXCEEDED",
            FfdHardConstraintCode.LoadUnitVolumeExceeded => "LOAD_UNIT_VOLUME_EXCEEDED",
            FfdHardConstraintCode.LoadUnitDimensionMismatch => "LOAD_UNIT_DIMENSION_MISMATCH",
            FfdHardConstraintCode.DoorOpeningMismatch => "DOOR_OPENING_MISMATCH",
            FfdHardConstraintCode.CompatibilityBlock => "COMPATIBILITY_BLOCK",
            FfdHardConstraintCode.StackingNotAllowed => "STACKING_NOT_ALLOWED",
            FfdHardConstraintCode.OrientationNotAllowed => "ORIENTATION_NOT_ALLOWED",
            FfdHardConstraintCode.PackageStopMismatch => "PACKAGE_STOP_MISMATCH",
            FfdHardConstraintCode.StopAccessBlock => "STOP_ACCESS_BLOCK",
            _ => "FFD_INFEASIBLE",
        };

    private static string ComputeInputSnapshotHash(
        Guid shipmentId,
        LoadPlanRecord loadPlan,
        IReadOnlyCollection<PlanningItem> items,
        IReadOnlyCollection<VehicleRecord> vehicles,
        IReadOnlyDictionary<Guid, (VehicleCapacityRecord? Capacity, VehicleCapacityZoneRecord[] Zones)> capacityInputs,
        string algorithmVersion,
        string parameterSet)
    {
        var canonical = JsonSerializer.Serialize(new
        {
            shipmentId,
            loadPlanId = loadPlan.Id,
            loadPlan.Version,
            loadPlan.RoutePlanId,
            loadPlan.RoutePlanVersion,
            algorithmVersion,
            parameterSet,
            items = items.OrderBy(x => x.StableSortKey).Select(x => new
            {
                x.ShipmentPackageId,
                x.ShipmentItemId,
                x.ProductId,
                x.PackagingId,
                x.QuantityBase,
                x.PackageCount,
                x.GrossWeightKg,
                x.VolumeM3,
                x.LengthMm,
                x.WidthMm,
                x.HeightMm,
                x.CompatibilityGroup,
                x.IncompatibleGroups,
                x.IsFragile,
                x.KeepUpright,
                x.AllowedOrientations,
                x.SplitAllowed,
                x.RouteStopSequence,
                x.PhysicalProfilePresent,
            }).ToArray(),
            vehicles = vehicles.OrderBy(x => x.Id).Select(x => new
            {
                x.Id,
                x.VehicleTypeId,
                x.Status,
                x.PlateNumber,
                capacity = capacityInputs[x.Id].Capacity is { } capacity
                    ? new
                    {
                        capacity.Id,
                        capacity.EffectiveFrom,
                        capacity.EffectiveTo,
                        capacity.MaxGrossWeight,
                        capacity.TareWeight,
                        capacity.MaxUsableVolume,
                        capacity.MaxPalletCount,
                        capacity.MaxLoadHeight,
                        capacity.CapacityPolicySnapshot,
                        zones = capacityInputs[x.Id].Zones.Select(zone => new { zone.Id, zone.ZoneCode, zone.LengthMm, zone.WidthMm, zone.MaxLoadKg, zone.SequenceNo }).ToArray(),
                    }
                    : null,
            }).ToArray(),
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static string EvaluationKey(Guid vehicleId, Guid? capacityId)
        => $"{vehicleId:N}:{capacityId?.ToString("N") ?? Guid.Empty.ToString("N")}";

    private static VehicleFitEvaluationDto Map(VehicleFitEvaluationRecord record)
        => new(
            record.Id,
            record.LoadPlanId,
            record.VehicleId,
            record.VehicleCapacityId,
            record.CandidateStatus,
            record.RejectionCode,
            record.ReasonText,
            record.WeightRatio,
            record.VolumeRatio,
            record.PalletRatio,
            record.FloorAreaRatio,
            record.HeightRatio,
            record.DoorCheckStatus,
            record.DimensionCheckStatus,
            record.StackingCheckStatus,
            record.AxleCheckStatus,
            record.StopAccessStatus,
            record.FitScore,
            record.AlgorithmVersion,
            record.InputSnapshotHash,
            record.CapacitySnapshot,
            record.EvaluatedAt);

    private static VehicleFitEvaluationDto MapProjection(VehicleFitEvaluationRecord record)
        => Map(record);

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
        => idempotencyStore.SaveAsync(scope, key, payloadHash, statusCode, JsonSerializer.Serialize(result), DateTimeOffset.UtcNow.AddDays(30), cancellationToken);

    private static string ComputePayloadHash<T>(T payload)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload))));

    private static void EnsureExpectedVersion(long actual, long expected, string resourceType, Guid resourceId)
    {
        if (actual != expected)
        {
            throw new DomainException(new("RESOURCE_VERSION_CONFLICT", $"{resourceType} ({resourceId}) güncel değil."));
        }
    }

    private sealed record PhysicalSnapshot(
        decimal LengthMm,
        decimal WidthMm,
        decimal HeightMm,
        decimal NetWeightKg,
        decimal TareWeightKg,
        decimal GrossWeightKg,
        decimal VolumeM3,
        string? CompatibilityGroup,
        IReadOnlyCollection<string> IncompatibleGroups,
        bool IsFragile,
        bool KeepUpright,
        IReadOnlyCollection<string> AllowedOrientations,
        bool Present)
    {
        public static PhysicalSnapshot Missing { get; } = new(0, 0, 0, 0, 0, 0, 0, null, [], false, false, [], false);
    }
}
