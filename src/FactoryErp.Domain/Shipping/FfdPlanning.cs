using FactoryErp.Domain.Common;

namespace FactoryErp.Domain.Shipping;

public sealed record PlanningItem
{
    private PlanningItem(
        Guid shipmentPackageId,
        Guid shipmentItemId,
        Guid productId,
        Guid? packagingId,
        decimal quantityBase,
        decimal packageCount,
        decimal netWeightKg,
        decimal packagingTareKg,
        decimal grossWeightKg,
        decimal volumeM3,
        decimal lengthMm,
        decimal widthMm,
        decimal heightMm,
        string? compatibilityGroup,
        IReadOnlyCollection<string> incompatibleGroups,
        bool isFragile,
        bool keepUpright,
        IReadOnlyCollection<string> allowedOrientations,
        bool splitAllowed,
        int routeStopSequence,
        bool physicalProfilePresent)
    {
        ShipmentPackageId = shipmentPackageId;
        ShipmentItemId = shipmentItemId;
        ProductId = productId;
        PackagingId = packagingId;
        QuantityBase = quantityBase;
        PackageCount = packageCount;
        NetWeightKg = netWeightKg;
        PackagingTareKg = packagingTareKg;
        GrossWeightKg = grossWeightKg;
        VolumeM3 = volumeM3;
        LengthMm = lengthMm;
        WidthMm = widthMm;
        HeightMm = heightMm;
        CompatibilityGroup = string.IsNullOrWhiteSpace(compatibilityGroup) ? null : compatibilityGroup.Trim();
        IncompatibleGroups = incompatibleGroups
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        IsFragile = isFragile;
        KeepUpright = keepUpright;
        AllowedOrientations = allowedOrientations
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        SplitAllowed = splitAllowed;
        RouteStopSequence = routeStopSequence;
        PhysicalProfilePresent = physicalProfilePresent;
        StableSortKey = PlanningItemSortKey.Create(this);
    }

    public Guid ShipmentPackageId { get; }
    public Guid ShipmentItemId { get; }
    public Guid ProductId { get; }
    public Guid? PackagingId { get; }
    public decimal QuantityBase { get; }
    public decimal PackageCount { get; }
    public decimal NetWeightKg { get; }
    public decimal PackagingTareKg { get; }
    public decimal GrossWeightKg { get; }
    public decimal VolumeM3 { get; }
    public decimal LengthMm { get; }
    public decimal WidthMm { get; }
    public decimal HeightMm { get; }
    public decimal FloorFootprint => LengthMm * WidthMm;
    public string? CompatibilityGroup { get; }
    public IReadOnlyCollection<string> IncompatibleGroups { get; }
    public bool IsFragile { get; }
    public bool KeepUpright { get; }
    public IReadOnlyCollection<string> AllowedOrientations { get; }
    public bool SplitAllowed { get; }
    public int RouteStopSequence { get; }
    public bool PhysicalProfilePresent { get; }
    public PlanningItemSortKey StableSortKey { get; }

