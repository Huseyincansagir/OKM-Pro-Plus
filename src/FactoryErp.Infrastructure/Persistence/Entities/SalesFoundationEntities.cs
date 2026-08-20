namespace FactoryErp.Infrastructure.Persistence.Entities;

public sealed class CustomerRecord
{
    public Guid Id { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Status { get; set; } = "Candidate";
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<CustomerAddressRecord> Addresses { get; } = new List<CustomerAddressRecord>();
    public ICollection<CustomerContactRecord> Contacts { get; } = new List<CustomerContactRecord>();
}

public sealed class CustomerAddressRecord
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string AddressType { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string? District { get; set; }
    public string City { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public string CountryCode { get; set; } = "TR";
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }

    public CustomerRecord Customer { get; set; } = null!;
}

public sealed class CustomerContactRecord
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? RoleTitle { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }

    public CustomerRecord Customer { get; set; } = null!;
}

public sealed class PriceListRecord
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "TRY";
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CustomerPriceGroupRecord
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid PriceListId { get; set; }
    public bool IsActive { get; set; }

    public PriceListRecord PriceList { get; set; } = null!;
}

public sealed class CustomerPriceGroupMemberRecord
{
    public Guid CustomerId { get; set; }
    public Guid CustomerPriceGroupId { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
}

public sealed class ProductPriceRecord
{
    public Guid Id { get; set; }
    public Guid PriceListId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? PackagingId { get; set; }
    public decimal UnitPrice { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public string? TaxCode { get; set; }
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
}

public sealed class QuoteRequestRecord
{
    public Guid Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string Source { get; set; } = "Public";
    public string Status { get; set; } = "Received";
    public Guid? CustomerId { get; set; }
    public string? CustomerCandidateName { get; set; }
    public string? CustomerCandidateEmail { get; set; }
    public string? CustomerCandidatePhone { get; set; }
    public DateTimeOffset? ConsentAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<QuoteRequestItemRecord> Items { get; } = new List<QuoteRequestItemRecord>();
}

public sealed class QuoteRequestItemRecord
{
    public Guid Id { get; set; }
    public Guid QuoteRequestId { get; set; }
    public Guid ProductId { get; set; }
    public decimal EnteredQuantity { get; set; }
    public Guid? EnteredPackagingId { get; set; }
    public decimal QuantityBase { get; set; }
    public string PackagingSnapshot { get; set; } = "{}";

    public QuoteRequestRecord QuoteRequest { get; set; } = null!;
}

public sealed class StockReservationRecord
{
    public Guid Id { get; set; }
    public Guid SalesOrderItemId { get; set; }
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal QuantityBase { get; set; }
    public decimal ConsumedQtyBase { get; set; }
    public decimal ReleasedQtyBase { get; set; }
    public string Status { get; set; } = "Open";
    public long RowVersion { get; set; }
}

public sealed class SalesOrderRecord
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid? SourceQuoteId { get; set; }
    public string Status { get; set; } = "Draft";
    public string CurrencyCode { get; set; } = "TRY";
    public string? PriceSnapshotVersion { get; set; }
    public decimal TotalNet { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalGross { get; set; }
    public long RowVersion { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<SalesOrderItemRecord> Items { get; } = new List<SalesOrderItemRecord>();
    public ICollection<SalesOrderApprovalRecord> Approvals { get; } = new List<SalesOrderApprovalRecord>();
    public QuoteRecord? SourceQuote { get; set; }
}

public sealed class SalesOrderItemRecord
{
    public Guid Id { get; set; }
    public Guid SalesOrderId { get; set; }
    public Guid ProductId { get; set; }
    public decimal OrderedQty { get; set; }
    public decimal ReservedQty { get; set; }
    public decimal ShippedQty { get; set; }
    public decimal CancelledQty { get; set; }
    public decimal RemainingQty { get; set; }
    public decimal EnteredQuantity { get; set; }
    public Guid? EnteredPackagingId { get; set; }
    public string PackagingSnapshot { get; set; } = "{}";
    public bool PartialDeliveryAllowed { get; set; }
    public decimal UnitPrice { get; set; }
    public string? TaxCode { get; set; }
    public string PriceSnapshot { get; set; } = "{}";
    public long RowVersion { get; set; }

    public SalesOrderRecord SalesOrder { get; set; } = null!;
}

public sealed class SalesOrderApprovalRecord
{
    public Guid Id { get; set; }
    public Guid SalesOrderId { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public Guid DecidedBy { get; set; }
    public DateTimeOffset DecidedAt { get; set; }

    public SalesOrderRecord SalesOrder { get; set; } = null!;
}

public sealed class QuoteRecord
{
    public Guid Id { get; set; }
    public string QuoteNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public Guid CustomerId { get; set; }
    public Guid QuoteRequestId { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public decimal TotalNet { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalGross { get; set; }
    public DateTimeOffset? ValidUntil { get; set; }
    public DateTimeOffset? IssuedAt { get; set; }
    public Guid? IssuedBy { get; set; }
    public long RowVersion { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<QuoteItemRecord> Items { get; } = new List<QuoteItemRecord>();
}

public sealed class QuoteItemRecord
{
    public Guid Id { get; set; }
    public Guid QuoteId { get; set; }
    public Guid ProductId { get; set; }
    public Guid QuoteRequestItemId { get; set; }
    public decimal EnteredQuantity { get; set; }
    public Guid? EnteredPackagingId { get; set; }
    public decimal QuantityBase { get; set; }
    public string PackagingSnapshot { get; set; } = "{}";
    public decimal UnitPrice { get; set; }
    public decimal? ListUnitPrice { get; set; }
    public Guid? PriceListId { get; set; }
    public string? TaxCode { get; set; }
    public string PriceSnapshot { get; set; } = "{}";
    public decimal LineNet { get; set; }
    public long RowVersion { get; set; }

    public QuoteRecord Quote { get; set; } = null!;
}

public sealed class CustomerOutboundEmailRecord
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? ContactId { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = "Queued";
    public string? LastError { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
}
