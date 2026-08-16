using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FactoryErp.Infrastructure.UnitTests.Production;

public sealed class ProductionModelTests
{
    [Fact]
    public void Production_order_has_positive_planned_and_bounded_completed_constraints()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(ProductionOrderRecord))!;

        entity.GetTableName().Should().Be("production_orders");
        entity.FindProperty(nameof(ProductionOrderRecord.PlannedQtyBase))!.GetPrecision().Should().Be(18);
        entity.FindProperty(nameof(ProductionOrderRecord.PlannedQtyBase))!.GetScale().Should().Be(6);
        entity.GetCheckConstraints().Should().Contain(constraint =>
            constraint.Name == "ck_production_orders_planned_positive");
        entity.GetCheckConstraints().Should().Contain(constraint =>
            constraint.Name == "ck_production_orders_completed_valid");
    }

    [Fact]
    public void Production_record_has_positive_quantity_constraint_and_order_foreign_key()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(ProductionRecord))!;

        entity.GetTableName().Should().Be("production_records");
        entity.GetCheckConstraints().Should().Contain(constraint =>
            constraint.Name == "ck_production_records_quantity_positive");
        entity.GetForeignKeys().Should().Contain(foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(ProductionOrderRecord)
            && foreignKey.Properties.Select(x => x.Name)
                .SequenceEqual(new[] { nameof(ProductionRecord.ProductionOrderId) }));
    }

    [Fact]
    public void Production_order_row_version_is_a_concurrency_token()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(ProductionOrderRecord))!;

        var rowVersion = entity.FindProperty(nameof(ProductionOrderRecord.RowVersion))!;
        rowVersion.IsConcurrencyToken.Should().BeTrue();
        rowVersion.GetColumnName().Should().Be("row_version");
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
