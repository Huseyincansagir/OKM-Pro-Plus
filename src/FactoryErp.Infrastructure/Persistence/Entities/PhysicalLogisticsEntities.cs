namespace FactoryErp.Infrastructure.Persistence.Entities;

public sealed class ProductPhysicalProfileRecord
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public decimal LengthMm { get; set; }
    public decimal WidthMm { get; set; }
    public decimal HeightMm { get; set; }
    public decimal NetWeightKg { get; set; }
    public decimal? VolumeM3 { get; set; }
    public bool IsStackable { get; set; }
    public int? MaxStackCount { get; set; }
    public decimal? MaxLoadAboveKg { get; set; }
    public bool KeepUpright { get; set; }
    public bool IsFragile { get; set; }
    public string? CompatibilityGroup { get; set; }
    public string IncompatibleGroups { get; set; } = "[]";
    public string AllowedOrientations { get; set; } = "[\"LWH\", \"WLH\"]";
    public string PhysicalPolicySnapshot { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long RowVersion { get; set; }

    public ProductRecord Product { get; set; } = null!;
}

public sealed class PackagingPhysicalProfileRecord
{
    public Guid Id { get; set; }
    public Guid PackagingId { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public decimal UnitsPerPackage { get; set; }
    public decimal LengthMm { get; set; }
    public decimal WidthMm { get; set; }
    public decimal HeightMm { get; set; }
    public decimal? NetWeightKg { get; set; }
    public decimal TareWeightKg { get; set; }
    public decimal? GrossWeightKg { get; set; }
    public decimal? VolumeM3 { get; set; }
    public bool IsStackable { get; set; }
    public int? MaxStackCount { get; set; }
    public decimal? MaxLoadAboveKg { get; set; }
    public bool KeepUpright { get; set; }
    public bool IsFragile { get; set; }
    public string? CompatibilityGroup { get; set; }
    public string IncompatibleGroups { get; set; } = "[]";
    public string AllowedOrientations { get; set; } = "[\"LWH\", \"WLH\"]";
    public string PhysicalPolicySnapshot { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long RowVersion { get; set; }

    public ProductPackagingRecord Packaging { get; set; } = null!;
}

public sealed class PalletTypeRecord
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal LengthMm { get; set; }
    public decimal WidthMm { get; set; }
    public decimal HeightMm { get; set; }
    public decimal TareWeightKg { get; set; }
    public decimal? MaxGrossWeightKg { get; set; }
    public decimal? MaxPayloadKg { get; set; }
    public decimal? MaxLoadHeightMm { get; set; }
    public int? MaxStackCount { get; set; }
    public bool IsStackable { get; set; }
    public bool IsActive { get; set; }
    public string PolicySnapshot { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long RowVersion { get; set; }

    public ICollection<VehicleCapacityPalletTypeRecord> VehicleCapacityPalletTypes { get; } = new List<VehicleCapacityPalletTypeRecord>();
}

public sealed class VehicleCapacityPalletTypeRecord
{
    public Guid VehicleCapacityId { get; set; }
    public Guid PalletTypeId { get; set; }

    public VehicleCapacityRecord VehicleCapacity { get; set; } = null!;
    public PalletTypeRecord PalletType { get; set; } = null!;
}

public sealed class VehicleCapacityZoneRecord
{
    public Guid Id { get; set; }
    public Guid VehicleCapacityId { get; set; }
    public string ZoneCode { get; set; } = string.Empty;
    public decimal LengthMm { get; set; }
    public decimal WidthMm { get; set; }
    public decimal? MaxLoadKg { get; set; }
    public string? AccessSide { get; set; }
    public int SequenceNo { get; set; }

    public VehicleCapacityRecord VehicleCapacity { get; set; } = null!;
}
