using FactoryErp.Domain.Common;
using FactoryErp.Domain.Sales;
using FactoryErp.Domain.Shared;

namespace FactoryErp.Domain.UnitTests.Sales;

public sealed class SalesOrderItemTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ReserveAndShipPartially_UpdatesRemainingAndStatus()
    {
        var item = CreateItem(partialDeliveryAllowed: true);
        item.Reserve(PositiveQuantity.Create(20_000, 0), Now);

        item.AllocateShipment(PositiveQuantity.Create(12_000, 0), Now.AddMinutes(1));

        Assert.Equal(12_000, item.ShippedQuantity.BaseValue);
        Assert.Equal(8_000, item.RemainingQuantity.BaseValue);
        Assert.Equal(SalesOrderItemStatus.PartiallyShipped, item.Status);
    }

    [Fact]
    public void FinalShipment_MovesItemToFulfilled()
    {
        var item = CreateItem(partialDeliveryAllowed: true);
        item.Reserve(PositiveQuantity.Create(20_000, 0), Now);

        item.AllocateShipment(PositiveQuantity.Create(20_000, 0), Now.AddMinutes(1));

        Assert.Equal(0, item.RemainingQuantity.BaseValue);
        Assert.Equal(SalesOrderItemStatus.Fulfilled, item.Status);
    }

    [Fact]
    public void PartialDeliveryNotAllowed_RejectsShipmentSmallerThanRemaining()
    {
        var item = CreateItem(partialDeliveryAllowed: false);
        item.Reserve(PositiveQuantity.Create(20_000, 0), Now);

        var exception = Assert.Throws<DomainException>(() =>
            item.AllocateShipment(PositiveQuantity.Create(12_000, 0), Now.AddMinutes(1)));

        Assert.Equal("PARTIAL_DELIVERY_NOT_ALLOWED", exception.Error.Code);
    }

    [Fact]
    public void ShipmentWithoutEnoughReservationIsRejected()
    {
        var item = CreateItem(partialDeliveryAllowed: true);
        item.Reserve(PositiveQuantity.Create(5_000, 0), Now);

        var exception = Assert.Throws<DomainException>(() =>
            item.AllocateShipment(PositiveQuantity.Create(6_000, 0), Now.AddMinutes(1)));

        Assert.Equal("SHIPMENT_EXCEEDS_RESERVATION", exception.Error.Code);
    }

    [Fact]
    public void ShipmentBeyondRemainingIsRejected()
    {
        var item = CreateItem(partialDeliveryAllowed: true);
        item.Reserve(PositiveQuantity.Create(20_000, 0), Now);
        item.AllocateShipment(PositiveQuantity.Create(20_000, 0), Now.AddMinutes(1));

        var exception = Assert.Throws<DomainException>(() =>
            item.AllocateShipment(PositiveQuantity.Create(1, 0), Now.AddMinutes(2)));

        Assert.Equal("OVER_SHIPMENT", exception.Error.Code);
    }

    [Fact]
    public void CancelledItemCannotBeReserved()
    {
        var item = CreateItem(partialDeliveryAllowed: true);
        item.Cancel(PositiveQuantity.Create(20_000, 0), Now);

        var exception = Assert.Throws<DomainException>(() =>
            item.Reserve(PositiveQuantity.Create(1, 0), Now.AddMinutes(1)));

        Assert.Equal("SALES_ORDER_ITEM_CANCELLED", exception.Error.Code);
    }

    private static SalesOrderItem CreateItem(bool partialDeliveryAllowed)
    {
        var packaging = PackagingSnapshot.Create(
            Guid.NewGuid(),
            "Koli",
            "Koli",
            UomCode.Create("piece"),
            2_000,
            allowPartial: false,
            "v1");
        var quantity = packaging.ToBaseQuantity(10, 0);
        var snapshot = new QuantitySnapshot(
            EnteredQuantity: 10,
            EnteredPackagingId: packaging.PackagingId,
            QuantityBase: quantity,
            BaseUomCode: UomCode.Create("piece"),
            ViewMode: "Packaging",
            PackagingSnapshot: packaging,
            Breakdown: []);

        return SalesOrderItem.Create(
            Guid.NewGuid(),
            Now,
            Guid.NewGuid(),
            Guid.NewGuid(),
            snapshot,
            partialDeliveryAllowed);
    }
}
