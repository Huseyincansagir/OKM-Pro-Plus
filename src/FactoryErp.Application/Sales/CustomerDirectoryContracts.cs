namespace FactoryErp.Application.Sales;

public sealed record CustomerContactDto(
    Guid Id,
    string FullName,
    string? Email,
    string? Phone,
    string? RoleTitle,
    bool IsPrimary,
    bool IsActive);

public sealed record CustomerAddressDto(
    Guid Id,
    string AddressType,
    string? Title,
    string Line1,
    string? Line2,
    string? District,
    string City,
    string? PostalCode,
    string CountryCode,
    bool IsDefault,
    bool IsActive);

public sealed record CreateCustomerContactRequest(
    string FullName,
    string? Email,
    string? Phone,
    string? RoleTitle,
    bool IsPrimary = false);

public sealed record SendCustomerEmailRequest(
    Guid? ContactId,
    string? To,
    string Subject,
    string Body);

public sealed record CustomerOutboundEmailDto(
    Guid Id,
    Guid CustomerId,
    Guid? ContactId,
    string To,
    string Subject,
    string Body,
    string Status,
    string? LastError,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SentAt);

public sealed record CustomerCardDto(
    Guid Id,
    string CustomerCode,
    string LegalName,
    string Status,
    string? Email,
    string? Phone,
    string? TaxNumber,
    string? TaxOffice,
    DateTimeOffset CreatedAt,
    string? PriceGroupCode,
    string? PriceGroupName,
    Guid? PriceListId,
    string? PriceListCode,
    IReadOnlyCollection<CustomerContactDto> Contacts,
    IReadOnlyCollection<CustomerAddressDto> Addresses);

public interface ICustomerDirectoryService
{
    Task<CustomerCardDto?> GetCustomerCardAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task<CustomerContactDto> CreateContactAsync(
        Guid customerId,
        CreateCustomerContactRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<CustomerOutboundEmailDto> SendOutboundEmailAsync(
        Guid customerId,
        SendCustomerEmailRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CustomerOutboundEmailDto>> ListOutboundEmailsAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
}

public sealed record EmailDispatchResult(bool Sent, string? Error);

public interface ICustomerEmailSender
{
    bool IsConfigured { get; }

    Task<EmailDispatchResult> SendAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}
