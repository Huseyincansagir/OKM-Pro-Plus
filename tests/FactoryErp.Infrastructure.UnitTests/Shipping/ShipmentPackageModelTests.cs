using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FactoryErp.Infrastructure.UnitTests.Shipping;

public sealed class ShipmentPackageModelTests
{
    [Fact]
    public void Shipment_package_has_expected_table_constraints_and_concurrency()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(ShipmentPackageRecord))!;

        entity.GetTableName().Should().Be("shipment_packages");
        entity.GetCheckConstraints().Select(x => x.Name).Should().Contain(new[]
        {
            "ck_shipment_packages_type",
            "ck_shipment_packages_status",
            "ck_shipment_packages_quantity_positive",
            "ck_shipment_packages_quantity_formula",
        });
        entity.FindProperty(nameof(ShipmentPackageRecord.RowVersion))!.IsConcurrencyToken.Should().BeTrue();
        entity.FindProperty(nameof(ShipmentPackageRecord.QuantityBase))!.GetPrecision().Should().Be(18);
        entity.FindProperty(nameof(ShipmentPackageRecord.QuantityBase))!.GetScale().Should().Be(6);
    }

    [Fact]
    public void Shipment_package_has_item_status_stop_and_filtered_code_indexes()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(ShipmentPackageRecord))!;
        var indexes = entity.GetIndexes().ToArray();

        indexes.Should().Contain(x => x.Properties.Select(p => p.Name).SequenceEqual(new[]
        {
            nameof(ShipmentPackageRecord.ShipmentId), nameof(ShipmentPackageRecord.Status),
        }));
        indexes.Should().Contain(x => x.Properties.Select(p => p.Name).SequenceEqual(new[]
        {
            nameof(ShipmentPackageRecord.ShipmentItemId),
        }));
        indexes.Should().Contain(x => x.IsUnique
            && x.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(ShipmentPackageRecord.PackageCode) })
            && x.GetFilter() == "package_code is not null and status <> 'Cancelled'");
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
