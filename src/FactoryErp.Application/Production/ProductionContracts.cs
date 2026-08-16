namespace FactoryErp.Application.Production;

public sealed record CreateProductionOrderRequest(
    Guid ProductId,
    Guid WarehouseId,
    decimal PlannedQuantityBase);

public sealed record AddProductionRecordRequest(
    Guid WarehouseId,
    Guid LocationId,
    decimal EnteredQuantity,
    Guid? EnteredPackagingId,
    string ViewMode = "Packaging");

public sealed record CompleteProductionRequest(
    Guid WarehouseId,
    Guid LocationId);

public sealed record ProductionRecordDto(
    Guid Id,
    Guid ProductionOrderId,
    Guid ProductId,
    Guid WarehouseId,
    Guid LocationId,
    decimal QuantityBase,
    decimal EnteredQuantity,
    Guid? EnteredPackagingId,
    string PackagingSnapshot,
    DateTimeOffset CompletedAt);

public sealed record ProductionOrderDto(
    Guid Id,
    Guid ProductId,
    Guid WarehouseId,
    decimal PlannedQuantityBase,
    decimal CompletedQuantityBase,
    decimal RemainingQuantityBase,
    string Status,
    long RowVersion,
    IReadOnlyCollection<ProductionRecordDto> Records);

public interface IProductionCommandService
{
    Task<ProductionOrderDto> CreateProductionOrderAsync(
        CreateProductionOrderRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<ProductionOrderDto?> GetProductionOrderAsync(
        Guid productionOrderId,
        CancellationToken cancellationToken = default);

    Task<ProductionOrderDto?> ReleaseProductionOrderAsync(
        Guid productionOrderId,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<ProductionOrderDto?> StartProductionOrderAsync(
        Guid productionOrderId,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<ProductionOrderDto?> AddProductionRecordAsync(
        Guid productionOrderId,
        AddProductionRecordRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<ProductionOrderDto?> CompleteProductionOrderAsync(
        Guid productionOrderId,
        CompleteProductionRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default);
}
