namespace FactoryErp.Application.Warehouse;

public sealed record WarehouseDto(
    Guid Id,
    string Code,
    string Name,
    bool IsActive);

public sealed record WarehouseLocationDto(
    Guid Id,
    Guid WarehouseId,
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

public sealed record StockMovementRowDto(
    Guid Id,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    Guid WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    Guid LocationId,
    string LocationCode,
    string MovementType,
    string Effect,
    decimal QuantityBase,
    string SourceEntityType,
    Guid? SourceEntityId,
    Guid? ReversedFromId,
    string? PackagingSnapshot,
    DateTimeOffset CreatedAt);

public static class StockMovementEffects
{
    public const string In = "In";
    public const string Out = "Out";
    public const string Unknown = "Unknown";

    public static string FromMovementType(string movementType) => movementType switch
    {
        "ProductionIn" or "WarehouseTransferIn" or "CountIn" => In,
        "WarehouseTransferOut" or "DeliveryIssue" or "CountOut" => Out,
        _ => Unknown,
    };
}

public interface IStockQueryService
{
    Task<IReadOnlyCollection<WarehouseDto>> ListWarehousesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<WarehouseLocationDto>?> ListWarehouseLocationsAsync(
        Guid warehouseId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<StockRowDto>> ListStocksAsync(CancellationToken cancellationToken = default);

    Task<StockRowDto?> GetStockAsync(Guid stockId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<StockMovementRowDto>> ListStockMovementsAsync(CancellationToken cancellationToken = default);

    Task<StockMovementRowDto?> GetStockMovementAsync(Guid movementId, CancellationToken cancellationToken = default);
}
