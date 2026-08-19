namespace FactoryErp.Application.Sales;

public sealed record QuoteRequestLineInput(
    Guid ProductId,
    decimal EnteredQuantity,
    Guid? EnteredPackagingId,
    string ViewMode = "Packaging");

public sealed record CreatePublicQuoteRequest(
    string CompanyName,
    string ContactName,
    string Phone,
    string Email,
    IReadOnlyCollection<QuoteRequestLineInput> Items,
    string? Note,
    bool ConsentAccepted);

public sealed record QuoteRequestItemDto(
    Guid Id,
    Guid ProductId,
    decimal EnteredQuantity,
    Guid? EnteredPackagingId,
    decimal QuantityBase,
    string PackagingSnapshot);

public sealed record QuoteRequestDto(
    Guid Id,
    string RequestNumber,
    string Status,
    string Source,
    string CandidateName,
    string CandidateEmail,
    string CandidatePhone,
    IReadOnlyCollection<QuoteRequestItemDto> Items,
    DateTimeOffset CreatedAt,
    Guid? CustomerId = null);

public sealed record CustomerDto(
    Guid Id,
    string CustomerCode,
    string LegalName,
    string Status,
    string? Email,
    string? Phone,
    DateTimeOffset CreatedAt,
    string? PrimaryContactName = null,
    string? PriceGroupCode = null,
    string? PriceGroupName = null);

public sealed record CreateCustomerRequest(
    string LegalName,
    string? Email,
    string? Phone,
    string? TaxNumber,
    string? TaxOffice);

public sealed record CreateSalesOrderItemInput(
    Guid ProductId,
    decimal EnteredQuantity,
    Guid? EnteredPackagingId,
    decimal UnitPrice,
    string? TaxCode,
    bool PartialDeliveryAllowed = true,
    string ViewMode = "Packaging");

public sealed record CreateSalesOrderRequest(
    Guid CustomerId,
    string CurrencyCode,
    IReadOnlyCollection<CreateSalesOrderItemInput> Items);

public sealed record SalesOrderItemDto(
    Guid Id,
    Guid ProductId,
    decimal OrderedQty,
    decimal ReservedQty,
    decimal ShippedQty,
    decimal CancelledQty,
    decimal RemainingQty,
    decimal EnteredQuantity,
    Guid? EnteredPackagingId,
    string PackagingSnapshot,
    bool PartialDeliveryAllowed,
    decimal UnitPrice,
    string? TaxCode,
    long RowVersion);

public sealed record SalesOrderDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string Status,
    string CurrencyCode,
    decimal TotalNet,
    decimal TotalTax,
    decimal TotalGross,
    long RowVersion,
    IReadOnlyCollection<SalesOrderItemDto> Items,
    DateTimeOffset CreatedAt,
    string? CustomerCode = null,
    string? CustomerLegalName = null);

public sealed record RejectOrderRequest(string Comment);
public sealed record ApproveOrderRequest(string? Comment);

public sealed record CreateQuoteItemInput(
    Guid QuoteRequestItemId,
    decimal UnitPrice,
    string? TaxCode,
    string ViewMode = "Packaging");

public sealed record CreateQuoteRequest(
    Guid QuoteRequestId,
    string CurrencyCode,
    DateTimeOffset? ValidUntil,
    IReadOnlyCollection<CreateQuoteItemInput> Items);

public sealed record QuoteItemDto(
    Guid Id,
    Guid ProductId,
    Guid QuoteRequestItemId,
    decimal EnteredQuantity,
    Guid? EnteredPackagingId,
    decimal QuantityBase,
    string PackagingSnapshot,
    decimal UnitPrice,
    decimal? ListUnitPrice,
    Guid? PriceListId,
    string? TaxCode,
    decimal LineNet,
    long RowVersion);

public sealed record QuoteDto(
    Guid Id,
    string QuoteNumber,
    string Status,
    Guid CustomerId,
    string CustomerCode,
    string CustomerLegalName,
    Guid QuoteRequestId,
    string CurrencyCode,
    decimal TotalNet,
    decimal TotalTax,
    decimal TotalGross,
    DateTimeOffset? ValidUntil,
    DateTimeOffset? IssuedAt,
    Guid? IssuedBy,
    long RowVersion,
    IReadOnlyCollection<QuoteItemDto> Items,
    DateTimeOffset CreatedAt);

public interface ISalesCommandService
{
    Task<QuoteRequestDto> CreatePublicQuoteRequestAsync(
        CreatePublicQuoteRequest request,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<QuoteRequestDto>> ListQuoteRequestsAsync(CancellationToken cancellationToken = default);

    Task<QuoteRequestDto?> GetQuoteRequestAsync(Guid quoteRequestId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CustomerDto>> ListCustomersAsync(CancellationToken cancellationToken = default);

    Task<CustomerDto?> GetCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task<CustomerDto> CreateCustomerAsync(
        CreateCustomerRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<QuoteRequestDto?> ReviewQuoteRequestAsync(
        Guid quoteRequestId,
        Guid actorId,
        Guid? customerId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<SalesOrderDto> CreateSalesOrderAsync(
        CreateSalesOrderRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SalesOrderDto>> ListSalesOrdersAsync(CancellationToken cancellationToken = default);

    Task<SalesOrderDto?> GetSalesOrderAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<SalesOrderDto?> SubmitSalesOrderAsync(
        Guid orderId,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<SalesOrderDto?> ApproveSalesOrderAsync(
        Guid orderId,
        Guid actorId,
        string? comment,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<SalesOrderDto?> RejectSalesOrderAsync(
        Guid orderId,
        Guid actorId,
        string comment,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<QuoteDto>> ListQuotesAsync(CancellationToken cancellationToken = default);

    Task<QuoteDto?> GetQuoteAsync(Guid quoteId, CancellationToken cancellationToken = default);

    Task<QuoteDto> CreateQuoteAsync(
        CreateQuoteRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<QuoteDto?> IssueQuoteAsync(
        Guid quoteId,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);
}
