using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FactoryErp.Infrastructure.UnitTests.Shipping;

public sealed class LoadPlanningModelTests
{
    [Fact]
    public void LoadPlan_has_state_version_snapshot_and_lock_constraints()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(LoadPlanRecord))!;

        entity.GetTableName().Should().Be("load_plans");
        entity.GetCheckConstraints().Select(x => x.Name).Should().Contain(new[]
        {
            "ck_load_plans_status",
            "ck_load_plans_feasibility",
            "ck_load_plans_version_positive",
            "ck_load_plans_approval_pair",
            "ck_load_plans_lock_pair",
            "ck_load_plans_locked_requirements",
        });
        entity.FindProperty(nameof(LoadPlanRecord.RowVersion))!.IsConcurrencyToken.Should().BeTrue();
        entity.GetIndexes().Should().Contain(x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual(new[]
        {
            nameof(LoadPlanRecord.ShipmentId), nameof(LoadPlanRecord.Version),
        }));
    }

    [Fact]
    public void LoadUnits_have_deterministic_code_priority_and_physical_constraints()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(LoadUnitRecord))!;

        entity.GetTableName().Should().Be("load_units");
        entity.GetCheckConstraints().Select(x => x.Name).Should().Contain(new[]
        {
            "ck_load_units_type",
            "ck_load_units_status",
            "ck_load_units_dimensions",
            "ck_load_units_weight",
            "ck_load_units_volume",
            "ck_load_units_priority",
        });
        entity.FindProperty(nameof(LoadUnitRecord.RowVersion))!.IsConcurrencyToken.Should().BeTrue();
        entity.GetIndexes().Should().Contain(x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual(new[]
        {
            nameof(LoadUnitRecord.LoadPlanId), nameof(LoadUnitRecord.UnitCode),
        }));
    }

    [Fact]
    public void LoadUnitItems_and_stop_allocations_have_ceiling_indexes_and_restricted_relations()
    {
        using var context = CreateContext();
        var item = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(LoadUnitItemRecord))!;
        var stop = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(LoadUnitStopAllocationRecord))!;

        item.GetCheckConstraints().Select(x => x.Name).Should().Contain(new[]
        {
            "ck_load_unit_items_quantity_positive",
            "ck_load_unit_items_weight_non_negative",
            "ck_load_unit_items_volume_non_negative",
        });
        item.GetIndexes().Should().Contain(x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual(new[]
        {
            nameof(LoadUnitItemRecord.ShipmentPackageId),
        }) && x.GetFilter() == "quantity_base > 0");
        stop.GetCheckConstraints().Select(x => x.Name).Should().Contain(new[]
        {
            "ck_load_unit_stop_allocations_quantity_positive",
            "ck_load_unit_stop_allocations_sequence_positive",
        });
        stop.GetIndexes().Should().Contain(x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual(new[]
        {
            nameof(LoadUnitStopAllocationRecord.LoadUnitItemId), nameof(LoadUnitStopAllocationRecord.RouteStopId),
        }));
    }

    private static FactoryErpDbContext CreateContext()
    {
        var connectionString = Environment.GetEnvironmentVariable("FactoryErpTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__FactoryErp")
            ?? "Host=127.0.0.1;Port=5432;Database=factory_erp_g1;Username=factory_erp;Password=dev_only_change_me";
        var options = new DbContextOptionsBuilder<FactoryErpDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new FactoryErpDbContext(options);
    }
}
