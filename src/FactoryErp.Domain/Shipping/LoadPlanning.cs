using FactoryErp.Domain.Common;

namespace FactoryErp.Domain.Shipping;

public enum LoadPlanStatus
{
    Draft,
    Proposed,
    Validating,
    Valid,
    NeedsReview,
    Locked,
    Superseded,
}

public enum LoadPlanFeasibilityStatus
{
    Infeasible,
    FeasibleWithWarnings,
    Feasible,
}

public enum LoadUnitType
{
    Pallet,
    Cage,
    CartonGroup,
    Loose,
}

public enum LoadUnitStatus
{
    Draft,
    Validated,
    Locked,
    Loaded,
    Cancelled,
}

public sealed class LoadPlan : AggregateRoot
{
    private readonly List<LoadUnit> _loadUnits = [];

    private LoadPlan(
        Guid id,
        DateTimeOffset createdAt,
        Guid shipmentId,
        Guid routePlanId,
        int routePlanVersion,
        int version,
        Guid? replannedFromId,
        LoadPlanFeasibilityStatus feasibilityStatus,
        DateTimeOffset updatedAt)
        : base(id, createdAt)
    {
        ShipmentId = shipmentId;
        RoutePlanId = routePlanId;
        RoutePlanVersion = routePlanVersion;
        Version = version;
        ReplannedFromId = replannedFromId;
        Status = LoadPlanStatus.Draft;
        FeasibilityStatus = feasibilityStatus;
        RestoreUpdatedAt(updatedAt);
        ValidationSummary = "{}";
    }

