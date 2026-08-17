using FactoryErp.Application.Abstractions.Persistence;
using FactoryErp.Application.Shipping;
using FactoryErp.Domain.Common;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using FactoryErp.Infrastructure.Shipping;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.UnitTests.Shipping;

public sealed class DispatchRunIntegrationTests
{
    [Fact]
    public async Task Prepare_replays_same_payload_and_rejects_same_key_with_different_payload()
    {
        var fixture = await CreateFixtureAsync();
        try
        {
            var handler = CreateHandler();
            var command = fixture.PrepareCommand("b6-idempotency");
            var first = await handler.HandleAsync(command);
            var replay = await handler.HandleAsync(command);
            replay.Id.Should().Be(first.Id);
            replay.RowVersion.Should().Be(first.RowVersion);

            var mismatch = command with { PlannedDepartureAt = command.PlannedDepartureAt!.Value.AddMinutes(1) };
            var action = () => handler.HandleAsync(mismatch);
            await action.Should().ThrowAsync<DomainException>().WithMessage("*farklı payload*");
        }
        finally
        {
            await CleanupAsync(fixture);
        }
    }

    [Fact]
    public async Task Prepare_rejects_when_b5_verification_is_not_completed()
    {
        var fixture = await CreateFixtureAsync(completedVerification: false);
        try
        {
            var command = fixture.PrepareCommand("b6-no-verification");
            var action = () => CreateHandler().HandleAsync(command);
            await action.Should().ThrowAsync<DomainException>().WithMessage("*Tamamlanmış LoadVerificationSession bulunamadı*");
        }
        finally
        {
            await CleanupAsync(fixture);
        }
    }

    [Fact]
    public async Task Prepare_depart_execute_and_complete_updates_transactional_projections()
    {
        var fixture = await CreateFixtureAsync(stopCount: 2);
        try
        {
            var handler = CreateHandler();
            var prepared = await handler.HandleAsync(fixture.PrepareCommand("b6-prepare"));
            prepared.Status.Should().Be("Prepared");
            prepared.Stops.Should().OnlyContain(x => x.Status == "Pending");

            var confirmed = await handler.HandleAsync(new ConfirmDispatchCommand(
                prepared.Id, prepared.RowVersion, fixture.ActorId, "b6-confirm", "b6-test"));
            confirmed.Status.Should().Be("Dispatched");

            var departed = await handler.HandleAsync(new DepartDispatchRunCommand(
                confirmed.Id,
                DateTimeOffset.UtcNow,
                "İstanbul depo kapısı",
                null,
                null,
                confirmed.RowVersion,
                fixture.ActorId,
                "b6-depart",
                "b6-test"));
            departed.Status.Should().Be("InTransit");

            var first = departed.Stops.OrderBy(x => x.SequenceNo).First();
            var arrived = await handler.HandleAsync(new ArriveAtStopCommand(
                departed.Id,
                first.RouteStopId,
                DateTimeOffset.UtcNow,
                "İlk durak",
                null,
                null,
                departed.RowVersion,
                fixture.ActorId,
                "b6-arrive-1",
                "b6-test"));
            arrived.Stops.Single(x => x.RouteStopId == first.RouteStopId).Status.Should().Be("Arrived");

            var departedStop = await handler.HandleAsync(new DepartStopCommand(
                arrived.Id,
                first.RouteStopId,
                DateTimeOffset.UtcNow,
                "İlk durak",
                null,
                null,
                arrived.RowVersion,
                fixture.ActorId,
                "b6-depart-stop-1",
                "b6-test"));
            departedStop.Stops.Single(x => x.RouteStopId == first.RouteStopId).Status.Should().Be("Departed");

            var second = departedStop.Stops.OrderBy(x => x.SequenceNo).Last();
            var skipped = await handler.HandleAsync(new SkipStopCommand(
                departedStop.Id,
                second.RouteStopId,
                DateTimeOffset.UtcNow,
                "Müşteri adresi kapalı",
                departedStop.RowVersion,
                fixture.ActorId,
                "b6-skip-2",
                "b6-test"));
            skipped.Stops.Single(x => x.RouteStopId == second.RouteStopId).Status.Should().Be("Skipped");

            var completed = await handler.HandleAsync(new CompleteRouteCommand(
                skipped.Id,
                DateTimeOffset.UtcNow,
                skipped.RowVersion,
                fixture.ActorId,
                "b6-complete",
                "b6-test"));
            completed.Status.Should().Be("Completed");
            completed.Events.Select(x => x.EventType).Should().Contain(new[] { "Departed", "ArrivedAtStop", "DepartedStop", "SkippedStop", "RouteCompleted" });

            await using var context = CreateContext();
            (await context.Shipments.Where(x => x.Id == fixture.ShipmentId).Select(x => x.Status).SingleAsync()).Should().Be("InTransit");
            (await context.RoutePlans.Where(x => x.Id == fixture.RoutePlanId).Select(x => x.Status).SingleAsync()).Should().Be("Completed");
            (await context.Vehicles.Where(x => x.Id == fixture.VehicleId).Select(x => new { x.Status, x.CurrentRoutePlanId }).SingleAsync()).Should().BeEquivalentTo(new { Status = "Available", CurrentRoutePlanId = (Guid?)null });
        }
        finally
        {
            await CleanupAsync(fixture);
        }
    }

