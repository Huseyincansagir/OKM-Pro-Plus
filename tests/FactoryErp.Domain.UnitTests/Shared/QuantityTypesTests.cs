using FactoryErp.Domain.Common;
using FactoryErp.Domain.Shared;

namespace FactoryErp.Domain.UnitTests.Shared;

public sealed class QuantityTypesTests
{
    [Fact]
    public void PositiveQuantity_ZeroIsRejected()
    {
        var exception = Assert.Throws<DomainException>(() => PositiveQuantity.Create(0, 0));

        Assert.Equal("QUANTITY_MUST_BE_POSITIVE", exception.Error.Code);
    }

    [Fact]
    public void PositiveQuantity_NegativeIsRejected()
    {
        var exception = Assert.Throws<DomainException>(() => PositiveQuantity.Create(-1, 0));

        Assert.Equal("QUANTITY_MUST_BE_POSITIVE", exception.Error.Code);
    }

    [Fact]
    public void NonNegativeQuantity_ZeroIsAcceptedForProjection()
    {
        var quantity = NonNegativeQuantity.Zero(0);

        Assert.Equal(0, quantity.BaseValue);
        Assert.Equal(0, quantity.Scale);
    }

    [Fact]
    public void NonNegativeQuantity_NegativeIsRejected()
    {
        var exception = Assert.Throws<DomainException>(() => NonNegativeQuantity.Create(-1, 0));

        Assert.Equal("QUANTITY_MUST_BE_NON_NEGATIVE", exception.Error.Code);
    }

    [Fact]
    public void Quantity_ExceedingUomPrecisionIsRejected()
    {
        var exception = Assert.Throws<DomainException>(() => PositiveQuantity.Create(1.2345m, 3));

        Assert.Equal("QUANTITY_PRECISION_EXCEEDED", exception.Error.Code);
    }

    [Fact]
    public void PackagingSnapshot_ConvertsEnteredPackagingToBaseQuantity()
    {
        var packaging = PackagingSnapshot.Create(
            packagingId: Guid.NewGuid(),
            level: "Koli",
            name: "Koli",
            baseUomCode: UomCode.Create("piece"),
            quantityInBaseUom: 2_000,
            allowPartial: false,
            effectiveVersion: "v1");

        var result = packaging.ToBaseQuantity(5, 0);

        Assert.Equal(10_000, result.BaseValue);
        Assert.Equal(0, result.Scale);
    }

    [Fact]
    public void ClosedPackaging_RejectsFractionalEnteredQuantity()
    {
        var packaging = PackagingSnapshot.Create(
            packagingId: Guid.NewGuid(),
            level: "Koli",
            name: "Kapalı koli",
            baseUomCode: UomCode.Create("piece"),
            quantityInBaseUom: 2_000,
            allowPartial: false,
            effectiveVersion: "v1");

        var exception = Assert.Throws<DomainException>(() => packaging.ToBaseQuantity(0.5m, 0));

        Assert.Equal("PACKAGING_PARTIAL_NOT_ALLOWED", exception.Error.Code);
    }

    [Fact]
    public void NonNegativeQuantity_SubtractingMoreThanAvailableIsRejected()
    {
        var current = NonNegativeQuantity.Create(10, 0);
        var consumed = PositiveQuantity.Create(11, 0);

        var exception = Assert.Throws<DomainException>(() => current.Subtract(consumed));

        Assert.Equal("QUANTITY_RESULT_NEGATIVE", exception.Error.Code);
    }

    [Fact]
    public void PackagingSnapshot_IsImmutableByPublicApi()
    {
        var properties = typeof(PackagingSnapshot).GetProperties();

        Assert.All(properties, property => Assert.False(property.SetMethod?.IsPublic == true));
    }
}
