using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FactoryErp.Application.Abstractions.Persistence;
using FactoryErp.Application.Products;
using FactoryErp.Application.Warehouse;
using FactoryErp.Domain.Common;
using FactoryErp.Domain.Shared;
using FactoryErp.Domain.Warehouse;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.Warehouse;

public sealed class StockTransferCommandService(
    FactoryErpDbContext dbContext,
    IProductCatalogService productCatalogService,
    IAuditWriter auditWriter,
    IIdempotencyStore idempotencyStore) : IStockTransferCommandService
{
    public async Task<StockTransferDto> CreateAsync(
        CreateStockTransferRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var scope = $"stock-transfer:create:{actorId}";
        var payloadHash = ComputePayloadHash(request);
        var replay = await TryReplayAsync<StockTransferDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await EnsureContextAsync(request, cancellationToken);
        var preview = await productCatalogService.PreviewQuantityAsync(
            new QuantityPreviewRequest(
                request.ProductId,
                request.EnteredQuantity,
                request.EnteredPackagingId,
                request.ViewMode,
                "WarehouseTransfer",
                request.SourceWarehouseId),
            cancellationToken);
        if (preview is null)
        {
            throw new DomainException(new(
                "PRODUCT_OR_PACKAGING_NOT_FOUND",
                "Transfer ürün veya ambalaj bilgisi bulunamadı."));
        }

        var transfer = StockTransfer.Create(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            request.ProductId,
            request.SourceWarehouseId,
            request.SourceLocationId,
            request.TargetWarehouseId,
            request.TargetLocationId,
            request.EnteredQuantity,
            request.EnteredPackagingId,
            request.ViewMode,
            PositiveQuantity.Create(preview.QuantityBase, 6),
            JsonSerializer.Serialize(preview.EnteredPackaging));

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var record = ToRecord(transfer);
        dbContext.StockTransfers.Add(record);
        await auditWriter.AppendAsync(new(
            "StockTransferCreated",
            nameof(StockTransferRecord),
            record.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new
            {
                record.ProductId,
                record.SourceWarehouseId,
                record.SourceLocationId,
                record.TargetWarehouseId,
                record.TargetLocationId,
                record.QuantityBase,
                record.Status,
            })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = Map(record);
        await idempotencyStore.SaveAsync(
            scope,
            idempotencyKey,
            payloadHash,
            201,
            JsonSerializer.Serialize(result),
            DateTimeOffset.UtcNow.AddDays(30),
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<StockTransferDto?> GetAsync(Guid transferId, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.StockTransfers
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == transferId, cancellationToken);
        return record is null ? null : Map(record);
    }

    public async Task<StockTransferDto?> CompleteAsync(
        Guid transferId,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var scope = $"stock-transfer:complete:{actorId}:{transferId}";
        var payloadHash = ComputePayloadHash(new { transferId, action = "complete" });
        var replay = await TryReplayAsync<StockTransferDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var record = await LockTransferAsync(transferId, cancellationToken);
        if (record is null)
        {
            return null;
        }

        var transfer = ToAggregate(record);
        var now = DateTimeOffset.UtcNow;
        transfer.Complete(now);

        await EnsureStockRowAsync(record.ProductId, record.SourceWarehouseId, record.SourceLocationId, cancellationToken);
        await EnsureStockRowAsync(record.ProductId, record.TargetWarehouseId, record.TargetLocationId, cancellationToken);
        var stocks = await LockStocksAsync(record, cancellationToken);
        var sourceStock = stocks.Single(x => x.WarehouseId == record.SourceWarehouseId && x.LocationId == record.SourceLocationId);
        var targetStock = stocks.Single(x => x.WarehouseId == record.TargetWarehouseId && x.LocationId == record.TargetLocationId);
        var availableBase = sourceStock.OnHandQtyBase - sourceStock.ReservedQtyBase;
        if (availableBase < record.QuantityBase)
        {
            throw new DomainException(new(
                "INSUFFICIENT_AVAILABLE_STOCK",
                "Transfer için kaynak depoda yeterli kullanılabilir stok yok.",
                new Dictionary<string, object?>
                {
                    ["availableBaseQuantity"] = availableBase,
                    ["requestedBaseQuantity"] = record.QuantityBase,
                }));
        }

        sourceStock.OnHandQtyBase -= record.QuantityBase;
        targetStock.OnHandQtyBase += record.QuantityBase;
        record.Status = transfer.Status.ToString();
        record.CompletedAt = transfer.CompletedAt;
        dbContext.StockMovements.Add(new StockMovementRecord
        {
            Id = Guid.NewGuid(),
            ProductId = record.ProductId,
            WarehouseId = record.SourceWarehouseId,
            LocationId = record.SourceLocationId,
            MovementType = "WarehouseTransferOut",
            QuantityBase = record.QuantityBase,
            SourceEntityType = nameof(StockTransferRecord),
            SourceEntityId = record.Id,
            PackagingSnapshot = record.PackagingSnapshot,
            CreatedAt = now,
        });
        dbContext.StockMovements.Add(new StockMovementRecord
        {
            Id = Guid.NewGuid(),
            ProductId = record.ProductId,
            WarehouseId = record.TargetWarehouseId,
            LocationId = record.TargetLocationId,
            MovementType = "WarehouseTransferIn",
            QuantityBase = record.QuantityBase,
            SourceEntityType = nameof(StockTransferRecord),
            SourceEntityId = record.Id,
            PackagingSnapshot = record.PackagingSnapshot,
            CreatedAt = now,
        });
        await auditWriter.AppendAsync(new(
            "StockTransferCompleted",
            nameof(StockTransferRecord),
            record.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new
            {
                record.Status,
                record.QuantityBase,
                sourceMovement = "WarehouseTransferOut",
                targetMovement = "WarehouseTransferIn",
            })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = Map(record);
        await idempotencyStore.SaveAsync(
            scope,
            idempotencyKey,
            payloadHash,
            200,
            JsonSerializer.Serialize(result),
            DateTimeOffset.UtcNow.AddDays(30),
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<StockTransferDto?> CancelAsync(
        Guid transferId,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var scope = $"stock-transfer:cancel:{actorId}:{transferId}";
        var payloadHash = ComputePayloadHash(new { transferId, action = "cancel" });
        var replay = await TryReplayAsync<StockTransferDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var record = await LockTransferAsync(transferId, cancellationToken);
        if (record is null)
        {
            return null;
        }

        var transfer = ToAggregate(record);
        var now = DateTimeOffset.UtcNow;
        transfer.Cancel(now);
        record.Status = transfer.Status.ToString();
        record.CancelledAt = transfer.CancelledAt;
        await auditWriter.AppendAsync(new(
            "StockTransferCancelled",
            nameof(StockTransferRecord),
            record.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { record.Status, record.CancelledAt })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = Map(record);
        await idempotencyStore.SaveAsync(
            scope,
            idempotencyKey,
            payloadHash,
            200,
            JsonSerializer.Serialize(result),
            DateTimeOffset.UtcNow.AddDays(30),
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private async Task EnsureContextAsync(CreateStockTransferRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Products.AnyAsync(x => x.Id == request.ProductId && x.IsActive, cancellationToken))
        {
            throw new DomainException(new("PRODUCT_NOT_FOUND", "Aktif transfer ürünü bulunamadı."));
        }

        if (!await dbContext.Warehouses.AnyAsync(x => x.Id == request.SourceWarehouseId && x.IsActive, cancellationToken)
            || !await dbContext.Warehouses.AnyAsync(x => x.Id == request.TargetWarehouseId && x.IsActive, cancellationToken))
        {
            throw new DomainException(new("WAREHOUSE_NOT_FOUND", "Aktif kaynak veya hedef depo bulunamadı."));
        }

        if (!await dbContext.WarehouseLocations.AnyAsync(
                x => x.Id == request.SourceLocationId && x.WarehouseId == request.SourceWarehouseId && x.IsActive,
                cancellationToken))
        {
            throw new DomainException(new("SOURCE_LOCATION_NOT_FOUND", "Kaynak konum kaynak depoya ait değil veya aktif değil."));
        }

        if (!await dbContext.WarehouseLocations.AnyAsync(
                x => x.Id == request.TargetLocationId && x.WarehouseId == request.TargetWarehouseId && x.IsActive,
                cancellationToken))
        {
            throw new DomainException(new("TARGET_LOCATION_NOT_FOUND", "Hedef konum hedef depoya ait değil veya aktif değil."));
        }
    }

    private async Task EnsureStockRowAsync(
        Guid productId,
        Guid warehouseId,
        Guid locationId,
        CancellationToken cancellationToken)
    {
        var stockId = Guid.NewGuid();
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO stocks (id, product_id, warehouse_id, location_id, on_hand_qty_base, reserved_qty_base, row_version)
            VALUES ({stockId}, {productId}, {warehouseId}, {locationId}, 0, 0, 1)
            ON CONFLICT (product_id, warehouse_id, location_id) DO NOTHING
            """, cancellationToken);
    }

    private async Task<List<StockRecord>> LockStocksAsync(StockTransferRecord record, CancellationToken cancellationToken)
        => await dbContext.Stocks
            .FromSqlInterpolated($"""
                SELECT * FROM stocks
                WHERE product_id = {record.ProductId}
                  AND ((warehouse_id = {record.SourceWarehouseId} AND location_id = {record.SourceLocationId})
                    OR (warehouse_id = {record.TargetWarehouseId} AND location_id = {record.TargetLocationId}))
                ORDER BY warehouse_id, location_id
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken);

    private async Task<StockTransferRecord?> LockTransferAsync(Guid transferId, CancellationToken cancellationToken)
        => await dbContext.StockTransfers
            .FromSqlInterpolated($"SELECT * FROM stock_transfers WHERE id = {transferId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    private static StockTransferRecord ToRecord(StockTransfer transfer)
        => new()
        {
            Id = transfer.Id,
            ProductId = transfer.ProductId,
            SourceWarehouseId = transfer.SourceWarehouseId,
            SourceLocationId = transfer.SourceLocationId,
            TargetWarehouseId = transfer.TargetWarehouseId,
            TargetLocationId = transfer.TargetLocationId,
            EnteredQuantity = transfer.EnteredQuantity,
            EnteredPackagingId = transfer.EnteredPackagingId,
            ViewMode = transfer.ViewMode,
            QuantityBase = transfer.QuantityBase.BaseValue,
            PackagingSnapshot = transfer.PackagingSnapshot,
            Status = transfer.Status.ToString(),
            CreatedAt = transfer.CreatedAt,
            CompletedAt = transfer.CompletedAt,
            CancelledAt = transfer.CancelledAt,
            RowVersion = 1,
        };

    private static StockTransfer ToAggregate(StockTransferRecord record)
        => StockTransfer.Rehydrate(
            record.Id,
            record.CreatedAt,
            record.ProductId,
            record.SourceWarehouseId,
            record.SourceLocationId,
            record.TargetWarehouseId,
            record.TargetLocationId,
            record.EnteredQuantity,
            record.EnteredPackagingId,
            record.ViewMode,
            PositiveQuantity.Create(record.QuantityBase, 6),
            record.PackagingSnapshot,
            Enum.Parse<StockTransferStatus>(record.Status, ignoreCase: false),
            record.CompletedAt,
            record.CancelledAt);

    private static StockTransferDto Map(StockTransferRecord record)
        => new(
            record.Id,
            record.ProductId,
            record.SourceWarehouseId,
            record.SourceLocationId,
            record.TargetWarehouseId,
            record.TargetLocationId,
            record.EnteredQuantity,
            record.EnteredPackagingId,
            record.ViewMode,
            record.QuantityBase,
            record.PackagingSnapshot,
            record.Status,
            record.CreatedAt,
            record.CompletedAt,
            record.CancelledAt);

    private async Task<T?> TryReplayAsync<T>(
        string scope,
        string key,
        string payloadHash,
        CancellationToken cancellationToken)
    {
        var stored = await idempotencyStore.FindAsync(scope, key, cancellationToken);
        if (stored is null)
        {
            return default;
        }

        if (!string.Equals(stored.PayloadHash, payloadHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException(new(
                "IDEMPOTENCY_PAYLOAD_MISMATCH",
                "Aynı Idempotency-Key farklı payload ile tekrar kullanılamaz."));
        }

        return JsonSerializer.Deserialize<T>(stored.ResponseBody);
    }

    private static string ComputePayloadHash(object payload)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)))).ToLowerInvariant();
}
