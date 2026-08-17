using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FactoryErp.Infrastructure.Persistence.Configurations;

public sealed class LoadPlanValidationResultRecordConfiguration : IEntityTypeConfiguration<LoadPlanValidationResultRecord>
{
    public void Configure(EntityTypeBuilder<LoadPlanValidationResultRecord> builder)
    {
        builder.ToTable("load_plan_validation_results", table =>
        {
            table.HasCheckConstraint(
                "ck_load_plan_validation_severity",
                "severity in ('HardError', 'Warning', 'Info')");
            table.HasCheckConstraint(
                "ck_load_plan_validation_resolution",
                "resolution_status in ('Open', 'Resolved', 'Overridden', 'NotApplicable')");
            table.HasCheckConstraint(
                "ck_load_plan_validation_resolution_pair",
                "(resolution_status = 'Open' and resolved_by is null and resolved_at is null) or (resolution_status <> 'Open' and resolved_by is not null and resolved_at is not null and resolution_reason is not null)");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.LoadPlanId).HasColumnName("load_plan_id").IsRequired();
        builder.Property(x => x.ValidationKey).HasColumnName("validation_key").HasMaxLength(160).IsRequired();
        builder.Property(x => x.Severity).HasColumnName("severity").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Message).HasColumnName("message").IsRequired();
        builder.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(80);
        builder.Property(x => x.EntityId).HasColumnName("entity_id");
        builder.Property(x => x.ResolutionStatus).HasColumnName("resolution_status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.ResolvedBy).HasColumnName("resolved_by");
        builder.Property(x => x.ResolvedAt).HasColumnName("resolved_at").HasColumnType("timestamptz");
        builder.Property(x => x.ResolutionReason).HasColumnName("resolution_reason");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasIndex(x => new { x.LoadPlanId, x.ValidationKey })
            .IsUnique()
            .HasDatabaseName("ux_load_plan_validation_key");
        builder.HasIndex(x => new { x.LoadPlanId, x.Severity, x.ResolutionStatus })
            .HasDatabaseName("ix_load_plan_validation_open");

        builder.HasOne(x => x.LoadPlan)
            .WithMany()
            .HasForeignKey(x => x.LoadPlanId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Resolver)
            .WithMany()
            .HasForeignKey(x => x.ResolvedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class LoadPlanManualChangeRecordConfiguration : IEntityTypeConfiguration<LoadPlanManualChangeRecord>
{
    public void Configure(EntityTypeBuilder<LoadPlanManualChangeRecord> builder)
    {
        builder.ToTable("load_plan_manual_changes", table =>
        {
            table.HasCheckConstraint(
                "ck_load_plan_manual_change_type",
                "change_type in ('AddLoadUnit', 'RemoveLoadUnit', 'MovePackage', 'ChangeQuantity', 'ChangeStopAllocation', 'ChangeVehicle', 'ChangeCapacity', 'Other')");
            table.HasCheckConstraint(
                "ck_load_plan_manual_change_entity",
                "entity_id <> '00000000-0000-0000-0000-000000000000'");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.LoadPlanId).HasColumnName("load_plan_id").IsRequired();
        builder.Property(x => x.ActorUserId).HasColumnName("actor_user_id").IsRequired();
        builder.Property(x => x.ChangeType).HasColumnName("change_type").HasMaxLength(60).IsRequired();
        builder.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(80).IsRequired();
        builder.Property(x => x.EntityId).HasColumnName("entity_id").IsRequired();
        builder.Property(x => x.BeforeJson).HasColumnName("before_json").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.AfterJson).HasColumnName("after_json").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Reason).HasColumnName("reason").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();

        builder.HasIndex(x => new { x.LoadPlanId, x.CreatedAt })
            .HasDatabaseName("ix_load_plan_manual_changes_time");
        builder.HasIndex(x => new { x.LoadPlanId, x.EntityType, x.EntityId })
            .HasDatabaseName("ix_load_plan_manual_changes_entity");

        builder.HasOne(x => x.LoadPlan)
            .WithMany()
            .HasForeignKey(x => x.LoadPlanId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ActorUser)
            .WithMany()
            .HasForeignKey(x => x.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
