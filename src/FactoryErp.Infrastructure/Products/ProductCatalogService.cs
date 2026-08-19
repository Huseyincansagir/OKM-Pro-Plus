using System.Globalization;
using FactoryErp.Application.Products;
using FactoryErp.Domain.Shared;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.Products;

public sealed class ProductCatalogService(FactoryErpDbContext dbContext) : IProductCatalogService
{
    public async Task<ProductPage> GetPublicProductsAsync(ProductListQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var now = DateTimeOffset.UtcNow;

        var products = dbContext.Products
            .AsNoTracking()
            .Include(x => x.BaseUom)
            .Include(x => x.Category)
            .Include(x => x.Packagings)
            .Include(x => x.Images)
            .Where(x => x.IsActive && x.IsPublic);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            products = products.Where(x =>
                EF.Functions.ILike(x.Name, $"%{search}%")
                || EF.Functions.ILike(x.Code, $"%{search}%")
                || EF.Functions.ILike(x.Slug, $"%{search}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var category = query.Category.Trim();
            products = products.Where(x => x.Category != null && x.Category.Slug == category);
        }

        var totalCount = await products.CountAsync(cancellationToken);
        var records = await products
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        var items = records.Select(record => MapPublicProduct(record, now)).ToArray();
        return new ProductPage(items, page, pageSize, totalCount, page * pageSize < totalCount);
    }

    public async Task<PublicProductDto?> GetPublicProductBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var record = await dbContext.Products
            .AsNoTracking()
            .Include(x => x.BaseUom)
            .Include(x => x.Category)
            .Include(x => x.Packagings)
            .Include(x => x.Images)
            .SingleOrDefaultAsync(x => x.IsActive && x.IsPublic && x.Slug == slug, cancellationToken);

        return record is null ? null : MapPublicProduct(record, now);
    }