    public static PlanningItem Create(
        Guid shipmentPackageId,
        Guid shipmentItemId,
        Guid productId,
        Guid? packagingId,
        decimal quantityBase,
        decimal packageCount,
        decimal netWeightKg,
        decimal packagingTareKg,
        decimal grossWeightKg,
        decimal volumeM3,
        decimal lengthMm,
        decimal widthMm,
        decimal heightMm,
        string? compatibilityGroup,
        IEnumerable<string>? incompatibleGroups,
        bool isFragile,
        bool keepUpright,
        IEnumerable<string>? allowedOrientations,
        bool splitAllowed,
        int routeStopSequence,
        bool physicalProfilePresent)
    {
        DomainGuard.AgainstEmpty(shipmentPackageId, "SHIPMENT_PACKAGE_REQUIRED", "PlanningItem ShipmentPackage kaydına bağlı olmalıdır.");
        DomainGuard.AgainstEmpty(shipmentItemId, "SHIPMENT_ITEM_REQUIRED", "PlanningItem ShipmentItem kaydına bağlı olmalıdır.");
        DomainGuard.AgainstEmpty(productId, "PRODUCT_REQUIRED", "PlanningItem ürün kaydına bağlı olmalıdır.");
        if (quantityBase <= 0 || packageCount <= 0)
        {
            throw new DomainException(new("PLANNING_ITEM_QUANTITY_INVALID", "PlanningItem miktarları sıfırdan büyük olmalıdır."));
        }

        if (netWeightKg < 0 || packagingTareKg < 0 || grossWeightKg < 0 || volumeM3 < 0)
        {
            throw new DomainException(new("PLANNING_ITEM_PHYSICAL_VALUE_INVALID", "PlanningItem ağırlık ve hacim değerleri negatif olamaz."));
        }

        if (grossWeightKg < netWeightKg + packagingTareKg)
        {
            throw new DomainException(new("PLANNING_ITEM_GROSS_WEIGHT_INVALID", "Gross weight net ve packaging tare toplamından küçük olamaz."));
        }

        if (physicalProfilePresent && (lengthMm <= 0 || widthMm <= 0 || heightMm <= 0 || volumeM3 <= 0))
        {
            throw new DomainException(new("PLANNING_ITEM_PHYSICAL_PROFILE_INVALID", "Geçerli fiziksel profile sahip PlanningItem ölçü ve hacim taşımalıdır."));
        }

        if (routeStopSequence <= 0)
        {
            throw new DomainException(new("PLANNING_ITEM_STOP_SEQUENCE_INVALID", "PlanningItem route stop sequence pozitif olmalıdır."));
        }

        return new PlanningItem(
            shipmentPackageId,
            shipmentItemId,
            productId,
            packagingId,
            quantityBase,
            packageCount,
            netWeightKg,
            packagingTareKg,
            grossWeightKg,
            volumeM3,
            lengthMm,
            widthMm,
            heightMm,
            compatibilityGroup,
            (incompatibleGroups ?? []).ToArray(),
            isFragile,
            keepUpright,
            (allowedOrientations ?? []).ToArray(),
            splitAllowed,
            routeStopSequence,
            physicalProfilePresent);
    }
}

public readonly record struct PlanningItemSortKey(
    string CompatibilityGroup,
    bool KeepUpright,
    bool IsFragile,
    decimal GrossWeightKg,
    decimal VolumeM3,
    decimal FloorFootprint,
    int RouteStopSequence,
    Guid ShipmentItemId,
    Guid PackagingId,
    Guid ShipmentPackageId) : IComparable<PlanningItemSortKey>
{
    public static PlanningItemSortKey Create(PlanningItem item)
        => new(
            item.CompatibilityGroup ?? string.Empty,
            item.KeepUpright,
            item.IsFragile,
            item.GrossWeightKg,
            item.VolumeM3,
            item.FloorFootprint,
            item.RouteStopSequence,
            item.ShipmentItemId,
            item.PackagingId ?? Guid.Empty,
            item.ShipmentPackageId);

    public int CompareTo(PlanningItemSortKey other)
    {
        var result = string.Compare(CompatibilityGroup, other.CompatibilityGroup, StringComparison.Ordinal);
        if (result != 0) return result;
        result = other.KeepUpright.CompareTo(KeepUpright);
        if (result != 0) return result;
        result = other.IsFragile.CompareTo(IsFragile);
        if (result != 0) return result;
        result = other.GrossWeightKg.CompareTo(GrossWeightKg);
        if (result != 0) return result;
        result = other.VolumeM3.CompareTo(VolumeM3);
        if (result != 0) return result;
        result = other.FloorFootprint.CompareTo(FloorFootprint);
        if (result != 0) return result;
        result = RouteStopSequence.CompareTo(other.RouteStopSequence);
        if (result != 0) return result;
        result = ShipmentItemId.CompareTo(other.ShipmentItemId);
        if (result != 0) return result;
        result = PackagingId.CompareTo(other.PackagingId);
        if (result != 0) return result;
        return ShipmentPackageId.CompareTo(other.ShipmentPackageId);
    }
}

