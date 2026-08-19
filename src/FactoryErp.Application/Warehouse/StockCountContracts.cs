namespace FactoryErp.Application.Warehouse;

public sealed record CreateStockCountRequest(
    Guid WarehouseId,
    Guid LocationId);

public sealed record AddStockCountItemRequest(
    Guid ProductId,
    decimal CountedQtyBase);

public sealed record StockCountItemDto(
    Guid Id,
    Guid ProductId,
    string ProductCode,
    decimal CountedQtyBase,
    decimal SystemOnHandQtyBase,
    decimal VarianceQtyBase);

public sealed record StockCountDto(
    Guid Id,
    string DocumentNumber,
    Guid WarehouseId,
    string WarehouseCode,
    Guid LocationId,
    string LocationCode,
    string Status,
    IReadOnlyCollection<StockCountItemDto> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

public interface IStockCountCommandService
{
    Task<IReadOnlyCollection<StockCountDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<StockCountDto?> GetAsync(Guid countId, CancellationToken cancellationToken = default);

    Task<StockCountDto> CreateAsync(
        CreateStockCountRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<StockCountDto?> AddItemAsync(
        Guid countId,
        AddStockCountItemRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<StockCountDto?> CompleteAsync(
        Guid countId,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);
}
