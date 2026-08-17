using FactoryErp.Domain.Common;
using FactoryErp.Domain.Shipping;
using FluentAssertions;

namespace FactoryErp.Domain.UnitTests.Shipping;

public sealed class PhysicalLogisticsTests
{
    private static readonly DateTimeOffset EffectiveFrom = new(2026, 8, 17, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ProductPhysicalProfile_rejects_nonpositive_dimensions()
    {
        var action = () => ProductPhysicalProfile.Create(
            Guid.NewGuid(), Guid.NewGuid(), EffectiveFrom, null, 0, 100, 100, 1);

        action.Should().Throw<DomainException>().Which.Error.Code.Should().Be("PHYSICAL_DIMENSIONS_INVALID");
    }

    [Fact]
    public void ProductPhysicalProfile_rejects_reversed_effective_range()
    {
        var action = () => ProductPhysicalProfile.Create(
            Guid.NewGuid(), Guid.NewGuid(), EffectiveFrom, EffectiveFrom, 100, 100, 100, 1);

        action.Should().Throw<DomainException>().Which.Error.Code.Should().Be("PHYSICAL_EFFECTIVE_RANGE_INVALID");
    }

    [Fact]
    public void ProductPhysicalProfile_rejects_conflicting_stack_rules()
    {
        var profile = ProductPhysicalProfile.Create(
            Guid.NewGuid(), Guid.NewGuid(), EffectiveFrom, null, 100, 100, 100, 1);

        var action = () => profile.SetHandlingRules(false, 2, null, false, false);

        action.Should().Throw<DomainException>().Which.Error.Code.Should().Be("STACK_RULE_CONFLICT");
    }

    [Fact]
    public void PackagingPhysicalProfile_rejects_gross_weight_below_net_plus_tare()
    {
        var profile = PackagingPhysicalProfile.Create(
            Guid.NewGuid(), Guid.NewGuid(), EffectiveFrom, null, 10, 600, 400, 300, 0.5m);

        var action = () => profile.SetWeights(12m, 12.4m, 0.072m);

        action.Should().Throw<DomainException>().Which.Error.Code.Should().Be("PACKAGING_GROSS_WEIGHT_INVALID");
    }

    [Fact]
    public void PackagingPhysicalProfile_accepts_valid_5_case_physical_profile()
    {
        var profile = PackagingPhysicalProfile.Create(
            Guid.NewGuid(), Guid.NewGuid(), EffectiveFrom, null, 10, 600, 400, 300, 0.5m);

        profile.SetWeights(12m, 12.5m, 0.072m);
        profile.SetHandlingRules(true, 5, false);

        profile.GrossWeightKg.Should().Be(12.5m);
        profile.VolumeM3.Should().Be(0.072m);
        profile.MaxStackCount.Should().Be(5);
    }

    [Fact]
    public void PalletType_rejects_payload_above_gross_capacity()
    {
        var pallet = PalletType.Create(Guid.NewGuid(), "EURO", "Euro Palet", 1200, 800, 150, 25);

        var action = () => pallet.SetCapacity(500, 501, 1800, 1, false);

        action.Should().Throw<DomainException>().Which.Error.Code.Should().Be("PALLET_PAYLOAD_OVER_GROSS");
    }

    [Fact]
    public void PalletType_accepts_valid_capacity_and_can_be_deactivated()
    {
        var pallet = PalletType.Create(Guid.NewGuid(), "EURO", "Euro Palet", 1200, 800, 150, 25);
        pallet.SetCapacity(1025, 1000, 1800, 1, false);
        pallet.Deactivate();

        pallet.MaxPayloadKg.Should().Be(1000m);
        pallet.IsActive.Should().BeFalse();
    }
}
