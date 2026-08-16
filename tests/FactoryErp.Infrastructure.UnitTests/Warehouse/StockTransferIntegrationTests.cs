using FactoryErp.Application.Abstractions.Persistence;
using FactoryErp.Application.Products;
using FactoryErp.Application.Warehouse;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using FactoryErp.Infrastructure.Products;
using FactoryErp.Infrastructure.Warehouse;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.UnitTests.Warehouse;

public sealed class StockTransferIntegrationTests
{
    [Fact]
    public async Task Transfer_completion_moves_stock_atomically_and_replays_without_duplicate_movements()
    {
        await using var setupContext = CreateContext();
        var product = await setupContext.Products.SingleAsync(x => x.Code == "NAP-001");
        var warehouse = await setupContext.Warehouses.SingleAsync(x => x.Code == "MAIN");
        var packagingId = await setupContext.ProductPackagings
            .Where(x => x.ProductId == product.Id && x.Level == "Case")
            .Select(x => (Guid?)x.Id)
            .SingleAsync();
        var actorId = await setupContext.Users
            .Where(x => x.IsActive)
            .Select(x => x.Id)
            .FirstAsync();
        var movementCountBefore = await setupContext.StockMovements
            .CountAsync(x => x.SourceEntityType == "StockTransferRecord");
        var sourceLocationId = Guid.NewGuid();
        var targetLocationId = Guid.NewGuid();
        var sourceLocation = new WarehouseLocationRecord
        {
            Id = sourceLocationId,
            WarehouseId = warehouse.Id,
            Code = $"G6S-{Guid.NewGuid():N}"[..10],
            Name = "G6.2 test kaynak konumu",
            IsActive = true,
        };
        var targetLocation = new WarehouseLocationRecord
        {
            Id = targetLocationId,
            WarehouseId = warehouse.Id,
            Code = $"G6T-{Guid.NewGuid():N}"[..10],
            Name = "G6.2 test hedef konumu",
            IsActive = true,
        };
        setupContext.WarehouseLocations.AddRange(sourceLocation, targetLocation);
        setupContext.Stocks.Add(new StockRecord
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            WarehouseId = warehouse.Id,
            LocationId = sourceLocationId,
            OnHandQtyBase = 10_000m,
            ReservedQtyBase = 0m,
            RowVersion = 1,
        });
        await setupContext.SaveChangesAsync();
        const decimal sourceStockBefore = 10_000m;

        var createKey = $"g62-transfer-create-{Guid.NewGuid():N}";
        var completeKey = $"g62-transfer-complete-{Guid.NewGuid():N}";
        Guid? transferId = null;
        try
        {
            var service = CreateService(setupContext);
            var created = await service.CreateAsync(
                new CreateStockTransferRequest(
                    product.Id,
                    warehouse.Id,
                    sourceLocationId,
                    warehouse.Id,
                    targetLocation.Id,
                    1m,
                    packagingId,
                    "Packaging"),
                actorId,
                createKey,
                "g6.2-transfer-create");
            transferId = created.Id;
            created.Status.Should().Be("Draft");
            created.QuantityBase.Should().Be(2_000m);

            var completed = await service.CompleteAsync(
                created.Id,
                actorId,
                completeKey,
                "g6.2-transfer-complete");
            completed!.Status.Should().Be("Completed");

            var replay = await service.CompleteAsync(
                created.Id,
                actorId,
                completeKey,
                "g6.2-transfer-complete-replay");
            replay.Should().BeEquivalentTo(completed);

            await using var verificationContext = CreateContext();
            var sourceStockAfter = await verificationContext.Stocks
                .Where(x => x.ProductId == product.Id && x.WarehouseId == warehouse.Id && x.LocationId == sourceLocation.Id)
                .Select(x => x.OnHandQtyBase)
                .SingleAsync();
            var targetStockAfter = await verificationContext.Stocks
                .Where(x => x.ProductId == product.Id && x.WarehouseId == warehouse.Id && x.LocationId == targetLocation.Id)
                .Select(x => x.OnHandQtyBase)
                .SingleAsync();
            sourceStockAfter.Should().Be(sourceStockBefore - 2_000m);
            targetStockAfter.Should().Be(2_000m);
            (await verificationContext.StockMovements.CountAsync(x => x.SourceEntityType == "StockTransferRecord"))
                .Should().Be(movementCountBefore + 2);
            (await verificationContext.StockTransfers.CountAsync(x => x.Id == created.Id && x.Status == "Completed"))
                .Should().Be(1);
        }
        finally
        {
            await CleanupAsync(
                product.Id,
                warehouse.Id,
                sourceLocationId,
                targetLocationId,
                sourceStockBefore,
                transferId,
                createKey,
                completeKey);
        }
    }

    private static StockTransferCommandService CreateService(FactoryErpDbContext context)
        => new(
            context,
            new ProductCatalogService(context),
            new NoopAuditWriter(),
            new EfIdempotencyStore(context));

    private static async Task CleanupAsync(
        Guid productId,
        Guid warehouseId,
        Guid sourceLocationId,
        Guid targetLocationId,
        decimal sourceStockBefore,
        Guid? transferId,
        string createKey,
        string completeKey)
    {
        await using var context = CreateContext();
        var sourceStock = await context.Stocks
            .SingleAsync(x => x.ProductId == productId && x.WarehouseId == warehouseId && x.LocationId == sourceLocationId);
        sourceStock.OnHandQtyBase = sourceStockBefore;
        await context.SaveChangesAsync();

        await context.IdempotencyRecords
            .Where(x => x.Key == createKey || x.Key == completeKey)
            .ExecuteDeleteAsync();
        if (transferId.HasValue)
        {
            await context.AuditLogs.Where(x => x.EntityId == transferId).ExecuteDeleteAsync();
            await context.StockMovements
                .Where(x => x.SourceEntityType == "StockTransferRecord" && x.SourceEntityId == transferId)
                .ExecuteDeleteAsync();
            await context.StockTransfers.Where(x => x.Id == transferId).ExecuteDeleteAsync();
        }

        await context.Stocks
            .Where(x => x.ProductId == productId
                && x.WarehouseId == warehouseId
                && (x.LocationId == sourceLocationId || x.LocationId == targetLocationId))
            .ExecuteDeleteAsync();
        await context.WarehouseLocations
            .Where(x => x.Id == sourceLocationId || x.Id == targetLocationId)
            .ExecuteDeleteAsync();
    }

    private static FactoryErpDbContext CreateContext()
    {
        var connectionString = Environment.GetEnvironmentVariable("FactoryErpTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__FactoryErp")
            ?? "Host=127.0.0.1;Port=5432;Database=factory_erp_g1;Username=factory_erp;Password=dev_only_change_me";
        var options = new DbContextOptionsBuilder<FactoryErpDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new FactoryErpDbContext(options);
    }

    private sealed class NoopAuditWriter : IAuditWriter
    {
        public Task AppendAsync(AuditEntry entry, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
