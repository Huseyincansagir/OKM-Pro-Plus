namespace FactoryErp.Application.Warehouse;

public sealed record WarehouseDto(
    Guid Id,
    string Code,
    string Name,
    bool IsActive);

public sealed record StockRowDto(
    Guid Id,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    Guid WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    Guid LocationId,
    string LocationCode,
    decimal OnHandQtyBase,
    decimal ReservedQtyBase,
    decimal AvailableQtyBase,
    long RowVersion);

public interface IStockQueryService
{
    Task<IReadOnlyCollection<WarehouseDto>> ListWarehousesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<StockRowDto>> ListStocksAsync(CancellationToken cancellationToken = default);

    Task<StockRowDto?> GetStockAsync(Guid stockId, CancellationToken cancellationToken = default);
}
