using FactoryErp.Domain.Common;
using FactoryErp.Domain.Shipping;
using FluentAssertions;

namespace FactoryErp.Domain.UnitTests.Shipping;

public sealed class FfdPlanningTests
{
    private static readonly Guid PackageA = Guid.Parse("71000000-0000-0000-0000-000000000001");
    private static readonly Guid PackageB = Guid.Parse("71000000-0000-0000-0000-000000000002");
    private static readonly Guid ItemA = Guid.Parse("72000000-0000-0000-0000-000000000001");
    private static readonly Guid ItemB = Guid.Parse("72000000-0000-0000-0000-000000000002");
    private static readonly Guid ProductId = Guid.Parse("73000000-0000-0000-0000-000000000001");

    [Fact]
    public void PlanningItem_normalizes_compatibility_orientation_and_stable_key_inputs()
    {
        var item = CreateItem(PackageA, ItemA, compatibilityGroup: "  FOOD ", incompatibleGroups: ["CHEM", "CHEM"], allowedOrientations: ["lwh", "LWH"]);

        item.CompatibilityGroup.Should().Be("FOOD");
        item.IncompatibleGroups.Should().Equal("CHEM");
        item.AllowedOrientations.Should().Equal("LWH");
        item.StableSortKey.CompatibilityGroup.Should().Be("FOOD");
        item.StableSortKey.ShipmentPackageId.Should().Be(PackageA);
    }

    [Theory]
    [InlineData(0, "PLANNING_ITEM_QUANTITY_INVALID")]
    [InlineData(-1, "PLANNING_ITEM_QUANTITY_INVALID")]
    public void PlanningItem_rejects_invalid_quantity(decimal quantity, string code)
    {
        var action = () => PlanningItem.Create(PackageA, ItemA, ProductId, null, quantity, 1, 1, 0, 1, 1, 100, 100, 100, null, null, false, false, ["LWH"], false, 1, true);

        action.Should().Throw<DomainException>().Which.Error.Code.Should().Be(code);
    }

    [Fact]
    public void Same_input_items_have_same_sorted_order_independent_of_insertion_order()
    {
        var first = CreateItem(PackageA, ItemA, grossWeightKg: 100, volumeM3: 2);
        var second = CreateItem(PackageB, ItemB, grossWeightKg: 100, volumeM3: 2);

        var orderA = new[] { first, second }.OrderBy(x => x.StableSortKey).Select(x => x.ShipmentPackageId).ToArray();
        var orderB = new[] { second, first }.OrderBy(x => x.StableSortKey).Select(x => x.ShipmentPackageId).ToArray();

        orderA.Should().Equal(orderB);
    }

    [Fact]
    public void Ffd_accepts_exact_weight_and_volume_boundary()
    {
        var item = CreateItem(PackageA, ItemA, quantityBase: 10, grossWeightKg: 100, volumeM3: 2);
        var capacity = FfdUnitCapacity.Create("PAL-001", 1200, 800, 150, 100, 2, true, 1, "ZONE-A");

        var result = DeterministicFfdEngine.Execute([item], [capacity]);

        result.IsFeasible.Should().BeTrue();
        result.Placements.Should().ContainSingle(x => x.QuantityBase == 10);
        result.Rejections.Should().BeEmpty();
    }

    [Fact]
    public void Ffd_returns_physical_profile_missing_as_hard_rejection()
    {
        var item = CreateItem(PackageA, ItemA, physicalProfilePresent: false);
        var capacity = FfdUnitCapacity.Create("PAL-001", 1200, 800, 150, 1000, 20, true, 1, "ZONE-A");

        var result = DeterministicFfdEngine.Execute([item], [capacity]);

        result.IsFeasible.Should().BeFalse();
        result.Rejections.Should().ContainSingle(x => x.Code == FfdHardConstraintCode.PhysicalProfileMissing);
    }

    [Fact]
    public void Ffd_returns_weight_rejection_when_no_new_unit_template_exists()
    {
        var item = CreateItem(PackageA, ItemA, quantityBase: 10, grossWeightKg: 101, volumeM3: 1);
        var capacity = FfdUnitCapacity.Create("PAL-001", 1200, 800, 150, 100, 10, true, 1, "ZONE-A");

        var result = DeterministicFfdEngine.Execute([item], [capacity]);

        result.Rejections.Should().ContainSingle();
        result.Rejections.Single().Code.Should().Be(FfdHardConstraintCode.LoadUnitWeightExceeded);
        result.Placements.Should().BeEmpty();
    }

    [Fact]
    public void Ffd_creates_deterministic_new_unit_codes_for_split_allowed_item()
    {
        var item = CreateItem(PackageA, ItemA, quantityBase: 20, grossWeightKg: 100, volumeM3: 1, splitAllowed: true);
        var capacity = FfdUnitCapacity.Create("PAL", 1200, 800, 150, 60, 10, true, 1, "ZONE-A", allowNewUnit: true);

        var result = DeterministicFfdEngine.Execute([item], [capacity]);

        result.Rejections.Should().BeEmpty();
        result.UnitCodes.Should().Contain(new[] { "PAL", "PAL-001" });
        result.Placements.Sum(x => x.QuantityBase).Should().Be(20);
    }

    [Fact]
    public void Ffd_rejects_unsplittable_item_that_does_not_fit()
    {
        var item = CreateItem(PackageA, ItemA, quantityBase: 20, grossWeightKg: 100, volumeM3: 1, splitAllowed: false);
        var capacity = FfdUnitCapacity.Create("PAL", 1200, 800, 150, 60, 10, true, 1, "ZONE-A", allowNewUnit: true);

        var result = DeterministicFfdEngine.Execute([item], [capacity]);

        result.Rejections.Should().ContainSingle();
        result.Rejections.Single().Code.Should().Be(FfdHardConstraintCode.LoadUnitWeightExceeded);
        result.Placements.Should().BeEmpty();
    }

    [Fact]
    public void Ffd_applies_compatibility_hard_block_before_second_item()
    {
        var first = CreateItem(PackageA, ItemA, compatibilityGroup: "FOOD", incompatibleGroups: ["CHEM"]);
        var second = CreateItem(PackageB, ItemB, compatibilityGroup: "CHEM", incompatibleGroups: ["FOOD"]);
        var capacity = FfdUnitCapacity.Create("PAL-001", 1200, 800, 150, 1000, 20, true, 1, "ZONE-A");

        var result = DeterministicFfdEngine.Execute([first, second], [capacity]);

        result.Rejections.Should().ContainSingle(x => x.Code == FfdHardConstraintCode.CompatibilityBlock);
    }

    private static PlanningItem CreateItem(
        Guid packageId,
        Guid itemId,
        decimal quantityBase = 10,
        decimal grossWeightKg = 10,
        decimal volumeM3 = 1,
        string? compatibilityGroup = null,
        IEnumerable<string>? incompatibleGroups = null,
        IEnumerable<string>? allowedOrientations = null,
        bool splitAllowed = false,
        bool physicalProfilePresent = true)
        => PlanningItem.Create(
            packageId,
            itemId,
            ProductId,
            Guid.Parse("74000000-0000-0000-0000-000000000001"),
            quantityBase,
            1,
            grossWeightKg - 1,
            1,
            grossWeightKg,
            volumeM3,
            100,
            100,
            100,
            compatibilityGroup,
            incompatibleGroups,
            false,
            false,
            allowedOrientations ?? ["LWH"],
            splitAllowed,
            1,
            physicalProfilePresent);
}
