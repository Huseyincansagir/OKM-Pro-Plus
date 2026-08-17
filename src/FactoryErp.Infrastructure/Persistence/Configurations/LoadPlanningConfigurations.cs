using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FactoryErp.Infrastructure.Persistence.Configurations;

public sealed class LoadPlanRecordConfiguration : IEntityTypeConfiguration<LoadPlanRecord>
{
    public void Configure(EntityTypeBuilder<LoadPlanRecord> builder)
    {
        builder.ToTable("load_plans", table =>
        {
            table.HasCheckConstraint(
                "ck_load_plans_status",
                "status in ('Draft', 'Proposed', 'Validating', 'Valid', 'NeedsReview', 'Locked', 'Superseded')");
            table.HasCheckConstraint(
                "ck_load_plans_feasibility",
                "feasibility_status in ('Infeasible', 'FeasibleWithWarnings', 'Feasible')");
            table.HasCheckConstraint(
                "ck_load_plans_version_positive",
                "version > 0 and route_plan_version > 0");
            table.HasCheckConstraint(
                "ck_load_plans_approval_pair",
                "(approved_by is null and approved_at is null) or (approved_by is not null and approved_at is not null)");
            table.HasCheckConstraint(
                "ck_load_plans_lock_pair",
                "(locked_by is null and locked_at is null) or (locked_by is not null and locked_at is not null)");
            table.HasCheckConstraint(
                "ck_load_plans_locked_requirements",
                "status <> 'Locked' or (vehicle_id is not null and vehicle_capacity_id is not null and input_snapshot_hash is not null and locked_by is not null)");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ShipmentId).HasColumnName("shipment_id").IsRequired();
        builder.Property(x => x.RoutePlanId).HasColumnName("route_plan_id").IsRequired();
        builder.Property(x => x.RoutePlanVersion).HasColumnName("route_plan_version").IsRequired();
        builder.Property(x => x.Version).HasColumnName("version").IsRequired();
        builder.Property(x => x.ReplannedFromId).HasColumnName("replanned_from_id");
        builder.Property(x => x.VehicleId).HasColumnName("vehicle_id");
        builder.Property(x => x.VehicleCapacityId).HasColumnName("vehicle_capacity_id");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.FeasibilityStatus).HasColumnName("feasibility_status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.AlgorithmName).HasColumnName("algorithm_name").HasMaxLength(100);
        builder.Property(x => x.AlgorithmVersion).HasColumnName("algorithm_version").HasMaxLength(40);
        builder.Property(x => x.ParameterSet).HasColumnName("parameter_set").HasMaxLength(120);
        builder.Property(x => x.InputSnapshotHash).HasColumnName("input_snapshot_hash").HasMaxLength(128);
        builder.Property(x => x.CapacitySnapshot).HasColumnName("capacity_snapshot").HasColumnType("jsonb");
        builder.Property(x => x.UtilizationSnapshot).HasColumnName("utilization_snapshot").HasColumnType("jsonb");
        builder.Property(x => x.ValidationSummary).HasColumnName("validation_summary").HasColumnType("jsonb").HasDefaultValue("{}").IsRequired();
        builder.Property(x => x.ApprovedBy).HasColumnName("approved_by");
        builder.Property(x => x.ApprovedAt).HasColumnName("approved_at").HasColumnType("timestamptz");
        builder.Property(x => x.LockedBy).HasColumnName("locked_by");
        builder.Property(x => x.LockedAt).HasColumnName("locked_at").HasColumnType("timestamptz");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.RowVersion)
            .HasColumnName("row_version")
            .HasColumnType("bigint")
            .IsConcurrencyToken()
            .ValueGeneratedOnAddOrUpdate()
            .HasDefaultValue(1L);

        builder.HasIndex(x => new { x.ShipmentId, x.Version })
            .IsUnique()
            .HasDatabaseName("ux_load_plans_shipment_version");
        builder.HasIndex(x => new { x.RoutePlanId, x.RoutePlanVersion, x.Status })
            .HasDatabaseName("ix_load_plans_route");
        builder.HasIndex(x => new { x.VehicleId, x.Status })
            .HasDatabaseName("ix_load_plans_vehicle")
            .HasFilter("vehicle_id is not null");

        builder.HasOne(x => x.Shipment)
            .WithMany()
            .HasForeignKey(x => x.ShipmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RoutePlan)
            .WithMany()
            .HasForeignKey(x => x.RoutePlanId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ReplannedFrom)
            .WithMany(x => x.Replans)
            .HasForeignKey(x => x.ReplannedFromId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VehicleRecord>()
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VehicleCapacityRecord>()
            .WithMany()
            .HasForeignKey(x => x.VehicleCapacityId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserRecord>()
            .WithMany()
            .HasForeignKey(x => x.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserRecord>()
            .WithMany()
            .HasForeignKey(x => x.LockedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class LoadUnitRecordConfiguration : IEntityTypeConfiguration<LoadUnitRecord>
{
    public void Configure(EntityTypeBuilder<LoadUnitRecord> builder)
    {
        builder.ToTable("load_units", table =>
        {
            table.HasCheckConstraint(
                "ck_load_units_type",
                "unit_type in ('Pallet', 'Cage', 'CartonGroup', 'Loose')");
            table.HasCheckConstraint(
                "ck_load_units_status",
                "status in ('Draft', 'Validated', 'Locked', 'Loaded', 'Cancelled')");
            table.HasCheckConstraint(
                "ck_load_units_dimensions",
                "length_mm > 0 and width_mm > 0 and height_mm > 0");
            table.HasCheckConstraint(
                "ck_load_units_weight",
                "gross_weight_kg >= tare_weight_kg and tare_weight_kg >= 0");
            table.HasCheckConstraint(
                "ck_load_units_volume",
                "volume_m3 > 0");
            table.HasCheckConstraint(
                "ck_load_units_priority",
                "unloading_priority > 0");
            table.HasCheckConstraint(
                "ck_load_units_stack_count",
                "max_stack_count is null or max_stack_count >= 1");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.LoadPlanId).HasColumnName("load_plan_id").IsRequired();
        builder.Property(x => x.UnitCode).HasColumnName("unit_code").HasMaxLength(120).IsRequired();
        builder.Property(x => x.UnitType).HasColumnName("unit_type").HasMaxLength(30).IsRequired();
        builder.Property(x => x.PalletTypeId).HasColumnName("pallet_type_id");
        builder.Property(x => x.IsMixed).HasColumnName("is_mixed").IsRequired();
        builder.Property(x => x.LengthMm).HasColumnName("length_mm").HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.WidthMm).HasColumnName("width_mm").HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.HeightMm).HasColumnName("height_mm").HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.TareWeightKg).HasColumnName("tare_weight_kg").HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.GrossWeightKg).HasColumnName("gross_weight_kg").HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.VolumeM3).HasColumnName("volume_m3").HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.MaxStackCount).HasColumnName("max_stack_count");
        builder.Property(x => x.PlacementZone).HasColumnName("placement_zone").HasMaxLength(80);
        builder.Property(x => x.UnloadingPriority).HasColumnName("unloading_priority").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.RowVersion)
            .HasColumnName("row_version")
            .HasColumnType("bigint")
            .IsConcurrencyToken()
            .ValueGeneratedOnAddOrUpdate()
            .HasDefaultValue(1L);

        builder.HasIndex(x => new { x.LoadPlanId, x.UnitCode })
            .IsUnique()
            .HasDatabaseName("ux_load_units_plan_code");
        builder.HasIndex(x => new { x.LoadPlanId, x.UnloadingPriority, x.UnitCode })
            .HasDatabaseName("ix_load_units_plan_priority");
        builder.HasOne(x => x.LoadPlan)
            .WithMany(x => x.LoadUnits)
            .HasForeignKey(x => x.LoadPlanId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PalletType)
            .WithMany()
            .HasForeignKey(x => x.PalletTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class LoadUnitItemRecordConfiguration : IEntityTypeConfiguration<LoadUnitItemRecord>
{
    public void Configure(EntityTypeBuilder<LoadUnitItemRecord> builder)
    {
        builder.ToTable("load_unit_items", table =>
        {
            table.HasCheckConstraint("ck_load_unit_items_quantity_positive", "quantity_base > 0");
            table.HasCheckConstraint("ck_load_unit_items_weight_non_negative", "gross_weight_kg >= 0");
            table.HasCheckConstraint("ck_load_unit_items_volume_non_negative", "volume_m3 >= 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.LoadUnitId).HasColumnName("load_unit_id").IsRequired();
        builder.Property(x => x.ShipmentPackageId).HasColumnName("shipment_package_id").IsRequired();
        builder.Property(x => x.ShipmentItemId).HasColumnName("shipment_item_id").IsRequired();
        builder.Property(x => x.QuantityBase).HasColumnName("quantity_base").HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.GrossWeightKg).HasColumnName("gross_weight_kg").HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.VolumeM3).HasColumnName("volume_m3").HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.AllocationSnapshot).HasColumnName("allocation_snapshot").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.RowVersion)
            .HasColumnName("row_version")
            .HasColumnType("bigint")
            .IsConcurrencyToken()
            .ValueGeneratedOnAddOrUpdate()
            .HasDefaultValue(1L);

        builder.HasIndex(x => x.LoadUnitId).HasDatabaseName("ix_load_unit_items_unit");
        builder.HasIndex(x => x.ShipmentPackageId).HasDatabaseName("ix_load_unit_items_package");
        builder.HasIndex(x => x.ShipmentPackageId)
            .IsUnique()
            .HasDatabaseName("ux_active_package_load_unit")
            .HasFilter("quantity_base > 0");
        builder.HasOne(x => x.LoadUnit)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.LoadUnitId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ShipmentPackage)
            .WithMany()
            .HasForeignKey(x => x.ShipmentPackageId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ShipmentItem)
            .WithMany()
            .HasForeignKey(x => x.ShipmentItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class LoadUnitStopAllocationRecordConfiguration : IEntityTypeConfiguration<LoadUnitStopAllocationRecord>
{
    public void Configure(EntityTypeBuilder<LoadUnitStopAllocationRecord> builder)
    {
        builder.ToTable("load_unit_stop_allocations", table =>
        {
            table.HasCheckConstraint("ck_load_unit_stop_allocations_quantity_positive", "quantity_base > 0");
            table.HasCheckConstraint("ck_load_unit_stop_allocations_sequence_positive", "sequence_no > 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.LoadUnitItemId).HasColumnName("load_unit_item_id").IsRequired();
        builder.Property(x => x.RouteStopId).HasColumnName("route_stop_id").IsRequired();
        builder.Property(x => x.QuantityBase).HasColumnName("quantity_base").HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.SequenceNo).HasColumnName("sequence_no").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasIndex(x => new { x.LoadUnitItemId, x.RouteStopId })
            .IsUnique()
            .HasDatabaseName("ux_load_unit_stop_allocation");
        builder.HasIndex(x => new { x.RouteStopId, x.SequenceNo })
            .HasDatabaseName("ix_load_unit_stop_route_order");
        builder.HasOne(x => x.LoadUnitItem)
            .WithMany(x => x.StopAllocations)
            .HasForeignKey(x => x.LoadUnitItemId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RouteStop)
            .WithMany()
            .HasForeignKey(x => x.RouteStopId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
