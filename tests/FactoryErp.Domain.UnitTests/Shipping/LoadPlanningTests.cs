using FactoryErp.Domain.Common;
using FactoryErp.Domain.Shipping;
using FluentAssertions;

namespace FactoryErp.Domain.UnitTests.Shipping;

public sealed class LoadPlanningTests
{
    private static readonly Guid ShipmentId = Guid.Parse("61000000-0000-0000-0000-000000000001");
    private static readonly Guid RoutePlanId = Guid.Parse("62000000-0000-0000-0000-000000000001");
    private static readonly DateTimeOffset Now = new(2026, 8, 17, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateDraft_starts_infeasible_draft_with_positive_versions()
    {
        var plan = LoadPlan.CreateDraft(Guid.NewGuid(), Now, ShipmentId, RoutePlanId, 2, 1);

        plan.Status.Should().Be(LoadPlanStatus.Draft);
        plan.FeasibilityStatus.Should().Be(LoadPlanFeasibilityStatus.Infeasible);
        plan.RoutePlanVersion.Should().Be(2);
        plan.Version.Should().Be(1);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    public void CreateDraft_rejects_non_positive_versions(int routeVersion, int planVersion)
    {
        var action = () => LoadPlan.CreateDraft(Guid.NewGuid(), Now, ShipmentId, RoutePlanId, routeVersion, planVersion);

        action.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("LOAD_PLAN_VERSION_INVALID");
    }

    [Fact]
    public void AddLoadUnit_rejects_duplicate_unit_code()
    {
        var plan = LoadPlan.CreateDraft(Guid.NewGuid(), Now, ShipmentId, RoutePlanId, 1, 1);
        plan.AddLoadUnit(CreateUnit(plan.Id, "PAL-001"));

        var action = () => plan.AddLoadUnit(CreateUnit(plan.Id, "PAL-001"));

        action.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("LOAD_UNIT_CODE_DUPLICATE");
    }

    [Fact]
    public void Valid_plan_cannot_be_mutated()
    {
        var plan = LoadPlan.CreateDraft(Guid.NewGuid(), Now, ShipmentId, RoutePlanId, 1, 1);
        plan.MarkProposed();
        plan.MarkValidating();
        plan.MarkValid(LoadPlanFeasibilityStatus.Feasible, "{\"hardErrors\":0}");

        var action = () => plan.SetPlanningSnapshot(null, null, null, null, null, null, null, null, "{}", Now.AddMinutes(1));

        action.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("LOAD_PLAN_IMMUTABLE");
    }

    [Fact]
    public void LoadUnit_rejects_gross_weight_below_tare_and_invalid_dimensions()
    {
        var weightAction = () => LoadUnit.Create(Guid.NewGuid(), Now, Guid.NewGuid(), "PAL-001", LoadUnitType.Pallet, null, false, 1200, 800, 150, 30, 20, 1, 1, null, 1);
        var dimensionAction = () => LoadUnit.Create(Guid.NewGuid(), Now, Guid.NewGuid(), "PAL-002", LoadUnitType.Pallet, null, false, 0, 800, 150, 30, 40, 1, 1, null, 1);

        weightAction.Should().Throw<DomainException>().Which.Error.Code.Should().Be("LOAD_UNIT_WEIGHT_INVALID");
        dimensionAction.Should().Throw<DomainException>().Which.Error.Code.Should().Be("LOAD_UNIT_DIMENSIONS_INVALID");
    }

    [Fact]
    public void LoadUnitItem_rejects_over_allocation_and_duplicate_unsplittable_package()
    {
        var overAction = () => LoadUnitItem.EnsureQuantityCeiling(90, 11, 100, false, false);
        var duplicateAction = () => LoadUnitItem.EnsureQuantityCeiling(0, 10, 100, false, true);

        overAction.Should().Throw<DomainException>().Which.Error.Code.Should().Be("QUANTITY_EXCEEDED");
        duplicateAction.Should().Throw<DomainException>().Which.Error.Code.Should().Be("PACKAGE_ALREADY_ASSIGNED");
    }

    [Fact]
    public void LoadUnitItem_accepts_exact_quantity_boundary_and_rejects_stop_overallocation()
    {
        LoadUnitItem.EnsureQuantityCeiling(60, 40, 100, true, false);
        var item = LoadUnitItem.Create(Guid.NewGuid(), Now, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100, 5, 1, "{}");
        var stop = LoadUnitStopAllocation.Create(Guid.NewGuid(), Now, item.Id, Guid.NewGuid(), 101, 1);

        var action = () => item.SetStopAllocations([stop]);

        action.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("LOAD_UNIT_STOP_QUANTITY_EXCEEDED");
    }

    [Fact]
    public void LoadUnitStopAllocation_rejects_zero_sequence_and_quantity()
    {
        var zeroQuantity = () => LoadUnitStopAllocation.Create(Guid.NewGuid(), Now, Guid.NewGuid(), Guid.NewGuid(), 0, 1);
        var zeroSequence = () => LoadUnitStopAllocation.Create(Guid.NewGuid(), Now, Guid.NewGuid(), Guid.NewGuid(), 1, 0);

        zeroQuantity.Should().Throw<DomainException>().Which.Error.Code.Should().Be("LOAD_UNIT_STOP_QUANTITY_INVALID");
        zeroSequence.Should().Throw<DomainException>().Which.Error.Code.Should().Be("LOAD_UNIT_STOP_SEQUENCE_INVALID");
    }

    private static LoadUnit CreateUnit(Guid planId, string code)
        => LoadUnit.Create(Guid.NewGuid(), Now, planId, code, LoadUnitType.Pallet, null, false, 1200, 800, 150, 30, 230, 1.44m, 1, "ZONE-A", 1);
}
