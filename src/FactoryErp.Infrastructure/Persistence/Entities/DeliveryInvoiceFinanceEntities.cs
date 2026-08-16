namespace FactoryErp.Infrastructure.Persistence.Entities;

public sealed class DeliveryNoteRecord
{
    public Guid Id { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public Guid SalesOrderId { get; set; }
    public Guid CustomerId { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTimeOffset? IssuedAt { get; set; }
    public Guid? IssuedBy { get; set; }
    public long RowVersion { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<DeliveryNoteItemRecord> Items { get; } = new List<DeliveryNoteItemRecord>();
}

public sealed class DeliveryNoteItemRecord
{
    public Guid Id { get; set; }
    public Guid DeliveryNoteId { get; set; }
    public Guid SalesOrderItemId { get; set; }
    public Guid ProductId { get; set; }
    public decimal QuantityBase { get; set; }
    public decimal EnteredQuantity { get; set; }
    public Guid? EnteredPackagingId { get; set; }
    public string PackagingSnapshot { get; set; } = "{}";
    public decimal ShippedQty { get; set; }
    public decimal InvoicedQty { get; set; }
    public decimal WaivedQty { get; set; }
    public decimal RemainingToInvoice { get; set; }
    public long RowVersion { get; set; }

    public DeliveryNoteRecord DeliveryNote { get; set; } = null!;
}

public sealed class DeliveryNoteItemAllocationRecord
{
    public Guid Id { get; set; }
    public Guid SalesOrderItemId { get; set; }
    public Guid DeliveryNoteItemId { get; set; }
    public decimal QuantityBase { get; set; }
    public Guid BaseUomId { get; set; }
    public string PackagingSnapshot { get; set; } = "{}";
    public string AllocationKind { get; set; } = "Original";
    public string Status { get; set; } = "Active";
    public string IdempotencyKey { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public Guid? ReversedFromId { get; set; }
    public string? ReversalReason { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public long RowVersion { get; set; }
}

public sealed class TaxCodeRecord
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
    public bool IsActive { get; set; }
}

public sealed class InvoiceRecord
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string Status { get; set; } = "Draft";
    public string CurrencyCode { get; set; } = "TRY";
    public decimal Subtotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string TaxSnapshot { get; set; } = "{}";
    public DateTimeOffset? IssuedAt { get; set; }
    public Guid? IssuedBy { get; set; }
    public long RowVersion { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<InvoiceItemRecord> Items { get; } = new List<InvoiceItemRecord>();
}

public sealed class InvoiceItemRecord
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid DeliveryNoteItemId { get; set; }
    public Guid ProductId { get; set; }
    public decimal QuantityBase { get; set; }
    public decimal EnteredQuantity { get; set; }
    public Guid? EnteredPackagingId { get; set; }
    public string PackagingSnapshot { get; set; } = "{}";
    public decimal UnitPrice { get; set; }
    public Guid? TaxCodeId { get; set; }
    public string TaxSnapshot { get; set; } = "{}";
    public decimal LineTotal { get; set; }

    public InvoiceRecord Invoice { get; set; } = null!;
}

public sealed class InvoiceItemAllocationRecord
{
    public Guid Id { get; set; }
    public Guid DeliveryNoteItemId { get; set; }
    public Guid InvoiceItemId { get; set; }
    public decimal QuantityBase { get; set; }
    public Guid BaseUomId { get; set; }
    public string PackagingSnapshot { get; set; } = "{}";
    public string PriceSnapshot { get; set; } = "{}";
    public string TaxSnapshot { get; set; } = "{}";
    public string AllocationKind { get; set; } = "Original";
    public string Status { get; set; } = "Active";
    public string IdempotencyKey { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public Guid? CreditedFromId { get; set; }
    public string? CreditReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public long RowVersion { get; set; }
}

public sealed class CurrentAccountRecord
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public decimal DebitTotal { get; set; }
    public decimal CreditTotal { get; set; }
    public decimal Balance { get; set; }
    public long RowVersion { get; set; }
}

public sealed class CurrentTransactionRecord
{
    public Guid Id { get; set; }
    public Guid CurrentAccountId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public string SourceEntityType { get; set; } = string.Empty;
    public Guid SourceEntityId { get; set; }
    public string? IdempotencyKey { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class PaymentMethodRecord
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class PaymentRecord
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public Guid PaymentMethodId { get; set; }
    public string Status { get; set; } = "Draft";
    public string? Reference { get; set; }
    public DateTimeOffset? AppliedAt { get; set; }
    public long RowVersion { get; set; }
}

public sealed class PaymentAllocationRecord
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
}
