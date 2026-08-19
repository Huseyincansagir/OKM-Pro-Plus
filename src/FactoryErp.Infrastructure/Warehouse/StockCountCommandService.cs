using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FactoryErp.Application.Abstractions.Persistence;
using FactoryErp.Application.Warehouse;
using FactoryErp.Domain.Common;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.Warehouse;

public sealed class StockCountCommandService(
    FactoryErpDbContext dbContext,
    IAuditWriter auditWriter,
    IIdempotencyStore idempotencyStore) : IStockCountCommandService
{
    public async Task<IReadOnlyCollection<StockCountDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.StockCounts
            .AsNoTracking()
            .Include(x => x.Items)
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .ToArrayAsync(cancellationToken);
        return await MapManyAsync(rows, cancellationToken);
    }

    public async Task<StockCountDto?> GetAsync(Guid countId, CancellationToken cancellationToken = default)
    {
        var row = await dbContext.StockCounts
            .AsNoTracking()
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == countId, cancellationToken);
        if (row is null)
        {
            return null;
        }

        var mapped = await MapManyAsync(new[] { row }, cancellationToken);
        return mapped[0];
    }

    public async Task<StockCountDto> CreateAsync(
        CreateStockCountRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var scope = $"stock-count:create:{actorId}";
        var payloadHash = ComputePayloadHash(request);
        var replay = await TryReplayAsync<StockCountDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        var now = DateTimeOffset.UtcNow;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (!await dbContext.Warehouses.AnyAsync(x => x.Id == request.WarehouseId && x.IsActive, cancellationToken)
            || !await dbContext.WarehouseLocations.AnyAsync(
                x => x.Id == request.LocationId && x.WarehouseId == request.WarehouseId && x.IsActive,
                cancellationToken))
        {
            throw new DomainException(new("COUNT_LOCATION_INVALID", "Sayım için aktif depo/lokasyon bulunamadı."));
        }

        var record = new StockCountRecord
        {
            Id = Guid.NewGuid(),
            DocumentNumber = await NextNumberAsync("stock_count", "CNT", now, cancellationToken),
            WarehouseId = request.WarehouseId,
            LocationId = request.LocationId,
            Status = "Draft",
            CreatedAt = now,
            CreatedBy = actorId,
        };
        dbContext.StockCounts.Add(record);
        await auditWriter.AppendAsync(new(
            "StockCountCreated",
            nameof(StockCountRecord),
            record.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { record.DocumentNumber, record.WarehouseId, record.LocationId })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var mapped = await MapManyAsync(new[] { record }, cancellationToken);
        var result = mapped[0];
        await idempotencyStore.SaveAsync(scope, idempotencyKey, payloadHash, 201, JsonSerializer.Serialize(result), now.AddDays(30), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<StockCountDto?> AddItemAsync(
        Guid countId,
        AddStockCountItemRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (request.CountedQtyBase < 0)
        {
            throw new DomainException(new("COUNT_QTY_INVALID", "Sayılan temel miktar negatif olamaz."));
        }

        var scope = $"stock-count:item:{actorId}:{countId}";
        var payloadHash = ComputePayloadHash(new { countId, request });
        var replay = await TryReplayAsync<StockCountDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var record = await dbContext.StockCounts
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == countId, cancellationToken);
        if (record is null)
        {
            return null;
        }

        if (record.Status != "Draft")
        {
            throw new DomainException(new("COUNT_NOT_DRAFT", "Kalem yalnızca taslak sayıma eklenir."));
        }

        if (!await dbContext.Products.AnyAsync(x => x.Id == request.ProductId && x.IsActive, cancellationToken))
        {
            throw new DomainException(new("PRODUCT_NOT_FOUND", "Sayım ürünü bulunamadı."));
        }

        if (record.Items.Any(x => x.ProductId == request.ProductId))
        {
            throw new DomainException(new("COUNT_ITEM_DUPLICATE", "Bu ürün bu sayımda zaten var."));
        }

        var onHand = await dbContext.Stocks
            .AsNoTracking()
            .Where(x => x.ProductId == request.ProductId && x.WarehouseId == record.WarehouseId && x.LocationId == record.LocationId)
            .Select(x => (decimal?)x.OnHandQtyBase)
            .SingleOrDefaultAsync(cancellationToken) ?? 0m;

        record.Items.Add(new StockCountItemRecord
        {
            Id = Guid.NewGuid(),
            StockCountId = record.Id,
            ProductId = request.ProductId,
            CountedQtyBase = request.CountedQtyBase,
            SystemOnHandQtyBase = onHand,
            VarianceQtyBase = request.CountedQtyBase - onHand,
        });
        await auditWriter.AppendAsync(new(
            "StockCountItemAdded",
            nameof(StockCountRecord),
            record.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { request.ProductId, request.CountedQtyBase, onHand })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var mapped = await MapManyAsync(new[] { record }, cancellationToken);
        var result = mapped[0];
        await idempotencyStore.SaveAsync(scope, idempotencyKey, payloadHash, 200, JsonSerializer.Serialize(result), DateTimeOffset.UtcNow.AddDays(30), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<StockCountDto?> CompleteAsync(
        Guid countId,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var scope = $"stock-count:complete:{actorId}:{countId}";
        var payloadHash = ComputePayloadHash(new { countId, action = "complete" });
        var replay = await TryReplayAsync<StockCountDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        var now = DateTimeOffset.UtcNow;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var record = await dbContext.StockCounts
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == countId, cancellationToken);
        if (record is null)
        {
            return null;
        }

        if (record.Status != "Draft")
        {
            throw new DomainException(new("COUNT_NOT_DRAFT", "Yalnızca taslak sayım tamamlanır."));
        }

        if (record.Items.Count == 0)
        {
            throw new DomainException(new("COUNT_ITEMS_REQUIRED", "Sayım en az bir kalem içermelidir."));
        }

        foreach (var item in record.Items)
        {
            var stock = await dbContext.Stocks
                .FromSqlInterpolated($"SELECT * FROM stocks WHERE product_id = {item.ProductId} AND warehouse_id = {record.WarehouseId} AND location_id = {record.LocationId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (stock is null)
            {
                stock = new StockRecord
                {
                    Id = Guid.NewGuid(),
                    ProductId = item.ProductId,
                    WarehouseId = record.WarehouseId,
                    LocationId = record.LocationId,
                    OnHandQtyBase = 0,
                    ReservedQtyBase = 0,
                    RowVersion = 1,
                };
                dbContext.Stocks.Add(stock);
            }

            var liveOnHand = stock.OnHandQtyBase;
            var variance = item.CountedQtyBase - liveOnHand;
            item.SystemOnHandQtyBase = liveOnHand;
            item.VarianceQtyBase = variance;
            if (variance == 0)
            {
                continue;
            }

            if (variance < 0 && stock.OnHandQtyBase + variance < stock.ReservedQtyBase)
            {
                throw new DomainException(new(
                    "COUNT_WOULD_BREAK_RESERVATION",
                    "Sayım sonucu rezerve miktarın altına inemez.",
                    new Dictionary<string, object?>
                    {
                        ["onHand"] = stock.OnHandQtyBase,
                        ["reserved"] = stock.ReservedQtyBase,
                        ["counted"] = item.CountedQtyBase,
                    }));
            }

            stock.OnHandQtyBase = item.CountedQtyBase;
            dbContext.StockMovements.Add(new StockMovementRecord
            {
                Id = Guid.NewGuid(),
                ProductId = item.ProductId,
                WarehouseId = record.WarehouseId,
                LocationId = record.LocationId,
                MovementType = variance > 0 ? "CountIn" : "CountOut",
                QuantityBase = Math.Abs(variance),
                SourceEntityType = nameof(StockCountRecord),
                SourceEntityId = record.Id,
                CreatedAt = now,
            });
        }

        record.Status = "Completed";
        record.CompletedAt = now;
        await auditWriter.AppendAsync(new(
            "StockCountCompleted",
            nameof(StockCountRecord),
            record.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new { record.Status, itemCount = record.Items.Count })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var mapped = await MapManyAsync(new[] { record }, cancellationToken);
        var result = mapped[0];
        await idempotencyStore.SaveAsync(scope, idempotencyKey, payloadHash, 200, JsonSerializer.Serialize(result), now.AddDays(30), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private async Task<IReadOnlyList<StockCountDto>> MapManyAsync(
        IReadOnlyCollection<StockCountRecord> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return Array.Empty<StockCountDto>();
        }

        var warehouseIds = rows.Select(x => x.WarehouseId).Distinct().ToArray();
        var locationIds = rows.Select(x => x.LocationId).Distinct().ToArray();
        var productIds = rows.SelectMany(x => x.Items.Select(i => i.ProductId)).Distinct().ToArray();
        var warehouses = await dbContext.Warehouses.AsNoTracking().Where(x => warehouseIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Code, cancellationToken);
        var locations = await dbContext.WarehouseLocations.AsNoTracking().Where(x => locationIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Code, cancellationToken);
        var products = productIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await dbContext.Products.AsNoTracking().Where(x => productIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Code, cancellationToken);

        return rows.Select(row => new StockCountDto(
            row.Id,
            row.DocumentNumber,
            row.WarehouseId,
            warehouses.GetValueOrDefault(row.WarehouseId, string.Empty),
            row.LocationId,
            locations.GetValueOrDefault(row.LocationId, string.Empty),
            row.Status,
            row.Items.Select(item => new StockCountItemDto(
                item.Id,
                item.ProductId,
                products.GetValueOrDefault(item.ProductId, string.Empty),
                item.CountedQtyBase,
                item.SystemOnHandQtyBase,
                item.VarianceQtyBase)).ToArray(),
            row.CreatedAt,
            row.CompletedAt)).ToArray();
    }

    private async Task<string> NextNumberAsync(string documentType, string prefix, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var year = now.Year;
        var sequence = await dbContext.DocumentSequences
            .FromSqlInterpolated($"SELECT * FROM document_sequences WHERE document_type = {documentType} AND year = {year} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (sequence is null)
        {
            sequence = new DocumentSequenceRecord { Id = Guid.NewGuid(), DocumentType = documentType, Year = year, CurrentValue = 1, UpdatedAt = now };
            dbContext.DocumentSequences.Add(sequence);
        }
        else
        {
            sequence.CurrentValue++;
            sequence.UpdatedAt = now;
        }

        return $"{prefix}-{year}-{sequence.CurrentValue:D6}";
    }

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
