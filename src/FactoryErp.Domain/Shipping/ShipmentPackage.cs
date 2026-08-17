using FactoryErp.Domain.Common;

namespace FactoryErp.Domain.Shipping;

public enum ShipmentPackageType
{
    Case,
    Package,
    Pallet,
    Loose,
}

public enum ShipmentPackageStatus
{
    Available,
    Allocated,
    Loaded,
    Cancelled,
}

public sealed class ShipmentPackage : AggregateRoot
{
    private ShipmentPackage(
        Guid id,
        DateTimeOffset now,
        Guid shipmentId,
        Guid shipmentItemId,
        Guid? packagingId,
        Guid? routeStopId,
        ShipmentPackageType packageType,
        decimal packageCount,
        decimal quantityBasePerPackage,
        decimal? enteredQuantity,
        string? packageCode,
        string packagingSnapshot,
        string physicalSnapshot,
        bool splitAllowed)
        : base(id, now)
    {
        ShipmentId = shipmentId;
        ShipmentItemId = shipmentItemId;
        PackagingId = packagingId;
        RouteStopId = routeStopId;
        PackageType = packageType;
        PackageCount = packageCount;
        QuantityBasePerPackage = quantityBasePerPackage;
        QuantityBase = packageCount * quantityBasePerPackage;
        EnteredQuantity = enteredQuantity;
        PackageCode = NormalizeOptional(packageCode);
        PackagingSnapshot = packagingSnapshot;
        PhysicalSnapshot = physicalSnapshot;
        SplitAllowed = splitAllowed;
        Status = ShipmentPackageStatus.Available;
    }

    public Guid ShipmentId { get; }
    public Guid ShipmentItemId { get; }
    public Guid? PackagingId { get; }
    public Guid? RouteStopId { get; }
    public ShipmentPackageType PackageType { get; }
    public decimal PackageCount { get; }
    public decimal QuantityBasePerPackage { get; }
    public decimal QuantityBase { get; }
    public decimal? EnteredQuantity { get; }
    public string? PackageCode { get; }
    public string PackagingSnapshot { get; }
    public string PhysicalSnapshot { get; }
    public bool SplitAllowed { get; }
    public ShipmentPackageStatus Status { get; private set; }

    public static ShipmentPackage Create(
        Guid id,
        DateTimeOffset now,
        Guid shipmentId,
        Guid shipmentItemId,
        Guid? packagingId,
        Guid? routeStopId,
        ShipmentPackageType packageType,
        decimal packageCount,
        decimal quantityBasePerPackage,
        decimal? enteredQuantity,
        string? packageCode,
        string packagingSnapshot,
        string physicalSnapshot,
        bool splitAllowed)
    {
        DomainGuard.AgainstEmpty(id, "SHIPMENT_PACKAGE_ID_REQUIRED", "Shipment package kimliği zorunludur.");
        DomainGuard.AgainstEmpty(shipmentId, "SHIPMENT_REQUIRED", "Shipment zorunludur.");
        DomainGuard.AgainstEmpty(shipmentItemId, "SHIPMENT_ITEM_REQUIRED", "Shipment item zorunludur.");
        DomainGuard.AgainstBlank(packagingSnapshot, "PACKAGING_SNAPSHOT_REQUIRED", "Ambalaj snapshot zorunludur.");
        DomainGuard.AgainstBlank(physicalSnapshot, "PHYSICAL_SNAPSHOT_REQUIRED", "Fiziksel snapshot zorunludur.");

        if (!Enum.IsDefined(packageType))
        {
            throw new DomainException(new("SHIPMENT_PACKAGE_TYPE_INVALID", "Shipment package tipi geçersizdir."));
        }

        if (packageCount <= 0 || quantityBasePerPackage <= 0)
        {
            throw new DomainException(new(
                "SHIPMENT_PACKAGE_QUANTITY_INVALID",
                "Package count ve paket başına temel miktar sıfırdan büyük olmalıdır."));
        }

        if (enteredQuantity is <= 0)
        {
            throw new DomainException(new(
                "SHIPMENT_PACKAGE_ENTERED_QUANTITY_INVALID",
                "Girilen miktar varsa sıfırdan büyük olmalıdır."));
        }

        var quantityBase = packageCount * quantityBasePerPackage;
        if (quantityBase <= 0)
        {
            throw new DomainException(new(
                "SHIPMENT_PACKAGE_QUANTITY_INVALID",
                "Hesaplanan temel miktar sıfırdan büyük olmalıdır."));
        }

        return new ShipmentPackage(
            id,
            now,
            shipmentId,
            shipmentItemId,
            packagingId,
            routeStopId,
            packageType,
            packageCount,
            quantityBasePerPackage,
            enteredQuantity,
            packageCode,
            packagingSnapshot.Trim(),
            physicalSnapshot.Trim(),
            splitAllowed);
    }

    public static void EnsureOwnership(
        Guid shipmentId,
        Guid shipmentItemShipmentId,
        Guid? routeStopId,
        Guid? routeStopShipmentId)
    {
        if (shipmentId == Guid.Empty || shipmentItemShipmentId == Guid.Empty || shipmentId != shipmentItemShipmentId)
        {
            throw new DomainException(new(
                "SHIPMENT_PACKAGE_CROSS_SHIPMENT",
                "Shipment package ve shipment item aynı shipment'a ait olmalıdır."));
        }

        if (routeStopId is not null && (routeStopShipmentId is null || routeStopShipmentId != shipmentId))
        {
            throw new DomainException(new(
                "SHIPMENT_PACKAGE_ROUTE_STOP_OWNERSHIP",
                "Route stop aynı shipment'a ait olmalıdır."));
        }
    }

    public void Allocate()
    {
        if (Status != ShipmentPackageStatus.Available)
        {
            throw new DomainException(new(
                "SHIPMENT_PACKAGE_INVALID_TRANSITION",
                $"{Status} durumundaki paket allocation için uygun değildir."));
        }

        Status = ShipmentPackageStatus.Allocated;
    }

    public void Load()
    {
        if (Status is not (ShipmentPackageStatus.Available or ShipmentPackageStatus.Allocated))
        {
            throw new DomainException(new(
                "SHIPMENT_PACKAGE_INVALID_TRANSITION",
                $"{Status} durumundaki paket yüklenemez."));
        }

        Status = ShipmentPackageStatus.Loaded;
    }

    public void Cancel()
    {
        if (Status is ShipmentPackageStatus.Loaded or ShipmentPackageStatus.Cancelled)
        {
            throw new DomainException(new(
                "SHIPMENT_PACKAGE_INVALID_TRANSITION",
                $"{Status} durumundaki paket iptal edilemez."));
        }

        Status = ShipmentPackageStatus.Cancelled;
    }

    public static ShipmentPackage Rehydrate(
        Guid id,
        DateTimeOffset createdAt,
        Guid shipmentId,
        Guid shipmentItemId,
        Guid? packagingId,
        Guid? routeStopId,
        ShipmentPackageType packageType,
        decimal packageCount,
        decimal quantityBasePerPackage,
        decimal? enteredQuantity,
        string? packageCode,
        string packagingSnapshot,
        string physicalSnapshot,
        bool splitAllowed,
        ShipmentPackageStatus status)
    {
        var package = Create(
            id,
            createdAt,
            shipmentId,
            shipmentItemId,
            packagingId,
            routeStopId,
            packageType,
            packageCount,
            quantityBasePerPackage,
            enteredQuantity,
            packageCode,
            packagingSnapshot,
            physicalSnapshot,
            splitAllowed);
        package.Status = status;
        return package;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