    public Guid ShipmentId { get; }
    public Guid RoutePlanId { get; }
    public int RoutePlanVersion { get; }
    public int Version { get; }
    public Guid? ReplannedFromId { get; }
    public Guid? VehicleId { get; private set; }
    public Guid? VehicleCapacityId { get; private set; }
    public LoadPlanStatus Status { get; private set; }
    public LoadPlanFeasibilityStatus FeasibilityStatus { get; private set; }
    public string? AlgorithmName { get; private set; }
    public string? AlgorithmVersion { get; private set; }
    public string? ParameterSet { get; private set; }
    public string? InputSnapshotHash { get; private set; }
    public string? CapacitySnapshot { get; private set; }
    public string? UtilizationSnapshot { get; private set; }
    public string ValidationSummary { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public Guid? LockedBy { get; private set; }
    public DateTimeOffset? LockedAt { get; private set; }
    public IReadOnlyCollection<LoadUnit> LoadUnits => _loadUnits;

    public static LoadPlan CreateDraft(
        Guid id,
        DateTimeOffset createdAt,
        Guid shipmentId,
        Guid routePlanId,
        int routePlanVersion,
        int version,
        Guid? replannedFromId = null)
    {
        DomainGuard.AgainstEmpty(id, "LOAD_PLAN_ID_REQUIRED", "LoadPlan kimliği zorunludur.");
        DomainGuard.AgainstEmpty(shipmentId, "SHIPMENT_REQUIRED", "LoadPlan shipment kaydına bağlı olmalıdır.");
        DomainGuard.AgainstEmpty(routePlanId, "ROUTE_PLAN_REQUIRED", "LoadPlan route plan kaydına bağlı olmalıdır.");
        if (routePlanVersion <= 0 || version <= 0)
        {
            throw new DomainException(new(
                "LOAD_PLAN_VERSION_INVALID",
                "LoadPlan ve route plan versiyonları pozitif olmalıdır."));
        }

        if (replannedFromId == id)
        {
            throw new DomainException(new(
                "LOAD_PLAN_REPLAN_SELF_REFERENCE",
                "LoadPlan kendisini replan kaynağı olarak gösteremez."));
        }

        return new LoadPlan(
            id,
            createdAt,
            shipmentId,
            routePlanId,
            routePlanVersion,
            version,
            replannedFromId,
            LoadPlanFeasibilityStatus.Infeasible,
            createdAt);
    }

    public void AddLoadUnit(LoadUnit loadUnit)
    {
        EnsureDraftMutable();
        ArgumentNullException.ThrowIfNull(loadUnit);
        if (loadUnit.LoadPlanId != Id)
        {
            throw new DomainException(new(
                "LOAD_UNIT_PLAN_MISMATCH",
                "LoadUnit aynı LoadPlan kaydına ait olmalıdır."));
        }

        if (_loadUnits.Any(x => string.Equals(x.UnitCode, loadUnit.UnitCode, StringComparison.Ordinal)))
        {
            throw new DomainException(new(
                "LOAD_UNIT_CODE_DUPLICATE",
                "Aynı LoadPlan içinde unit code tekrar edemez."));
        }

        _loadUnits.Add(loadUnit);
    }

    public void MarkProposed()
    {
        if (Status != LoadPlanStatus.Draft)
        {
            throw new DomainException(new("LOAD_PLAN_INVALID_TRANSITION", $"{Status} durumundaki plan öneri durumuna geçemez."));
        }

        Status = LoadPlanStatus.Proposed;
    }

    public void MarkValidating()
    {
        if (Status is not (LoadPlanStatus.Draft or LoadPlanStatus.Proposed or LoadPlanStatus.NeedsReview))
        {
            throw new DomainException(new("LOAD_PLAN_INVALID_TRANSITION", $"{Status} durumundaki plan doğrulamaya alınamaz."));
        }

        Status = LoadPlanStatus.Validating;
    }

    public void MarkValid(LoadPlanFeasibilityStatus feasibilityStatus, string validationSummary)
    {
        if (Status != LoadPlanStatus.Validating)
        {
            throw new DomainException(new("LOAD_PLAN_INVALID_TRANSITION", $"{Status} durumundaki plan valid yapılamaz."));
        }

        DomainGuard.AgainstBlank(validationSummary, "LOAD_PLAN_VALIDATION_SUMMARY_REQUIRED", "Validation summary zorunludur.");
        if (feasibilityStatus == LoadPlanFeasibilityStatus.Infeasible)
        {
            throw new DomainException(new("LOAD_PLAN_INFEASIBLE", "Infeasible plan valid yapılamaz."));
        }

        FeasibilityStatus = feasibilityStatus;
        ValidationSummary = validationSummary.Trim();
        Status = feasibilityStatus == LoadPlanFeasibilityStatus.FeasibleWithWarnings
            ? LoadPlanStatus.NeedsReview
            : LoadPlanStatus.Valid;
    }

    public void Lock(
        Guid actorId,
        DateTimeOffset now,
        bool approval,
        bool hasOpenHardErrors,
        bool hasOpenWarnings,
        bool warningOverrideApproved)
    {
        LoadPlanLockPolicy.EnsureLockAllowed(
            Status,
            FeasibilityStatus,
            hasOpenHardErrors,
            hasOpenWarnings,
            approval,
            warningOverrideApproved,
            VehicleId,
            VehicleCapacityId,
            InputSnapshotHash);
        DomainGuard.AgainstEmpty(actorId, "LOAD_PLAN_LOCK_ACTOR_REQUIRED", "LoadPlan lock actor zorunludur.");

        foreach (var loadUnit in _loadUnits.OrderBy(x => x.UnitCode, StringComparer.Ordinal))
        {
            loadUnit.LockForPlan();
        }

        ApprovedBy = actorId;
        ApprovedAt = now;
        LockedBy = actorId;
        LockedAt = now;
        Status = LoadPlanStatus.Locked;
        Touch(now);
    }

    public void Supersede()
    {
        if (Status != LoadPlanStatus.Locked)
        {
            throw new DomainException(new("LOAD_PLAN_INVALID_TRANSITION", "Yalnızca locked plan superseded yapılabilir."));
        }

        Status = LoadPlanStatus.Superseded;
    }

    public void SetPlanningSnapshot(
        Guid? vehicleId,
        Guid? vehicleCapacityId,
        string? algorithmName,
        string? algorithmVersion,
        string? parameterSet,
        string? inputSnapshotHash,
        string? capacitySnapshot,
        string? utilizationSnapshot,
        string validationSummary,
        DateTimeOffset now)
    {
        EnsureDraftMutable();
        if (vehicleId is not null && vehicleId == Guid.Empty || vehicleCapacityId is not null && vehicleCapacityId == Guid.Empty)
        {
            throw new DomainException(new("LOAD_PLAN_SNAPSHOT_INVALID", "Snapshot foreign key değerleri geçerli olmalıdır."));
        }

        DomainGuard.AgainstBlank(validationSummary, "LOAD_PLAN_VALIDATION_SUMMARY_REQUIRED", "Validation summary zorunludur.");
        AlgorithmName = NormalizeOptional(algorithmName);
        AlgorithmVersion = NormalizeOptional(algorithmVersion);
        ParameterSet = NormalizeOptional(parameterSet);
        InputSnapshotHash = NormalizeOptional(inputSnapshotHash);
        CapacitySnapshot = NormalizeOptional(capacitySnapshot);
        UtilizationSnapshot = NormalizeOptional(utilizationSnapshot);
        ValidationSummary = validationSummary.Trim();
        VehicleId = vehicleId;
        VehicleCapacityId = vehicleCapacityId;
        Touch(now);
    }

    public static LoadPlan Rehydrate(
        Guid id,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        Guid shipmentId,
        Guid routePlanId,
        int routePlanVersion,
        int version,
        Guid? replannedFromId,
        Guid? vehicleId,
        Guid? vehicleCapacityId,
        LoadPlanStatus status,
        LoadPlanFeasibilityStatus feasibilityStatus,
        string? algorithmName,
        string? algorithmVersion,
        string? parameterSet,
        string? inputSnapshotHash,
        string? capacitySnapshot,
        string? utilizationSnapshot,
        string validationSummary,
        Guid? approvedBy,
        DateTimeOffset? approvedAt,
        Guid? lockedBy,
        DateTimeOffset? lockedAt,
        IEnumerable<LoadUnit> loadUnits)
    {
        var plan = CreateDraft(id, createdAt, shipmentId, routePlanId, routePlanVersion, version, replannedFromId);
        plan.RestoreUpdatedAt(updatedAt);
        plan.VehicleId = vehicleId;
        plan.VehicleCapacityId = vehicleCapacityId;
        plan.FeasibilityStatus = feasibilityStatus;
        plan.AlgorithmName = NormalizeOptional(algorithmName);
        plan.AlgorithmVersion = NormalizeOptional(algorithmVersion);
        plan.ParameterSet = NormalizeOptional(parameterSet);
        plan.InputSnapshotHash = NormalizeOptional(inputSnapshotHash);
        plan.CapacitySnapshot = NormalizeOptional(capacitySnapshot);
        plan.UtilizationSnapshot = NormalizeOptional(utilizationSnapshot);
        DomainGuard.AgainstBlank(validationSummary, "LOAD_PLAN_VALIDATION_SUMMARY_REQUIRED", "Validation summary zorunludur.");
        plan.ValidationSummary = validationSummary.Trim();
        plan.ApprovedBy = approvedBy;
        plan.ApprovedAt = approvedAt;
        plan.LockedBy = lockedBy;
        plan.LockedAt = lockedAt;

        foreach (var loadUnit in loadUnits.OrderBy(x => x.UnitCode, StringComparer.Ordinal))
        {
            plan.AddLoadUnit(loadUnit);
        }

        plan.Status = status;
        return plan;
    }

    private void EnsureDraftMutable()
    {
        if (Status is not (LoadPlanStatus.Draft or LoadPlanStatus.Proposed or LoadPlanStatus.NeedsReview))
        {
            throw new DomainException(new(
                "LOAD_PLAN_IMMUTABLE",
                $"{Status} durumundaki LoadPlan değiştirilemez."));
        }
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class LoadUnit : Entity
{
    private readonly List<LoadUnitItem> _items = [];

    private LoadUnit(
        Guid id,
        DateTimeOffset createdAt,
        Guid loadPlanId,
        string unitCode,
        LoadUnitType unitType,
        Guid? palletTypeId,
        bool isMixed,
        decimal lengthMm,
        decimal widthMm,
        decimal heightMm,
        decimal tareWeightKg,
        decimal grossWeightKg,
        decimal volumeM3,
        int? maxStackCount,
        string? placementZone,
        int unloadingPriority)
        : base(id, createdAt)
    {
        LoadPlanId = loadPlanId;
        UnitCode = unitCode;
        UnitType = unitType;
        PalletTypeId = palletTypeId;
        IsMixed = isMixed;
        LengthMm = lengthMm;
        WidthMm = widthMm;
        HeightMm = heightMm;
        TareWeightKg = tareWeightKg;
        GrossWeightKg = grossWeightKg;
        VolumeM3 = volumeM3;
        MaxStackCount = maxStackCount;
        PlacementZone = string.IsNullOrWhiteSpace(placementZone) ? null : placementZone.Trim();
        UnloadingPriority = unloadingPriority;
        Status = LoadUnitStatus.Draft;
    }

    public Guid LoadPlanId { get; }
    public string UnitCode { get; }
    public LoadUnitType UnitType { get; }
    public Guid? PalletTypeId { get; }
    public bool IsMixed { get; }
    public decimal LengthMm { get; }
    public decimal WidthMm { get; }
    public decimal HeightMm { get; }
    public decimal TareWeightKg { get; }
    public decimal GrossWeightKg { get; }
    public decimal VolumeM3 { get; }
    public int? MaxStackCount { get; }
    public string? PlacementZone { get; }
    public int UnloadingPriority { get; }
    public LoadUnitStatus Status { get; private set; }
    public IReadOnlyCollection<LoadUnitItem> Items => _items;

    public static LoadUnit Create(
        Guid id,
        DateTimeOffset createdAt,
        Guid loadPlanId,
        string unitCode,
        LoadUnitType unitType,
        Guid? palletTypeId,
        bool isMixed,
        decimal lengthMm,
        decimal widthMm,
        decimal heightMm,
        decimal tareWeightKg,
        decimal grossWeightKg,
        decimal volumeM3,
        int? maxStackCount,
        string? placementZone,
        int unloadingPriority)
    {
        DomainGuard.AgainstEmpty(id, "LOAD_UNIT_ID_REQUIRED", "LoadUnit kimliği zorunludur.");
        DomainGuard.AgainstEmpty(loadPlanId, "LOAD_PLAN_REQUIRED", "LoadUnit LoadPlan kaydına bağlı olmalıdır.");
        DomainGuard.AgainstBlank(unitCode, "LOAD_UNIT_CODE_REQUIRED", "LoadUnit code zorunludur.");
        if (!Enum.IsDefined(unitType))
        {
            throw new DomainException(new("LOAD_UNIT_TYPE_INVALID", "LoadUnit tipi geçersizdir."));
        }

        if (lengthMm <= 0 || widthMm <= 0 || heightMm <= 0)
        {
            throw new DomainException(new("LOAD_UNIT_DIMENSIONS_INVALID", "LoadUnit ölçüleri sıfırdan büyük olmalıdır."));
        }

        if (tareWeightKg < 0 || grossWeightKg < tareWeightKg)
        {
            throw new DomainException(new("LOAD_UNIT_WEIGHT_INVALID", "LoadUnit brüt ve dara ağırlığı geçerli olmalıdır."));
        }

        if (volumeM3 <= 0)
        {
            throw new DomainException(new("LOAD_UNIT_VOLUME_INVALID", "LoadUnit hacmi sıfırdan büyük olmalıdır."));
        }

        if (maxStackCount is <= 0 || unloadingPriority <= 0)
        {
            throw new DomainException(new("LOAD_UNIT_STACK_OR_PRIORITY_INVALID", "Stack count ve unloading priority geçerli olmalıdır."));
        }

        return new LoadUnit(
            id,
            createdAt,
            loadPlanId,
            unitCode.Trim(),
            unitType,
            palletTypeId,
            isMixed,
            lengthMm,
            widthMm,
            heightMm,
            tareWeightKg,
            grossWeightKg,
            volumeM3,
            maxStackCount,
            placementZone,
            unloadingPriority);
    }

    public void MarkValidated()
    {
        if (Status is not LoadUnitStatus.Draft)
        {
            throw new DomainException(new("LOAD_UNIT_INVALID_TRANSITION", $"{Status} durumundaki LoadUnit validated yapılamaz."));
        }

        Status = LoadUnitStatus.Validated;
    }

    public void LockForPlan()
    {
        if (Status is LoadUnitStatus.Loaded or LoadUnitStatus.Cancelled)
        {
            throw new DomainException(new("LOAD_UNIT_INVALID_TRANSITION", $"{Status} durumundaki LoadUnit lock edilemez."));
        }

        Status = LoadUnitStatus.Locked;
    }

    public void AddItem(LoadUnitItem item)
    {
        if (item.LoadUnitId != Id)
        {
            throw new DomainException(new("LOAD_UNIT_ITEM_UNIT_MISMATCH", "LoadUnitItem aynı LoadUnit kaydına ait olmalıdır."));
        }

        if (_items.Any(x => x.Id == item.Id))
        {
            throw new DomainException(new("LOAD_UNIT_ITEM_DUPLICATE", "Aynı LoadUnitItem tekrar eklenemez."));
        }

        _items.Add(item);
    }

    public static LoadUnit Rehydrate(
        Guid id,
        DateTimeOffset createdAt,
        Guid loadPlanId,
        string unitCode,
        LoadUnitType unitType,
        Guid? palletTypeId,
        bool isMixed,
        decimal lengthMm,
        decimal widthMm,
        decimal heightMm,
        decimal tareWeightKg,
        decimal grossWeightKg,
        decimal volumeM3,
        int? maxStackCount,
        string? placementZone,
        int unloadingPriority,
        LoadUnitStatus status,
        IEnumerable<LoadUnitItem> items)
    {
        var unit = Create(id, createdAt, loadPlanId, unitCode, unitType, palletTypeId, isMixed, lengthMm, widthMm, heightMm, tareWeightKg, grossWeightKg, volumeM3, maxStackCount, placementZone, unloadingPriority);
        foreach (var item in items.OrderBy(x => x.Id))
        {
            unit.AddItem(item);
        }

        unit.Status = status;
        return unit;
    }
}

public sealed class LoadUnitItem : Entity
{
    private LoadUnitItem(
        Guid id,
        DateTimeOffset createdAt,
        Guid loadUnitId,
        Guid shipmentPackageId,
        Guid shipmentItemId,
        decimal quantityBase,
        decimal grossWeightKg,
        decimal volumeM3,
        string allocationSnapshot)
        : base(id, createdAt)
    {
        LoadUnitId = loadUnitId;
        ShipmentPackageId = shipmentPackageId;
        ShipmentItemId = shipmentItemId;
        QuantityBase = quantityBase;
        GrossWeightKg = grossWeightKg;
        VolumeM3 = volumeM3;
        AllocationSnapshot = allocationSnapshot;
    }

    public Guid LoadUnitId { get; }
    public Guid ShipmentPackageId { get; }
    public Guid ShipmentItemId { get; }
    public decimal QuantityBase { get; }
    public decimal GrossWeightKg { get; }
    public decimal VolumeM3 { get; }
    public string AllocationSnapshot { get; }
    public IReadOnlyCollection<LoadUnitStopAllocation> StopAllocations { get; private set; } = Array.Empty<LoadUnitStopAllocation>();

    public static LoadUnitItem Create(
        Guid id,
        DateTimeOffset createdAt,
        Guid loadUnitId,
        Guid shipmentPackageId,
        Guid shipmentItemId,
        decimal quantityBase,
        decimal grossWeightKg,
        decimal volumeM3,
        string allocationSnapshot)
    {
        DomainGuard.AgainstEmpty(id, "LOAD_UNIT_ITEM_ID_REQUIRED", "LoadUnitItem kimliği zorunludur.");
        DomainGuard.AgainstEmpty(loadUnitId, "LOAD_UNIT_REQUIRED", "LoadUnitItem LoadUnit kaydına bağlı olmalıdır.");
        DomainGuard.AgainstEmpty(shipmentPackageId, "SHIPMENT_PACKAGE_REQUIRED", "LoadUnitItem ShipmentPackage kaydına bağlı olmalıdır.");
        DomainGuard.AgainstEmpty(shipmentItemId, "SHIPMENT_ITEM_REQUIRED", "LoadUnitItem ShipmentItem kaydına bağlı olmalıdır.");
        DomainGuard.AgainstBlank(allocationSnapshot, "LOAD_UNIT_ALLOCATION_SNAPSHOT_REQUIRED", "Allocation snapshot zorunludur.");
        if (quantityBase <= 0)
        {
            throw new DomainException(new("LOAD_UNIT_ITEM_QUANTITY_INVALID", "LoadUnitItem miktarı sıfırdan büyük olmalıdır."));
        }

        if (grossWeightKg < 0 || volumeM3 < 0)
        {
            throw new DomainException(new("LOAD_UNIT_ITEM_PHYSICAL_VALUE_INVALID", "LoadUnitItem ağırlık ve hacim değerleri negatif olamaz."));
        }

        return new LoadUnitItem(id, createdAt, loadUnitId, shipmentPackageId, shipmentItemId, quantityBase, grossWeightKg, volumeM3, allocationSnapshot.Trim());
    }

    public void SetStopAllocations(IEnumerable<LoadUnitStopAllocation> allocations)
    {
        var materialized = allocations.OrderBy(x => x.SequenceNo).ToArray();
        if (materialized.Any(x => x.LoadUnitItemId != Id))
        {
            throw new DomainException(new("LOAD_UNIT_STOP_ITEM_MISMATCH", "Stop allocation aynı LoadUnitItem kaydına ait olmalıdır."));
        }

        if (materialized.GroupBy(x => x.RouteStopId).Any(x => x.Count() > 1))
        {
            throw new DomainException(new("LOAD_UNIT_STOP_DUPLICATE", "Aynı route stop için duplicate allocation olamaz."));
        }

        var total = materialized.Sum(x => x.QuantityBase);
        if (total > QuantityBase)
        {
            throw new DomainException(new("LOAD_UNIT_STOP_QUANTITY_EXCEEDED", "Stop allocation toplamı LoadUnitItem miktarını aşamaz."));
        }

        StopAllocations = materialized;
    }

    public static void EnsureQuantityCeiling(
        decimal existingQuantity,
        decimal newQuantity,
        decimal shipmentItemQuantity,
        bool splitAllowed,
        bool packageAlreadyAssigned)
    {
        if (newQuantity <= 0 || existingQuantity < 0 || shipmentItemQuantity <= 0)
        {
            throw new DomainException(new("QUANTITY_EXCEEDED", "Allocation quantity değerleri geçerli olmalıdır."));
        }

        if (!splitAllowed && packageAlreadyAssigned)
        {
            throw new DomainException(new("PACKAGE_ALREADY_ASSIGNED", "Split edilmeyen package birden fazla LoadUnit’e atanamaz."));
        }

        if (existingQuantity + newQuantity > shipmentItemQuantity)
        {
            throw new DomainException(new("QUANTITY_EXCEEDED", "LoadUnitItem toplamı shipment item miktarını aşamaz."));
        }
    }

    public static LoadUnitItem Rehydrate(
        Guid id,
        DateTimeOffset createdAt,
        Guid loadUnitId,
        Guid shipmentPackageId,
        Guid shipmentItemId,
        decimal quantityBase,
        decimal grossWeightKg,
        decimal volumeM3,
        string allocationSnapshot,
        IEnumerable<LoadUnitStopAllocation> stopAllocations)
    {
        var item = Create(id, createdAt, loadUnitId, shipmentPackageId, shipmentItemId, quantityBase, grossWeightKg, volumeM3, allocationSnapshot);
        item.SetStopAllocations(stopAllocations);
        return item;
    }
}

public sealed class LoadUnitStopAllocation : Entity
{
    private LoadUnitStopAllocation(
        Guid id,
        DateTimeOffset createdAt,
        Guid loadUnitItemId,
        Guid routeStopId,
        decimal quantityBase,
        int sequenceNo)
        : base(id, createdAt)
    {
        LoadUnitItemId = loadUnitItemId;
        RouteStopId = routeStopId;
        QuantityBase = quantityBase;
        SequenceNo = sequenceNo;
    }

    public Guid LoadUnitItemId { get; }
    public Guid RouteStopId { get; }
    public decimal QuantityBase { get; }
    public int SequenceNo { get; }

    public static LoadUnitStopAllocation Create(
        Guid id,
        DateTimeOffset createdAt,
        Guid loadUnitItemId,
        Guid routeStopId,
        decimal quantityBase,
        int sequenceNo)
    {
        DomainGuard.AgainstEmpty(id, "LOAD_UNIT_STOP_ALLOCATION_ID_REQUIRED", "Stop allocation kimliği zorunludur.");
        DomainGuard.AgainstEmpty(loadUnitItemId, "LOAD_UNIT_ITEM_REQUIRED", "Stop allocation LoadUnitItem kaydına bağlı olmalıdır.");
        DomainGuard.AgainstEmpty(routeStopId, "ROUTE_STOP_REQUIRED", "Stop allocation route stop kaydına bağlı olmalıdır.");
        if (quantityBase <= 0)
        {
            throw new DomainException(new("LOAD_UNIT_STOP_QUANTITY_INVALID", "Stop allocation miktarı sıfırdan büyük olmalıdır."));
        }

        if (sequenceNo <= 0)
        {
            throw new DomainException(new("LOAD_UNIT_STOP_SEQUENCE_INVALID", "Stop allocation sequence pozitif olmalıdır."));
        }

        return new LoadUnitStopAllocation(id, createdAt, loadUnitItemId, routeStopId, quantityBase, sequenceNo);
    }
}
