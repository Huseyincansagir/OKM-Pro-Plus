using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FactoryErp.Infrastructure.Persistence.Configurations;

public sealed class VehicleFitEvaluationRecordConfiguration : IEntityTypeConfiguration<VehicleFitEvaluationRecord>
{
    public void Configure(EntityTypeBuilder<VehicleFitEvaluationRecord> builder)
    {
        builder.ToTable("vehicle_fit_evaluations", table =>
        {
            table.HasCheckConstraint("ck_vehicle_fit_candidate_status", "candidate_status in ('Candidate', 'Recommended', 'Rejected', 'NeedsReview')");
            table.HasCheckConstraint("ck_vehicle_fit_check_statuses", "door_check_status in ('NotChecked', 'Pass', 'Fail', 'Warning') and dimension_check_status in ('NotChecked', 'Pass', 'Fail', 'Warning') and stacking_check_status in ('NotChecked', 'Pass', 'Fail', 'Warning') and axle_check_status in ('NotChecked', 'Pass', 'Fail', 'Warning') and stop_access_status in ('NotChecked', 'Pass', 'Fail', 'Warning')");
            table.HasCheckConstraint("ck_vehicle_fit_ratios_non_negative", "(weight_ratio is null or weight_ratio >= 0) and (volume_ratio is null or volume_ratio >= 0) and (pallet_ratio is null or pallet_ratio >= 0) and (floor_area_ratio is null or floor_area_ratio >= 0) and (height_ratio is null or height_ratio >= 0) and (fit_score is null or fit_score >= 0)");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.LoadPlanId).HasColumnName("load_plan_id").IsRequired();
        builder.Property(x => x.VehicleId).HasColumnName("vehicle_id").IsRequired();
        builder.Property(x => x.VehicleCapacityId).HasColumnName("vehicle_capacity_id");
        builder.Property(x => x.CandidateStatus).HasColumnName("candidate_status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.RejectionCode).HasColumnName("rejection_code").HasMaxLength(80);
        builder.Property(x => x.ReasonText).HasColumnName("reason_text").HasColumnType("text");
        builder.Property(x => x.WeightRatio).HasColumnName("weight_ratio").HasPrecision(18, 6);
        builder.Property(x => x.VolumeRatio).HasColumnName("volume_ratio").HasPrecision(18, 6);
        builder.Property(x => x.PalletRatio).HasColumnName("pallet_ratio").HasPrecision(18, 6);
        builder.Property(x => x.FloorAreaRatio).HasColumnName("floor_area_ratio").HasPrecision(18, 6);
        builder.Property(x => x.HeightRatio).HasColumnName("height_ratio").HasPrecision(18, 6);
        builder.Property(x => x.DoorCheckStatus).HasColumnName("door_check_status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.DimensionCheckStatus).HasColumnName("dimension_check_status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.StackingCheckStatus).HasColumnName("stacking_check_status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.AxleCheckStatus).HasColumnName("axle_check_status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.StopAccessStatus).HasColumnName("stop_access_status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.FitScore).HasColumnName("fit_score").HasPrecision(18, 6);
        builder.Property(x => x.AlgorithmVersion).HasColumnName("algorithm_version").HasMaxLength(40).IsRequired();
        builder.Property(x => x.InputSnapshotHash).HasColumnName("input_snapshot_hash").HasMaxLength(128).IsRequired();
        builder.Property(x => x.CapacitySnapshot).HasColumnName("capacity_snapshot").HasColumnType("jsonb");
        builder.Property(x => x.EvaluatedAt).HasColumnName("evaluated_at").HasColumnType("timestamptz").IsRequired();

        builder.HasIndex(x => new { x.LoadPlanId, x.VehicleId, x.VehicleCapacityId, x.InputSnapshotHash })
            .IsUnique()
            .HasDatabaseName("ux_vehicle_fit_snapshot_candidate");
        builder.HasIndex(x => new { x.LoadPlanId, x.CandidateStatus, x.FitScore })
            .HasDatabaseName("ix_vehicle_fit_plan_status_score");
        builder.HasIndex(x => new { x.VehicleId, x.EvaluatedAt })
            .HasDatabaseName("ix_vehicle_fit_vehicle_evaluated");

        builder.HasOne(x => x.LoadPlan).WithMany().HasForeignKey(x => x.LoadPlanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Vehicle).WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.VehicleCapacity).WithMany().HasForeignKey(x => x.VehicleCapacityId).OnDelete(DeleteBehavior.Restrict);
    }
}
