using FactoryErp.Application.Shipping;
using FactoryErp.Domain.Common;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using FactoryErp.Infrastructure.Shipping;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.UnitTests.Shipping;

public sealed class LoadPlanIntegrationTests
{
    private static readonly Guid ProductId = Guid.Parse("30000000-0000-0000-0000-000000000201");
    private static readonly Guid PackagingId = Guid.Parse("30000000-0000-0000-0000-000000000213");
    private static readonly Guid CustomerId = Guid.Parse("40000000-0000-0000-0000-000000000101");
    private static readonly Guid AddressId = Guid.Parse("40000000-0000-0000-0000-000000000102");

    [Fact]
    public async Task Create_draft_persists_nested_units_items_and_stop_allocations_and_replays()
    {
        var actorId = await GetAdminIdAsync();
        var profileId = Guid.NewGuid();
        var fixture = default(DeliveryFixture);
        var shipmentId = Guid.Empty;
        var routePlanId = Guid.Empty;
        var loadPlanId = Guid.Empty;

        try
        {
            await InsertPhysicalProfileAsync(profileId);
            fixture = await CreateDeliveryFixtureAsync(actorId, "l4b2-draft");
            var setup = await CreateShipmentAndRouteAsync(actorId, fixture!, "l4b2-draft");
            shipmentId = setup.Shipment.Id;
            routePlanId = setup.RoutePlan.Id;

            await using var context = CreateContext();
            var service = CreateLoadPlanService(context);
            var request = BuildRequest(setup.Shipment, setup.RoutePlan, setup.ShipmentItemId, setup.PackageId, setup.StopId, 4000);
            var key = "l4b2-load-plan-" + Guid.NewGuid();

            var created = await service.CreateLoadPlanAsync(shipmentId, request, actorId, key, "l4b2-draft", CancellationToken.None);
            var replay = await service.CreateLoadPlanAsync(shipmentId, request, actorId, key, "l4b2-draft", CancellationToken.None);
            loadPlanId = created.Id;

            created.Status.Should().Be("Draft");
            created.FeasibilityStatus.Should().Be("Infeasible");
            created.LoadUnits.Should().ContainSingle();
            created.LoadUnits.Single().Items.Should().ContainSingle();
            created.LoadUnits.Single().Items.Single().QuantityBase.Should().Be(4000);
            created.LoadUnits.Single().Items.Single().StopAllocations.Should().ContainSingle(x => x.QuantityBase == 4000);
            replay.Id.Should().Be(created.Id);

            var loaded = await service.GetLoadPlanAsync(loadPlanId, CancellationToken.None);
            loaded.Should().NotBeNull();
            loaded!.LoadUnits.Single().Items.Single().StopAllocations.Should().ContainSingle();
        }
        finally
        {
            await CleanupAsync(fixture, shipmentId, routePlanId, loadPlanId, profileId);
        }
    }

    [Fact]
    public async Task Create_draft_rejects_quantity_above_shipment_item_ceiling()
    {
        var actorId = await GetAdminIdAsync();
        var profileId = Guid.NewGuid();
        var fixture = default(DeliveryFixture);
        var shipmentId = Guid.Empty;
        var routePlanId = Guid.Empty;

        try
        {
            await InsertPhysicalProfileAsync(profileId);
            fixture = await CreateDeliveryFixtureAsync(actorId, "l4b2-ceiling");
            var setup = await CreateShipmentAndRouteAsync(actorId, fixture!, "l4b2-ceiling");
            shipmentId = setup.Shipment.Id;
            routePlanId = setup.RoutePlan.Id;

            await using var context = CreateContext();
            var service = CreateLoadPlanService(context);
            var request = BuildRequest(setup.Shipment, setup.RoutePlan, setup.ShipmentItemId, setup.PackageId, setup.StopId, 4001);

            var action = () => service.CreateLoadPlanAsync(
                shipmentId,
                request,
                actorId,
                "l4b2-ceiling-" + Guid.NewGuid(),
                "l4b2-ceiling",
                CancellationToken.None);

            await action.Should().ThrowAsync<DomainException>()
                .Where(x => x.Error.Code == "QUANTITY_EXCEEDED");
        }
        finally
        {
            await CleanupAsync(fixture, shipmentId, routePlanId, Guid.Empty, profileId);
        }
    }

