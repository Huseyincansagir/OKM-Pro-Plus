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
    DateTimeOffset CreatedAt);

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
    DateTimeOffset CreatedAt);

public sealed record RejectOrderRequest(string Comment);
public sealed record ApproveOrderRequest(string? Comment);

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
}
