namespace FactoryErp.Application.Shipping;

public sealed record CreateDeliveryNoteItemInput(
    Guid SalesOrderItemId,
    decimal EnteredQuantity,
    Guid? EnteredPackagingId,
    string ViewMode = "Packaging");

public sealed record CreateDeliveryNoteRequest(
    Guid SalesOrderId,
    IReadOnlyCollection<CreateDeliveryNoteItemInput> Items);

public sealed record DeliveryNoteItemDto(
    Guid Id,
    Guid SalesOrderItemId,
    Guid ProductId,
    decimal QuantityBase,
    decimal EnteredQuantity,
    Guid? EnteredPackagingId,
    decimal ShippedQty,
    decimal InvoicedQty,
    decimal WaivedQty,
    decimal RemainingToInvoice,
    string PackagingSnapshot,
    long RowVersion);

public sealed record DeliveryNoteDto(
    Guid Id,
    string DocumentNumber,
    Guid SalesOrderId,
    Guid CustomerId,
    string Status,
    DateTimeOffset? IssuedAt,
    IReadOnlyCollection<DeliveryNoteItemDto> Items,
    long RowVersion);

public sealed record CreateInvoiceItemInput(
    Guid DeliveryNoteItemId,
    decimal EnteredQuantity,
    Guid? EnteredPackagingId,
    decimal UnitPrice,
    Guid? TaxCodeId,
    string ViewMode = "Packaging");

public sealed record CreateInvoiceRequest(
    Guid CustomerId,
    string CurrencyCode,
    IReadOnlyCollection<CreateInvoiceItemInput> Items);

public sealed record InvoiceItemDto(
    Guid Id,
    Guid DeliveryNoteItemId,
    Guid ProductId,
    decimal QuantityBase,
    decimal EnteredQuantity,
    Guid? EnteredPackagingId,
    decimal UnitPrice,
    decimal LineTotal,
    long RowVersion);

public sealed record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    Guid CustomerId,
    string Status,
    string CurrencyCode,
    decimal Subtotal,
    decimal TaxTotal,
    decimal GrandTotal,
    IReadOnlyCollection<InvoiceItemDto> Items,
    DateTimeOffset? IssuedAt,
    long RowVersion);

public sealed record ApplyPaymentRequest(
    Guid CustomerId,
    decimal Amount,
    Guid PaymentMethodId,
    Guid? InvoiceId,
    string? Reference);

public sealed record CurrentAccountDto(
    Guid CustomerId,
    string CurrencyCode,
    decimal DebitTotal,
    decimal CreditTotal,
    decimal Balance,
    long RowVersion);

public sealed record PaymentDto(
    Guid Id,
    Guid CustomerId,
    decimal Amount,
    Guid PaymentMethodId,
    string Status,
    Guid? InvoiceId,
    DateTimeOffset? AppliedAt,
    CurrentAccountDto CurrentAccount);

public interface IShippingFinanceCommandService
{
    Task<DeliveryNoteDto> CreateDeliveryNoteAsync(
        CreateDeliveryNoteRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DeliveryNoteDto>> ListDeliveryNotesAsync(CancellationToken cancellationToken = default);

    Task<DeliveryNoteDto?> GetDeliveryNoteAsync(Guid deliveryNoteId, CancellationToken cancellationToken = default);

    Task<DeliveryNoteDto?> IssueDeliveryNoteAsync(
        Guid deliveryNoteId,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<InvoiceDto> CreateInvoiceAsync(
        CreateInvoiceRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<InvoiceDto>> ListInvoicesAsync(CancellationToken cancellationToken = default);

    Task<InvoiceDto?> GetInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default);

    Task<InvoiceDto?> IssueInvoiceAsync(
        Guid invoiceId,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<PaymentDto> ApplyPaymentAsync(
        ApplyPaymentRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CurrentAccountDto>> ListCurrentAccountsAsync(CancellationToken cancellationToken = default);

    Task<CurrentAccountDto?> GetCurrentAccountAsync(Guid customerId, CancellationToken cancellationToken = default);
}
