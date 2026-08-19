using FactoryErp.Application.Warehouse;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.Warehouse;

public sealed class StockQueryService(FactoryErpDbContext dbContext) : IStockQueryService
{
    public async Task<IReadOnlyCollection<WarehouseDto>> ListWarehousesAsync(CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Warehouses
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .Take(100)
            .ToArrayAsync(cancellationToken);
        return rows.Select(x => new WarehouseDto(x.Id, x.Code, x.Name, x.IsActive)).ToArray();
    }

    public async Task<IReadOnlyCollection<StockRowDto>> ListStocksAsync(CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Stocks
            .AsNoTracking()
            .OrderBy(x => x.ProductId)
            .ThenBy(x => x.WarehouseId)
            .Take(100)
            .ToArrayAsync(cancellationToken);
        return await MapStocksAsync(rows, cancellationToken);
    }

    public async Task<StockRowDto?> GetStockAsync(Guid stockId, CancellationToken cancellationToken = default)
    {
        var row = await dbContext.Stocks
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == stockId, cancellationToken);
        if (row is null)
        {
            return null;
        }

        var mapped = await MapStocksAsync(new[] { row }, cancellationToken);
        return mapped[0];
    }

    private async Task<IReadOnlyList<StockRowDto>> MapStocksAsync(
        IReadOnlyCollection<StockRecord> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return Array.Empty<StockRowDto>();
        }

        var productIds = rows.Select(x => x.ProductId).Distinct().ToArray();
        var warehouseIds = rows.Select(x => x.WarehouseId).Distinct().ToArray();
        var locationIds = rows.Select(x => x.LocationId).Distinct().ToArray();

        var products = await dbContext.Products
            .AsNoTracking()
            .Where(x => productIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var warehouses = await dbContext.Warehouses
            .AsNoTracking()
            .Where(x => warehouseIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var locations = await dbContext.WarehouseLocations
            .AsNoTracking()
            .Where(x => locationIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        return rows.Select(row =>
        {
            products.TryGetValue(row.ProductId, out var product);
            warehouses.TryGetValue(row.WarehouseId, out var warehouse);
            locations.TryGetValue(row.LocationId, out var location);
            return new StockRowDto(
                row.Id,
                row.ProductId,
                product?.Code ?? string.Empty,
                product?.Name ?? string.Empty,
                row.WarehouseId,
                warehouse?.Code ?? string.Empty,
                warehouse?.Name ?? string.Empty,
                row.LocationId,
                location?.Code ?? string.Empty,
                row.OnHandQtyBase,
                row.ReservedQtyBase,
                row.OnHandQtyBase - row.ReservedQtyBase,
                row.RowVersion);
        }).ToArray();
    }
}