    [Fact]
    public async Task Create_draft_rejects_stop_from_another_route_plan()
    {
        var actorId = await GetAdminIdAsync();
        var profileId = Guid.NewGuid();
        var fixture = default(DeliveryFixture);
        var shipmentId = Guid.Empty;
        var routePlanId = Guid.Empty;
        var foreignRoutePlanId = Guid.Empty;

        try
        {
            await InsertPhysicalProfileAsync(profileId);
            fixture = await CreateDeliveryFixtureAsync(actorId, "l4b2-stop");
            var setup = await CreateShipmentAndRouteAsync(actorId, fixture!, "l4b2-stop");
            shipmentId = setup.Shipment.Id;
            routePlanId = setup.RoutePlan.Id;
            var foreign = await CreateRouteAsync(actorId, setup.Shipment, "l4b2-foreign-stop");
            foreignRoutePlanId = foreign.RoutePlan.Id;

            await using var context = CreateContext();
            var service = CreateLoadPlanService(context);
            var request = BuildRequest(setup.Shipment, setup.RoutePlan, setup.ShipmentItemId, setup.PackageId, foreign.StopId, 4000);

            var action = () => service.CreateLoadPlanAsync(
                shipmentId,
                request,
                actorId,
                "l4b2-stop-" + Guid.NewGuid(),
                "l4b2-stop",
                CancellationToken.None);

            await action.Should().ThrowAsync<DomainException>()
                .Where(x => x.Error.Code == "PACKAGE_STOP_MISMATCH");
        }
        finally
        {
            await CleanupAsync(fixture, shipmentId, routePlanId, Guid.Empty, profileId, foreignRoutePlanId);
        }
    }

    private static CreateLoadPlanRequest BuildRequest(
        ShipmentDto shipment,
        RoutePlanDto routePlan,
        Guid shipmentItemId,
        Guid packageId,
        Guid stopId,
        decimal quantity)
        => new(
            routePlan.Id,
            routePlan.Version,
            shipment.RowVersion,
            [new CreateLoadUnitRequest(
                "PAL-001",
                "Pallet",
                null,
                false,
                1200,
                800,
                150,
                30,
                230,
                1.44m,
                1,
                "ZONE-A",
                1,
                [new CreateLoadUnitItemRequest(
                    packageId,
                    shipmentItemId,
                    quantity,
                    [new CreateLoadUnitStopAllocationRequest(stopId, quantity, 1)])])]);

    private static async Task<ShipmentRouteSetup> CreateShipmentAndRouteAsync(Guid actorId, DeliveryFixture fixture, string scope)
    {
        await using var context = CreateContext();
        var audit = new EfAuditWriter(context);
        var idempotency = new EfIdempotencyStore(context);
        var logistics = new LogisticsCommandService(context, audit, idempotency);
        var shipment = await logistics.CreateShipmentAsync(
            new CreateShipmentRequest(fixture.DeliveryNoteId, 1),
            actorId,
            scope + "-shipment-" + Guid.NewGuid(),
            scope,
            CancellationToken.None);
        var route = await CreateRouteAsync(logistics, actorId, shipment, scope);
        var shipmentItemId = shipment.Items.Single().Id;
        var packageService = new ShipmentPackageCommandService(context, audit, idempotency);
        var package = await packageService.CreateShipmentPackageAsync(
            shipment.Id,
            new CreateShipmentPackageRequest(shipmentItemId, PackagingId, route.StopId, "Case", 2, 2000, 2, scope + "-package-" + Guid.NewGuid(), true),
            actorId,
            scope + "-package-key-" + Guid.NewGuid(),
            scope,
            CancellationToken.None);
        return new ShipmentRouteSetup(shipment, route.RoutePlan, route.StopId, shipmentItemId, package.Id);
    }

