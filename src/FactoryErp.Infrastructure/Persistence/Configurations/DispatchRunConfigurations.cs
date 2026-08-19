using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FactoryErp.Infrastructure.Persistence.Configurations;

public sealed class DispatchRunRecordConfiguration : IEntityTypeConfiguration<DispatchRunRecord>
{
    public void Configure(EntityTypeBuilder<DispatchRunRecord> builder)
    {
        builder.ToTable("dispatch_runs", table =>
        {
            table.HasCheckConstraint(
                "ck_dispatch_runs_status",
                "status in ('Prepared', 'Dispatched', 'InTransit', 'Completed', 'Cancelled')");
            table.HasCheckConstraint(
                "ck_dispatch_runs_completed_pair",
                "(status <> 'Completed' and completed_at is null and completed_by is null) or (status = 'Completed' and completed_at is not null and completed_by is not null)");
            table.HasCheckConstraint(
                "ck_dispatch_runs_cancelled_pair",
                "(status <> 'Cancelled' and cancelled_at is null and cancelled_by is null) or (status = 'Cancelled' and cancelled_at is not null and cancelled_by is not null and nullif(btrim(exception_reason), '') is not null)");
            table.HasCheckConstraint(
                "ck_dispatch_runs_departed_pair",
                "status in ('Prepared', 'Dispatched', 'Cancelled') or actual_departed_at is not null");
            table.HasCheckConstraint(
                "ck_dispatch_runs_time_order",
                "completed_at is null or actual_departed_at is null or completed_at >= actual_departed_at");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ShipmentId).HasColumnName("shipment_id").IsRequired();
        builder.Property(x => x.LoadPlanId).HasColumnName("load_plan_id").IsRequired();
        builder.Property(x => x.RoutePlanId).HasColumnName("route_plan_id").IsRequired();
        builder.Property(x => x.VehicleId).HasColumnName("vehicle_id").IsRequired();
        builder.Property(x => x.DriverId).HasColumnName("driver_id").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.PlannedDepartureAt).HasColumnName("planned_departure_at").HasColumnType("timestamptz");
        builder.Property(x => x.ActualDepartedAt).HasColumnName("actual_departed_at").HasColumnType("timestamptz");
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamptz");
        builder.Property(x => x.CancelledAt).HasColumnName("cancelled_at").HasColumnType("timestamptz");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(x => x.DispatchedBy).HasColumnName("dispatched_by");
        builder.Property(x => x.CompletedBy).HasColumnName("completed_by");
        builder.Property(x => x.CancelledBy).HasColumnName("cancelled_by");
        builder.Property(x => x.ExceptionReason).HasColumnName("exception_reason").HasColumnType("text");
        builder.Property(x => x.RowVersion)
            .HasColumnName("row_version")
            .HasColumnType("bigint")
            .IsConcurrencyToken()
            .ValueGeneratedNever()
            .HasDefaultValue(1L);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();

        builder.HasIndex(x => x.RoutePlanId)
            .HasDatabaseName("ux_dispatch_runs_active_route_plan")
            .IsUnique()
            .HasFilter("status in ('Prepared', 'Dispatched', 'InTransit')");
        builder.HasIndex(x => x.ShipmentId)
            .HasDatabaseName("ux_dispatch_runs_active_shipment")
            .IsUnique()
            .HasFilter("status in ('Prepared', 'Dispatched', 'InTransit')");
        builder.HasIndex(x => x.VehicleId)
            .HasDatabaseName("ux_dispatch_runs_active_vehicle")
            .IsUnique()
            .HasFilter("status in ('Prepared', 'Dispatched', 'InTransit')");
        builder.HasIndex(x => x.DriverId)
            .HasDatabaseName("ux_dispatch_runs_active_driver")
            .IsUnique()
            .HasFilter("status in ('Prepared', 'Dispatched', 'InTransit')");
        builder.HasIndex(x => new { x.Status, x.PlannedDepartureAt, x.Id })
            .HasDatabaseName("ix_dispatch_runs_board");
        builder.HasIndex(x => new { x.ShipmentId, x.CreatedAt, x.Id })
            .HasDatabaseName("ix_dispatch_runs_shipment_history");
        builder.HasIndex(x => new { x.VehicleId, x.CreatedAt, x.Id })
            .HasDatabaseName("ix_dispatch_runs_vehicle_history");

        builder.HasOne(x => x.Shipment)
            .WithMany()
            .HasForeignKey(x => x.ShipmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.LoadPlan)
            .WithMany()
            .HasForeignKey(x => x.LoadPlanId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RoutePlan)
            .WithMany()
            .HasForeignKey(x => x.RoutePlanId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserRecord>()
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserRecord>()
            .WithMany()
            .HasForeignKey(x => x.DispatchedBy)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserRecord>()
            .WithMany()
            .HasForeignKey(x => x.CompletedBy)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserRecord>()
            .WithMany()
            .HasForeignKey(x => x.CancelledBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class RouteExecutionEventRecordConfiguration : IEntityTypeConfiguration<RouteExecutionEventRecord>
{
    public void Configure(EntityTypeBuilder<RouteExecutionEventRecord> builder)
    {
        builder.ToTable("route_execution_events", table =>
        {
            table.HasCheckConstraint(
                "ck_route_execution_events_type",
                "event_type in ('Departed', 'ArrivedAtStop', 'DeliveredStop', 'DepartedStop', 'SkippedStop', 'RouteCompleted', 'Cancelled')");
            table.HasCheckConstraint(
                "ck_route_execution_events_sequence",
                "sequence_no > 0");
            table.HasCheckConstraint(
                "ck_route_execution_events_location",
                "(latitude is null and longitude is null) or (latitude between -90 and 90 and longitude between -180 and 180)");
            table.HasCheckConstraint(
                "ck_route_execution_events_reason",
                "event_type not in ('SkippedStop', 'Cancelled') or nullif(btrim(reason), '') is not null");
            table.HasCheckConstraint(
                "ck_route_execution_events_stop_pair",
                "event_type in ('Departed', 'RouteCompleted', 'Cancelled') or route_stop_id is not null");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.DispatchRunId).HasColumnName("dispatch_run_id").IsRequired();
        builder.Property(x => x.RoutePlanId).HasColumnName("route_plan_id").IsRequired();
        builder.Property(x => x.RouteStopId).HasColumnName("route_stop_id");
        builder.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(40).IsRequired();
        builder.Property(x => x.SequenceNo).HasColumnName("sequence_no").IsRequired();
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.LocationText).HasColumnName("location_text").HasMaxLength(240);
        builder.Property(x => x.Latitude).HasColumnName("latitude").HasPrecision(10, 7);
        builder.Property(x => x.Longitude).HasColumnName("longitude").HasPrecision(10, 7);
        builder.Property(x => x.Reason).HasColumnName("reason").HasColumnType("text");
        builder.Property(x => x.ActorId).HasColumnName("actor_id").IsRequired();
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(160).IsRequired();
        builder.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(120).IsRequired();
        builder.Property(x => x.PayloadSnapshot).HasColumnName("payload_snapshot").HasColumnType("jsonb").HasDefaultValue("{}").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasIndex(x => new { x.DispatchRunId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ux_route_execution_events_idempotency");
        builder.HasIndex(x => new { x.DispatchRunId, x.SequenceNo })
            .IsUnique()
            .HasDatabaseName("ux_route_execution_events_sequence");
        builder.HasIndex(x => new { x.DispatchRunId, x.SequenceNo, x.OccurredAt, x.Id })
            .HasDatabaseName("ix_route_execution_events_timeline");
        builder.HasIndex(x => new { x.RouteStopId, x.OccurredAt, x.Id })
            .HasDatabaseName("ix_route_execution_events_stop")
            .HasFilter("route_stop_id is not null");
        builder.HasIndex(x => new { x.EventType, x.OccurredAt })
            .HasDatabaseName("ix_route_execution_events_type_time");

        builder.HasOne(x => x.DispatchRun)
            .WithMany(x => x.Events)
            .HasForeignKey(x => x.DispatchRunId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RoutePlan)
            .WithMany()
            .HasForeignKey(x => x.RoutePlanId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RouteStop)
            .WithMany()
            .HasForeignKey(x => x.RouteStopId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserRecord>()
            .WithMany()
            .HasForeignKey(x => x.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
