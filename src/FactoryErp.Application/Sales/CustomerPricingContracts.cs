namespace FactoryErp.Application.Sales;

public sealed record PriceCandidate(
    Guid ProductId,
    Guid? PackagingId,
    decimal UnitPrice,
    string CurrencyCode,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo);

public sealed record PriceGroupMembershipCandidate(
    Guid CustomerPriceGroupId,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo);

public static class CustomerPriceResolver
{
    public static PriceGroupMembershipCandidate? SelectMembership(
        IEnumerable<PriceGroupMembershipCandidate> memberships,
        DateTimeOffset at)
        => memberships
            .Where(x => x.EffectiveFrom <= at && (x.EffectiveTo is null || x.EffectiveTo > at))
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefault();

    public static PriceCandidate? SelectPrice(
        IEnumerable<PriceCandidate> candidates,
        Guid productId,
        Guid? packagingId,
        DateTimeOffset at)
    {
        var live = candidates
            .Where(x => x.ProductId == productId && x.ValidFrom <= at && (x.ValidTo is null || x.ValidTo > at))
            .ToArray();

        var exact = live
            .Where(x => x.PackagingId == packagingId)
            .OrderByDescending(x => x.ValidFrom)
            .FirstOrDefault();
        if (exact is not null)
        {
            return exact;
        }

        if (packagingId is null)
        {
            return null;
        }

        return live
            .Where(x => x.PackagingId is null)
            .OrderByDescending(x => x.ValidFrom)
            .FirstOrDefault();
    }
}

public sealed record CreatePriceListRequest(
    string Code,
    string Name,
    string CurrencyCode,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidTo);

public sealed record PriceListDto(
    Guid Id,
    string Code,
    string Name,
    string CurrencyCode,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    bool IsActive);

public sealed record ProductPriceDto(
    Guid Id,
    Guid PriceListId,
    Guid ProductId,
    Guid? PackagingId,
    decimal UnitPrice,
    string CurrencyCode,
    string? TaxCode,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo);

public sealed record PriceListDetailDto(
    Guid Id,
    string Code,
    string Name,
    string CurrencyCode,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    bool IsActive,
    IReadOnlyCollection<ProductPriceDto> Prices);

public sealed record CreateProductPriceRequest(
    Guid PriceListId,
    Guid? PackagingId,
    decimal UnitPrice,
    string? TaxCode,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidTo);

public sealed record CreateCustomerPriceGroupRequest(
    string Code,
    string Name,
    Guid PriceListId);

public sealed record CustomerPriceGroupDto(
    Guid Id,
    string Code,
    string Name,
    Guid PriceListId,
    string PriceListCode,
    bool IsActive);

public sealed record AssignCustomerPriceGroupRequest(Guid CustomerPriceGroupId);

public sealed record ResolvedProductPriceDto(
    Guid ProductId,
    Guid? PackagingId,
    decimal UnitPrice,
    string CurrencyCode,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo);

public sealed record CustomerPriceContextDto(
    Guid CustomerId,
    bool BoundToCurrentAccount,
    Guid? CustomerPriceGroupId,
    string? CustomerPriceGroupCode,
    string? CustomerPriceGroupName,
    Guid? PriceListId,
    string? PriceListCode,
    string? PriceListName,
    string? CurrencyCode,
    IReadOnlyCollection<ResolvedProductPriceDto> Prices);

public interface ISalesPricingService
{
    Task<IReadOnlyCollection<PriceListDto>> ListPriceListsAsync(CancellationToken cancellationToken = default);

    Task<PriceListDetailDto?> GetPriceListAsync(Guid priceListId, CancellationToken cancellationToken = default);

    Task<PriceListDto> CreatePriceListAsync(
        CreatePriceListRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<ProductPriceDto> AddProductPriceAsync(
        Guid productId,
        CreateProductPriceRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CustomerPriceGroupDto>> ListCustomerPriceGroupsAsync(CancellationToken cancellationToken = default);

    Task<CustomerPriceGroupDto> CreateCustomerPriceGroupAsync(
        CreateCustomerPriceGroupRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task AssignCustomerPriceGroupAsync(
        Guid customerId,
        AssignCustomerPriceGroupRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<CustomerPriceContextDto?> GetCustomerPriceContextAsync(
        Guid customerId,
        Guid? productId,
        Guid? packagingId,
        CancellationToken cancellationToken = default);
}
