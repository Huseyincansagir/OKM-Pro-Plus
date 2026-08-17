namespace FactoryErp.Infrastructure.Persistence.Entities;

public sealed class ShipmentRecord
{
    public Guid Id { get; set; }
    public Guid DeliveryNoteId { get; set; }
    public Guid CustomerId { get; set; }
    public string Status { get; set; } = "Preparing";
    public long RowVersion { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public DeliveryNoteRecord DeliveryNote { get; set; } = null!;
    public ICollection<ShipmentItemRecord> Items { get; } = new List<ShipmentItemRecord>();
    public ICollection<RoutePlanRecord> RoutePlans { get; } = new List<RoutePlanRecord>();
}

public sealed class ShipmentItemRecord
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public Guid DeliveryNoteItemId { get; set; }
    public Guid ProductId { get; set; }
    public decimal QuantityBase { get; set; }
    public string PackagingSnapshot { get; set; } = "{}";

    public ShipmentRecord Shipment { get; set; } = null!;
    public DeliveryNoteItemRecord DeliveryNoteItem { get; set; } = null!;
}

public sealed class VehicleTypeRecord
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public ICollection<VehicleCapacityRecord> Capacities { get; } = new List<VehicleCapacityRecord>();
    public ICollection<VehicleRecord> Vehicles { get; } = new List<VehicleRecord>();
}

public sealed class VehicleCapacityRecord
{
    public Guid Id { get; set; }
    public Guid VehicleTypeId { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public decimal MaxGrossWeight { get; set; }
    public decimal TareWeight { get; set; }
    public decimal MaxUsableVolume { get; set; }
    public int MaxPalletCount { get; set; }
    public decimal MaxLoadHeight { get; set; }
    public string CapacityPolicySnapshot { get; set; } = "{}";
    public long RowVersion { get; set; }

    public VehicleTypeRecord VehicleType { get; set; } = null!;
    public ICollection<VehicleCapacityPalletTypeRecord> PalletTypes { get; } = new List<VehicleCapacityPalletTypeRecord>();
    public ICollection<VehicleCapacityZoneRecord> Zones { get; } = new List<VehicleCapacityZoneRecord>();
}

public sealed class VehicleRecord
{
    public Guid Id { get; set; }
    public Guid VehicleTypeId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "Available";
    public DateTimeOffset? MaintenanceUntil { get; set; }
    public Guid? CurrentRoutePlanId { get; set; }
    public string? LastKnownLocationText { get; set; }
    public DateTimeOffset LastStatusAt { get; set; }
    public long RowVersion { get; set; }

    public VehicleTypeRecord VehicleType { get; set; } = null!;
    public ICollection<RoutePlanRecord> RoutePlans { get; } = new List<RoutePlanRecord>();
}

public sealed class DriverRecord
{
    public Guid Id { get; set; }
    public Guid? EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public DateOnly LicenseExpiry { get; set; }
    public string Status { get; set; } = "Active";
    public bool IsActive { get; set; } = true;
    public long RowVersion { get; set; }

    public ICollection<RoutePlanRecord> RoutePlans { get; } = new List<RoutePlanRecord>();
}

public sealed class RoutePlanRecord
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid? DriverId { get; set; }
    public string Status { get; set; } = "Draft";
    public int Version { get; set; } = 1;
    public Guid? ReplannedFromId { get; set; }
    public DateTimeOffset? PlannedStartAt { get; set; }
    public DateTimeOffset? PlannedEndAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long RowVersion { get; set; }

    public ShipmentRecord Shipment { get; set; } = null!;
    public VehicleRecord? Vehicle { get; set; }
    public DriverRecord? Driver { get; set; }
    public RoutePlanRecord? ReplannedFrom { get; set; }
    public ICollection<RoutePlanRecord> Replans { get; } = new List<RoutePlanRecord>();
    public ICollection<RouteStopRecord> Stops { get; } = new List<RouteStopRecord>();
}

public sealed class RouteStopRecord
{
    public Guid Id { get; set; }
    public Guid RoutePlanId { get; set; }
    public int SequenceNo { get; set; }
    public Guid CustomerId { get; set; }
    public Guid AddressId { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTimeOffset? PlannedArrivalAt { get; set; }
    public DateTimeOffset? ActualArrivalAt { get; set; }
    public string? ExceptionReason { get; set; }
    public long RowVersion { get; set; }

    public RoutePlanRecord RoutePlan { get; set; } = null!;
    public CustomerRecord Customer { get; set; } = null!;
    public CustomerAddressRecord Address { get; set; } = null!;
}


public sealed class ShipmentPackageRecord
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public Guid ShipmentItemId { get; set; }
    public Guid? PackagingId { get; set; }
    public Guid? RouteStopId { get; set; }
    public string PackageType { get; set; } = string.Empty;
    public decimal PackageCount { get; set; }
    public decimal QuantityBasePerPackage { get; set; }
    public decimal QuantityBase { get; set; }
    public decimal? EnteredQuantity { get; set; }
    public string? PackageCode { get; set; }
    public string PackagingSnapshot { get; set; } = "{}";
    public string PhysicalSnapshot { get; set; } = "{}";
    public bool SplitAllowed { get; set; }
    public string Status { get; set; } = "Available";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long RowVersion { get; set; }

    public ShipmentRecord Shipment { get; set; } = null!;
    public ShipmentItemRecord ShipmentItem { get; set; } = null!;
    public ProductPackagingRecord? Packaging { get; set; }
    public RouteStopRecord? RouteStop { get; set; }
}
