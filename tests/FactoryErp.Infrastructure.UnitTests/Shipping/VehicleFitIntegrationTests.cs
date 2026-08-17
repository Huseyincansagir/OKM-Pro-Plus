using FactoryErp.Application.Shipping;
using FactoryErp.Domain.Common;
using FactoryErp.Domain.Shipping;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using FactoryErp.Infrastructure.Shipping;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.UnitTests.Shipping;

public sealed class VehicleFitIntegrationTests
{
    private static readonly Guid ProductId = Guid.Parse("30000000-0000-0000-0000-000000000201");
    private static readonly Guid PackagingId = Guid.Parse("30000000-0000-0000-0000-000000000213");
    private static readonly Guid CustomerId = Guid.Parse("40000000-0000-0000-0000-000000000101");
    private static readonly Guid AddressId = Guid.Parse("40000000-0000-0000-0000-000000000102");

    [Fact]
    public async Task Evaluate_is_deterministic_and_replays_same_snapshot()
    {
        var actorId = await GetAdminIdAsync();
        var profileId = Guid.NewGuid();
        var fixture = default(DeliveryFixture);
        var setup = default(FitSetup);
        var vehicleTypeId = Guid.NewGuid();
        var vehicleCapacityId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var unavailableVehicleId = Guid.NewGuid();

        try
        {
            await InsertPhysicalProfileAsync(profileId);
            fixture = await CreateDeliveryFixtureAsync();
            setup = await CreateFitSetupAsync(actorId, fixture!, vehicleTypeId, vehicleCapacityId, vehicleId, unavailableVehicleId, "l4b3-determinism");

            await using var context = CreateContext();
            var service = new VehicleFitCommandService(context, new EfAuditWriter(context), new EfIdempotencyStore(context));
            var request = new EvaluateVehicleFitRequest(
                setup.LoadPlan.Id,
                setup.LoadPlan.RowVersion,
                [unavailableVehicleId, vehicleId],
                DeterministicFfdEngine.AlgorithmVersion,
                null);
            var key = "l4b3-fit-" + Guid.NewGuid();

            var first = await service.EvaluateVehicleFitAsync(setup.Shipment.Id, request, actorId, key, "l4b3-determinism", CancellationToken.None);
            var replay = await service.EvaluateVehicleFitAsync(setup.Shipment.Id, request, actorId, key, "l4b3-determinism", CancellationToken.None);
            var reversed = await service.EvaluateVehicleFitAsync(
                setup.Shipment.Id,
                request with { VehicleIds = [vehicleId, unavailableVehicleId] },
                actorId,
                "l4b3-fit-reordered-" + Guid.NewGuid(),
                "l4b3-determinism",
                CancellationToken.None);

            first.InputSnapshotHash.Should().NotBeNullOrWhiteSpace();
            replay.InputSnapshotHash.Should().Be(first.InputSnapshotHash);
            replay.Evaluations.Select(x => x.Id).Should().Equal(first.Evaluations.Select(x => x.Id));
            reversed.InputSnapshotHash.Should().Be(first.InputSnapshotHash);
            reversed.Evaluations.Select(x => x.VehicleId).Should().Equal(first.Evaluations.Select(x => x.VehicleId));
            first.Evaluations.Should().ContainSingle(x => x.VehicleId == vehicleId && x.CandidateStatus == "Candidate");
            first.Evaluations.Should().ContainSingle(x => x.VehicleId == unavailableVehicleId && x.RejectionCode == "VEHICLE_NOT_AVAILABLE");

            var candidates = await service.GetVehicleFitCandidatesAsync(setup.Shipment.Id, setup.LoadPlan.Id, CancellationToken.None);
            candidates.Select(x => x.VehicleId).Should().Equal(first.Evaluations.Select(x => x.VehicleId));
        }
        finally
        {
            await CleanupAsync(fixture, setup, profileId, vehicleTypeId, vehicleCapacityId, vehicleId, unavailableVehicleId);
        }
    }

