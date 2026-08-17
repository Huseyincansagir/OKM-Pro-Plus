using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FactoryErp.Infrastructure.UnitTests.Shipping;

public sealed class DispatchRunModelTests
{
    [Fact]
    public void Dispatch_runs_have_state_checks_active_resource_indexes_and_manual_row_version()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(DispatchRunRecord))!;

        entity.GetTableName().Should().Be("dispatch_runs");
        entity.GetCheckConstraints().Select(x => x.Name).Should().Contain(new[]
        {
            "ck_dispatch_runs_status",
            "ck_dispatch_runs_completed_pair",
            "ck_dispatch_runs_cancelled_pair",
            "ck_dispatch_runs_departed_pair",
            "ck_dispatch_runs_time_order",
        });
        entity.FindProperty(nameof(DispatchRunRecord.RowVersion))!.IsConcurrencyToken.Should().BeTrue();
        entity.FindProperty(nameof(DispatchRunRecord.RowVersion))!.ValueGenerated.Should().Be(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never);

        foreach (var property in new[]
        {
            nameof(DispatchRunRecord.RoutePlanId),
            nameof(DispatchRunRecord.ShipmentId),
            nameof(DispatchRunRecord.VehicleId),
            nameof(DispatchRunRecord.DriverId),
        })
        {
            entity.GetIndexes().Should().Contain(index =>
                index.IsUnique
                && index.GetFilter() == "status in ('Prepared', 'Dispatched', 'InTransit')"
                && index.Properties.Select(p => p.Name).SequenceEqual(new[] { property }));
        }
    }

    [Fact]
    public void Route_execution_events_have_immutable_event_checks_and_idempotency_indexes()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(RouteExecutionEventRecord))!;

        entity.GetTableName().Should().Be("route_execution_events");
        entity.GetCheckConstraints().Select(x => x.Name).Should().Contain(new[]
        {
            "ck_route_execution_events_type",
            "ck_route_execution_events_sequence",
            "ck_route_execution_events_location",
            "ck_route_execution_events_reason",
            "ck_route_execution_events_stop_pair",
        });
        entity.GetIndexes().Should().Contain(index =>
            index.IsUnique
            && index.Properties.Select(p => p.Name).SequenceEqual(new[]
            {
                nameof(RouteExecutionEventRecord.DispatchRunId), nameof(RouteExecutionEventRecord.IdempotencyKey),
            }));
        entity.GetIndexes().Should().Contain(index =>
            index.IsUnique
            && index.Properties.Select(p => p.Name).SequenceEqual(new[]
            {
                nameof(RouteExecutionEventRecord.DispatchRunId), nameof(RouteExecutionEventRecord.SequenceNo),
            }));
    }

    [Fact]
    public void Route_stop_model_contains_execution_projection_fields_and_checks()
    {
        using var context = CreateContext();
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(RouteStopRecord))!;

        entity.FindProperty(nameof(RouteStopRecord.ActualDepartureAt)).Should().NotBeNull();
        entity.FindProperty(nameof(RouteStopRecord.SkippedAt)).Should().NotBeNull();
        entity.GetCheckConstraints().Select(x => x.Name).Should().Contain(new[]
        {
            "ck_route_stops_execution_time_order",
            "ck_route_stops_skipped_reason",
        });
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
