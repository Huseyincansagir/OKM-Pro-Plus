using FactoryErp.Domain.Common;
using FactoryErp.Domain.Shipping;
using FluentAssertions;

namespace FactoryErp.Domain.UnitTests.Shipping;

public sealed class LogisticsTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid ShipmentId = Guid.Parse("50000000-0000-0000-0000-000000000001");
    private static readonly Guid VehicleId = Guid.Parse("50000000-0000-0000-0000-000000000002");
    private static readonly Guid DriverId = Guid.Parse("50000000-0000-0000-0000-000000000003");

    [Fact]
    public void VehicleType_requires_code_and_name()
    {
        var action = () => VehicleType.Create(Guid.NewGuid(), " ", "Panelvan");

        action.Should().Throw<DomainException>().Which.Error.Code.Should().Be("VEHICLE_TYPE_CODE_REQUIRED");
    }

    [Fact]
    public void Capacity_rejects_invalid_effective_range_and_weight()
    {
        var rangeAction = () => VehicleCapacity.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now,
            Now,
            3_500m,
            1_500m,
            10m,
            4,
            2m,
            "{}");
        var weightAction = () => VehicleCapacity.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now,
            null,
            1_000m,
            1_000m,
            10m,
            4,
            2m,
            "{}");

        rangeAction.Should().Throw<DomainException>().Which.Error.Code.Should().Be("CAPACITY_EFFECTIVE_RANGE_INVALID");
        weightAction.Should().Throw<DomainException>().Which.Error.Code.Should().Be("CAPACITY_WEIGHT_INVALID");
    }

    [Fact]
    public void Vehicle_rejects_assignment_when_maintenance_or_out_of_service()
    {
        var vehicle = Vehicle.Create(VehicleId, Guid.NewGuid(), "34 abc 123", Now.AddHours(2));
        var maintenanceAction = () => vehicle.AssignToRoute(Guid.NewGuid(), Now);

        maintenanceAction.Should().Throw<DomainException>().Which.Error.Code.Should().Be("VEHICLE_MAINTENANCE");
        vehicle.ChangeStatus(VehicleStatus.OutOfService, Now);
        var unavailableAction = () => vehicle.AssignToRoute(Guid.NewGuid(), Now);

        unavailableAction.Should().Throw<DomainException>().Which.Error.Code.Should().Be("VEHICLE_UNAVAILABLE");
    }

    [Fact]
    public void Driver_rejects_expired_license_for_route_end()
    {
        var licenseExpiry = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var driver = Driver.Create(
            DriverId,
            null,
            "Örnek Şoför",
            null,
            "B-123456",
            licenseExpiry);

        var action = () => driver.EnsureAssignable(licenseExpiry.AddDays(1));

        action.Should().Throw<DomainException>().Which.Error.Code.Should().Be("DRIVER_LICENSE_EXPIRED");
    }

    [Fact]
    public void RoutePlan_rejects_duplicate_or_non_contiguous_stop_sequence()
    {
        var plan = RoutePlan.Create(Guid.NewGuid(), Now, ShipmentId, 1, Now, Now.AddHours(4));
        var stops = new[]
        {
            RouteStop.Create(Guid.NewGuid(), 1, Guid.NewGuid(), Guid.NewGuid(), Now.AddHours(1)),
            RouteStop.Create(Guid.NewGuid(), 3, Guid.NewGuid(), Guid.NewGuid(), Now.AddHours(2)),
        };

        var action = () => plan.ReplaceStops(stops);

        action.Should().Throw<DomainException>().Which.Error.Code.Should().Be("ROUTE_STOP_SEQUENCE_INVALID");
    }

    [Fact]
    public void RoutePlan_requires_resources_and_stops_before_planning()
    {
        var plan = RoutePlan.Create(Guid.NewGuid(), Now, ShipmentId, 1, Now, Now.AddHours(4));
        var action = () => plan.Plan();

        action.Should().Throw<DomainException>().Which.Error.Code.Should().Be("ROUTE_RESOURCES_REQUIRED");
    }

    [Fact]
    public void RoutePlan_transitions_draft_to_planned_to_locked()
    {
        var plan = RoutePlan.Create(Guid.NewGuid(), Now, ShipmentId, 1, Now, Now.AddHours(4));
        plan.ReplaceStops([
            RouteStop.Create(Guid.NewGuid(), 1, Guid.NewGuid(), Guid.NewGuid(), Now.AddHours(1)),
        ]);
        plan.AssignResources(VehicleId, DriverId);
        plan.Plan();
        plan.Lock();

        plan.Status.Should().Be(RoutePlanStatus.Locked);
    }

    [Fact]
    public void RoutePlan_rejects_edit_after_lock()
    {
        var plan = RoutePlan.Create(Guid.NewGuid(), Now, ShipmentId, 1, Now, Now.AddHours(4));
        plan.ReplaceStops([
            RouteStop.Create(Guid.NewGuid(), 1, Guid.NewGuid(), Guid.NewGuid(), Now.AddHours(1)),
        ]);
        plan.AssignResources(VehicleId, DriverId);
        plan.Plan();
        plan.Lock();

        var action = () => plan.ReplaceStops([]);

        action.Should().Throw<DomainException>().Which.Error.Code.Should().Be("ROUTE_STATE_CONFLICT");
    }

    [Fact]
    public void RoutePlan_accepts_exact_boundary_but_rejects_reversed_window()
    {
        var adjacent = RoutePlan.Create(Guid.NewGuid(), Now, ShipmentId, 1, Now, Now.AddHours(2));
        adjacent.PlannedEndAt.Should().Be(Now.AddHours(2));

        var action = () => RoutePlan.Create(Guid.NewGuid(), Now, ShipmentId, 1, Now.AddHours(2), Now);

        action.Should().Throw<DomainException>().Which.Error.Code.Should().Be("ROUTE_TIME_WINDOW_INVALID");
    }
}
