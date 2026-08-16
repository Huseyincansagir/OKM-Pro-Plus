using FactoryErp.Domain.Common;
using FactoryErp.Domain.Sales;
using FactoryErp.Domain.Shared;

namespace FactoryErp.Domain.UnitTests.Sales;

public sealed class SalesOrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SubmitWithoutItems_IsRejected()
    {
        var order = SalesOrder.Create(Guid.NewGuid(), Guid.NewGuid(), Now);

        var exception = Assert.Throws<DomainException>(() => order.Submit(Now.AddMinutes(1)));

        Assert.Equal("INVALID_ORDER_SUBMISSION", exception.Error.Code);
    }

    [Fact]
    public void SubmitAndApprove_ProduceTypedDomainEvents()
    {
        var order = CreateOrderWithItem();
        order.Submit(Now.AddMinutes(1));

        Assert.Equal(SalesOrderStatus.PendingApproval, order.Status);
        Assert.Contains(order.DomainEvents, domainEvent => domainEvent is SalesOrderSubmitted);

        order.Approve(Guid.NewGuid(), Now.AddMinutes(2));

        Assert.Equal(SalesOrderStatus.Approved, order.Status);
        Assert.Contains(order.DomainEvents, domainEvent => domainEvent is SalesOrderApproved);
    }

    [Fact]
    public void ApprovedOrderWithPartialShipment_MovesToPartiallyShipped()
    {
        var order = CreateOrderWithItem();
        var item = order.Items.Single();
        item.Reserve(PositiveQuantity.Create(20_000, 0), Now);
        item.AllocateShipment(PositiveQuantity.Create(12_000, 0), Now.AddMinutes(1));
        order.Submit(Now.AddMinutes(2));
        order.Approve(Guid.NewGuid(), Now.AddMinutes(3));

        order.RecordShipment(Now.AddMinutes(4));

        Assert.Equal(SalesOrderStatus.PartiallyShipped, order.Status);
    }

    [Fact]
    public void FullyShippedOrder_MovesToFulfilled()
    {
        var order = CreateOrderWithItem();
        var item = order.Items.Single();
        item.Reserve(PositiveQuantity.Create(20_000, 0), Now);
        item.AllocateShipment(PositiveQuantity.Create(20_000, 0), Now.AddMinutes(1));
        order.Submit(Now.AddMinutes(2));
        order.Approve(Guid.NewGuid(), Now.AddMinutes(3));

        order.RecordShipment(Now.AddMinutes(4));

        Assert.Equal(SalesOrderStatus.Fulfilled, order.Status);
    }

    [Fact]
    public void DraftOrderCannotBeShipped()
    {
        var order = CreateOrderWithItem();

        var exception = Assert.Throws<DomainException>(() => order.RecordShipment(Now.AddMinutes(1)));

        Assert.Equal("ORDER_NOT_SHIPPABLE", exception.Error.Code);
    }

    private static SalesOrder CreateOrderWithItem()
    {
        var order = SalesOrder.Create(Guid.NewGuid(), Guid.NewGuid(), Now);
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
            10,
            packaging.PackagingId,
            quantity,
            UomCode.Create("piece"),
            "Packaging",
            packaging,
            []);
        var item = SalesOrderItem.Create(
            Guid.NewGuid(),
            Now,
            order.Id,
            Guid.NewGuid(),
            snapshot,
            partialDeliveryAllowed: true);

        order.AddItem(item, Now);
        return order;
    }
}