    [Fact]
    public async Task Evaluate_surfaces_missing_physical_profile_as_hard_rejection()
    {
        var actorId = await GetAdminIdAsync();
        var profileId = Guid.NewGuid();
        var fixture = default(DeliveryFixture);
        var setup = default(FitSetup);
        var vehicleTypeId = Guid.NewGuid();
        var vehicleCapacityId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var missingPackageId = Guid.NewGuid();

        try
        {
            await InsertPhysicalProfileAsync(profileId);
            fixture = await CreateDeliveryFixtureAsync();
            setup = await CreateFitSetupAsync(actorId, fixture!, vehicleTypeId, vehicleCapacityId, vehicleId, null, "l4b3-missing-profile");
            await InsertMissingPhysicalPackageAsync(setup.Shipment.Id, setup.ShipmentItemId, missingPackageId);

            await using var context = CreateContext();
            var service = new VehicleFitCommandService(context, new EfAuditWriter(context), new EfIdempotencyStore(context));
            var result = await service.EvaluateVehicleFitAsync(
                setup.Shipment.Id,
                new EvaluateVehicleFitRequest(setup.LoadPlan.Id, setup.LoadPlan.RowVersion, [vehicleId], null, null),
                actorId,
                "l4b3-missing-" + Guid.NewGuid(),
                "l4b3-missing-profile",
                CancellationToken.None);

            result.MissingPhysicalProfilePackageIds.Should().Contain(missingPackageId);
            result.Evaluations.Should().ContainSingle(x => x.RejectionCode == "PHYSICAL_PROFILE_MISSING");
            result.Evaluations.Single().CandidateStatus.Should().Be("Rejected");
        }
        finally
        {
            await CleanupAsync(fixture, setup, profileId, vehicleTypeId, vehicleCapacityId, vehicleId, null, missingPackageId);
        }
    }

