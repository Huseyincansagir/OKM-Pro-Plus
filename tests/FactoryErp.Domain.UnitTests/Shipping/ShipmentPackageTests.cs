using FactoryErp.Domain.Common;
using FactoryErp.Domain.Shipping;
using FluentAssertions;

namespace FactoryErp.Domain.UnitTests.Shipping;

public sealed class ShipmentPackageTests
{
    private static readonly Guid ShipmentId = Guid.Parse("51000000-0000-0000-0000-000000000001");
    private static readonly Guid ShipmentItemId = Guid.Parse("52000000-0000-0000-0000-000000000001");
    private static readonly DateTimeOffset Now = new(2026, 8, 17, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_calculates_server_quantity_from_count_and_quantity_per_package()
    {
        var package = CreatePackage(packageCount: 3, quantityBasePerPackage: 10);

        package.QuantityBase.Should().Be(30);
        package.Status.Should().Be(ShipmentPackageStatus.Available);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, -10)]
    public void Create_rejects_zero_or_negative_package_quantity(decimal count, decimal perPackage)
    {
        var action = () => CreatePackage(count, perPackage);

        action.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("SHIPMENT_PACKAGE_QUANTITY_INVALID");
    }

    [Fact]
    public void Create_preserves_exact_decimal_boundary_without_rounding()
    {
        var package = CreatePackage(packageCount: 2.5m, quantityBasePerPackage: 4.4m);

        package.QuantityBase.Should().Be(11m);
    }

    [Fact]
    public void EnsureOwnership_rejects_cross_shipment_item()
    {
        var action = () => ShipmentPackage.EnsureOwnership(
            ShipmentId,
            Guid.Parse("52000000-0000-0000-0000-000000000999"),
            null,
            null);

        action.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("SHIPMENT_PACKAGE_CROSS_SHIPMENT");
    }

    [Fact]
    public void EnsureOwnership_rejects_route_stop_from_another_shipment()
    {
        var action = () => ShipmentPackage.EnsureOwnership(
            ShipmentId,
            ShipmentId,
            Guid.NewGuid(),
            Guid.Parse("53000000-0000-0000-0000-000000000999"));

        action.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("SHIPMENT_PACKAGE_ROUTE_STOP_OWNERSHIP");
    }

    [Fact]
    public void Allocate_twice_is_rejected_as_duplicate_allocation_transition()
    {
        var package = CreatePackage();
        package.Allocate();

        var action = () => package.Allocate();

        action.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("SHIPMENT_PACKAGE_INVALID_TRANSITION");
    }

    [Fact]
    public void Load_without_allocation_is_rejected_as_invalid_state()
    {
        var package = CreatePackage();

        var action = () => package.Load();

        action.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("SHIPMENT_PACKAGE_INVALID_TRANSITION");
    }

    [Fact]
    public void Loaded_package_cannot_be_cancelled()
    {
        var package = CreatePackage();
        package.Allocate();
        package.Load();

        var action = () => package.Cancel();

        action.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("SHIPMENT_PACKAGE_INVALID_TRANSITION");
    }

    [Fact]
    public void Create_requires_both_snapshots()
    {
        var action = () => ShipmentPackage.Create(
            Guid.NewGuid(),
            Now,
            ShipmentId,
            ShipmentItemId,
            null,
            null,
            ShipmentPackageType.Case,
            1,
            10,
            null,
            null,
            "{}",
            " ",
            false);

        action.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("PHYSICAL_SNAPSHOT_REQUIRED");
    }

    private static ShipmentPackage CreatePackage(
        decimal packageCount = 1,
        decimal quantityBasePerPackage = 10)
        => ShipmentPackage.Create(
            Guid.NewGuid(),
            Now,
            ShipmentId,
            ShipmentItemId,
            null,
            null,
            ShipmentPackageType.Case,
            packageCount,
            quantityBasePerPackage,
            null,
            null,
            "{\"level\":\"Case\"}",
            "{\"lengthMm\":100}",
            false);
}
