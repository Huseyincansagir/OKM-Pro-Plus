using FactoryErp.Domain.Common;
using FactoryErp.Domain.Shipping;
using FluentAssertions;

namespace FactoryErp.Domain.UnitTests.Shipping;

public sealed class DispatchRunTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 17, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid ShipmentId = Guid.Parse("65000000-0000-0000-0000-000000000001");
    private static readonly Guid LoadPlanId = Guid.Parse("65000000-0000-0000-0000-000000000002");
    private static readonly Guid RoutePlanId = Guid.Parse("65000000-0000-0000-0000-000000000003");
    private static readonly Guid VehicleId = Guid.Parse("65000000-0000-0000-0000-000000000004");
    private static readonly Guid DriverId = Guid.Parse("65000000-0000-0000-0000-000000000005");
    private static readonly Guid StopOne = Guid.Parse("65000000-0000-0000-0000-000000000011");
    private static readonly Guid StopTwo = Guid.Parse("65000000-0000-0000-0000-000000000012");
    private static readonly Guid ActorId = Guid.Parse("65000000-0000-0000-0000-000000000099");

    [Fact]
    public void Create_prepared_requires_unique_pending_route_stops()
    {
        var action = () => DispatchRun.CreatePrepared(
            Guid.NewGuid(), Now, ShipmentId, LoadPlanId, RoutePlanId, VehicleId, DriverId, null,
            [new DispatchRunStop(StopOne, 1), new DispatchRunStop(StopOne, 2)]);

        action.Should().Throw<DomainException>().Which.Error.Code.Should().Be("ROUTE_STOP_DUPLICATE");
    }

    [Fact]
    public void Confirm_and_depart_moves_run_to_in_transit_and_creates_first_event()
    {
        var run = Prepared();
        run.ConfirmDispatch(ActorId, Now.AddMinutes(1));

        var routeEvent = run.Depart(ActorId, Now.AddMinutes(2), "dep-1", "corr-1");

        run.Status.Should().Be(DispatchRunStatus.InTransit);
        run.ActualDepartedAt.Should().Be(Now.AddMinutes(2));
        routeEvent.EventType.Should().Be(RouteExecutionEventType.Departed);
        routeEvent.SequenceNo.Should().Be(1);
    }

    [Fact]
    public void Departure_cannot_be_repeated()
    {
        var run = Departed();
        var action = () => run.Depart(ActorId, Now.AddMinutes(3), "dep-2", "corr-2");

        action.Should().Throw<DomainException>().Which.Error.Code.Should().Be("DISPATCH_ALREADY_DEPARTED");
    }

    [Fact]
    public void Stop_execution_requires_deterministic_sequence()
    {
        var run = Departed();
        var action = () => run.ArriveAtStop(StopTwo, ActorId, Now.AddMinutes(3), "arrive-2", "corr-3");

        action.Should().Throw<DomainException>().Which.Error.Code.Should().Be("ROUTE_STOP_OUT_OF_ORDER");
    }

    [Fact]
    public void Arrival_departure_and_completion_move_the_route_to_completed()
    {
        var run = Departed();
        run.ArriveAtStop(StopOne, ActorId, Now.AddMinutes(3), "arrive-1", "corr-3");
        run.DepartStop(StopOne, ActorId, Now.AddMinutes(4), "depart-stop-1", "corr-4");
        run.ArriveAtStop(StopTwo, ActorId, Now.AddMinutes(5), "arrive-2", "corr-5");
        run.DepartStop(StopTwo, ActorId, Now.AddMinutes(6), "depart-stop-2", "corr-6");

        run.CompleteRoute(ActorId, Now.AddMinutes(7), "complete-1", "corr-7");

        run.Status.Should().Be(DispatchRunStatus.Completed);
        run.Events.Should().ContainSingle(x => x.EventType == RouteExecutionEventType.RouteCompleted);
    }

    [Fact]
    public void Skip_requires_reason_and_allows_completion_when_reasoned()
    {
        var run = Departed();
        var missingReason = () => run.SkipStop(StopOne, ActorId, Now.AddMinutes(3), " ", "skip-1", "corr-3");
        missingReason.Should().Throw<DomainException>().Which.Error.Code.Should().Be("ROUTE_STOP_REASON_REQUIRED");

        run.SkipStop(StopOne, ActorId, Now.AddMinutes(3), "Müşteri adresi kapalı", "skip-1", "corr-3");
        run.SkipStop(StopTwo, ActorId, Now.AddMinutes(4), "Aynı rota istisnası", "skip-2", "corr-4");
        run.CompleteRoute(ActorId, Now.AddMinutes(5), "complete-2", "corr-5");

        run.Status.Should().Be(DispatchRunStatus.Completed);
    }

    [Fact]
    public void Prepared_run_can_be_cancelled_but_completed_run_is_terminal()
    {
        var prepared = Prepared();
        prepared.Cancel(ActorId, Now.AddMinutes(1), "Araç arızası", "cancel-1", "corr-1");
        prepared.Status.Should().Be(DispatchRunStatus.Cancelled);

        var completed = Completed();
        var action = () => completed.Cancel(ActorId, Now.AddMinutes(9), "Geçersiz", "cancel-2", "corr-9");
        action.Should().Throw<DomainException>().Which.Error.Code.Should().Be("DISPATCH_INVALID_STATE");
    }

    [Fact]
    public void Event_idempotency_key_and_time_order_are_guarded()
    {
        var run = Departed();
        var duplicateKey = () => run.ArriveAtStop(StopOne, ActorId, Now.AddMinutes(3), "dep-1", "corr-3");
        duplicateKey.Should().Throw<DomainException>().Which.Error.Code.Should().Be("ROUTE_EXECUTION_IDEMPOTENCY_MISMATCH");

        var oldTime = () => run.ArriveAtStop(StopOne, ActorId, Now.AddMinutes(1), "arrive-old", "corr-old");
        oldTime.Should().Throw<DomainException>().Which.Error.Code.Should().Be("ROUTE_EXECUTION_TIME_ORDER_INVALID");
    }

    private static DispatchRun Prepared()
        => DispatchRun.CreatePrepared(
            Guid.NewGuid(), Now, ShipmentId, LoadPlanId, RoutePlanId, VehicleId, DriverId, Now.AddHours(1),
            [new DispatchRunStop(StopOne, 1), new DispatchRunStop(StopTwo, 2)]);

    private static DispatchRun Departed()
    {
        var run = Prepared();
        run.ConfirmDispatch(ActorId, Now.AddMinutes(1));
        run.Depart(ActorId, Now.AddMinutes(2), "dep-1", "corr-2");
        return run;
    }

    private static DispatchRun Completed()
    {
        var run = Departed();
        run.ArriveAtStop(StopOne, ActorId, Now.AddMinutes(3), "arrive-1", "corr-3");
        run.DepartStop(StopOne, ActorId, Now.AddMinutes(4), "depart-stop-1", "corr-4");
        run.ArriveAtStop(StopTwo, ActorId, Now.AddMinutes(5), "arrive-2", "corr-5");
        run.DepartStop(StopTwo, ActorId, Now.AddMinutes(6), "depart-stop-2", "corr-6");
        run.CompleteRoute(ActorId, Now.AddMinutes(7), "complete-1", "corr-7");
        return run;
    }
}
