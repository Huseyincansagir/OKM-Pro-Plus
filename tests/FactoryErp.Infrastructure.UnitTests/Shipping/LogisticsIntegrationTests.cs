using FactoryErp.Application.Shipping;
using FactoryErp.Domain.Common;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Shipping;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.UnitTests.Shipping;

public sealed class LogisticsIntegrationTests
{
    private static readonly Guid DeliveryNoteId = Guid.Parse("eade528b-1cc3-42c9-8009-93066dac675f");
    private static readonly Guid CustomerId = Guid.Parse("40000000-0000-0000-0000-000000000101");
    private static readonly Guid AddressId = Guid.Parse("40000000-0000-0000-0000-000000000102");

    [Fact]
    public async Task Concurrent_assignment_serializes_on_vehicle_and_driver_rows()
    {
        await CleanupExistingFixturesAsync();

        var actorId = await GetAdminIdAsync();
        var vehicleTypeId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var shipmentId = Guid.Empty;
        var routeAId = Guid.Empty;
        var routeBId = Guid.Empty;

        try
        {
            await using var setupContext = CreateContext();
            var setupService = CreateService(setupContext);
            var type = await setupService.CreateVehicleTypeAsync(
                new CreateVehicleTypeRequest("TEST-PANELVAN-" + Guid.NewGuid().ToString("N"), "Test Panelvan"),
                actorId,
                "logistics-type-" + Guid.NewGuid(),
                "corr-logistics-setup",
                CancellationToken.None);
            vehicleTypeId = type.Id;

            var vehicle = await setupService.CreateVehicleAsync(
                new CreateVehicleRequest(vehicleTypeId, "99 TEST 001", null, "Test fixture"),
                actorId,
                "logistics-vehicle-" + Guid.NewGuid(),
                "corr-logistics-setup",
                CancellationToken.None);
            vehicleId = vehicle.Id;

            var driver = await setupService.CreateDriverAsync(
                new CreateDriverRequest(null, "Test Driver", null, "TEST-" + Guid.NewGuid().ToString("N"), DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1))),
                actorId,
                "logistics-driver-" + Guid.NewGuid(),
                "corr-logistics-setup",
                CancellationToken.None);
            driverId = driver.Id;

            var shipment = await setupService.CreateShipmentAsync(
                new CreateShipmentRequest(DeliveryNoteId, 1),
                actorId,
                "logistics-shipment-" + Guid.NewGuid(),
                "corr-logistics-setup",
                CancellationToken.None);
            shipmentId = shipment.Id;

            var start = DateTimeOffset.UtcNow.AddHours(2);
            var end = start.AddHours(2);
            var routeA = await setupService.CreateRoutePlanAsync(
                shipmentId,
                new CreateRoutePlanRequest(start, end, shipment.RowVersion),
                actorId,
                "logistics-route-a-" + Guid.NewGuid(),
                "corr-logistics-setup",
                CancellationToken.None);
            routeAId = routeA.Id;

            var routeB = await setupService.CreateRoutePlanAsync(
                shipmentId,
                new CreateRoutePlanRequest(start, end, shipment.RowVersion),
                actorId,
                "logistics-route-b-" + Guid.NewGuid(),
                "corr-logistics-setup",
                CancellationToken.None);
            routeBId = routeB.Id;

            await setupService.ReplaceStopsAsync(
                routeAId,
                new ReplaceRouteStopsRequest([
                    new RouteStopInput(1, CustomerId, AddressId, start.AddHours(1)),
                ]),
                routeA.RowVersion,
                actorId,
                "logistics-stop-a-" + Guid.NewGuid(),
                "corr-logistics-setup",
                CancellationToken.None);
            await setupService.ReplaceStopsAsync(
                routeBId,
                new ReplaceRouteStopsRequest([
                    new RouteStopInput(1, CustomerId, AddressId, start.AddHours(1)),
                ]),
                routeB.RowVersion,
                actorId,
                "logistics-stop-b-" + Guid.NewGuid(),
                "corr-logistics-setup",
                CancellationToken.None);

