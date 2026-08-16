using FactoryErp.Domain.Common;
using FactoryErp.Domain.Shared;

namespace FactoryErp.Domain.Shipping;

public enum AllocationKind
{
    Original = 0,
    Reversal = 1
}

public enum AllocationStatus
{
    Active = 0,
    Reversed = 1,
    Voided = 2
}

public sealed class DeliveryNoteItem : Entity
{
    private readonly List<DeliveryNoteItemAllocation> _allocations = [];

    private DeliveryNoteItem(
        Guid id,
        DateTimeOffset now,
        Guid deliveryNoteId,
        Guid salesOrderItemId,
        Guid productId,
        PositiveQuantity quantityBase)
        : base(id, now)
    {
        DomainGuard.AgainstEmpty(deliveryNoteId, "DELIVERY_NOTE_REQUIRED", "İrsaliye kimliği zorunludur.");
        DomainGuard.AgainstEmpty(salesOrderItemId, "SALES_ORDER_ITEM_REQUIRED", "Sipariş kalemi kimliği zorunludur.");
        DomainGuard.AgainstEmpty(productId, "PRODUCT_REQUIRED", "Ürün kimliği zorunludur.");

        DeliveryNoteId = deliveryNoteId;
        SalesOrderItemId = salesOrderItemId;
        ProductId = productId;
        QuantityBase = quantityBase;
        InvoicedQuantity = NonNegativeQuantity.Zero(quantityBase.Scale);
        WaivedQuantity = NonNegativeQuantity.Zero(quantityBase.Scale);
    }

    public Guid DeliveryNoteId { get; }
    public Guid SalesOrderItemId { get; }
    public Guid ProductId { get; }
    public PositiveQuantity QuantityBase { get; }
    public NonNegativeQuantity InvoicedQuantity { get; private set; }
    public NonNegativeQuantity WaivedQuantity { get; private set; }
    public NonNegativeQuantity RemainingToInvoice
        => NonNegativeQuantity.Create(
            QuantityBase.BaseValue - InvoicedQuantity.BaseValue - WaivedQuantity.BaseValue,
            QuantityBase.Scale);
    public IReadOnlyCollection<DeliveryNoteItemAllocation> Allocations => _allocations.AsReadOnly();

    public static DeliveryNoteItem Create(
        Guid id,
        DateTimeOffset now,
        Guid deliveryNoteId,
        Guid salesOrderItemId,
        Guid productId,
        PositiveQuantity quantityBase)
        => new(id, now, deliveryNoteId, salesOrderItemId, productId, quantityBase);

    public NonNegativeQuantity ActiveAllocatedQuantity()
    {
        var total = _allocations
            .Where(allocation => allocation.Kind == AllocationKind.Original
                && allocation.Status == AllocationStatus.Active)
            .Sum(allocation => allocation.QuantityBase.BaseValue);

        return NonNegativeQuantity.Create(total, QuantityBase.Scale);
    }

    public void AddAllocation(
        DeliveryNoteItemAllocation allocation,
        NonNegativeQuantity sourceRemaining,
        DateTimeOffset now)
    {
        if (allocation.Kind != AllocationKind.Original
            || allocation.Status != AllocationStatus.Active)
        {
            throw new DomainException(new(
                "ALLOCATION_KIND_INVALID",
                "İrsaliye kalemine yalnızca aktif original allocation eklenebilir."));
        }

        if (allocation.SalesOrderItemId != SalesOrderItemId
            || allocation.DeliveryNoteItemId != Id)
        {
            throw new DomainException(new(
                "ALLOCATION_SCOPE_MISMATCH",
                "Allocation source ve target kalemiyle eşleşmiyor."));
        }

        if (_allocations.Any(existing =>
            existing.Kind == AllocationKind.Original
            && existing.Status == AllocationStatus.Active
            && existing.SalesOrderItemId == allocation.SalesOrderItemId
            && existing.DeliveryNoteItemId == allocation.DeliveryNoteItemId))
        {
            throw new DomainException(new(
                "DUPLICATE_ALLOCATION",
                "Aynı source-target çifti için aktif allocation zaten mevcut."));
        }

        if (allocation.QuantityBase.BaseValue > sourceRemaining.BaseValue)
        {
            throw new DomainException(new(
                "OVER_ALLOCATION",
                "Allocation kaynak kalan miktarını aşamaz."));
        }

        var newTotal = ActiveAllocatedQuantity().BaseValue + allocation.QuantityBase.BaseValue;
        if (newTotal > QuantityBase.BaseValue)
        {
            throw new DomainException(new(
                "OVER_ALLOCATION",
                "Allocation toplamı planlanan miktarı aşamaz."));
        }

        _allocations.Add(allocation);
        Touch(now);
    }

