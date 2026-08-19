using FactoryErp.Application.Sales;
using FluentAssertions;

namespace FactoryErp.Infrastructure.UnitTests.Sales;

public sealed class CustomerPriceResolverTests
{
    [Fact]
    public void SelectMembership_picks_latest_open_assignment_not_account_linked()
    {
        var at = new DateTimeOffset(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);
        var standard = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
        var credit = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");

        var selected = CustomerPriceResolver.SelectMembership(
            new[]
            {
                new PriceGroupMembershipCandidate(standard, at.AddDays(-30), at.AddDays(-1)),
                new PriceGroupMembershipCandidate(credit, at.AddDays(-1), null),
            },
            at);

        selected!.CustomerPriceGroupId.Should().Be(credit);
    }

    [Fact]
    public void SelectPrice_prefers_exact_packaging_then_base_fallback()
    {
        var at = new DateTimeOffset(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);
        var product = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
        var carton = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2");
        var candidates = new[]
        {
            new PriceCandidate(product, null, 10m, "TRY", at.AddDays(-10), null),
            new PriceCandidate(product, carton, 12.5m, "TRY", at.AddDays(-10), null),
            new PriceCandidate(product, carton, 14.5m, "TRY", at.AddDays(-1), null),
        };

        CustomerPriceResolver.SelectPrice(candidates, product, carton, at)!.UnitPrice.Should().Be(14.5m);
        CustomerPriceResolver.SelectPrice(candidates, product, Guid.NewGuid(), at)!.UnitPrice.Should().Be(10m);
        CustomerPriceResolver.SelectPrice(candidates, product, null, at)!.UnitPrice.Should().Be(10m);
    }

    [Fact]
    public void SelectPrice_ignores_expired_rows()
    {
        var at = new DateTimeOffset(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);
        var product = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
        var selected = CustomerPriceResolver.SelectPrice(
            new[]
            {
                new PriceCandidate(product, null, 99m, "TRY", at.AddDays(-20), at.AddDays(-1)),
            },
            product,
            null,
            at);

        selected.Should().BeNull();
    }
}
