using FactoryErp.Domain.Common;
using FactoryErp.Domain.Shared;

namespace FactoryErp.Domain.Sales;

public enum SalesOrderItemStatus
{
    Open = 0,
    PartiallyShipped = 1,
    Fulfilled = 2,
    Cancelled = 3
}

public sealed class SalesOrderItem : Entity
{
    private SalesOrderItem(
        Guid id,
        DateTimeOffset now,
        Guid salesOrderId,
        Guid productId,
        QuantitySnapshot orderedQuantity,
        bool partialDeliveryAllowed)
        : base(id, now)
    {
        DomainGuard.AgainstEmpty(salesOrderId, "SALES_ORDER_REQUIRED", "Sipariş kimliği zorunludur.");
        DomainGuard.AgainstEmpty(productId, "PRODUCT_REQUIRED", "Ürün kimliği zorunludur.");

        SalesOrderId = salesOrderId;
        ProductId = productId;
        OrderedQuantity = orderedQuantity;
        PartialDeliveryAllowed = partialDeliveryAllowed;
        ReservedQuantity = NonNegativeQuantity.Zero(orderedQuantity.QuantityBase.Scale);
        ShippedQuantity = NonNegativeQuantity.Zero(orderedQuantity.QuantityBase.Scale);
        CancelledQuantity = NonNegativeQuantity.Zero(orderedQuantity.QuantityBase.Scale);
        Status = SalesOrderItemStatus.Open;
    }

    public Guid SalesOrderId { get; }
    public Guid ProductId { get; }
    public QuantitySnapshot OrderedQuantity { get; }
    public NonNegativeQuantity ReservedQuantity { get; private set; }
    public NonNegativeQuantity ShippedQuantity { get; private set; }
    public NonNegativeQuantity CancelledQuantity { get; private set; }
    public NonNegativeQuantity RemainingQuantity
        => NonNegativeQuantity.Create(
            OrderedQuantity.QuantityBase.BaseValue
            - ShippedQuantity.BaseValue
            - CancelledQuantity.BaseValue,
            OrderedQuantity.QuantityBase.Scale);

    public bool PartialDeliveryAllowed { get; }
    public SalesOrderItemStatus Status { get; private set; }

    public static SalesOrderItem Create(
        Guid id,
        DateTimeOffset now,
        Guid salesOrderId,
        Guid productId,
        QuantitySnapshot orderedQuantity,
        bool partialDeliveryAllowed)
        => new(id, now, salesOrderId, productId, orderedQuantity, partialDeliveryAllowed);

    public void Reserve(PositiveQuantity quantity, DateTimeOffset now)
    {
        EnsureNotCancelled();
        EnsureWithinRemaining(quantity, "RESERVATION_EXCEEDS_REMAINING");
        ReservedQuantity = ReservedQuantity.Add(quantity);
        Touch(now);
    }

    public void ReleaseReservation(PositiveQuantity quantity, DateTimeOffset now)
    {
        if (quantity.BaseValue > ReservedQuantity.BaseValue)
        {
            throw new DomainException(new(
                "RESERVATION_RELEASE_EXCEEDS_RESERVED",
                "Serbest bırakılan miktar mevcut rezervasyonu aşamaz."));
        }

        ReservedQuantity = ReservedQuantity.Subtract(quantity);
        Touch(now);
    }

    public void AllocateShipment(PositiveQuantity quantity, DateTimeOffset now)
    {
        EnsureNotCancelled();

        if (!PartialDeliveryAllowed && quantity.BaseValue != RemainingQuantity.BaseValue)
        {
            throw new DomainException(new(
                "PARTIAL_DELIVERY_NOT_ALLOWED",
                "Bu sipariş kalemi parçalı sevkiyata izin vermiyor."));
        }

        EnsureWithinRemaining(quantity, "OVER_SHIPMENT");

        if (quantity.BaseValue > ReservedQuantity.BaseValue - ShippedQuantity.BaseValue)
        {
            throw new DomainException(new(
                "SHIPMENT_EXCEEDS_RESERVATION",
                "Sevk miktarı açık rezervasyonu aşamaz."));
        }

        ShippedQuantity = ShippedQuantity.Add(quantity);
        Status = RemainingQuantity.BaseValue == 0
            ? SalesOrderItemStatus.Fulfilled
            : SalesOrderItemStatus.PartiallyShipped;
        Touch(now);
    }

    public void Cancel(PositiveQuantity quantity, DateTimeOffset now)
    {
        EnsureNotCancelled();
        EnsureWithinRemaining(quantity, "CANCELLATION_EXCEEDS_REMAINING");
        CancelledQuantity = CancelledQuantity.Add(quantity);
        Status = RemainingQuantity.BaseValue == 0
            ? SalesOrderItemStatus.Cancelled
            : Status;
        Touch(now);
    }

    private void EnsureNotCancelled()
    {
        if (Status == SalesOrderItemStatus.Cancelled)
        {
            throw new DomainException(new(
                "SALES_ORDER_ITEM_CANCELLED",
                "İptal edilmiş sipariş kalemi değiştirilemez."));
        }
    }

    private void EnsureWithinRemaining(PositiveQuantity quantity, string code)
    {
        if (quantity.BaseValue > RemainingQuantity.BaseValue)
        {
            throw new DomainException(new(
                code,
                "İşlem miktarı kalan sipariş miktarını aşamaz.",
                new Dictionary<string, object?>
                {
                    ["requestedQuantityBase"] = quantity.BaseValue,
                    ["remainingQuantityBase"] = RemainingQuantity.BaseValue
                }));
        }
    }
}
