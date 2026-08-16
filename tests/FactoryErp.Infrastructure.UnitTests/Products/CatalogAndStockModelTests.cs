using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FactoryErp.Infrastructure.UnitTests.Products;

public sealed class CatalogAndStockModelTests
{
    [Fact]
    public void Product_uses_public_catalog_indexes_and_explicit_columns()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(ProductRecord))!;

        entity.GetTableName().Should().Be("products");
        entity.FindProperty(nameof(ProductRecord.IsPublic))!.GetColumnName().Should().Be("is_public");
        entity.GetIndexes().Should().Contain(index => index.IsUnique && index.Properties.Select(x => x.Name)
            .SequenceEqual(new[] { nameof(ProductRecord.Code) }));
        entity.GetIndexes().Should().Contain(index => index.IsUnique && index.Properties.Select(x => x.Name)
            .SequenceEqual(new[] { nameof(ProductRecord.Slug) }));
    }

    [Fact]
    public void Packaging_uses_precision_and_positive_quantity_constraints()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var entity = model.FindEntityType(typeof(ProductPackagingRecord))!;

        entity.FindProperty(nameof(ProductPackagingRecord.QuantityInBaseUom))!.GetPrecision().Should().Be(18);
        entity.FindProperty(nameof(ProductPackagingRecord.QuantityInBaseUom))!.GetScale().Should().Be(6);
        entity.GetCheckConstraints().Should().Contain(constraint =>
            constraint.Name == "ck_product_packagings_quantity_positive");
    }

    [Fact]
    public void Stock_uses_unique_location_key_and_non_negative_constraints()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var entity = model.FindEntityType(typeof(StockRecord))!;

        entity.GetIndexes().Should().Contain(index => index.IsUnique && index.Properties.Select(x => x.Name)
            .SequenceEqual(new[]
            {
                nameof(StockRecord.ProductId),
                nameof(StockRecord.WarehouseId),
                nameof(StockRecord.LocationId),
            }));
        entity.GetCheckConstraints().Should().Contain(constraint =>
            constraint.Name == "ck_stocks_reserved_not_above_on_hand");
    }

    [Fact]
    public void Barcode_uses_filtered_unique_active_index()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(ProductBarcodeRecord))!;

        entity.GetIndexes().Should().Contain(index =>
            index.IsUnique
            && index.GetFilter() == "is_active = true"
            && index.Properties.Select(x => x.Name).SequenceEqual(new[] { nameof(ProductBarcodeRecord.Barcode) }));
    }

    private static FactoryErpDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FactoryErpDbContext>()
            .UseNpgsql("Host=localhost;Database=factory_erp_g1;Username=factory_erp;Password=dev_only_change_me")
            .Options;

        return new FactoryErpDbContext(options);
    }
}
