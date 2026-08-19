using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FactoryErp.Application.Abstractions.Persistence;
using FactoryErp.Application.Products;
using FactoryErp.Application.Production;
using FactoryErp.Domain.Common;
using FactoryErp.Domain.Production;
using FactoryErp.Domain.Shared;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.Production;

public sealed class ProductionCommandService(
    FactoryErpDbContext dbContext,
    IProductCatalogService productCatalogService,
    IAuditWriter auditWriter,
    IIdempotencyStore idempotencyStore) : IProductionCommandService
{
    public async Task<ProductionOrderDto> CreateProductionOrderAsync(
        CreateProductionOrderRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var plannedQuantity = PositiveQuantity.Create(request.PlannedQuantityBase, 6);
        var idempotencyScope = $"production-order:create:{actorId}";
        var payloadHash = ComputePayloadHash(request);
        var replay = await TryReplayAsync<ProductionOrderDto>(idempotencyScope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await EnsureProductAndWarehouseAsync(request.ProductId, request.WarehouseId, cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var order = ProductionOrder.Create(
            Guid.NewGuid(),
            request.ProductId,
            request.WarehouseId,
            plannedQuantity,
            now);
        var record = ToRecord(order);
        dbContext.ProductionOrders.Add(record);
        await auditWriter.AppendAsync(new(
            "ProductionOrderCreated",
            nameof(ProductionOrderRecord),
            record.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { record.Id, record.ProductId, record.PlannedQtyBase, record.Status })));
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = Map(record, Array.Empty<ProductionRecord>());
        await idempotencyStore.SaveAsync(
            idempotencyScope,
            idempotencyKey,
            payloadHash,
            201,
            JsonSerializer.Serialize(result),
            DateTimeOffset.UtcNow.AddDays(30),
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyCollection<ProductionOrderDto>> ListProductionOrdersAsync(
        CancellationToken cancellationToken = default)
    {
        var orders = await dbContext.ProductionOrders
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .Take(100)
            .ToArrayAsync(cancellationToken);
        return orders.Select(order => Map(order, Array.Empty<ProductionRecord>())).ToArray();
    }

    public async Task<ProductionOrderDto?> GetProductionOrderAsync(
        Guid productionOrderId,
        CancellationToken cancellationToken = default)
    {
        var order = await dbContext.ProductionOrders
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == productionOrderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        var records = await dbContext.ProductionRecords
            .AsNoTracking()
            .Where(x => x.ProductionOrderId == productionOrderId)
            .OrderBy(x => x.CompletedAt)
            .ToArrayAsync(cancellationToken);
        return Map(order, records);
    }

    public Task<ProductionOrderDto?> ReleaseProductionOrderAsync(
        Guid productionOrderId,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
        => TransitionAsync(
            productionOrderId,
            actorId,
            idempotencyKey,
            correlationId,
            "release",
            static (order, now) => order.Release(now),
            "ProductionOrderReleased",
            cancellationToken);

    public Task<ProductionOrderDto?> StartProductionOrderAsync(
        Guid productionOrderId,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
        => TransitionAsync(
            productionOrderId,
            actorId,
            idempotencyKey,
            correlationId,
            "start",
            static (order, now) => order.Start(now),
            "ProductionOrderStarted",
            cancellationToken);

    public async Task<ProductionOrderDto?> AddProductionRecordAsync(
        Guid productionOrderId,
        AddProductionRecordRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var orderSnapshot = await dbContext.ProductionOrders
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == productionOrderId, cancellationToken);
        if (orderSnapshot is null)
        {
            return null;
        }

        if (orderSnapshot.WarehouseId != request.WarehouseId)
        {
            throw new DomainException(new("PRODUCTION_WAREHOUSE_MISMATCH", "Üretim kaydı deposu iş emrinin deposuyla eşleşmelidir."));
        }

        var locationExists = await dbContext.WarehouseLocations
            .AnyAsync(x => x.Id == request.LocationId && x.WarehouseId == request.WarehouseId && x.IsActive, cancellationToken);
        if (!locationExists)
        {
            throw new DomainException(new("WAREHOUSE_LOCATION_NOT_FOUND", "Üretim kaydı için aktif depo konumu bulunamadı."));
        }

        var preview = await productCatalogService.PreviewQuantityAsync(
            new QuantityPreviewRequest(
                orderSnapshot.ProductId,
                request.EnteredQuantity,
                request.EnteredPackagingId,
                request.ViewMode,
                "ProductionRecord",
                request.WarehouseId),
            cancellationToken);
        if (preview is null)
        {
            throw new DomainException(new("PRODUCT_OR_PACKAGING_NOT_FOUND", "Üretim kaydındaki ürün veya ambalaj bulunamadı."));
        }

        var idempotencyScope = $"production-record:create:{actorId}:{productionOrderId}";
        var payloadHash = ComputePayloadHash(new { productionOrderId, request, quantityBase = preview.QuantityBase });
        var replay = await TryReplayAsync<ProductionOrderDto>(idempotencyScope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var orderRecord = await LockOrderAsync(productionOrderId, cancellationToken);
        if (orderRecord is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var order = Rehydrate(orderRecord);
        var quantity = PositiveQuantity.Create(preview.QuantityBase, 6);
        order.RecordProduction(quantity, now);
        ApplyAggregate(orderRecord, order);
        var productionRecord = new ProductionRecord
        {
            Id = Guid.NewGuid(),
            ProductionOrderId = orderRecord.Id,
            ProductId = orderRecord.ProductId,
            WarehouseId = request.WarehouseId,
            LocationId = request.LocationId,
            QuantityBase = quantity.BaseValue,
            EnteredQuantity = request.EnteredQuantity,
            EnteredPackagingId = request.EnteredPackagingId,
            PackagingSnapshot = JsonSerializer.Serialize(preview.EnteredPackaging),
            CompletedAt = now,
        };
        dbContext.ProductionRecords.Add(productionRecord);
        await auditWriter.AppendAsync(new(
            "ProductionRecordCreated",
            nameof(ProductionRecord),
            productionRecord.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { productionRecord.QuantityBase, productionRecord.EnteredQuantity })));
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = await LoadMappedOrderAsync(orderRecord.Id, cancellationToken);
        await idempotencyStore.SaveAsync(
            idempotencyScope,
            idempotencyKey,
            payloadHash,
            200,
            JsonSerializer.Serialize(result),
            DateTimeOffset.UtcNow.AddDays(30),
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<ProductionOrderDto?> CompleteProductionOrderAsync(
        Guid productionOrderId,
        CompleteProductionRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var idempotencyScope = $"production-order:complete:{actorId}:{productionOrderId}";
        var payloadHash = ComputePayloadHash(new { productionOrderId, request });
        var replay = await TryReplayAsync<ProductionOrderDto>(idempotencyScope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var orderRecord = await LockOrderAsync(productionOrderId, cancellationToken);
        if (orderRecord is null)
        {
            return null;
        }

        if (orderRecord.WarehouseId != request.WarehouseId)
        {
            throw new DomainException(new("PRODUCTION_WAREHOUSE_MISMATCH", "Tamamlama deposu iş emrinin deposuyla eşleşmelidir."));
        }

        var locationExists = await dbContext.WarehouseLocations
            .AnyAsync(x => x.Id == request.LocationId && x.WarehouseId == request.WarehouseId && x.IsActive, cancellationToken);
        if (!locationExists)
        {
            throw new DomainException(new("WAREHOUSE_LOCATION_NOT_FOUND", "Üretim tamamlanacak aktif depo konumu bulunamadı."));
        }

        var now = DateTimeOffset.UtcNow;
        var order = Rehydrate(orderRecord);
        order.Complete(now);
        ApplyAggregate(orderRecord, order);

        var stock = await dbContext.Stocks
            .FromSqlInterpolated($"SELECT * FROM stocks WHERE product_id = {orderRecord.ProductId} AND warehouse_id = {request.WarehouseId} AND location_id = {request.LocationId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (stock is null)
        {
            stock = new StockRecord
            {
                Id = Guid.NewGuid(),
                ProductId = orderRecord.ProductId,
                WarehouseId = request.WarehouseId,
                LocationId = request.LocationId,
                OnHandQtyBase = 0,
                ReservedQtyBase = 0,
                RowVersion = 1,
            };
            dbContext.Stocks.Add(stock);
        }

        stock.OnHandQtyBase += order.CompletedQuantity.BaseValue;
        dbContext.StockMovements.Add(new StockMovementRecord
        {
            Id = Guid.NewGuid(),
            ProductId = orderRecord.ProductId,
            WarehouseId = request.WarehouseId,
            LocationId = request.LocationId,
            MovementType = "ProductionIn",
            QuantityBase = order.CompletedQuantity.BaseValue,
            SourceEntityType = nameof(ProductionOrderRecord),
            SourceEntityId = orderRecord.Id,
            CreatedAt = now,
        });
        await auditWriter.AppendAsync(new(
            "ProductionOrderCompleted",
            nameof(ProductionOrderRecord),
            orderRecord.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { orderRecord.Status, order.CompletedQuantity.BaseValue, movementType = "ProductionIn" })));
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = await LoadMappedOrderAsync(orderRecord.Id, cancellationToken);
        await idempotencyStore.SaveAsync(
            idempotencyScope,
            idempotencyKey,
            payloadHash,
            200,
            JsonSerializer.Serialize(result),
            DateTimeOffset.UtcNow.AddDays(30),
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private async Task<ProductionOrderDto?> TransitionAsync(
        Guid productionOrderId,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        string action,
        Action<ProductionOrder, DateTimeOffset> transition,
        string auditAction,
        CancellationToken cancellationToken)
    {
        var idempotencyScope = $"production-order:{action}:{actorId}:{productionOrderId}";
        var payloadHash = ComputePayloadHash(new { productionOrderId, action });
        var replay = await TryReplayAsync<ProductionOrderDto>(idempotencyScope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var orderRecord = await LockOrderAsync(productionOrderId, cancellationToken);
        if (orderRecord is null)
        {
            return null;
        }

        var order = Rehydrate(orderRecord);
        transition(order, DateTimeOffset.UtcNow);
        ApplyAggregate(orderRecord, order);
        await auditWriter.AppendAsync(new(auditAction, nameof(ProductionOrderRecord), orderRecord.Id, actorId, correlationId));
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = await LoadMappedOrderAsync(orderRecord.Id, cancellationToken);
        await idempotencyStore.SaveAsync(
            idempotencyScope,
            idempotencyKey,
            payloadHash,
            200,
            JsonSerializer.Serialize(result),
            DateTimeOffset.UtcNow.AddDays(30),
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private async Task EnsureProductAndWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken)
    {
        if (!await dbContext.Products.AnyAsync(x => x.Id == productId && x.IsActive, cancellationToken))
        {
            throw new DomainException(new("PRODUCT_NOT_FOUND", "Aktif üretim ürünü bulunamadı."));
        }

        if (!await dbContext.Warehouses.AnyAsync(x => x.Id == warehouseId && x.IsActive, cancellationToken))
        {
            throw new DomainException(new("WAREHOUSE_NOT_FOUND", "Aktif üretim deposu bulunamadı."));
        }
    }

    private async Task<ProductionOrderRecord?> LockOrderAsync(Guid productionOrderId, CancellationToken cancellationToken)
        => await dbContext.ProductionOrders
            .FromSqlInterpolated($"SELECT * FROM production_orders WHERE id = {productionOrderId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<ProductionOrderDto> LoadMappedOrderAsync(Guid productionOrderId, CancellationToken cancellationToken)
    {
        var order = await dbContext.ProductionOrders
            .AsNoTracking()
            .SingleAsync(x => x.Id == productionOrderId, cancellationToken);
        var records = await dbContext.ProductionRecords
            .AsNoTracking()
            .Where(x => x.ProductionOrderId == productionOrderId)
            .OrderBy(x => x.CompletedAt)
            .ToArrayAsync(cancellationToken);
        return Map(order, records);
    }

    private static ProductionOrder Rehydrate(ProductionOrderRecord record)
        => ProductionOrder.Rehydrate(
            record.Id,
            record.ProductId,
            record.WarehouseId,
            PositiveQuantity.Create(record.PlannedQtyBase, 6),
            NonNegativeQuantity.Create(record.CompletedQtyBase, 6),
            Enum.Parse<ProductionOrderStatus>(record.Status, ignoreCase: false),
            DateTimeOffset.UtcNow);

    private static ProductionOrderRecord ToRecord(ProductionOrder order)
        => new()
        {
            Id = order.Id,
            ProductId = order.ProductId,
            WarehouseId = order.WarehouseId,
            PlannedQtyBase = order.PlannedQuantity.BaseValue,
            CompletedQtyBase = order.CompletedQuantity.BaseValue,
            Status = order.Status.ToString(),
            RowVersion = 1,
        };

    private static void ApplyAggregate(ProductionOrderRecord record, ProductionOrder order)
    {
        record.CompletedQtyBase = order.CompletedQuantity.BaseValue;
        record.Status = order.Status.ToString();
    }

    private static ProductionOrderDto Map(ProductionOrderRecord order, IReadOnlyCollection<ProductionRecord> records)
        => new(
            order.Id,
            order.ProductId,
            order.WarehouseId,
            order.PlannedQtyBase,
            order.CompletedQtyBase,
            order.PlannedQtyBase - order.CompletedQtyBase,
            order.Status,
            order.RowVersion,
            records.Select(Map).ToArray());

    private static ProductionRecordDto Map(ProductionRecord record)
        => new(
            record.Id,
            record.ProductionOrderId ?? Guid.Empty,
            record.ProductId,
            record.WarehouseId,
            record.LocationId,
            record.QuantityBase,
            record.EnteredQuantity,
            record.EnteredPackagingId,
            record.PackagingSnapshot ?? "{}",
            record.CompletedAt);

    private async Task<T?> TryReplayAsync<T>(string scope, string key, string payloadHash, CancellationToken cancellationToken)
    {
        var stored = await idempotencyStore.FindAsync(scope, key, cancellationToken);
        if (stored is null)
        {
            return default;
        }

        if (!string.Equals(stored.PayloadHash, payloadHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException(new("IDEMPOTENCY_PAYLOAD_MISMATCH", "Aynı Idempotency-Key farklı payload ile tekrar kullanılamaz."));
        }

        return JsonSerializer.Deserialize<T>(stored.ResponseBody);
    }

    private static string ComputePayloadHash(object payload)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)))).ToLowerInvariant();
}