    [Fact]
    public async Task Prepare_rejects_expired_driver_license()
    {
        var fixture = await CreateFixtureAsync(licenseExpiry: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));
        try
        {
            var action = () => CreateHandler().HandleAsync(fixture.PrepareCommand("b6-expired-license"));
            await action.Should().ThrowAsync<DomainException>().WithMessage("*lisansı departure tarihinde geçerli değil*");
        }
        finally
        {
            await CleanupAsync(fixture);
        }
    }

    [Fact]
    public async Task Duplicate_arrival_is_rejected_by_route_stop_state_guard()
    {
        var fixture = await CreateFixtureAsync();
        try
        {
            var handler = CreateHandler();
            var prepared = await handler.HandleAsync(fixture.PrepareCommand("b6-dup-prepare"));
            var confirmed = await handler.HandleAsync(new ConfirmDispatchCommand(prepared.Id, prepared.RowVersion, fixture.ActorId, "b6-dup-confirm", "b6-test"));
            var departed = await handler.HandleAsync(new DepartDispatchRunCommand(confirmed.Id, DateTimeOffset.UtcNow, null, null, null, confirmed.RowVersion, fixture.ActorId, "b6-dup-depart", "b6-test"));
            var stop = departed.Stops.Single();
            var arrived = await handler.HandleAsync(new ArriveAtStopCommand(departed.Id, stop.RouteStopId, DateTimeOffset.UtcNow, null, null, null, departed.RowVersion, fixture.ActorId, "b6-dup-arrive-1", "b6-test"));

            var action = () => handler.HandleAsync(new ArriveAtStopCommand(arrived.Id, stop.RouteStopId, DateTimeOffset.UtcNow, null, null, null, arrived.RowVersion, fixture.ActorId, "b6-dup-arrive-2", "b6-test"));
            await action.Should().ThrowAsync<DomainException>().WithMessage("*pending stop*");
        }
        finally
        {
            await CleanupAsync(fixture);
        }
    }

    [Fact]
    public async Task Concurrent_prepare_on_same_vehicle_returns_one_success_and_one_explicit_conflict()
    {
        var fixture = await CreateFixtureAsync();
        try
        {
            var commandOne = fixture.PrepareCommand("b6-race-1");
            var commandTwo = fixture.PrepareCommand("b6-race-2");
            var taskOne = Task.Run(async () => await CreateHandler().HandleAsync(commandOne));
            var taskTwo = Task.Run(async () => await CreateHandler().HandleAsync(commandTwo));
            var outcomes = await Task.WhenAll(
                CaptureAsync(taskOne),
                CaptureAsync(taskTwo));

            outcomes.Count(x => x.Success).Should().Be(1);
            outcomes.Count(x => !x.Success).Should().Be(1);
            outcomes.Single(x => !x.Success).Exception.Should().Match<Exception>(x => x.Message.Contains("aktif DispatchRun", StringComparison.Ordinal));
        }
        finally
        {
            await CleanupAsync(fixture);
        }
    }

    private static async Task<Outcome> CaptureAsync(Task<DispatchRunDto> task)
    {
        try
        {
            await task;
            return new(true, null);
        }
        catch (Exception exception)
        {
            return new(false, exception);
        }
    }

    private static DispatchRunCommandHandler CreateHandler()
    {
        var context = CreateContext();
        return new DispatchRunCommandHandler(context, new EfAuditWriter(context), new EfIdempotencyStore(context));
    }

    private static async Task<Fixture> CreateFixtureAsync(
        bool completedVerification = true,
        int stopCount = 1,
        DateOnly? licenseExpiry = null)
    {
        await using var context = CreateContext();
        var now = DateTimeOffset.UtcNow;
        var actorId = await context.Users.Where(x => x.UserName == "admin").Select(x => x.Id).SingleAsync();
        var customerId = await context.Customers.Where(x => x.CustomerCode == "DEMO-001").Select(x => x.Id).SingleAsync();
        var addressId = await context.CustomerAddresses.Where(x => x.CustomerId == customerId && x.IsActive).Select(x => x.Id).FirstAsync();
        var productId = Guid.Parse("30000000-0000-0000-0000-000000000201");
        var packagingId = Guid.Parse("30000000-0000-0000-0000-000000000213");
        var suffix = Guid.NewGuid().ToString("N");
        var salesOrderId = Guid.NewGuid();
        var salesOrderItemId = Guid.NewGuid();
        var deliveryNoteId = Guid.NewGuid();
        var deliveryNoteItemId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var routePlanId = Guid.NewGuid();
        var loadPlanId = Guid.NewGuid();
        var vehicleTypeId = Guid.NewGuid();
        var vehicleCapacityId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var stopIds = Enumerable.Range(1, stopCount).Select(_ => Guid.NewGuid()).ToArray();

        var order = new SalesOrderRecord
        {
            Id = salesOrderId,
            OrderNumber = $"SO-B6-{suffix}",
            CustomerId = customerId,
            Status = "Fulfilled",
            CurrencyCode = "TRY",
            RowVersion = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
        order.Items.Add(new SalesOrderItemRecord
        {
            Id = salesOrderItemId,
            SalesOrderId = salesOrderId,
            ProductId = productId,
            OrderedQty = 1000m,
            ShippedQty = 1000m,
            RemainingQty = 0m,
            EnteredQuantity = 10m,
            EnteredPackagingId = packagingId,
            PackagingSnapshot = "{}",
            PartialDeliveryAllowed = true,
            PriceSnapshot = "{}",
            RowVersion = 1,
        });
        var deliveryNote = new DeliveryNoteRecord
        {
            Id = deliveryNoteId,
            DocumentNumber = $"DN-B6-{suffix}",
            SalesOrderId = salesOrderId,
            CustomerId = customerId,
            Status = "Issued",
            IssuedAt = now,
            CreatedAt = now,
            RowVersion = 1,
        };
        deliveryNote.Items.Add(new DeliveryNoteItemRecord
        {
            Id = deliveryNoteItemId,
            DeliveryNoteId = deliveryNoteId,
            SalesOrderItemId = salesOrderItemId,
            ProductId = productId,
            QuantityBase = 1000m,
            EnteredQuantity = 10m,
            EnteredPackagingId = packagingId,
            PackagingSnapshot = "{}",
            ShippedQty = 1000m,
            RemainingToInvoice = 1000m,
            RowVersion = 1,
        });
        var shipment = new ShipmentRecord
        {
            Id = shipmentId,
            DeliveryNoteId = deliveryNoteId,
            CustomerId = customerId,
            Status = "Loaded",
            RowVersion = 1,
            CreatedAt = now,
        };
        shipment.Items.Add(new ShipmentItemRecord
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentId,
            DeliveryNoteItemId = deliveryNoteItemId,
            ProductId = productId,
            QuantityBase = 1000m,
            PackagingSnapshot = "{}",
        });
        var routePlan = new RoutePlanRecord
        {
            Id = routePlanId,
            ShipmentId = shipmentId,
            Status = "Locked",
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now,
            RowVersion = 1,
        };
        for (var index = 0; index < stopCount; index++)
        {
            routePlan.Stops.Add(new RouteStopRecord
            {
                Id = stopIds[index],
                RoutePlanId = routePlanId,
                SequenceNo = index + 1,
                CustomerId = customerId,
                AddressId = addressId,
                Status = "Pending",
                RowVersion = 1,
            });
        }
        var loadPlan = new LoadPlanRecord
        {
            Id = loadPlanId,
            ShipmentId = shipmentId,
            RoutePlanId = routePlanId,
            RoutePlanVersion = 1,
            Version = 1,
            VehicleId = vehicleId,
            VehicleCapacityId = vehicleCapacityId,
            Status = "Locked",
            FeasibilityStatus = "Feasible",
            InputSnapshotHash = "b6-fixture-snapshot",
            CapacitySnapshot = "{}",
            UtilizationSnapshot = "{}",
            ValidationSummary = "{}",
            LockedBy = actorId,
            LockedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            RowVersion = 1,
        };
        var vehicleType = new VehicleTypeRecord { Id = vehicleTypeId, Code = $"B6-{suffix}", Name = "B6 Test Vehicle", IsActive = true };
        var capacity = new VehicleCapacityRecord
        {
            Id = vehicleCapacityId,
            VehicleTypeId = vehicleTypeId,
            EffectiveFrom = now.AddDays(-1),
            MaxGrossWeight = 1000m,
            TareWeight = 100m,
            MaxUsableVolume = 50m,
            MaxPalletCount = 10,
            MaxLoadHeight = 2000m,
            CapacityPolicySnapshot = "{}",
        };
        var vehicle = new VehicleRecord
        {
            Id = vehicleId,
            VehicleTypeId = vehicleTypeId,
            PlateNumber = $"B6 {suffix[..6]}",
            Status = "Available",
            LastStatusAt = now,
            RowVersion = 1,
        };
        var driver = new DriverRecord
        {
            Id = driverId,
            FullName = "B6 Test Driver",
            LicenseNumber = $"B6-{suffix}",
            LicenseExpiry = licenseExpiry ?? DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            Status = "Active",
            IsActive = true,
            RowVersion = 1,
        };
        context.SalesOrders.Add(order);
        context.DeliveryNotes.Add(deliveryNote);
        context.Shipments.Add(shipment);
        context.RoutePlans.Add(routePlan);
        context.LoadPlans.Add(loadPlan);
        context.VehicleTypes.Add(vehicleType);
        context.VehicleCapacities.Add(capacity);
        context.Vehicles.Add(vehicle);
        context.Drivers.Add(driver);
        if (completedVerification)
        {
            context.LoadVerificationSessions.Add(new LoadVerificationSessionRecord
            {
                Id = sessionId,
                LoadPlanId = loadPlanId,
                ShipmentId = shipmentId,
                Status = "Completed",
                StartedBy = actorId,
                CompletedBy = actorId,
                StartedAt = now,
                CompletedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
                RowVersion = 1,
            });
        }
        await context.SaveChangesAsync();
        return new Fixture(actorId, shipmentId, routePlanId, loadPlanId, vehicleId, driverId, deliveryNoteId, salesOrderId, vehicleTypeId, vehicleCapacityId, sessionId, stopIds);
    }

    private static async Task CleanupAsync(Fixture fixture)
    {
        await using var context = CreateContext();
        var runIds = await context.DispatchRuns.Where(x => x.ShipmentId == fixture.ShipmentId).Select(x => x.Id).ToArrayAsync();
        await context.RouteExecutionEvents.Where(x => runIds.Contains(x.DispatchRunId)).ExecuteDeleteAsync();
        await context.DispatchRuns.Where(x => runIds.Contains(x.Id)).ExecuteDeleteAsync();
        await context.LoadVerificationSessions.Where(x => x.Id == fixture.SessionId).ExecuteDeleteAsync();
        await context.LoadPlans.Where(x => x.Id == fixture.LoadPlanId).ExecuteDeleteAsync();
        await context.RouteStops.Where(x => x.RoutePlanId == fixture.RoutePlanId).ExecuteDeleteAsync();
        await context.RoutePlans.Where(x => x.Id == fixture.RoutePlanId).ExecuteDeleteAsync();
        await context.ShipmentItems.Where(x => x.ShipmentId == fixture.ShipmentId).ExecuteDeleteAsync();
        await context.Shipments.Where(x => x.Id == fixture.ShipmentId).ExecuteDeleteAsync();
        await context.DeliveryNoteItems.Where(x => x.DeliveryNoteId == fixture.DeliveryNoteId).ExecuteDeleteAsync();
        await context.DeliveryNotes.Where(x => x.Id == fixture.DeliveryNoteId).ExecuteDeleteAsync();
        await context.SalesOrderItems.Where(x => x.SalesOrderId == fixture.SalesOrderId).ExecuteDeleteAsync();
        await context.SalesOrders.Where(x => x.Id == fixture.SalesOrderId).ExecuteDeleteAsync();
        await context.Vehicles.Where(x => x.Id == fixture.VehicleId).ExecuteDeleteAsync();
        await context.VehicleCapacities.Where(x => x.Id == fixture.VehicleCapacityId).ExecuteDeleteAsync();
        await context.VehicleTypes.Where(x => x.Id == fixture.VehicleTypeId).ExecuteDeleteAsync();
        await context.Drivers.Where(x => x.Id == fixture.DriverId).ExecuteDeleteAsync();
    }

    private static FactoryErpDbContext CreateContext()
    {
        var connectionString = Environment.GetEnvironmentVariable("FactoryErpTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__FactoryErp")
            ?? "Host=127.0.0.1;Port=5432;Database=factory_erp_g1;Username=factory_erp;Password=dev_only_change_me";
        return new FactoryErpDbContext(new DbContextOptionsBuilder<FactoryErpDbContext>().UseNpgsql(connectionString).Options);
    }

    private sealed record Fixture(
        Guid ActorId,
        Guid ShipmentId,
        Guid RoutePlanId,
        Guid LoadPlanId,
        Guid VehicleId,
        Guid DriverId,
        Guid DeliveryNoteId,
        Guid SalesOrderId,
        Guid VehicleTypeId,
        Guid VehicleCapacityId,
        Guid SessionId,
        IReadOnlyCollection<Guid> StopIds)
    {
        public PrepareDispatchRunCommand PrepareCommand(string key)
            => new(
                ShipmentId,
                LoadPlanId,
                RoutePlanId,
                VehicleId,
                DriverId,
                DateTimeOffset.UtcNow.AddMinutes(10),
                StopIds.Select((id, index) => new DispatchStopInput(id, index + 1)).ToArray(),
                1,
                1,
                1,
                ActorId,
                key,
                "b6-test");
    }

    private sealed record Outcome(bool Success, Exception? Exception);
}
