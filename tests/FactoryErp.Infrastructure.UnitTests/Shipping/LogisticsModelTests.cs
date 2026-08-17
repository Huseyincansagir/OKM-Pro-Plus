using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FactoryErp.Infrastructure.UnitTests.Shipping;

public sealed class LogisticsModelTests
{
    [Fact]
    public void Vehicle_and_driver_rows_have_concurrency_tokens()
    {
        using var context = CreateContext();
        var vehicle = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(VehicleRecord))!;
        var driver = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(DriverRecord))!;

        vehicle.FindProperty(nameof(VehicleRecord.RowVersion))!.IsConcurrencyToken.Should().BeTrue();
        driver.FindProperty(nameof(DriverRecord.RowVersion))!.IsConcurrencyToken.Should().BeTrue();
        vehicle.GetTableName().Should().Be("vehicles");
        driver.GetTableName().Should().Be("drivers");
    }

    [Fact]
    public void Route_plan_has_status_time_and_version_constraints()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(RoutePlanRecord))!;

        entity.GetTableName().Should().Be("route_plans");
        entity.GetCheckConstraints().Should().Contain(x => x.Name == "ck_route_plans_status");
        entity.GetCheckConstraints().Should().Contain(x => x.Name == "ck_route_plans_valid_time");
        entity.GetCheckConstraints().Should().Contain(x => x.Name == "ck_route_plans_version_positive");
        entity.FindProperty(nameof(RoutePlanRecord.RowVersion))!.IsConcurrencyToken.Should().BeTrue();
        entity.GetIndexes().Should().Contain(x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual(new[] {
            nameof(RoutePlanRecord.ShipmentId), nameof(RoutePlanRecord.Version) }));
    }

    [Fact]
    public void Route_stop_has_unique_sequence_and_positive_sequence_constraint()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(RouteStopRecord))!;

        entity.GetCheckConstraints().Should().Contain(x => x.Name == "ck_route_stops_sequence_positive");
        entity.GetIndexes().Should().Contain(x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual(new[] {
            nameof(RouteStopRecord.RoutePlanId), nameof(RouteStopRecord.SequenceNo) }));
    }

    [Fact]
    public void Shipment_has_unique_delivery_note_source_and_item_unique_key()
    {
        using var context = CreateContext();
        var shipment = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(ShipmentRecord))!;
        var item = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(ShipmentItemRecord))!;

        shipment.GetIndexes().Should().Contain(x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual(new[] {
            nameof(ShipmentRecord.DeliveryNoteId) }));
        item.GetIndexes().Should().Contain(x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual(new[] {
            nameof(ShipmentItemRecord.ShipmentId), nameof(ShipmentItemRecord.DeliveryNoteItemId) }));
    }

    [Fact]
    public void Capacity_profile_has_effective_range_and_weight_constraints()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(VehicleCapacityRecord))!;

        entity.GetCheckConstraints().Should().Contain(x => x.Name == "ck_vehicle_capacities_effective_range");
        entity.GetCheckConstraints().Should().Contain(x => x.Name == "ck_vehicle_capacities_weight");
        entity.FindProperty(nameof(VehicleCapacityRecord.RowVersion))!.IsConcurrencyToken.Should().BeTrue();
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