    public async Task<IReadOnlyCollection<StaffProductDto>> ListStaffProductsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var records = await dbContext.Products
            .AsNoTracking()
            .Include(x => x.BaseUom)
            .Include(x => x.Category)
            .Include(x => x.Packagings)
            .Include(x => x.Images)
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Code)
            .Take(100)
            .ToArrayAsync(cancellationToken);
        return records.Select(record => MapStaffProduct(record, now)).ToArray();
    }

    public async Task<StaffProductDto?> GetStaffProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var record = await dbContext.Products
            .AsNoTracking()
            .Include(x => x.BaseUom)
            .Include(x => x.Category)
            .Include(x => x.Packagings)
            .Include(x => x.Images)
            .SingleOrDefaultAsync(x => x.Id == productId, cancellationToken);
        return record is null ? null : MapStaffProduct(record, now);
    }

    public async Task<QuantityPreviewResult?> PreviewQuantityAsync(
        QuantityPreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var product = await dbContext.Products
            .AsNoTracking()
            .Include(x => x.BaseUom)
            .Include(x => x.Packagings)
            .SingleOrDefaultAsync(x => x.Id == request.ProductId && x.IsActive, cancellationToken);

        if (product is null)
        {
            return null;
        }

        var packaging = request.EnteredPackagingId.HasValue
            ? product.Packagings.SingleOrDefault(x => x.Id == request.EnteredPackagingId.Value && IsEffective(x, now))
            : null;

        if (request.EnteredPackagingId.HasValue && packaging is null)
        {
            return null;
        }

        var option = packaging is null
            ? new PackagingOptionDto(
                Guid.Empty,
                "BaseUnit",
                product.BaseUom.DisplayName,
                1,
                true,
                true,
                product.UpdatedAt.ToString("O", CultureInfo.InvariantCulture))
            : MapPackaging(packaging);

        var snapshot = PackagingSnapshot.Create(
            packaging?.Id,
            option.Level,
            option.Name,
            UomCode.Create(product.BaseUom.Code),
            option.QuantityInBaseUom,
            option.AllowPartial,
            option.EffectiveVersion);
        var quantityBase = snapshot.ToBaseQuantity(request.EnteredQuantity, product.BaseUom.DecimalScale);

        decimal? availableBaseQuantity = null;
        var warnings = new List<string>();
        if (request.WarehouseId.HasValue)
        {
            availableBaseQuantity = await dbContext.Stocks
                .AsNoTracking()
                .Where(x => x.ProductId == product.Id && x.WarehouseId == request.WarehouseId.Value)
                .Select(x => (decimal?)(x.OnHandQtyBase - x.ReservedQtyBase))
                .SumAsync(cancellationToken) ?? 0;

            if (availableBaseQuantity < quantityBase.BaseValue)
            {
                warnings.Add("INSUFFICIENT_AVAILABLE_STOCK");
            }
        }

        var formattedEntered = request.EnteredQuantity.ToString("N", CultureInfo.GetCultureInfo("tr-TR"));
        var formattedBase = quantityBase.BaseValue.ToString("N", CultureInfo.GetCultureInfo("tr-TR"));
        var displayText = $"{formattedEntered} {option.Name} ({formattedBase} {product.BaseUom.DisplayName})";

        return new QuantityPreviewResult(
            product.Id,
            new UnitOfMeasureDto(product.BaseUom.Code, product.BaseUom.DisplayName, product.BaseUom.Dimension, product.BaseUom.DecimalScale),
            request.EnteredQuantity,
            request.EnteredPackagingId,
            option,
            quantityBase.BaseValue,
            displayText,
            availableBaseQuantity,
            warnings,
            request.ViewMode,
            request.OperationType,
            option.EffectiveVersion);
    }

    public async Task<BarcodeResolutionResult?> ResolveBarcodeAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        var record = await dbContext.ProductBarcodes
            .AsNoTracking()
            .Include(x => x.Product)
                .ThenInclude(x => x.BaseUom)
            .Include(x => x.Packaging)
            .SingleOrDefaultAsync(x => x.Barcode == barcode && x.IsActive && x.Product.IsActive, cancellationToken);

        if (record is null)
        {
            return null;
        }

        return new BarcodeResolutionResult(
            record.Barcode,
            record.ProductId,
            record.Product.Code,
            record.Product.Name,
            record.PackagingId,
            record.Packaging?.Level,
            record.Packaging?.Name,
            1,
            record.Packaging?.QuantityInBaseUom ?? 1,
            record.Product.BaseUom.Code);
    }

    private static StaffProductDto MapStaffProduct(ProductRecord record, DateTimeOffset now)
    {
        var packagings = record.Packagings
            .Where(packaging => IsEffective(packaging, now))
            .OrderBy(packaging => PackagingSortOrder(packaging.Level))
            .ThenBy(packaging => packaging.Name)
            .Select(MapPackaging)
            .ToArray();

        var primaryImage = record.Images
            .Where(image => image.IsPrimary)
            .OrderBy(image => image.SortOrder)
            .Select(image => image.Url)
            .FirstOrDefault();

        return new StaffProductDto(
            record.Id,
            record.Code,
            record.Slug,
            record.Name,
            record.Description,
            record.SizeLabel,
            record.Category?.Code ?? string.Empty,
            record.Category?.Name ?? string.Empty,
            record.IsActive,
            record.IsPublic,
            new UnitOfMeasureDto(record.BaseUom.Code, record.BaseUom.DisplayName, record.BaseUom.Dimension, record.BaseUom.DecimalScale),
            packagings,
            primaryImage,
            record.CreatedAt);
    }

    private static PublicProductDto MapPublicProduct(ProductRecord record, DateTimeOffset now)
    {
        var packagings = record.Packagings
            .Where(packaging => IsEffective(packaging, now) && packaging.IsSellable)
            .OrderBy(packaging => PackagingSortOrder(packaging.Level))
            .ThenBy(packaging => packaging.Name)
            .Select(MapPackaging)
            .ToArray();

        var primaryImage = record.Images
            .Where(image => image.IsPrimary)
            .OrderBy(image => image.SortOrder)
            .Select(image => image.Url)
            .FirstOrDefault();

        return new PublicProductDto(
            record.Id,
            record.Code,
            record.Slug,
            record.Name,
            record.Description,
            record.SizeLabel,
            record.Category?.Code ?? string.Empty,
            record.Category?.Name ?? string.Empty,
            new UnitOfMeasureDto(record.BaseUom.Code, record.BaseUom.DisplayName, record.BaseUom.Dimension, record.BaseUom.DecimalScale),
            packagings,
            primaryImage);
    }

    private static PackagingOptionDto MapPackaging(ProductPackagingRecord packaging)
        => new(
            packaging.Id,
            packaging.Level,
            packaging.Name,
            packaging.QuantityInBaseUom,
            packaging.IsSellable,
            packaging.AllowPartial,
            packaging.EffectiveFrom.ToString("O", CultureInfo.InvariantCulture));

    private static bool IsEffective(ProductPackagingRecord packaging, DateTimeOffset now)
        => packaging.EffectiveFrom <= now && (packaging.EffectiveTo is null || packaging.EffectiveTo > now);

    private static int PackagingSortOrder(string level)
        => level switch
        {
            "Pallet" => 1,
            "Case" => 2,
            "Package" => 3,
            "BaseUnit" => 4,
            _ => 5,
        };
}
