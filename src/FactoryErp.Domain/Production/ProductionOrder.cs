using FactoryErp.Domain.Common;
using FactoryErp.Domain.Shared;

namespace FactoryErp.Domain.Production;

public enum ProductionOrderStatus
{
    Planned = 0,
    Released = 1,
    InProgress = 2,
    Paused = 3,
    Completed = 4,
    Cancelled = 5,
}

public sealed class ProductionOrder : AggregateRoot
{
    private ProductionOrder(
        Guid id,
        Guid productId,
        Guid warehouseId,
        PositiveQuantity plannedQuantity,
        DateTimeOffset now)
        : base(id, now)
    {
        ProductId = productId;
        WarehouseId = warehouseId;
        PlannedQuantity = plannedQuantity;
        CompletedQuantity = NonNegativeQuantity.Zero(plannedQuantity.Scale);
        Status = ProductionOrderStatus.Planned;
    }

    public Guid ProductId { get; }
    public Guid WarehouseId { get; }
    public PositiveQuantity PlannedQuantity { get; }
    public NonNegativeQuantity CompletedQuantity { get; private set; }
    public NonNegativeQuantity RemainingQuantity => NonNegativeQuantity.Create(
        PlannedQuantity.BaseValue - CompletedQuantity.BaseValue,
        PlannedQuantity.Scale);
    public ProductionOrderStatus Status { get; private set; }
    public static ProductionOrder Create(
        Guid id,
        Guid productId,
        Guid warehouseId,
        PositiveQuantity plannedQuantity,
        DateTimeOffset now)
        => new(id, productId, warehouseId, plannedQuantity, now);

    public static ProductionOrder Rehydrate(
        Guid id,
        Guid productId,
        Guid warehouseId,
        PositiveQuantity plannedQuantity,
        NonNegativeQuantity completedQuantity,
        ProductionOrderStatus status,
        DateTimeOffset now)
    {
        if (completedQuantity.BaseValue > plannedQuantity.BaseValue)
        {
            throw new DomainException(new(
                "PRODUCTION_INVARIANT_VIOLATION",
                "Tamamlanan üretim planlanan miktarı aşamaz."));
        }

        if (!Enum.IsDefined(status))
        {
            throw new DomainException(new(
                "PRODUCTION_STATUS_INVALID",
                "Geçersiz üretim iş emri durumu."));
        }

        return new ProductionOrder(id, productId, warehouseId, plannedQuantity, now)
        {
            CompletedQuantity = completedQuantity,
            Status = status,
        };
    }

    public void Release(DateTimeOffset now)
    {
        EnsureStatus(ProductionOrderStatus.Planned, "PRODUCTION_ORDER_NOT_RELEASEABLE", "Yalnızca planlanan iş emri serbest bırakılabilir.");
        Status = ProductionOrderStatus.Released;
        Touch(now);
    }

    public void Start(DateTimeOffset now)
    {
        EnsureStatus(ProductionOrderStatus.Released, "PRODUCTION_ORDER_NOT_STARTABLE", "Yalnızca serbest bırakılmış iş emri başlatılabilir.");
        Status = ProductionOrderStatus.InProgress;
        Touch(now);
    }

    public void Pause(DateTimeOffset now)
    {
        EnsureStatus(ProductionOrderStatus.InProgress, "PRODUCTION_ORDER_NOT_PAUSABLE", "Yalnızca devam eden iş emri duraklatılabilir.");
        Status = ProductionOrderStatus.Paused;
        Touch(now);
    }

    public void Resume(DateTimeOffset now)
    {
        EnsureStatus(ProductionOrderStatus.Paused, "PRODUCTION_ORDER_NOT_RESUMABLE", "Yalnızca duraklatılmış iş emri devam ettirilebilir.");
        Status = ProductionOrderStatus.InProgress;
        Touch(now);
    }

    public void RecordProduction(PositiveQuantity quantity, DateTimeOffset now)
    {
        if (Status != ProductionOrderStatus.InProgress)
        {
            throw new DomainException(new(
                "PRODUCTION_RECORD_INVALID_STATE",
                "Üretim kaydı yalnızca devam eden iş emrine eklenebilir."));
        }

        var next = CompletedQuantity.BaseValue + quantity.BaseValue;
        if (next > PlannedQuantity.BaseValue)
        {
            throw new DomainException(new(
                "PRODUCTION_QUANTITY_EXCEEDS_PLAN",
                "Üretim kaydı planlanan miktarı aşamaz."));
        }

        CompletedQuantity = NonNegativeQuantity.Create(next, PlannedQuantity.Scale);
        Touch(now);
    }

    public void Complete(DateTimeOffset now)
    {
        EnsureStatus(ProductionOrderStatus.InProgress, "PRODUCTION_ORDER_NOT_COMPLETABLE", "Yalnızca devam eden iş emri tamamlanabilir.");
        if (CompletedQuantity.BaseValue <= 0)
        {
            throw new DomainException(new(
                "PRODUCTION_COMPLETION_REQUIRES_RECORD",
                "Üretim iş emri en az bir üretim kaydı olmadan tamamlanamaz."));
        }

        Status = ProductionOrderStatus.Completed;
        Touch(now);
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status is not (ProductionOrderStatus.Planned or ProductionOrderStatus.Released))
        {
            throw new DomainException(new(
                "PRODUCTION_ORDER_NOT_CANCELLABLE",
                "Yalnızca planlanan veya serbest bırakılmış iş emri iptal edilebilir."));
        }

        Status = ProductionOrderStatus.Cancelled;
        Touch(now);
    }

    private void EnsureStatus(ProductionOrderStatus expected, string code, string message)
    {
        if (Status != expected)
        {
            throw new DomainException(new(code, message));
        }
    }

}
