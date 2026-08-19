using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FactoryErp.Infrastructure.Persistence.Entities;

namespace FactoryErp.Infrastructure.Persistence.Configurations;

public sealed class ShipmentRecordConfiguration : IEntityTypeConfiguration<ShipmentRecord>
{
    public void Configure(EntityTypeBuilder<ShipmentRecord> builder)
    {
        builder.ToTable("shipments", table =>
        {
            table.HasCheckConstraint(
                "ck_shipments_status",
                "status in ('Preparing', 'Ready', 'Loaded', 'InTransit', 'PartiallyDelivered', 'Delivered', 'Exception', 'Returned')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.DeliveryNoteId).HasColumnName("delivery_note_id");
        builder.Property(x => x.CustomerId).HasColumnName("customer_id");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(40).IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasColumnType("bigint").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasDefaultValue(1L);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.HasIndex(x => x.DeliveryNoteId).IsUnique();
        builder.HasIndex(x => new { x.Status, x.CreatedAt });
        builder.HasOne(x => x.DeliveryNote).WithMany().HasForeignKey(x => x.DeliveryNoteId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ShipmentItemRecordConfiguration : IEntityTypeConfiguration<ShipmentItemRecord>
{
    public void Configure(EntityTypeBuilder<ShipmentItemRecord> builder)
    {
        builder.ToTable("shipment_items", table =>
        {
            table.HasCheckConstraint("ck_shipment_items_quantity_positive", "quantity_base > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ShipmentId).HasColumnName("shipment_id");
        builder.Property(x => x.DeliveryNoteItemId).HasColumnName("delivery_note_item_id");
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.QuantityBase).HasColumnName("quantity_base").HasPrecision(18, 6);
        builder.Property(x => x.PackagingSnapshot).HasColumnName("packaging_snapshot").HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => new { x.ShipmentId, x.DeliveryNoteItemId }).IsUnique();
        builder.HasOne(x => x.Shipment).WithMany(x => x.Items).HasForeignKey(x => x.ShipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DeliveryNoteItem).WithMany().HasForeignKey(x => x.DeliveryNoteItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductRecord>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class VehicleTypeRecordConfiguration : IEntityTypeConfiguration<VehicleTypeRecord>
{
    public void Configure(EntityTypeBuilder<VehicleTypeRecord> builder)
    {
        builder.ToTable("vehicle_types");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(80).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(160).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class VehicleCapacityRecordConfiguration : IEntityTypeConfiguration<VehicleCapacityRecord>
{
    public void Configure(EntityTypeBuilder<VehicleCapacityRecord> builder)
    {
        builder.ToTable("vehicle_capacities", table =>
        {
            table.HasCheckConstraint("ck_vehicle_capacities_effective_range", "effective_to is null or effective_to > effective_from");
            table.HasCheckConstraint("ck_vehicle_capacities_weight", "max_gross_weight > 0 and tare_weight >= 0 and tare_weight < max_gross_weight");
            table.HasCheckConstraint("ck_vehicle_capacities_limits", "max_usable_volume > 0 and max_pallet_count > 0 and max_load_height > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.VehicleTypeId).HasColumnName("vehicle_type_id");
        builder.Property(x => x.EffectiveFrom).HasColumnName("effective_from").HasColumnType("timestamptz");
        builder.Property(x => x.EffectiveTo).HasColumnName("effective_to").HasColumnType("timestamptz");
        builder.Property(x => x.MaxGrossWeight).HasColumnName("max_gross_weight").HasPrecision(18, 6);
        builder.Property(x => x.TareWeight).HasColumnName("tare_weight").HasPrecision(18, 6);
        builder.Property(x => x.MaxUsableVolume).HasColumnName("max_usable_volume").HasPrecision(18, 6);
        builder.Property(x => x.MaxPalletCount).HasColumnName("max_pallet_count");
        builder.Property(x => x.MaxLoadHeight).HasColumnName("max_load_height").HasPrecision(18, 6);
        builder.Property(x => x.CapacityPolicySnapshot).HasColumnName("capacity_policy_snapshot").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasColumnType("bigint").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasDefaultValue(1L);
        builder.HasIndex(x => new { x.VehicleTypeId, x.EffectiveFrom }).IsUnique();
        builder.HasIndex(x => new { x.VehicleTypeId, x.EffectiveFrom, x.EffectiveTo });
        builder.HasOne(x => x.VehicleType).WithMany(x => x.Capacities).HasForeignKey(x => x.VehicleTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class VehicleRecordConfiguration : IEntityTypeConfiguration<VehicleRecord>
{
    public void Configure(EntityTypeBuilder<VehicleRecord> builder)
    {
        builder.ToTable("vehicles", table =>
        {
            table.HasCheckConstraint(
                "ck_vehicles_status",
                "status in ('Available', 'Assigned', 'Loading', 'InTransit', 'Maintenance', 'OutOfService')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.VehicleTypeId).HasColumnName("vehicle_type_id");
        builder.Property(x => x.PlateNumber).HasColumnName("plate_number").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.MaintenanceUntil).HasColumnName("maintenance_until").HasColumnType("timestamptz");
        builder.Property(x => x.CurrentRoutePlanId).HasColumnName("current_route_plan_id");
        builder.Property(x => x.LastKnownLocationText).HasColumnName("last_known_location_text").HasMaxLength(240);
        builder.Property(x => x.LastStatusAt).HasColumnName("last_status_at").HasColumnType("timestamptz");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasColumnType("bigint").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasDefaultValue(1L);
        builder.HasIndex(x => x.PlateNumber).IsUnique();
        builder.HasIndex(x => new { x.Status, x.MaintenanceUntil });
        builder.HasOne(x => x.VehicleType).WithMany(x => x.Vehicles).HasForeignKey(x => x.VehicleTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DriverRecordConfiguration : IEntityTypeConfiguration<DriverRecord>
{
    public void Configure(EntityTypeBuilder<DriverRecord> builder)
    {
        builder.ToTable("drivers", table =>
        {
            table.HasCheckConstraint(
                "ck_drivers_status",
                "status in ('Active', 'Suspended', 'Inactive')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.EmployeeId).HasColumnName("employee_id");
        builder.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(160).IsRequired();
        builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(40);
        builder.Property(x => x.LicenseNumber).HasColumnName("license_number").HasMaxLength(80).IsRequired();
        builder.Property(x => x.LicenseExpiry).HasColumnName("license_expiry").HasColumnType("date");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasColumnType("bigint").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasDefaultValue(1L);
        builder.HasIndex(x => x.LicenseNumber).IsUnique();
        builder.HasIndex(x => new { x.IsActive, x.LicenseExpiry });
    }
}

public sealed class RoutePlanRecordConfiguration : IEntityTypeConfiguration<RoutePlanRecord>
{
    public void Configure(EntityTypeBuilder<RoutePlanRecord> builder)
    {
        builder.ToTable("route_plans", table =>
        {
            table.HasCheckConstraint(
                "ck_route_plans_status",
                "status in ('Draft', 'Planned', 'Locked', 'InProgress', 'Completed', 'Exception', 'Superseded')");
            table.HasCheckConstraint(
                "ck_route_plans_valid_time",
                "planned_start_at is null or planned_end_at is null or planned_end_at > planned_start_at");
            table.HasCheckConstraint("ck_route_plans_version_positive", "version > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ShipmentId).HasColumnName("shipment_id");
        builder.Property(x => x.VehicleId).HasColumnName("vehicle_id");
        builder.Property(x => x.DriverId).HasColumnName("driver_id");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Version).HasColumnName("version");
        builder.Property(x => x.ReplannedFromId).HasColumnName("replanned_from_id");
        builder.Property(x => x.PlannedStartAt).HasColumnName("planned_start_at").HasColumnType("timestamptz");
        builder.Property(x => x.PlannedEndAt).HasColumnName("planned_end_at").HasColumnType("timestamptz");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasColumnType("bigint").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasDefaultValue(1L);
        builder.HasIndex(x => new { x.ShipmentId, x.Version }).IsUnique();
        builder.HasIndex(x => new { x.VehicleId, x.PlannedStartAt, x.PlannedEndAt }).HasFilter("vehicle_id is not null");
        builder.HasIndex(x => new { x.DriverId, x.PlannedStartAt, x.PlannedEndAt }).HasFilter("driver_id is not null");
        builder.HasOne(x => x.Shipment).WithMany(x => x.RoutePlans).HasForeignKey(x => x.ShipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Vehicle).WithMany(x => x.RoutePlans).HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Driver).WithMany(x => x.RoutePlans).HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ReplannedFrom).WithMany(x => x.Replans).HasForeignKey(x => x.ReplannedFromId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class RouteStopRecordConfiguration : IEntityTypeConfiguration<RouteStopRecord>
{
    public void Configure(EntityTypeBuilder<RouteStopRecord> builder)
    {
        builder.ToTable("route_stops", table =>
        {
            table.HasCheckConstraint("ck_route_stops_sequence_positive", "sequence_no > 0");
            table.HasCheckConstraint(
                "ck_route_stops_status",
                "status in ('Pending', 'Arrived', 'Departed', 'InProgress', 'Delivered', 'Partial', 'Failed', 'Skipped')");
            table.HasCheckConstraint(
                "ck_route_stops_execution_time_order",
                "actual_departure_at is null or actual_arrival_at is null or actual_departure_at >= actual_arrival_at");
            table.HasCheckConstraint(
                "ck_route_stops_skipped_reason",
                "status <> 'Skipped' or nullif(btrim(exception_reason), '') is not null");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.RoutePlanId).HasColumnName("route_plan_id");
        builder.Property(x => x.SequenceNo).HasColumnName("sequence_no");
        builder.Property(x => x.CustomerId).HasColumnName("customer_id");
        builder.Property(x => x.AddressId).HasColumnName("address_id");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.PlannedArrivalAt).HasColumnName("planned_arrival_at").HasColumnType("timestamptz");
        builder.Property(x => x.ActualArrivalAt).HasColumnName("actual_arrival_at").HasColumnType("timestamptz");
        builder.Property(x => x.ActualDepartureAt).HasColumnName("actual_departure_at").HasColumnType("timestamptz");
        builder.Property(x => x.SkippedAt).HasColumnName("skipped_at").HasColumnType("timestamptz");
        builder.Property(x => x.DeliveredAt).HasColumnName("delivered_at").HasColumnType("timestamptz");
        builder.Property(x => x.ProofRecipient).HasColumnName("proof_recipient").HasMaxLength(160);
        builder.Property(x => x.ProofNote).HasColumnName("proof_note").HasMaxLength(500);
        builder.Property(x => x.ExceptionReason).HasColumnName("exception_reason").HasColumnType("text");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasColumnType("bigint").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasDefaultValue(1L);
        builder.HasIndex(x => new { x.RoutePlanId, x.SequenceNo }).IsUnique();
        builder.HasIndex(x => new { x.CustomerId, x.Status, x.PlannedArrivalAt });
        builder.HasOne(x => x.RoutePlan).WithMany(x => x.Stops).HasForeignKey(x => x.RoutePlanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Address).WithMany().HasForeignKey(x => x.AddressId).OnDelete(DeleteBehavior.Restrict);
    }
}