public enum FfdHardConstraintCode
{
    None,
    QuantityExceeded,
    PhysicalProfileMissing,
    LoadUnitWeightExceeded,
    LoadUnitVolumeExceeded,
    LoadUnitDimensionMismatch,
    DoorOpeningMismatch,
    CompatibilityBlock,
    StackingNotAllowed,
    OrientationNotAllowed,
    PackageStopMismatch,
    StopAccessBlock,
    Infeasible,
}

public sealed record FfdUnitCapacity(
    string UnitCode,
    decimal LengthMm,
    decimal WidthMm,
    decimal HeightMm,
    decimal MaxGrossWeightKg,
    decimal MaxVolumeM3,
    bool IsStackable,
    int? MaxStackCount,
    string? PlacementZone,
    bool AllowNewUnit = false)
{
    public static FfdUnitCapacity Create(
        string unitCode,
        decimal lengthMm,
        decimal widthMm,
        decimal heightMm,
        decimal maxGrossWeightKg,
        decimal maxVolumeM3,
        bool isStackable,
        int? maxStackCount,
        string? placementZone,
        bool allowNewUnit = false)
    {
        DomainGuard.AgainstBlank(unitCode, "FFD_UNIT_CODE_REQUIRED", "FFD unit code zorunludur.");
        if (lengthMm <= 0 || widthMm <= 0 || heightMm <= 0 || maxGrossWeightKg <= 0 || maxVolumeM3 <= 0)
        {
            throw new DomainException(new("FFD_UNIT_CAPACITY_INVALID", "FFD unit fiziksel kapasite değerleri pozitif olmalıdır."));
        }

        if (maxStackCount is <= 0)
        {
            throw new DomainException(new("FFD_UNIT_STACK_INVALID", "FFD unit stack count pozitif olmalıdır."));
        }

        return new FfdUnitCapacity(unitCode.Trim(), lengthMm, widthMm, heightMm, maxGrossWeightKg, maxVolumeM3, isStackable, maxStackCount, placementZone, allowNewUnit);
    }
}

public sealed record FfdPlacement(
    string UnitCode,
    Guid ShipmentPackageId,
    Guid ShipmentItemId,
    decimal QuantityBase,
    decimal GrossWeightKg,
    decimal VolumeM3);

public sealed record FfdRejection(
    Guid ShipmentPackageId,
    Guid ShipmentItemId,
    FfdHardConstraintCode Code,
    string Reason);

public sealed record FfdExecutionResult(
    IReadOnlyCollection<PlanningItem> OrderedItems,
    IReadOnlyCollection<FfdPlacement> Placements,
    IReadOnlyCollection<FfdRejection> Rejections,
    IReadOnlyCollection<string> UnitCodes,
    string AlgorithmName,
    string AlgorithmVersion)
{
    public bool IsFeasible => Rejections.Count == 0;
}

public static class DeterministicFfdEngine
{
    public const string AlgorithmName = "DeterministicFirstFitDecreasing";
    public const string AlgorithmVersion = "L4-B3.1";