    private static async Task<DeliveryFixture> CreateDeliveryFixtureAsync(Guid actorId, string scope)
    {
        await using var context = CreateContext();
        var now = DateTimeOffset.UtcNow;
        var salesOrderId = Guid.NewGuid();
        var salesOrderItemId = Guid.NewGuid();
        var deliveryNoteId = Guid.NewGuid();
        var deliveryNoteItemId = Guid.NewGuid();
        const decimal quantityBase = 4000m;

        context.SalesOrders.Add(new SalesOrderRecord
        {
            Id = salesOrderId,
            OrderNumber = "SO-L4B2-" + Guid.NewGuid().ToString("N"),
            CustomerId = CustomerId,
            Status = "Approved",
            CurrencyCode = "TRY",
            TotalNet = 100,
            TotalTax = 0,
            TotalGross = 100,
            CreatedBy = actorId,
            CreatedAt = now,
            UpdatedAt = now,
            RowVersion = 1,
        });
        context.SalesOrderItems.Add(new SalesOrderItemRecord
        {
            Id = salesOrderItemId,
            SalesOrderId = salesOrderId,
            ProductId = ProductId,
            OrderedQty = quantityBase,
            ReservedQty = quantityBase,
            ShippedQty = 0,
            CancelledQty = 0,
            RemainingQty = quantityBase,
            EnteredQuantity = 2,
            EnteredPackagingId = PackagingId,
            PackagingSnapshot = "{}",
            PartialDeliveryAllowed = true,
            UnitPrice = 100,
            PriceSnapshot = "{}",
            RowVersion = 1,
        });
        context.DeliveryNotes.Add(new DeliveryNoteRecord
        {
            Id = deliveryNoteId,
            DocumentNumber = "DN-L4B2-" + Guid.NewGuid().ToString("N"),
            SalesOrderId = salesOrderId,
            CustomerId = CustomerId,
            Status = "Issued",
            IssuedAt = now,
            IssuedBy = actorId,
            CreatedAt = now,
            RowVersion = 1,
        });
        context.DeliveryNoteItems.Add(new DeliveryNoteItemRecord
        {
            Id = deliveryNoteItemId,
            DeliveryNoteId = deliveryNoteId,
            SalesOrderItemId = salesOrderItemId,
            ProductId = ProductId,
            QuantityBase = quantityBase,
            EnteredQuantity = 2,
            EnteredPackagingId = PackagingId,
            PackagingSnapshot = "{\"quantityInBaseUom\":2000}",
            ShippedQty = quantityBase,
            InvoicedQty = 0,
            WaivedQty = 0,
            RemainingToInvoice = quantityBase,
            RowVersion = 1,
        });
        await context.SaveChangesAsync();
        return new DeliveryFixture(deliveryNoteId, salesOrderId, salesOrderItemId, deliveryNoteItemId, scope);
    }

    private static async Task<RouteSetup> CreateRouteAsync(Guid actorId, ShipmentDto shipment, string scope)
    {
        await using var context = CreateContext();
        var logistics = new LogisticsCommandService(context, new EfAuditWriter(context), new EfIdempotencyStore(context));
        return await CreateRouteAsync(logistics, actorId, shipment, scope);
    }

    private static async Task<RouteSetup> CreateRouteAsync(ILogisticsCommandService logistics, Guid actorId, ShipmentDto shipment, string scope)
    {
        var start = DateTimeOffset.UtcNow.AddDays(2);
        var route = await logistics.CreateRoutePlanAsync(
            shipment.Id,
            new CreateRoutePlanRequest(start, start.AddHours(2), shipment.RowVersion),
            actorId,
            scope + "-route-" + Guid.NewGuid(),
            scope,
            CancellationToken.None);
        var updated = await logistics.ReplaceStopsAsync(
            route.Id,
            new ReplaceRouteStopsRequest([new RouteStopInput(1, CustomerId, AddressId, start.AddHours(1))]),
            route.RowVersion,
            actorId,
            scope + "-stops-" + Guid.NewGuid(),
            scope,
            CancellationToken.None);
        return new RouteSetup(updated!, updated!.Stops.Single().Id);
    }

    private static LoadPlanCommandService CreateLoadPlanService(FactoryErpDbContext context)
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

