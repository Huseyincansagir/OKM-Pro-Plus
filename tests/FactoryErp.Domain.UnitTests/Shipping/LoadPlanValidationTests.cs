using FactoryErp.Domain.Common;
using FactoryErp.Domain.Shipping;
using FluentAssertions;

namespace FactoryErp.Domain.UnitTests.Shipping;

public sealed class LoadPlanValidationTests
{
    private static readonly Guid ShipmentId = Guid.Parse("63000000-0000-0000-0000-000000000001");
    private static readonly Guid RoutePlanId = Guid.Parse("63000000-0000-0000-0000-000000000002");
    private static readonly DateTimeOffset Now = new(2026, 8, 17, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Validation_result_resolves_once_with_actor_reason_and_status()
    {
        var result = LoadPlanValidationResult.Create(
            Guid.NewGuid(),
            Now,
            Guid.NewGuid(),
            "vehicle.capacity",
            LoadPlanValidationSeverity.Warning,
            "STOP_ACCESS_BLOCK",
            "Stop erişimi policy seviyesinde warning.");

        result.Resolve(LoadPlanValidationResolutionStatus.Overridden, Guid.NewGuid(), "Depo sorumlusu kontrol etti.", Now.AddMinutes(1));

        result.ResolutionStatus.Should().Be(LoadPlanValidationResolutionStatus.Overridden);
        result.ResolutionReason.Should().Be("Depo sorumlusu kontrol etti.");
        var second = () => result.Resolve(LoadPlanValidationResolutionStatus.Resolved, Guid.NewGuid(), "İkinci çözüm", Now.AddMinutes(2));
        second.Should().Throw<DomainException>().Which.Error.Code.Should().Be("VALIDATION_ALREADY_RESOLVED");
    }

    [Fact]
    public void Validation_result_rejects_open_resolution_and_missing_reason()
    {
        var result = LoadPlanValidationResult.Create(
            Guid.NewGuid(),
            Now,
            Guid.NewGuid(),
            "missing.profile",
            LoadPlanValidationSeverity.HardError,
            "PHYSICAL_PROFILE_MISSING",
            "Physical profile bulunamadı.");

        var open = () => result.Resolve(LoadPlanValidationResolutionStatus.Open, Guid.NewGuid(), "x", Now);
        var blankReason = () => result.Resolve(LoadPlanValidationResolutionStatus.Resolved, Guid.NewGuid(), " ", Now);

        open.Should().Throw<DomainException>().Which.Error.Code.Should().Be("VALIDATION_RESOLUTION_INVALID");
        blankReason.Should().Throw<DomainException>().Which.Error.Code.Should().Be("VALIDATION_RESOLUTION_REASON_REQUIRED");
    }

    [Fact]
    public void Manual_change_requires_actor_entity_snapshots_and_reason()
    {
        var action = () => LoadPlanManualChange.Create(
            Guid.NewGuid(),
            Now,
            Guid.NewGuid(),
            Guid.NewGuid(),
            LoadPlanManualChangeType.MovePackage,
            "LoadUnitItem",
            Guid.NewGuid(),
            "{}",
            " ",
            "Depo kararı");

        action.Should().Throw<DomainException>().Which.Error.Code.Should().Be("MANUAL_CHANGE_AFTER_REQUIRED");
    }

    [Fact]
    public void Lock_blocks_hard_errors_and_unresolved_warnings_but_allows_approved_warning_override()
    {
        var hardPlan = PreparedPlan(LoadPlanFeasibilityStatus.Feasible);
        var hardAction = () => hardPlan.Lock(Guid.NewGuid(), Now.AddMinutes(1), true, true, false, false);
        hardAction.Should().Throw<DomainException>().Which.Error.Code.Should().Be("LOAD_PLAN_INFEASIBLE");

        var warningPlan = PreparedPlan(LoadPlanFeasibilityStatus.FeasibleWithWarnings);
        var warningAction = () => warningPlan.Lock(Guid.NewGuid(), Now.AddMinutes(1), true, false, true, false);
        warningAction.Should().Throw<DomainException>().Which.Error.Code.Should().Be("LOAD_PLAN_APPROVAL_REQUIRED");

        warningPlan.Lock(Guid.NewGuid(), Now.AddMinutes(1), true, false, true, true);
        warningPlan.Status.Should().Be(LoadPlanStatus.Locked);
        warningPlan.ApprovedBy.Should().NotBeNull();
        warningPlan.LockedBy.Should().NotBeNull();
    }

    [Fact]
    public void Lock_requires_approval_and_effective_vehicle_snapshot()
    {
        var plan = LoadPlan.CreateDraft(Guid.NewGuid(), Now, ShipmentId, RoutePlanId, 1, 1);
        plan.MarkProposed();
        plan.MarkValidating();
        plan.MarkValid(LoadPlanFeasibilityStatus.Feasible, "{}");

        var approvalAction = () => plan.Lock(Guid.NewGuid(), Now.AddMinutes(1), false, false, false, false);
        approvalAction.Should().Throw<DomainException>().Which.Error.Code.Should().Be("LOAD_PLAN_APPROVAL_REQUIRED");

        var snapshotAction = () => plan.Lock(Guid.NewGuid(), Now.AddMinutes(1), true, false, false, false);
        snapshotAction.Should().Throw<DomainException>().Which.Error.Code.Should().Be("LOCKED_VEHICLE_REQUIRED");
    }

    [Fact]
    public void Locked_plan_rejects_supersede_until_explicit_supersede_transition()
    {
        var plan = PreparedPlan(LoadPlanFeasibilityStatus.Feasible);
        plan.Lock(Guid.NewGuid(), Now.AddMinutes(1), true, false, false, false);

        plan.Supersede();

        plan.Status.Should().Be(LoadPlanStatus.Superseded);
    }

    private static LoadPlan PreparedPlan(LoadPlanFeasibilityStatus feasibility)
    {
        var plan = LoadPlan.CreateDraft(Guid.NewGuid(), Now, ShipmentId, RoutePlanId, 1, 1);
        plan.SetPlanningSnapshot(
            Guid.Parse("63000000-0000-0000-0000-000000000010"),
            Guid.Parse("63000000-0000-0000-0000-000000000011"),
            "ffd",
            "L4-B3.1",
            "ffd:v1",
            "hash",
            "{}",
            "{}",
            "{}",
            Now);
        plan.MarkProposed();
        plan.MarkValidating();
        plan.MarkValid(feasibility, "{}");
        return plan;
    }
}
