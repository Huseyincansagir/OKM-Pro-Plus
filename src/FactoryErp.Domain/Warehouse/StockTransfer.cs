using FactoryErp.Domain.Common;
using FactoryErp.Domain.Shared;

namespace FactoryErp.Domain.Warehouse;

public enum StockTransferStatus
{
    Draft,
    Completed,
    Cancelled,
}

public sealed class StockTransfer : AggregateRoot
{
    private StockTransfer(
        Guid id,
        DateTimeOffset now,
        Guid productId,
        Guid sourceWarehouseId,
        Guid sourceLocationId,
        Guid targetWarehouseId,
        Guid targetLocationId,
        decimal enteredQuantity,
        Guid? enteredPackagingId,
        string viewMode,
        PositiveQuantity quantityBase,
        string packagingSnapshot)
        : base(id, now)
    {
        ProductId = productId;
        SourceWarehouseId = sourceWarehouseId;
        SourceLocationId = sourceLocationId;
        TargetWarehouseId = targetWarehouseId;
        TargetLocationId = targetLocationId;
        EnteredQuantity = enteredQuantity;
        EnteredPackagingId = enteredPackagingId;
        ViewMode = viewMode;
        QuantityBase = quantityBase;
        PackagingSnapshot = packagingSnapshot;
        Status = StockTransferStatus.Draft;
    }

    public Guid ProductId { get; }
    public Guid SourceWarehouseId { get; }
    public Guid SourceLocationId { get; }
    public Guid TargetWarehouseId { get; }
    public Guid TargetLocationId { get; }
    public decimal EnteredQuantity { get; }
    public Guid? EnteredPackagingId { get; }
    public string ViewMode { get; }
    public PositiveQuantity QuantityBase { get; }
    public string PackagingSnapshot { get; }
    public StockTransferStatus Status { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }

    public static StockTransfer Create(
        Guid id,
        DateTimeOffset now,
        Guid productId,
        Guid sourceWarehouseId,
        Guid sourceLocationId,
        Guid targetWarehouseId,
        Guid targetLocationId,
        decimal enteredQuantity,
        Guid? enteredPackagingId,
        string viewMode,
        PositiveQuantity quantityBase,
        string packagingSnapshot)
    {
        DomainGuard.AgainstEmpty(productId, "PRODUCT_REQUIRED", "Transfer ürünü zorunludur.");
        DomainGuard.AgainstEmpty(sourceWarehouseId, "SOURCE_WAREHOUSE_REQUIRED", "Kaynak depo zorunludur.");
        DomainGuard.AgainstEmpty(sourceLocationId, "SOURCE_LOCATION_REQUIRED", "Kaynak konum zorunludur.");
        DomainGuard.AgainstEmpty(targetWarehouseId, "TARGET_WAREHOUSE_REQUIRED", "Hedef depo zorunludur.");
        DomainGuard.AgainstEmpty(targetLocationId, "TARGET_LOCATION_REQUIRED", "Hedef konum zorunludur.");
        DomainGuard.AgainstBlank(viewMode, "QUANTITY_VIEW_MODE_REQUIRED", "Miktar görünüm modu zorunludur.");
        DomainGuard.AgainstBlank(packagingSnapshot, "PACKAGING_SNAPSHOT_REQUIRED", "Ambalaj snapshot zorunludur.");

        if (sourceWarehouseId == targetWarehouseId && sourceLocationId == targetLocationId)
        {
            throw new DomainException(new(
                "TRANSFER_SOURCE_TARGET_SAME",
                "Kaynak ve hedef depo-konum aynı olamaz."));
        }

        if (enteredQuantity <= 0)
        {
            throw new DomainException(new(
                "TRANSFER_ENTERED_QUANTITY_INVALID",
                "Transfer miktarı sıfırdan büyük olmalıdır."));
        }

        if (quantityBase.BaseValue <= 0)
        {
            throw new DomainException(new(
                "TRANSFER_QUANTITY_INVALID",
                "Transfer temel miktarı sıfırdan büyük olmalıdır."));
        }

        return new StockTransfer(
            id,
            now,
            productId,
            sourceWarehouseId,
            sourceLocationId,
            targetWarehouseId,
            targetLocationId,
            enteredQuantity,
            enteredPackagingId,
            viewMode.Trim(),
            quantityBase,
            packagingSnapshot.Trim());
    }

    public void Complete(DateTimeOffset now)
    {
        if (Status != StockTransferStatus.Draft)
        {
            throw new DomainException(new(
                "TRANSFER_INVALID_TRANSITION",
                $"{Status} durumundaki transfer tamamlanamaz."));
        }

        Status = StockTransferStatus.Completed;
        CompletedAt = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status != StockTransferStatus.Draft)
        {
            throw new DomainException(new(
                "TRANSFER_INVALID_TRANSITION",
                $"{Status} durumundaki transfer iptal edilemez."));
        }

        Status = StockTransferStatus.Cancelled;
        CancelledAt = now;
    }

    public static StockTransfer Rehydrate(
        Guid id,
        DateTimeOffset createdAt,
        Guid productId,
        Guid sourceWarehouseId,
        Guid sourceLocationId,
        Guid targetWarehouseId,
        Guid targetLocationId,
        decimal enteredQuantity,
        Guid? enteredPackagingId,
        string viewMode,
        PositiveQuantity quantityBase,
        string packagingSnapshot,
        StockTransferStatus status,
        DateTimeOffset? completedAt,
        DateTimeOffset? cancelledAt)
    {
        var transfer = Create(
            id,
            createdAt,
            productId,
            sourceWarehouseId,
            sourceLocationId,
            targetWarehouseId,
            targetLocationId,
            enteredQuantity,
            enteredPackagingId,
            viewMode,
            quantityBase,
            packagingSnapshot);
        transfer.Status = status;
        transfer.CompletedAt = completedAt;
        transfer.CancelledAt = cancelledAt;
        return transfer;
    }
}