    public static FfdExecutionResult Execute(
        IEnumerable<PlanningItem> sourceItems,
        IEnumerable<FfdUnitCapacity> sourceUnits)
    {
        var items = sourceItems
            .OrderBy(x => x.StableSortKey)
            .ThenBy(x => x.ShipmentPackageId)
            .ToArray();
        var capacities = sourceUnits
            .OrderBy(x => x.UnitCode, StringComparer.Ordinal)
            .ToArray();
        var bins = capacities
            .Select(x => new FfdBinState(x))
            .ToList();
        var placements = new List<FfdPlacement>();
        var rejections = new List<FfdRejection>();

        foreach (var item in items)
        {
            if (!item.PhysicalProfilePresent)
            {
                rejections.Add(new FfdRejection(item.ShipmentPackageId, item.ShipmentItemId, FfdHardConstraintCode.PhysicalProfileMissing, "Physical profile bulunamadı."));
                continue;
            }

            var remainingQuantity = item.QuantityBase;
            var attemptedConstraint = FfdHardConstraintCode.Infeasible;
            while (remainingQuantity > 0)
            {
                var candidate = bins.FirstOrDefault(x => x.CanFit(item, remainingQuantity, out _));
                if (candidate is not null)
                {
                    var quantity = candidate.MaxFittableQuantity(item, remainingQuantity);
                    if (quantity <= 0)
                    {
                        attemptedConstraint = FfdHardConstraintCode.Infeasible;
                        break;
                    }

                    candidate.Add(item, quantity);
                    placements.Add(new FfdPlacement(
                        candidate.UnitCode,
                        item.ShipmentPackageId,
                        item.ShipmentItemId,
                        quantity,
                        Scale(item.GrossWeightKg, quantity, item.QuantityBase),
                        Scale(item.VolumeM3, quantity, item.QuantityBase)));
                    remainingQuantity -= quantity;
                    if (!item.SplitAllowed && remainingQuantity > 0)
                    {
                        attemptedConstraint = FfdHardConstraintCode.QuantityExceeded;
                        break;
                    }
                    continue;
                }

                attemptedConstraint = FirstFailure(item, bins, capacities);
                var template = capacities.FirstOrDefault(x => x.AllowNewUnit);
                if (template is null)
                {
                    break;
                }

                var templateBin = new FfdBinState(template with { UnitCode = "__template__", AllowNewUnit = false });
                if (!templateBin.CanFit(item, remainingQuantity, out _))
                {
                    break;
                }

                var nextCode = NextUnitCode(bins, template.UnitCode);
                var newBin = new FfdBinState(template with { UnitCode = nextCode, AllowNewUnit = false });
                bins.Add(newBin);
            }

            if (remainingQuantity > 0)
            {
                placements.RemoveAll(x => x.ShipmentPackageId == item.ShipmentPackageId && x.ShipmentItemId == item.ShipmentItemId);
                rejections.Add(new FfdRejection(item.ShipmentPackageId, item.ShipmentItemId, attemptedConstraint, $"Item LoadUnit kapasitesine sığmadı: {attemptedConstraint}."));
            }
        }

        return new FfdExecutionResult(
            items,
            placements,
            rejections,
            bins.Select(x => x.UnitCode).Order(StringComparer.Ordinal).ToArray(),
            AlgorithmName,
            AlgorithmVersion);
    }

    private static FfdHardConstraintCode FirstFailure(
        PlanningItem item,
        IReadOnlyCollection<FfdBinState> bins,
        IReadOnlyCollection<FfdUnitCapacity> capacities)
    {
        var failures = bins
            .OrderBy(x => x.UnitCode, StringComparer.Ordinal)
            .Select(x => x.Failure(item))
            .Where(x => x != FfdHardConstraintCode.None)
            .ToArray();
        if (failures.Length > 0)
        {
            return failures[0];
        }

        var templateFailures = capacities
            .OrderBy(x => x.UnitCode, StringComparer.Ordinal)
            .Select(x => new FfdBinState(x).Failure(item))
            .Where(x => x != FfdHardConstraintCode.None)
            .ToArray();
        return templateFailures.FirstOrDefault(x => x != FfdHardConstraintCode.None, FfdHardConstraintCode.Infeasible);
    }

    private static decimal Scale(decimal total, decimal quantity, decimal sourceQuantity)
        => sourceQuantity == 0 ? 0 : total * quantity / sourceQuantity;

    private static string NextUnitCode(IReadOnlyCollection<FfdBinState> bins, string prefix)
    {
        var number = 1;
        var existing = bins.Select(x => x.UnitCode).ToHashSet(StringComparer.Ordinal);
        while (existing.Contains($"{prefix}-{number:D3}"))
        {
            number++;
        }

        return $"{prefix}-{number:D3}";
    }

    private sealed class FfdBinState(FfdUnitCapacity capacity)
    {
        private readonly HashSet<string> _compatibilityGroups = new(StringComparer.Ordinal);
        private readonly HashSet<string> _incompatibleGroups = new(StringComparer.Ordinal);
        private decimal _grossWeightKg;
        private decimal _volumeM3;
        private decimal _maxItemHeightMm;

