using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FactoryErp.Infrastructure.UnitTests.Shipping;

public sealed class VehicleFitEvaluationModelTests
{
    [Fact]
    public void Vehicle_fit_evaluation_has_candidate_ratio_and_check_status_constraints()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(VehicleFitEvaluationRecord))!;

        entity.GetTableName().Should().Be("vehicle_fit_evaluations");
        entity.GetCheckConstraints().Select(x => x.Name).Should().Contain(new[]
        {
            "ck_vehicle_fit_candidate_status",
            "ck_vehicle_fit_check_statuses",
            "ck_vehicle_fit_ratios_non_negative",
        });
        entity.FindProperty(nameof(VehicleFitEvaluationRecord.InputSnapshotHash))!.IsNullable.Should().BeFalse();
        entity.FindProperty(nameof(VehicleFitEvaluationRecord.AlgorithmVersion))!.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void Vehicle_fit_evaluation_has_snapshot_uniqueness_and_candidate_order_indexes()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(VehicleFitEvaluationRecord))!;

        entity.GetIndexes().Should().Contain(x => x.IsUnique && x.GetDatabaseName() == "ux_vehicle_fit_snapshot_candidate");
        entity.GetIndexes().Should().Contain(x => x.GetDatabaseName() == "ix_vehicle_fit_plan_status_score");
        entity.GetIndexes().Should().Contain(x => x.GetDatabaseName() == "ix_vehicle_fit_vehicle_evaluated");
    }

    private static FactoryErpDbContext CreateContext()
    {
        var connectionString = Environment.GetEnvironmentVariable("FactoryErpTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__FactoryErp")
            ?? "Host=127.0.0.1;Port=5432;Database=factory_erp_g1;Username=factory_erp;Password=dev_only_change_me";
        var options = new DbContextOptionsBuilder<FactoryErpDbContext>().UseNpgsql(connectionString).Options;
        return new FactoryErpDbContext(options);
    }
}