            await using var contextA = CreateContext();
            await using var contextB = CreateContext();
            var serviceA = CreateService(contextA);
            var serviceB = CreateService(contextB);
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var taskA = Task.Run(async () =>
            {
                await gate.Task;
                return await TryAssignAsync(serviceA, routeAId, vehicleId, driverId, actorId, "logistics-assign-a-" + Guid.NewGuid());
            });
            var taskB = Task.Run(async () =>
            {
                await gate.Task;
                return await TryAssignAsync(serviceB, routeBId, vehicleId, driverId, actorId, "logistics-assign-b-" + Guid.NewGuid());
            });
            gate.SetResult();

            var results = await Task.WhenAll(taskA, taskB);
            results.Count(x => x.Succeeded).Should().Be(1);
            results.Count(x => x.ErrorCode is "VEHICLE_SCHEDULE_CONFLICT" or "DRIVER_SCHEDULE_CONFLICT").Should().Be(1);

            var boundaryRouteId = Guid.Empty;
            try
            {
                await using var boundaryContext = CreateContext();
                var boundaryService = CreateService(boundaryContext);
                var boundary = await boundaryService.CreateRoutePlanAsync(
                    shipmentId,
                    new CreateRoutePlanRequest(end, end.AddHours(2), shipment.RowVersion),
                    actorId,
                    "logistics-route-boundary-" + Guid.NewGuid(),
                    "corr-logistics-boundary",
                    CancellationToken.None);
                boundaryRouteId = boundary.Id;
                var result = await boundaryService.AssignResourcesAsync(
                    boundary.Id,
                    new AssignRouteResourcesRequest(vehicleId, driverId),
                    boundary.RowVersion,
                    actorId,
                    "logistics-assign-boundary-" + Guid.NewGuid(),
                    "corr-logistics-boundary",
                    CancellationToken.None);
                result.Should().NotBeNull();
            }
            finally
            {
                if (boundaryRouteId != Guid.Empty)
                {
                    await DeleteRouteAsync(boundaryRouteId);
                }
            }
        }
        finally
        {
            await CleanupFixturesAsync(vehicleTypeId, vehicleId, driverId, shipmentId, routeAId, routeBId);
        }
    }

    private static async Task<AssignmentAttempt> TryAssignAsync(
        ILogisticsCommandService service,
        Guid routePlanId,
        Guid vehicleId,
        Guid driverId,
        Guid actorId,
        string idempotencyKey)
    {
        try
        {
            var result = await service.AssignResourcesAsync(
                routePlanId,
                new AssignRouteResourcesRequest(vehicleId, driverId),
                1,
                actorId,
                idempotencyKey,
                "corr-logistics-concurrency",
                CancellationToken.None);
            return new AssignmentAttempt(result is not null, null);
        }
        catch (DomainException exception)
        {
            return new AssignmentAttempt(false, exception.Error.Code);
        }
    }

    private static LogisticsCommandService CreateService(FactoryErpDbContext context)
        => new(context, new EfAuditWriter(context), new EfIdempotencyStore(context));

    private static FactoryErpDbContext CreateContext()
    {
        var connectionString = Environment.GetEnvironmentVariable("FactoryErpTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__FactoryErp")
            ?? "Host=127.0.0.1;Port=5432;Database=factory_erp_g1;Username=factory_erp;Password=dev_only_change_me";
        return new FactoryErpDbContext(new DbContextOptionsBuilder<FactoryErpDbContext>().UseNpgsql(connectionString).Options);
    }

    private static async Task<Guid> GetAdminIdAsync()
    {
        await using var context = CreateContext();
        return await context.Users.Where(x => x.UserName == "admin").Select(x => x.Id).SingleAsync();
    }

    private static async Task CleanupExistingFixturesAsync()
    {
        await using var context = CreateContext();
        var existing = await context.Shipments.Where(x => x.DeliveryNoteId == DeliveryNoteId).Select(x => x.Id).ToArrayAsync();
        foreach (var shipmentId in existing)
        {
            await DeleteRouteAndShipmentAsync(context, shipmentId);
        }
        await context.SaveChangesAsync();
    }

    private static async Task CleanupFixturesAsync(
        Guid vehicleTypeId,
        Guid vehicleId,
        Guid driverId,
        Guid shipmentId,
        Guid routeAId,
        Guid routeBId)
    {
        await using var context = CreateContext();
        if (routeAId != Guid.Empty)
        {
            var route = await context.RoutePlans.FindAsync(routeAId);
            if (route is not null)
            {
                await DeleteRouteAsync(context, routeAId);
            }
        }
        if (routeBId != Guid.Empty)
        {
            var route = await context.RoutePlans.FindAsync(routeBId);
            if (route is not null)
            {
                await DeleteRouteAsync(context, routeBId);
            }
        }
        if (shipmentId != Guid.Empty)
        {
            var shipment = await context.Shipments.FindAsync(shipmentId);
            if (shipment is not null)
            {
                context.ShipmentItems.RemoveRange(context.ShipmentItems.Where(x => x.ShipmentId == shipmentId));
                context.Shipments.Remove(shipment);
            }
        }
        if (vehicleId != Guid.Empty)
        {
            var vehicle = await context.Vehicles.FindAsync(vehicleId);
            if (vehicle is not null)
            {
                context.Vehicles.Remove(vehicle);
            }
        }
        if (driverId != Guid.Empty)
        {
            var driver = await context.Drivers.FindAsync(driverId);
            if (driver is not null)
            {
                context.Drivers.Remove(driver);
            }
        }
        if (vehicleTypeId != Guid.Empty)
        {
            context.VehicleCapacities.RemoveRange(context.VehicleCapacities.Where(x => x.VehicleTypeId == vehicleTypeId));
            var type = await context.VehicleTypes.FindAsync(vehicleTypeId);
            if (type is not null)
            {
                context.VehicleTypes.Remove(type);
            }
        }

        context.AuditLogs.RemoveRange(context.AuditLogs.Where(x => x.CorrelationId.StartsWith("corr-logistics")));
        context.IdempotencyRecords.RemoveRange(context.IdempotencyRecords.Where(x => x.Scope.StartsWith("vehicle-type:create:")
            || x.Scope.StartsWith("vehicle:create:")
            || x.Scope.StartsWith("driver:create:")
            || x.Scope.StartsWith("shipment:create:")
            || x.Scope.StartsWith("route-plan:")));
        await context.SaveChangesAsync();
    }

    private static async Task DeleteRouteAsync(Guid routePlanId)
    {
        await using var context = CreateContext();
        await DeleteRouteAsync(context, routePlanId);
        await context.SaveChangesAsync();
    }

    private static async Task DeleteRouteAsync(FactoryErpDbContext context, Guid routePlanId)
    {
        context.RouteStops.RemoveRange(context.RouteStops.Where(x => x.RoutePlanId == routePlanId));
        var route = await context.RoutePlans.FindAsync(routePlanId);
        if (route is not null)
        {
            context.RoutePlans.Remove(route);
        }
    }

    private static async Task DeleteRouteAndShipmentAsync(FactoryErpDbContext context, Guid shipmentId)
    {
        var routeIds = await context.RoutePlans.Where(x => x.ShipmentId == shipmentId).Select(x => x.Id).ToArrayAsync();
        foreach (var routeId in routeIds)
        {
            await DeleteRouteAsync(context, routeId);
        }
        context.ShipmentItems.RemoveRange(context.ShipmentItems.Where(x => x.ShipmentId == shipmentId));
        var shipment = await context.Shipments.FindAsync(shipmentId);
        if (shipment is not null)
        {
            context.Shipments.Remove(shipment);
        }
    }

    private sealed record AssignmentAttempt(bool Succeeded, string? ErrorCode);
}
