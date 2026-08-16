namespace FactoryErp.Application.Products;

public sealed record UnitOfMeasureDto(string Code, string DisplayName, string Dimension, int DecimalScale);

public sealed record PackagingOptionDto(
    Guid Id,
    string Level,
    string Name,
    decimal QuantityInBaseUom,
    bool IsSellable,
    bool AllowPartial,
    string EffectiveVersion);

public sealed record PublicProductDto(
    Guid Id,
    string Code,
    string Slug,
    string Name,
    string? Description,
    string? SizeLabel,
    string CategoryCode,
    string CategoryName,
    UnitOfMeasureDto BaseUom,
    IReadOnlyCollection<PackagingOptionDto> Packagings,
    string? PrimaryImageUrl);

public sealed record ProductListQuery(
    string? Search = null,
    string? Category = null,
    int Page = 1,
    int PageSize = 24);

public sealed record ProductPage(
    IReadOnlyCollection<PublicProductDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasNextPage);

public sealed record QuantityPreviewRequest(
    Guid ProductId,
    decimal EnteredQuantity,
    Guid? EnteredPackagingId,
    string ViewMode,
    string OperationType,
    Guid? WarehouseId);

public sealed record QuantityPreviewResult(
    Guid ProductId,
    UnitOfMeasureDto BaseUom,
    decimal EnteredQuantity,
    Guid? EnteredPackagingId,
    PackagingOptionDto EnteredPackaging,
    decimal QuantityBase,
    string DisplayText,
    decimal? AvailableBaseQuantity,
    IReadOnlyCollection<string> Warnings,
    string ViewMode,
    string OperationType,
    string PackagingSnapshotVersion);

public sealed record BarcodeResolutionResult(
    string Barcode,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    Guid? PackagingId,
    string? PackagingLevel,
    string? PackagingName,
    decimal DefaultEnteredQuantity,
    decimal QuantityInBaseUom,
    string BaseUomCode);

public interface IProductCatalogService
{
    Task<ProductPage> GetPublicProductsAsync(ProductListQuery query, CancellationToken cancellationToken = default);

    Task<PublicProductDto?> GetPublicProductBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<QuantityPreviewResult?> PreviewQuantityAsync(
        QuantityPreviewRequest request,
        CancellationToken cancellationToken = default);

    Task<BarcodeResolutionResult?> ResolveBarcodeAsync(
        string barcode,
        CancellationToken cancellationToken = default);
}
