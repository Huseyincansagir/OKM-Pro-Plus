using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FactoryErp.Infrastructure.Persistence.Configurations;

public sealed class ProductPhysicalProfileRecordConfiguration : IEntityTypeConfiguration<ProductPhysicalProfileRecord>
{
    public void Configure(EntityTypeBuilder<ProductPhysicalProfileRecord> builder)
    {
        builder.ToTable("product_physical_profiles", table =>
        {
            table.HasCheckConstraint("ck_product_physical_dimensions_positive", "length_mm > 0 and width_mm > 0 and height_mm > 0");
            table.HasCheckConstraint("ck_product_physical_weight_nonnegative", "net_weight_kg >= 0");
            table.HasCheckConstraint("ck_product_physical_volume_positive", "volume_m3 is null or volume_m3 > 0");
            table.HasCheckConstraint("ck_product_physical_effective_range", "effective_to is null or effective_to > effective_from");
            table.HasCheckConstraint("ck_product_physical_stack_rules", "max_stack_count is null or max_stack_count >= 1");
            table.HasCheckConstraint("ck_product_physical_load_above", "max_load_above_kg is null or max_load_above_kg >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.EffectiveFrom).HasColumnName("effective_from").HasColumnType("timestamptz");
        builder.Property(x => x.EffectiveTo).HasColumnName("effective_to").HasColumnType("timestamptz");
        builder.Property(x => x.LengthMm).HasColumnName("length_mm").HasPrecision(18, 6);
        builder.Property(x => x.WidthMm).HasColumnName("width_mm").HasPrecision(18, 6);
        builder.Property(x => x.HeightMm).HasColumnName("height_mm").HasPrecision(18, 6);
        builder.Property(x => x.NetWeightKg).HasColumnName("net_weight_kg").HasPrecision(18, 6);
        builder.Property(x => x.VolumeM3).HasColumnName("volume_m3").HasPrecision(18, 6);
        builder.Property(x => x.IsStackable).HasColumnName("is_stackable");
        builder.Property(x => x.MaxStackCount).HasColumnName("max_stack_count");
        builder.Property(x => x.MaxLoadAboveKg).HasColumnName("max_load_above_kg").HasPrecision(18, 6);
        builder.Property(x => x.KeepUpright).HasColumnName("keep_upright");
        builder.Property(x => x.IsFragile).HasColumnName("is_fragile");
        builder.Property(x => x.CompatibilityGroup).HasColumnName("compatibility_group").HasMaxLength(80);
        builder.Property(x => x.IncompatibleGroups).HasColumnName("incompatible_groups").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.AllowedOrientations).HasColumnName("allowed_orientations").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.PhysicalPolicySnapshot).HasColumnName("physical_policy_snapshot").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasColumnType("bigint").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasDefaultValue(1L);
        builder.HasIndex(x => new { x.ProductId, x.EffectiveFrom }).IsUnique();
        builder.HasIndex(x => x.ProductId).HasDatabaseName("ix_product_physical_product");
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PackagingPhysicalProfileRecordConfiguration : IEntityTypeConfiguration<PackagingPhysicalProfileRecord>
{
    public void Configure(EntityTypeBuilder<PackagingPhysicalProfileRecord> builder)
    {
        builder.ToTable("packaging_physical_profiles", table =>
        {
            table.HasCheckConstraint("ck_packaging_physical_units_positive", "units_per_package > 0");
            table.HasCheckConstraint("ck_packaging_physical_dimensions_positive", "length_mm > 0 and width_mm > 0 and height_mm > 0");
            table.HasCheckConstraint("ck_packaging_physical_weights_nonnegative", "(net_weight_kg is null or net_weight_kg >= 0) and tare_weight_kg >= 0 and (gross_weight_kg is null or gross_weight_kg >= 0)");
            table.HasCheckConstraint("ck_packaging_physical_gross_consistent", "gross_weight_kg is null or net_weight_kg is null or gross_weight_kg >= net_weight_kg + tare_weight_kg");
            table.HasCheckConstraint("ck_packaging_physical_effective_range", "effective_to is null or effective_to > effective_from");
            table.HasCheckConstraint("ck_packaging_physical_stack_rules", "max_stack_count is null or max_stack_count >= 1");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.PackagingId).HasColumnName("packaging_id");
        builder.Property(x => x.EffectiveFrom).HasColumnName("effective_from").HasColumnType("timestamptz");
        builder.Property(x => x.EffectiveTo).HasColumnName("effective_to").HasColumnType("timestamptz");
        builder.Property(x => x.UnitsPerPackage).HasColumnName("units_per_package").HasPrecision(18, 6);
        builder.Property(x => x.LengthMm).HasColumnName("length_mm").HasPrecision(18, 6);
        builder.Property(x => x.WidthMm).HasColumnName("width_mm").HasPrecision(18, 6);
        builder.Property(x => x.HeightMm).HasColumnName("height_mm").HasPrecision(18, 6);
        builder.Property(x => x.NetWeightKg).HasColumnName("net_weight_kg").HasPrecision(18, 6);
        builder.Property(x => x.TareWeightKg).HasColumnName("tare_weight_kg").HasPrecision(18, 6);
        builder.Property(x => x.GrossWeightKg).HasColumnName("gross_weight_kg").HasPrecision(18, 6);
        builder.Property(x => x.VolumeM3).HasColumnName("volume_m3").HasPrecision(18, 6);
        builder.Property(x => x.MaxLoadAboveKg).HasColumnName("max_load_above_kg").HasPrecision(18, 6);
        builder.Property(x => x.IsStackable).HasColumnName("is_stackable");
        builder.Property(x => x.MaxStackCount).HasColumnName("max_stack_count");
        builder.Property(x => x.KeepUpright).HasColumnName("keep_upright");
        builder.Property(x => x.IsFragile).HasColumnName("is_fragile");
        builder.Property(x => x.CompatibilityGroup).HasColumnName("compatibility_group").HasMaxLength(80);
        builder.Property(x => x.IncompatibleGroups).HasColumnName("incompatible_groups").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.AllowedOrientations).HasColumnName("allowed_orientations").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.PhysicalPolicySnapshot).HasColumnName("physical_policy_snapshot").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasColumnType("bigint").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasDefaultValue(1L);
        builder.HasIndex(x => new { x.PackagingId, x.EffectiveFrom }).IsUnique();
        builder.HasIndex(x => x.PackagingId).HasDatabaseName("ix_packaging_physical_packaging");
        builder.HasOne(x => x.Packaging).WithMany().HasForeignKey(x => x.PackagingId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PalletTypeRecordConfiguration : IEntityTypeConfiguration<PalletTypeRecord>
{
    public void Configure(EntityTypeBuilder<PalletTypeRecord> builder)
    {
        builder.ToTable("pallet_types", table =>
        {
            table.HasCheckConstraint("ck_pallet_dimensions_positive", "length_mm > 0 and width_mm > 0 and height_mm > 0");
            table.HasCheckConstraint("ck_pallet_weights_nonnegative", "tare_weight_kg >= 0 and (max_gross_weight_kg is null or max_gross_weight_kg >= 0) and (max_payload_kg is null or max_payload_kg >= 0)");
            table.HasCheckConstraint("ck_pallet_payload_not_over_gross", "max_payload_kg is null or max_gross_weight_kg is null or max_payload_kg <= max_gross_weight_kg");
            table.HasCheckConstraint("ck_pallet_stack_rules", "max_stack_count is null or max_stack_count >= 1");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(80).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(160).IsRequired();
        builder.Property(x => x.LengthMm).HasColumnName("length_mm").HasPrecision(18, 6);
        builder.Property(x => x.WidthMm).HasColumnName("width_mm").HasPrecision(18, 6);
        builder.Property(x => x.HeightMm).HasColumnName("height_mm").HasPrecision(18, 6);
        builder.Property(x => x.TareWeightKg).HasColumnName("tare_weight_kg").HasPrecision(18, 6);
        builder.Property(x => x.MaxGrossWeightKg).HasColumnName("max_gross_weight_kg").HasPrecision(18, 6);
        builder.Property(x => x.MaxPayloadKg).HasColumnName("max_payload_kg").HasPrecision(18, 6);
        builder.Property(x => x.MaxLoadHeightMm).HasColumnName("max_load_height_mm").HasPrecision(18, 6);
        builder.Property(x => x.MaxStackCount).HasColumnName("max_stack_count");
        builder.Property(x => x.IsStackable).HasColumnName("is_stackable");
        builder.Property(x => x.IsActive).HasColumnName("is_active");
        builder.Property(x => x.PolicySnapshot).HasColumnName("policy_snapshot").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasColumnType("bigint").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasDefaultValue(1L);
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class VehicleCapacityPalletTypeRecordConfiguration : IEntityTypeConfiguration<VehicleCapacityPalletTypeRecord>
{
    public void Configure(EntityTypeBuilder<VehicleCapacityPalletTypeRecord> builder)
    {
        builder.ToTable("vehicle_capacity_pallet_types");
        builder.HasKey(x => new { x.VehicleCapacityId, x.PalletTypeId });
        builder.Property(x => x.VehicleCapacityId).HasColumnName("vehicle_capacity_id");
        builder.Property(x => x.PalletTypeId).HasColumnName("pallet_type_id");
        builder.HasOne(x => x.VehicleCapacity).WithMany(x => x.PalletTypes).HasForeignKey(x => x.VehicleCapacityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PalletType).WithMany(x => x.VehicleCapacityPalletTypes).HasForeignKey(x => x.PalletTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class VehicleCapacityZoneRecordConfiguration : IEntityTypeConfiguration<VehicleCapacityZoneRecord>
{
    public void Configure(EntityTypeBuilder<VehicleCapacityZoneRecord> builder)
    {
        builder.ToTable("vehicle_capacity_zones", table =>
        {
            table.HasCheckConstraint("ck_vehicle_capacity_zone_dimensions_positive", "length_mm > 0 and width_mm > 0");
            table.HasCheckConstraint("ck_vehicle_capacity_zone_load_nonnegative", "max_load_kg is null or max_load_kg >= 0");
            table.HasCheckConstraint("ck_vehicle_capacity_zone_sequence_positive", "sequence_no >= 1");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.VehicleCapacityId).HasColumnName("vehicle_capacity_id");
        builder.Property(x => x.ZoneCode).HasColumnName("zone_code").HasMaxLength(80).IsRequired();
        builder.Property(x => x.LengthMm).HasColumnName("length_mm").HasPrecision(18, 6);
        builder.Property(x => x.WidthMm).HasColumnName("width_mm").HasPrecision(18, 6);
        builder.Property(x => x.MaxLoadKg).HasColumnName("max_load_kg").HasPrecision(18, 6);
        builder.Property(x => x.AccessSide).HasColumnName("access_side").HasMaxLength(30);
        builder.Property(x => x.SequenceNo).HasColumnName("sequence_no");
        builder.HasIndex(x => new { x.VehicleCapacityId, x.ZoneCode }).IsUnique();
        builder.HasIndex(x => new { x.VehicleCapacityId, x.SequenceNo }).IsUnique();
        builder.HasOne(x => x.VehicleCapacity).WithMany(x => x.Zones).HasForeignKey(x => x.VehicleCapacityId).OnDelete(DeleteBehavior.Restrict);
    }
}
