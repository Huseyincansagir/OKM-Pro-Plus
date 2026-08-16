using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.Products;

public sealed class CatalogSeeder(FactoryErpDbContext dbContext)
{
    private static readonly Guid PieceUomId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid NapkinCategoryId = Guid.Parse("30000000-0000-0000-0000-000000000101");
    private static readonly Guid NapkinProductId = Guid.Parse("30000000-0000-0000-0000-000000000201");
    private static readonly Guid MainWarehouseId = Guid.Parse("30000000-0000-0000-0000-000000000301");
    private static readonly Guid MainLocationId = Guid.Parse("30000000-0000-0000-0000-000000000302");

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!await dbContext.UnitsOfMeasure.AnyAsync(cancellationToken))
        {
            dbContext.UnitsOfMeasure.Add(new UnitOfMeasureRecord
            {
                Id = PieceUomId,
                Code = "Piece",
                DisplayName = "Adet",
                Dimension = "Count",
                DecimalScale = 0,
                IsActive = true,
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var category = await dbContext.ProductCategories.SingleOrDefaultAsync(x => x.Code == "NAPKIN", cancellationToken);
        if (category is null)
        {
            category = new ProductCategoryRecord
            {
                Id = NapkinCategoryId,
                Code = "NAPKIN",
                Name = "Peçeteler",
                Slug = "peceteler",
                IsActive = true,
            };
            dbContext.ProductCategories.Add(category);
        }

        var product = await dbContext.Products
            .Include(x => x.Packagings)
            .SingleOrDefaultAsync(x => x.Code == "NAP-001", cancellationToken);
        if (product is null)
        {
            var now = DateTimeOffset.UtcNow;
            product = new ProductRecord
            {
                Id = NapkinProductId,
                Code = "NAP-001",
                Slug = "premium-pecete-33x33",
                Name = "Premium Peçete 33x33",
                Description = "Yüksek emicilik ve düzgün katlama özelliğine sahip premium peçete.",
                SizeLabel = "33x33 cm",
                BaseUomId = PieceUomId,
                CategoryId = category.Id,
                IsActive = true,
                IsPublic = true,
                CreatedAt = now,
                UpdatedAt = now,
                RowVersion = 1,
            };
            dbContext.Products.Add(product);
            product.Packagings.Add(new ProductPackagingRecord
            {
                Id = Guid.Parse("30000000-0000-0000-0000-000000000211"),
                ProductId = product.Id,
                Level = "BaseUnit",
                Name = "Adet",
                UnitsPerParent = 1,
                QuantityInBaseUom = 1,
                IsSellable = false,
                AllowPartial = true,
                EffectiveFrom = now,
            });
            product.Packagings.Add(new ProductPackagingRecord
            {
                Id = Guid.Parse("30000000-0000-0000-0000-000000000212"),
                ProductId = product.Id,
                Level = "Package",
                Name = "Paket",
                UnitsPerParent = 100,
                QuantityInBaseUom = 100,
                IsSellable = true,
                AllowPartial = false,
                EffectiveFrom = now,
            });
            product.Packagings.Add(new ProductPackagingRecord
            {
                Id = Guid.Parse("30000000-0000-0000-0000-000000000213"),
                ProductId = product.Id,
                Level = "Case",
                Name = "Koli",
                UnitsPerParent = 20,
                QuantityInBaseUom = 2_000,
                IsSellable = true,
                AllowPartial = false,
                EffectiveFrom = now,
            });
            product.Packagings.Add(new ProductPackagingRecord
            {
                Id = Guid.Parse("30000000-0000-0000-0000-000000000214"),
                ProductId = product.Id,
                Level = "Pallet",
                Name = "Palet",
                UnitsPerParent = 40,
                QuantityInBaseUom = 80_000,
                IsSellable = true,
                AllowPartial = false,
                EffectiveFrom = now,
            });
            dbContext.ProductBarcodes.AddRange(
                new ProductBarcodeRecord
                {
                    Id = Guid.Parse("30000000-0000-0000-0000-000000000221"),
                    ProductId = product.Id,
                    PackagingId = Guid.Parse("30000000-0000-0000-0000-000000000212"),
                    Barcode = "869000000001",
                    IsActive = true,
                },
                new ProductBarcodeRecord
                {
                    Id = Guid.Parse("30000000-0000-0000-0000-000000000222"),
                    ProductId = product.Id,
                    PackagingId = Guid.Parse("30000000-0000-0000-0000-000000000213"),
                    Barcode = "869000000002",
                    IsActive = true,
                });
        }

        var warehouse = await dbContext.Warehouses.SingleOrDefaultAsync(x => x.Code == "MAIN", cancellationToken);
        if (warehouse is null)
        {
            warehouse = new WarehouseRecord
            {
                Id = MainWarehouseId,
                Code = "MAIN",
                Name = "Ana Depo",
                IsActive = true,
            };
            dbContext.Warehouses.Add(warehouse);
            dbContext.WarehouseLocations.Add(new WarehouseLocationRecord
            {
                Id = MainLocationId,
                WarehouseId = warehouse.Id,
                Code = "A-01",
                Name = "Ana Depo A-01",
                IsActive = true,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (!await dbContext.Stocks.AnyAsync(x => x.ProductId == NapkinProductId && x.WarehouseId == MainWarehouseId, cancellationToken))
        {
            dbContext.Stocks.Add(new StockRecord
            {
                Id = Guid.Parse("30000000-0000-0000-0000-000000000401"),
                ProductId = NapkinProductId,
                WarehouseId = MainWarehouseId,
                LocationId = MainLocationId,
                OnHandQtyBase = 18_000,
                ReservedQtyBase = 0,
                RowVersion = 1,
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