    private static async Task<FitSetup> CreateFitSetupAsync(
        Guid actorId,
        DeliveryFixture fixture,
        Guid vehicleTypeId,
        Guid vehicleCapacityId,
        Guid vehicleId,
        Guid? unavailableVehicleId,
        string scope)
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
        var start = DateTimeOffset.UtcNow.AddDays(2);
        var route = await logistics.CreateRoutePlanAsync(
            shipment.Id,
            new CreateRoutePlanRequest(start, start.AddHours(2), shipment.RowVersion),
            actorId,
            scope + "-route-" + Guid.NewGuid(),
            scope,
            CancellationToken.None);
        var routeWithStop = await logistics.ReplaceStopsAsync(
            route.Id,
            new ReplaceRouteStopsRequest([new RouteStopInput(1, CustomerId, AddressId, start.AddHours(1))]),
            route.RowVersion,
            actorId,
            scope + "-stop-" + Guid.NewGuid(),
            scope,
            CancellationToken.None);
        var stopId = routeWithStop!.Stops.Single().Id;
        var shipmentItemId = shipment.Items.Single().Id;
        var packageService = new ShipmentPackageCommandService(context, audit, idempotency);
        var package = await packageService.CreateShipmentPackageAsync(
            shipment.Id,
            new CreateShipmentPackageRequest(shipmentItemId, PackagingId, stopId, "Case", 1, 4000, 1, scope + "-package-" + Guid.NewGuid(), false),
            actorId,
            scope + "-package-key-" + Guid.NewGuid(),
            scope,
            CancellationToken.None);
        var planService = new LoadPlanCommandService(context, audit, idempotency);
        var plan = await planService.CreateLoadPlanAsync(
            shipment.Id,
            new CreateLoadPlanRequest(
                route.Id,
                route.Version,
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
                    35,
                    1.44m,
                    1,
                    "ZONE-A",
                    1,
                    [new CreateLoadUnitItemRequest(
                        package.Id,
                        shipmentItemId,
                        package.QuantityBase,
                        [new CreateLoadUnitStopAllocationRequest(stopId, package.QuantityBase, 1)])])]),
            actorId,
            scope + "-plan-key-" + Guid.NewGuid(),
            scope,
            CancellationToken.None);

        context.VehicleTypes.Add(new VehicleTypeRecord
        {
            Id = vehicleTypeId,
            Code = "L4B3-" + vehicleTypeId.ToString("N")[..8],
            Name = "L4-B3 Test Type",
            IsActive = true,
        });
        context.VehicleCapacities.Add(new VehicleCapacityRecord
        {
            Id = vehicleCapacityId,
            VehicleTypeId = vehicleTypeId,
            EffectiveFrom = DateTimeOffset.UtcNow.AddMinutes(-5),
            MaxGrossWeight = 1000,
            TareWeight = 100,
            MaxUsableVolume = 30,
            MaxPalletCount = 10,
            MaxLoadHeight = 1800,
            CapacityPolicySnapshot = "{}",
            RowVersion = 1,
        });
        context.VehicleCapacityZones.Add(new VehicleCapacityZoneRecord
        {
            Id = Guid.NewGuid(),
            VehicleCapacityId = vehicleCapacityId,
            ZoneCode = "ZONE-A",
            LengthMm = 1200,
            WidthMm = 800,
            MaxLoadKg = 1000,
            SequenceNo = 1,
        });
        context.Vehicles.Add(new VehicleRecord
        {
            Id = vehicleId,
            VehicleTypeId = vehicleTypeId,
            PlateNumber = "L4B3-" + vehicleId.ToString("N")[..6],
            Status = "Available",
            LastStatusAt = DateTimeOffset.UtcNow,
            RowVersion = 1,
        });
        if (unavailableVehicleId.HasValue)
        {
            var unavailableTypeId = Guid.NewGuid();
            context.VehicleTypes.Add(new VehicleTypeRecord
            {
                Id = unavailableTypeId,
                Code = "L4B3-U-" + unavailableVehicleId.Value.ToString("N")[..6],
                Name = "L4-B3 Unavailable Type",
                IsActive = true,
            });
            context.Vehicles.Add(new VehicleRecord
            {
                Id = unavailableVehicleId.Value,
                VehicleTypeId = unavailableTypeId,
                PlateNumber = "L4B3-U-" + unavailableVehicleId.Value.ToString("N")[..6],
                Status = "Maintenance",
                LastStatusAt = DateTimeOffset.UtcNow,
                RowVersion = 1,
            });
        }
        await context.SaveChangesAsync();
        return new FitSetup(shipment, routeWithStop, shipmentItemId, package.Id, plan);
    }

    private static async Task<DeliveryFixture> CreateDeliveryFixtureAsync()
    {
        await using var context = CreateContext();
        var now = DateTimeOffset.UtcNow;
        var salesOrderId = Guid.NewGuid();
        var salesOrderItemId = Guid.NewGuid();
        var deliveryNoteId = Guid.NewGuid();
        var deliveryNoteItemId = Guid.NewGuid();
        const decimal quantityBase = 4000m;
        var issuer = await context.Users.Select(x => x.Id).FirstAsync();
        context.SalesOrders.Add(new SalesOrderRecord
        {
            Id = salesOrderId,
            OrderNumber = "SO-L4B3-" + Guid.NewGuid().ToString("N"),
            CustomerId = CustomerId,
            Status = "Approved",
            CurrencyCode = "TRY",
            TotalNet = 100,
            TotalTax = 0,
            TotalGross = 100,
            CreatedBy = issuer,
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
            RemainingQty = quantityBase,
            EnteredQuantity = 1,
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
            DocumentNumber = "DN-L4B3-" + Guid.NewGuid().ToString("N"),
            SalesOrderId = salesOrderId,
            CustomerId = CustomerId,
            Status = "Issued",
            IssuedAt = now,
            IssuedBy = issuer,
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
            EnteredQuantity = 1,
            EnteredPackagingId = PackagingId,
            PackagingSnapshot = "{\"quantityInBaseUom\":4000}",
            ShippedQty = quantityBase,
            RemainingToInvoice = quantityBase,
            RowVersion = 1,
        });
        await context.SaveChangesAsync();
        return new DeliveryFixture(deliveryNoteId, salesOrderId, salesOrderItemId, deliveryNoteItemId);
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

    private static async Task InsertMissingPhysicalPackageAsync(Guid shipmentId, Guid shipmentItemId, Guid packageId)
    {
        await using var context = CreateContext();
        context.ShipmentPackages.Add(new ShipmentPackageRecord
        {
            Id = packageId,
            ShipmentId = shipmentId,
            ShipmentItemId = shipmentItemId,
            PackageType = "Case",
            PackageCount = 1,
            QuantityBasePerPackage = 1,
            QuantityBase = 1,
            PackagingSnapshot = "{}",
            PhysicalSnapshot = "{}",
            SplitAllowed = true,
            Status = "Available",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            RowVersion = 1,
        });
        await context.SaveChangesAsync();
    }

    private static async Task<Guid> GetAdminIdAsync()
    {
        await using var context = CreateContext();
        return await context.Users.Where(x => x.UserName == "admin").Select(x => x.Id).SingleAsync();
    }

    private static async Task CleanupAsync(
        DeliveryFixture? fixture,
        FitSetup? setup,
        Guid profileId,
        Guid vehicleTypeId,
        Guid vehicleCapacityId,
        Guid vehicleId,
        Guid? unavailableVehicleId,
        Guid missingPackageId = default)
    {
        await using var context = CreateContext();
        if (setup is not null)
        {
            var evaluationPlanId = setup!.LoadPlan.Id;
            context.VehicleFitEvaluations.RemoveRange(context.VehicleFitEvaluations.Where(x => x.LoadPlanId == evaluationPlanId));
            var unitIds = await context.LoadUnits.Where(x => x.LoadPlanId == evaluationPlanId).Select(x => x.Id).ToArrayAsync();
            var itemIds = await context.LoadUnitItems.Where(x => unitIds.Contains(x.LoadUnitId)).Select(x => x.Id).ToArrayAsync();
            context.LoadUnitStopAllocations.RemoveRange(context.LoadUnitStopAllocations.Where(x => itemIds.Contains(x.LoadUnitItemId)));
            context.LoadUnitItems.RemoveRange(context.LoadUnitItems.Where(x => unitIds.Contains(x.LoadUnitId)));
            context.LoadUnits.RemoveRange(context.LoadUnits.Where(x => unitIds.Contains(x.Id)));
            context.LoadPlans.RemoveRange(context.LoadPlans.Where(x => x.Id == evaluationPlanId));
            context.ShipmentPackages.RemoveRange(context.ShipmentPackages.Where(x => x.ShipmentId == setup!.Shipment.Id));
            if (missingPackageId != Guid.Empty)
            {
                context.ShipmentPackages.RemoveRange(context.ShipmentPackages.Where(x => x.Id == missingPackageId));
            }
            context.ShipmentItems.RemoveRange(context.ShipmentItems.Where(x => x.ShipmentId == setup!.Shipment.Id));
            context.Shipments.RemoveRange(context.Shipments.Where(x => x.Id == setup!.Shipment.Id));
            context.RouteStops.RemoveRange(context.RouteStops.Where(x => x.RoutePlanId == setup!.RoutePlan.Id));
            context.RoutePlans.RemoveRange(context.RoutePlans.Where(x => x.Id == setup!.RoutePlan.Id));
        }

        context.VehicleCapacityZones.RemoveRange(context.VehicleCapacityZones.Where(x => x.VehicleCapacityId == vehicleCapacityId));
        context.VehicleFitEvaluations.RemoveRange(context.VehicleFitEvaluations.Where(x => x.VehicleId == vehicleId || (unavailableVehicleId.HasValue && x.VehicleId == unavailableVehicleId.Value)));
        context.Vehicles.RemoveRange(context.Vehicles.Where(x => x.Id == vehicleId || (unavailableVehicleId.HasValue && x.Id == unavailableVehicleId.Value)));
        context.VehicleCapacities.RemoveRange(context.VehicleCapacities.Where(x => x.Id == vehicleCapacityId));
        context.VehicleTypes.RemoveRange(context.VehicleTypes.Where(x => x.Id == vehicleTypeId));

        if (fixture is not null)
        {
            context.DeliveryNoteItems.RemoveRange(context.DeliveryNoteItems.Where(x => x.Id == fixture!.DeliveryNoteItemId));
            context.DeliveryNotes.RemoveRange(context.DeliveryNotes.Where(x => x.Id == fixture!.DeliveryNoteId));
            context.SalesOrderItems.RemoveRange(context.SalesOrderItems.Where(x => x.Id == fixture!.SalesOrderItemId));
            context.SalesOrders.RemoveRange(context.SalesOrders.Where(x => x.Id == fixture!.SalesOrderId));
        }
        context.ProductPhysicalProfiles.RemoveRange(context.ProductPhysicalProfiles.Where(x => x.Id == profileId));
        await context.SaveChangesAsync();
    }

    private static FactoryErpDbContext CreateContext()
    {
        var connectionString = Environment.GetEnvironmentVariable("FactoryErpTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__FactoryErp")
            ?? "Host=127.0.0.1;Port=5432;Database=factory_erp_g1;Username=factory_erp;Password=dev_only_change_me";
        return new FactoryErpDbContext(new DbContextOptionsBuilder<FactoryErpDbContext>().UseNpgsql(connectionString).Options);
    }

    private sealed record DeliveryFixture(Guid DeliveryNoteId, Guid SalesOrderId, Guid SalesOrderItemId, Guid DeliveryNoteItemId);
    private sealed record FitSetup(ShipmentDto Shipment, RoutePlanDto RoutePlan, Guid ShipmentItemId, Guid PackageId, LoadPlanDto LoadPlan);
}
