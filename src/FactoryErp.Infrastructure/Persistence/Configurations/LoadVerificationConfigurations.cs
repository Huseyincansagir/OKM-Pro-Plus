using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FactoryErp.Infrastructure.Persistence.Entities;

namespace FactoryErp.Infrastructure.Persistence.Configurations;

public sealed class LoadVerificationSessionRecordConfiguration : IEntityTypeConfiguration<LoadVerificationSessionRecord>
{
    public void Configure(EntityTypeBuilder<LoadVerificationSessionRecord> builder)
    {
        builder.ToTable("load_verification_sessions", table =>
        {
            table.HasCheckConstraint(
                "ck_load_verification_session_status",
                "status in ('Draft', 'InProgress', 'Completed', 'Discrepancy', 'Cancelled')");
            table.HasCheckConstraint(
                "ck_load_verification_session_completion_pair",
                "(status in ('Completed', 'Discrepancy', 'Cancelled') and completed_by is not null and completed_at is not null) or (status in ('Draft', 'InProgress') and completed_by is null and completed_at is null)");
            table.HasCheckConstraint(
                "ck_load_verification_session_discrepancy_reason",
                "(status <> 'Discrepancy') or (completion_reason is not null and length(btrim(completion_reason)) > 0)");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.LoadPlanId).HasColumnName("load_plan_id").IsRequired();
        builder.Property(x => x.ShipmentId).HasColumnName("shipment_id").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.StartedBy).HasColumnName("started_by").IsRequired();
        builder.Property(x => x.StartedAt).HasColumnName("started_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.CompletedBy).HasColumnName("completed_by");
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamptz");
        builder.Property(x => x.CompletionReason).HasColumnName("completion_reason");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.RowVersion)
            .HasColumnName("row_version")
            .HasColumnType("bigint")
            .IsConcurrencyToken()
            .ValueGeneratedNever()
            .HasDefaultValue(1L);

        builder.HasIndex(x => x.LoadPlanId)
            .IsUnique()
            .HasFilter("status in ('Draft', 'InProgress')")
            .HasDatabaseName("ux_load_verification_active_session");
        builder.HasIndex(x => new { x.ShipmentId, x.Status })
            .HasDatabaseName("ix_load_verification_sessions_shipment_status");

        builder.HasOne(x => x.LoadPlan)
            .WithMany()
            .HasForeignKey(x => x.LoadPlanId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Shipment)
            .WithMany()
            .HasForeignKey(x => x.ShipmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.StartedByUser)
            .WithMany()
            .HasForeignKey(x => x.StartedBy)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CompletedByUser)
            .WithMany()
            .HasForeignKey(x => x.CompletedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class LoadVerificationScanRecordConfiguration : IEntityTypeConfiguration<LoadVerificationScanRecord>
{
    public void Configure(EntityTypeBuilder<LoadVerificationScanRecord> builder)
    {
        builder.ToTable("load_verification_scans", table =>
        {
            table.HasCheckConstraint(
                "ck_load_verification_scan_status",
                "status in ('Accepted', 'Duplicate', 'Unexpected', 'WrongUnit', 'CancelledPackage', 'Discrepancy')");
            table.HasCheckConstraint(
                "ck_load_verification_scan_mode",
                "scan_mode in ('Pallet', 'Case', 'Package', 'BaseUnit')");
            table.HasCheckConstraint(
                "ck_load_verification_scan_barcode",
                "length(btrim(barcode)) > 0");
            table.HasCheckConstraint(
                "ck_load_verification_scan_quantity",
                "quantity_base > 0");
            table.HasCheckConstraint(
                "ck_load_verification_scan_accepted_package",
                "status <> 'Accepted' or shipment_package_id is not null");
            table.HasCheckConstraint(
                "ck_load_verification_scan_keys",
                "length(btrim(idempotency_key)) > 0 and length(btrim(correlation_id)) > 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.SessionId).HasColumnName("session_id").IsRequired();
        builder.Property(x => x.LoadPlanId).HasColumnName("load_plan_id").IsRequired();
        builder.Property(x => x.ShipmentId).HasColumnName("shipment_id").IsRequired();
        builder.Property(x => x.ShipmentPackageId).HasColumnName("shipment_package_id");
        builder.Property(x => x.ExpectedLoadUnitId).HasColumnName("expected_load_unit_id");
        builder.Property(x => x.ActualLoadUnitId).HasColumnName("actual_load_unit_id");
        builder.Property(x => x.Barcode).HasColumnName("barcode").HasMaxLength(160).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.ScanMode).HasColumnName("scan_mode").HasMaxLength(30).IsRequired();
        builder.Property(x => x.QuantityBase).HasColumnName("quantity_base").HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.ReasonCode).HasColumnName("reason_code").HasMaxLength(100);
        builder.Property(x => x.ReasonText).HasColumnName("reason_text");
        builder.Property(x => x.ScannedBy).HasColumnName("scanned_by").IsRequired();
        builder.Property(x => x.ScannedAt).HasColumnName("scanned_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(160).IsRequired();
        builder.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(160).IsRequired();
        builder.Property(x => x.RowVersion)
            .HasColumnName("row_version")
            .HasColumnType("bigint")
            .IsConcurrencyToken()
            .ValueGeneratedNever()
            .HasDefaultValue(1L);

        builder.HasIndex(x => new { x.SessionId, x.ShipmentPackageId })
            .IsUnique()
            .HasFilter("status = 'Accepted' and shipment_package_id is not null")
            .HasDatabaseName("ux_load_verification_accepted_package");
        builder.HasIndex(x => new { x.SessionId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ux_load_verification_scan_idempotency");
        builder.HasIndex(x => new { x.SessionId, x.ScannedAt, x.Id })
            .HasDatabaseName("ix_load_verification_scans_time");
        builder.HasIndex(x => new { x.LoadPlanId, x.Barcode, x.ScannedAt })
            .HasDatabaseName("ix_load_verification_scans_plan_barcode_time");

        builder.HasOne(x => x.Session)
            .WithMany(x => x.Scans)
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.LoadPlan)
            .WithMany()
            .HasForeignKey(x => x.LoadPlanId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Shipment)
            .WithMany()
            .HasForeignKey(x => x.ShipmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ShipmentPackage)
            .WithMany()
            .HasForeignKey(x => x.ShipmentPackageId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ExpectedLoadUnit)
            .WithMany()
            .HasForeignKey(x => x.ExpectedLoadUnitId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ActualLoadUnit)
            .WithMany()
            .HasForeignKey(x => x.ActualLoadUnitId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ScannedByUser)
            .WithMany()
            .HasForeignKey(x => x.ScannedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