    public void AllocateInvoice(PositiveQuantity quantity, DateTimeOffset now)
    {
        if (quantity.BaseValue > RemainingToInvoice.BaseValue)
        {
            throw new DomainException(new(
                "OVER_INVOICING",
                "Fatura miktarı faturalanabilir sevk miktarını aşamaz.",
                new Dictionary<string, object?>
                {
                    ["requestedQuantityBase"] = quantity.BaseValue,
                    ["remainingToInvoiceBase"] = RemainingToInvoice.BaseValue
                }));
        }

        InvoicedQuantity = InvoicedQuantity.Add(quantity);
        Touch(now);
    }

    public void WaiveInvoiceableQuantity(PositiveQuantity quantity, DateTimeOffset now)
    {
        if (quantity.BaseValue > RemainingToInvoice.BaseValue)
        {
            throw new DomainException(new(
                "WAIVER_EXCEEDS_REMAINING",
                "Kapatılan miktar faturalanabilir kalan miktarı aşamaz."));
        }

        WaivedQuantity = WaivedQuantity.Add(quantity);
        Touch(now);
    }
}

public sealed class DeliveryNoteItemAllocation : Entity
{
    private DeliveryNoteItemAllocation(
        Guid id,
        DateTimeOffset now,
        Guid salesOrderItemId,
        Guid deliveryNoteItemId,
        PositiveQuantity quantityBase,
        QuantitySnapshot quantitySnapshot,
        AllocationKind kind,
        Guid? reversedFromId,
        string? reversalReason)
        : base(id, now)
    {
        DomainGuard.AgainstEmpty(salesOrderItemId, "SALES_ORDER_ITEM_REQUIRED", "Sipariş kalemi kimliği zorunludur.");
        DomainGuard.AgainstEmpty(deliveryNoteItemId, "DELIVERY_NOTE_ITEM_REQUIRED", "İrsaliye kalemi kimliği zorunludur.");

        SalesOrderItemId = salesOrderItemId;
        DeliveryNoteItemId = deliveryNoteItemId;
        QuantityBase = quantityBase;
        QuantitySnapshot = quantitySnapshot;
        Kind = kind;
        ReversedFromId = reversedFromId;
        ReversalReason = reversalReason;
        Status = AllocationStatus.Active;
    }

    public Guid SalesOrderItemId { get; }
    public Guid DeliveryNoteItemId { get; }
    public PositiveQuantity QuantityBase { get; }
    public QuantitySnapshot QuantitySnapshot { get; }
    public AllocationKind Kind { get; }
    public bool IsActiveOriginal
        => Kind == AllocationKind.Original && Status == AllocationStatus.Active;
    public bool IsReversal => Kind == AllocationKind.Reversal;
    public AllocationStatus Status { get; private set; }
    public Guid? ReversedFromId { get; private set; }
    public string? ReversalReason { get; private set; }

    public static DeliveryNoteItemAllocation Create(
        Guid id,
        DateTimeOffset now,
        Guid salesOrderItemId,
        Guid deliveryNoteItemId,
        PositiveQuantity quantityBase,
        QuantitySnapshot quantitySnapshot)
        => new(id, now, salesOrderItemId, deliveryNoteItemId, quantityBase, quantitySnapshot, AllocationKind.Original, null, null);

    public static DeliveryNoteItemAllocation CreateReversal(
        Guid reversalId,
        DateTimeOffset now,
        DeliveryNoteItemAllocation original,
        string reason)
    {
        if (original.Kind != AllocationKind.Original
            || original.Status != AllocationStatus.Active)
        {
            throw new DomainException(new(
                "ALLOCATION_NOT_ACTIVE",
                "Yalnızca aktif original allocation için reversal oluşturulabilir."));
        }

        DomainGuard.AgainstBlank(reason, "REVERSAL_REASON_REQUIRED", "Reversal gerekçesi zorunludur.");

        return new DeliveryNoteItemAllocation(
            reversalId,
            now,
            original.SalesOrderItemId,
            original.DeliveryNoteItemId,
            original.QuantityBase,
            original.QuantitySnapshot,
            AllocationKind.Reversal,
            original.Id,
            reason.Trim());
    }

    public void Reverse(string reason, DateTimeOffset now)
    {
        if (Kind != AllocationKind.Original || Status != AllocationStatus.Active)
        {
            throw new DomainException(new(
                "ALLOCATION_NOT_ACTIVE",
                "Yalnızca aktif original allocation tersine çevrilebilir."));
        }

        DomainGuard.AgainstBlank(reason, "REVERSAL_REASON_REQUIRED", "Reversal gerekçesi zorunludur.");
        Status = AllocationStatus.Reversed;
        ReversalReason = reason.Trim();
        Touch(now);
    }
}