        public string UnitCode => capacity.UnitCode;

        public bool CanFit(PlanningItem item, decimal quantity, out FfdHardConstraintCode code)
        {
            code = StructuralFailure(item);
            if (code != FfdHardConstraintCode.None)
            {
                return false;
            }

            if (!item.SplitAllowed)
            {
                code = CapacityFailure(item, quantity);
                return code == FfdHardConstraintCode.None;
            }

            var fittableQuantity = MaxFittableQuantity(item, quantity);
            if (fittableQuantity <= 0)
            {
                code = CapacityFailure(item, quantity);
                return false;
            }

            return true;
        }

        public FfdHardConstraintCode Failure(PlanningItem item)
        {
            var structural = StructuralFailure(item);
            return structural == FfdHardConstraintCode.None
                ? CapacityFailure(item, item.QuantityBase)
                : structural;
        }

        private FfdHardConstraintCode StructuralFailure(PlanningItem item)
        {
            if (!item.PhysicalProfilePresent)
            {
                return FfdHardConstraintCode.PhysicalProfileMissing;
            }

            if (item.LengthMm > capacity.LengthMm || item.WidthMm > capacity.WidthMm || item.HeightMm > capacity.HeightMm)
            {
                return FfdHardConstraintCode.LoadUnitDimensionMismatch;
            }

            if (item.KeepUpright && !item.AllowedOrientations.Contains("LWH", StringComparer.Ordinal))
            {
                return FfdHardConstraintCode.OrientationNotAllowed;
            }

            if (!capacity.IsStackable && _maxItemHeightMm > 0)
            {
                return FfdHardConstraintCode.StackingNotAllowed;
            }

            if (item.CompatibilityGroup is not null && _incompatibleGroups.Contains(item.CompatibilityGroup))
            {
                return FfdHardConstraintCode.CompatibilityBlock;
            }

            if (_compatibilityGroups.Any(x => item.IncompatibleGroups.Contains(x, StringComparer.Ordinal)))
            {
                return FfdHardConstraintCode.CompatibilityBlock;
            }

            return FfdHardConstraintCode.None;
        }

        private FfdHardConstraintCode CapacityFailure(PlanningItem item, decimal quantity)
        {
            var weight = _grossWeightKg + Scale(item.GrossWeightKg, quantity, item.QuantityBase);
            if (weight > capacity.MaxGrossWeightKg)
            {
                return FfdHardConstraintCode.LoadUnitWeightExceeded;
            }

            var volume = _volumeM3 + Scale(item.VolumeM3, quantity, item.QuantityBase);
            return volume > capacity.MaxVolumeM3
                ? FfdHardConstraintCode.LoadUnitVolumeExceeded
                : FfdHardConstraintCode.None;
        }

        public decimal MaxFittableQuantity(PlanningItem item, decimal remainingQuantity)
        {
            var byWeight = item.GrossWeightKg <= 0
                ? remainingQuantity
                : (capacity.MaxGrossWeightKg - _grossWeightKg) * item.QuantityBase / item.GrossWeightKg;
            var byVolume = item.VolumeM3 <= 0
                ? remainingQuantity
                : (capacity.MaxVolumeM3 - _volumeM3) * item.QuantityBase / item.VolumeM3;
            return Math.Min(remainingQuantity, Math.Max(0, Math.Min(byWeight, byVolume)));
        }

        public void Add(PlanningItem item, decimal quantity)
        {
            _grossWeightKg += Scale(item.GrossWeightKg, quantity, item.QuantityBase);
            _volumeM3 += Scale(item.VolumeM3, quantity, item.QuantityBase);
            _maxItemHeightMm = Math.Max(_maxItemHeightMm, item.HeightMm);
            if (item.CompatibilityGroup is not null)
            {
                _compatibilityGroups.Add(item.CompatibilityGroup);
            }

            foreach (var incompatible in item.IncompatibleGroups)
            {
                _incompatibleGroups.Add(incompatible);
            }
        }
    }
}
