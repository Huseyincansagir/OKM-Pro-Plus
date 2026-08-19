namespace FactoryErp.Application.Warehouse;

public sealed record CreateStockTransferRequest(
    Guid ProductId,
    Guid SourceWarehouseId,
    Guid SourceLocationId,
    Guid TargetWarehouseId,
    Guid TargetLocationId,
    decimal EnteredQuantity,
    Guid? EnteredPackagingId,
    string ViewMode);

public sealed record StockTransferDto(
    Guid Id,
    Guid ProductId,
    Guid SourceWarehouseId,
    Guid SourceLocationId,
    Guid TargetWarehouseId,
    Guid TargetLocationId,
    decimal EnteredQuantity,
    Guid? EnteredPackagingId,
    string ViewMode,
    decimal QuantityBase,
    string PackagingSnapshot,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? CancelledAt);

public interface IStockTransferCommandService
{
    Task<StockTransferDto> CreateAsync(
        CreateStockTransferRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<StockTransferDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<StockTransferDto?> GetAsync(
        Guid transferId,
        CancellationToken cancellationToken = default);

    Task<StockTransferDto?> CompleteAsync(
        Guid transferId,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<StockTransferDto?> CancelAsync(
        Guid transferId,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);
}
