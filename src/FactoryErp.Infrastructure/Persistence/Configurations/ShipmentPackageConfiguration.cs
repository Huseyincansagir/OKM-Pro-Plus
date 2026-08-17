using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FactoryErp.Infrastructure.Persistence.Configurations;

public sealed class ShipmentPackageRecordConfiguration : IEntityTypeConfiguration<ShipmentPackageRecord>
{
    public void Configure(EntityTypeBuilder<ShipmentPackageRecord> builder)
    {
        builder.ToTable("shipment_packages", table =>
        {
            table.HasCheckConstraint(
                "ck_shipment_packages_type",
                "package_type in ('Case', 'Package', 'Pallet', 'Loose')");
            table.HasCheckConstraint(
                "ck_shipment_packages_status",
                "status in ('Available', 'Allocated', 'Loaded', 'Cancelled')");
            table.HasCheckConstraint(
                "ck_shipment_packages_quantity_positive",
                "package_count > 0 and quantity_base_per_package > 0 and quantity_base > 0");
            table.HasCheckConstraint(
                "ck_shipment_packages_quantity_formula",
                "quantity_base = package_count * quantity_base_per_package");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ShipmentId).HasColumnName("shipment_id").IsRequired();
        builder.Property(x => x.ShipmentItemId).HasColumnName("shipment_item_id").IsRequired();
        builder.Property(x => x.PackagingId).HasColumnName("packaging_id");
        builder.Property(x => x.RouteStopId).HasColumnName("route_stop_id");
        builder.Property(x => x.PackageType).HasColumnName("package_type").HasMaxLength(30).IsRequired();
        builder.Property(x => x.PackageCount).HasColumnName("package_count").HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.QuantityBasePerPackage).HasColumnName("quantity_base_per_package").HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.QuantityBase).HasColumnName("quantity_base").HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.EnteredQuantity).HasColumnName("entered_quantity").HasPrecision(18, 6);
        builder.Property(x => x.PackageCode).HasColumnName("package_code").HasMaxLength(120);
        builder.Property(x => x.PackagingSnapshot).HasColumnName("packaging_snapshot").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.PhysicalSnapshot).HasColumnName("physical_snapshot").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.SplitAllowed).HasColumnName("split_allowed").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.RowVersion)
            .HasColumnName("row_version")
            .HasColumnType("bigint")
            .IsConcurrencyToken()
            .ValueGeneratedOnAddOrUpdate()
            .HasDefaultValue(1L);

        builder.HasIndex(x => new { x.ShipmentId, x.Status }).HasDatabaseName("ix_shipment_packages_shipment_status");
        builder.HasIndex(x => x.ShipmentItemId).HasDatabaseName("ix_shipment_packages_item");
        builder.HasIndex(x => x.RouteStopId).HasDatabaseName("ix_shipment_packages_stop");
        builder.HasIndex(x => x.PackageCode)
            .IsUnique()
            .HasDatabaseName("ux_shipment_packages_active_code")
            .HasFilter("package_code is not null and status <> 'Cancelled'");

        builder.HasOne(x => x.Shipment)
            .WithMany()
            .HasForeignKey(x => x.ShipmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ShipmentItem)
            .WithMany()
            .HasForeignKey(x => x.ShipmentItemId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Packaging)
            .WithMany()
            .HasForeignKey(x => x.PackagingId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RouteStop)
            .WithMany()
            .HasForeignKey(x => x.RouteStopId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