    private static async Task InsertPhysicalProfileAsync(Guid profileId)
    {
        await using var context = CreateContext();
        context.ProductPhysicalProfiles.Add(new ProductPhysicalProfileRecord
        {
            Id = profileId,
            ProductId = ProductId,
            EffectiveFrom = DateTimeOffset.UtcNow.AddMinutes(-5),
            LengthMm = 100,
            WidthMm = 100,
            HeightMm = 100,
            NetWeightKg = 1,
            VolumeM3 = 0.001m,
            IsStackable = true,
            MaxStackCount = 5,
            PhysicalPolicySnapshot = "{}",
            IncompatibleGroups = "[]",
            AllowedOrientations = "[\"LWH\"]",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            RowVersion = 1,
        });
        await context.SaveChangesAsync();
    }

    private static async Task CleanupAsync(
        DeliveryFixture? fixture,
        Guid shipmentId,
        Guid routePlanId,
        Guid loadPlanId,
        Guid profileId,
        Guid foreignRoutePlanId = default)
    {
        await using var context = CreateContext();
        var planIds = loadPlanId == Guid.Empty ? Array.Empty<Guid>() : new[] { loadPlanId };
        var unitIds = await context.LoadUnits.Where(x => planIds.Contains(x.LoadPlanId)).Select(x => x.Id).ToArrayAsync();
        var itemIds = await context.LoadUnitItems.Where(x => unitIds.Contains(x.LoadUnitId)).Select(x => x.Id).ToArrayAsync();
        context.LoadUnitStopAllocations.RemoveRange(context.LoadUnitStopAllocations.Where(x => itemIds.Contains(x.LoadUnitItemId)));
        context.LoadUnitItems.RemoveRange(context.LoadUnitItems.Where(x => unitIds.Contains(x.LoadUnitId)));
        context.LoadUnits.RemoveRange(context.LoadUnits.Where(x => unitIds.Contains(x.Id)));
        context.LoadPlans.RemoveRange(context.LoadPlans.Where(x => planIds.Contains(x.Id)));

        if (shipmentId != Guid.Empty)
        {
            context.ShipmentPackages.RemoveRange(context.ShipmentPackages.Where(x => x.ShipmentId == shipmentId));
            context.ShipmentItems.RemoveRange(context.ShipmentItems.Where(x => x.ShipmentId == shipmentId));
            context.Shipments.RemoveRange(context.Shipments.Where(x => x.Id == shipmentId));
        }

        var routeIds = new[] { routePlanId, foreignRoutePlanId }.Where(x => x != Guid.Empty).ToArray();
        context.RouteStops.RemoveRange(context.RouteStops.Where(x => routeIds.Contains(x.RoutePlanId)));
        context.RoutePlans.RemoveRange(context.RoutePlans.Where(x => routeIds.Contains(x.Id)));

        if (fixture is not null)
        {
            context.DeliveryNoteItems.RemoveRange(context.DeliveryNoteItems.Where(x => x.Id == fixture!.DeliveryNoteItemId));
            context.DeliveryNotes.RemoveRange(context.DeliveryNotes.Where(x => x.Id == fixture!.DeliveryNoteId));
            context.SalesOrderItems.RemoveRange(context.SalesOrderItems.Where(x => x.Id == fixture!.SalesOrderItemId));
            context.SalesOrders.RemoveRange(context.SalesOrders.Where(x => x.Id == fixture!.SalesOrderId));
        }

        var profile = await context.ProductPhysicalProfiles.FindAsync(profileId);
        if (profile is not null)
        {
            context.ProductPhysicalProfiles.Remove(profile);
        }

        await context.SaveChangesAsync();
    }

    private sealed record RouteSetup(RoutePlanDto RoutePlan, Guid StopId);
    private sealed record DeliveryFixture(Guid DeliveryNoteId, Guid SalesOrderId, Guid SalesOrderItemId, Guid DeliveryNoteItemId, string Scope);
    private sealed record ShipmentRouteSetup(ShipmentDto Shipment, RoutePlanDto RoutePlan, Guid StopId, Guid ShipmentItemId, Guid PackageId);
}
