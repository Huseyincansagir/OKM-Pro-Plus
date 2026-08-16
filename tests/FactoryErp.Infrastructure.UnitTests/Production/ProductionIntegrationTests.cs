using FactoryErp.Application.Abstractions.Persistence;
using FactoryErp.Application.Products;
using FactoryErp.Application.Production;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Products;
using FactoryErp.Infrastructure.Production;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.UnitTests.Production;

public sealed class ProductionIntegrationTests
{
    [Fact]
    public async Task Production_record_completion_receipts_finished_goods_and_replays_without_second_movement()
    {
        await using var setupContext = CreateContext();
        var product = await setupContext.Products.SingleAsync(x => x.Code == "NAP-001");
        var warehouse = await setupContext.Warehouses.SingleAsync(x => x.Code == "MAIN");
        var location = await setupContext.WarehouseLocations
            .SingleAsync(x => x.WarehouseId == warehouse.Id && x.Code == "A-01");
        var packagingId = await setupContext.ProductPackagings
            .Where(x => x.ProductId == product.Id && x.Level == "Case")
            .Select(x => (Guid?)x.Id)
            .SingleAsync();
        var actorId = await setupContext.Users
            .Where(x => x.IsActive)
            .Select(x => x.Id)
            .FirstAsync();
        var stockBefore = await setupContext.Stocks
            .Where(x => x.ProductId == product.Id && x.WarehouseId == warehouse.Id && x.LocationId == location.Id)
            .Select(x => x.OnHandQtyBase)
            .SingleAsync();
        var movementCountBefore = await setupContext.StockMovements
            .CountAsync(x => x.SourceEntityType == "ProductionOrderRecord");
        var keys = new[]
        {
            $"g6-production-create-{Guid.NewGuid():N}",
            $"g6-production-release-{Guid.NewGuid():N}",
            $"g6-production-start-{Guid.NewGuid():N}",
            $"g6-production-record-{Guid.NewGuid():N}",
            $"g6-production-complete-{Guid.NewGuid():N}",
        };
        Guid? orderId = null;
        Guid? recordId = null;

        try
        {
            var service = CreateService(setupContext);
            var created = await service.CreateProductionOrderAsync(
                new CreateProductionOrderRequest(product.Id, warehouse.Id, 2_000m),
                actorId,
                keys[0],
                "g6-production-create");
            orderId = created.Id;

            var released = await service.ReleaseProductionOrderAsync(
                created.Id,
                actorId,
                keys[1],
                "g6-production-release");
            released!.Status.Should().Be("Released");

            var started = await service.StartProductionOrderAsync(
                created.Id,
                actorId,
                keys[2],
                "g6-production-start");
            started!.Status.Should().Be("InProgress");

            var recorded = await service.AddProductionRecordAsync(
                created.Id,
                new AddProductionRecordRequest(
                    warehouse.Id,
                    location.Id,
                    1m,
                    packagingId,
                    "Packaging"),
                actorId,
                keys[3],
                "g6-production-record");
            recorded!.CompletedQuantityBase.Should().Be(2_000m);
            recordId = recorded.Records.Single().Id;

            var completed = await service.CompleteProductionOrderAsync(
                created.Id,
                new CompleteProductionRequest(warehouse.Id, location.Id),
                actorId,
                keys[4],
                "g6-production-complete");
            completed!.Status.Should().Be("Completed");
            completed.CompletedQuantityBase.Should().Be(2_000m);

            var replay = await service.CompleteProductionOrderAsync(
                created.Id,
                new CompleteProductionRequest(warehouse.Id, location.Id),
                actorId,
                keys[4],
                "g6-production-complete-replay");
            replay.Should().BeEquivalentTo(completed);

            await using var verificationContext = CreateContext();
            var stockAfter = await verificationContext.Stocks
                .Where(x => x.ProductId == product.Id && x.WarehouseId == warehouse.Id && x.LocationId == location.Id)
                .Select(x => x.OnHandQtyBase)
                .SingleAsync();
            stockAfter.Should().Be(stockBefore + 2_000m);
            (await verificationContext.StockMovements.CountAsync(x => x.SourceEntityType == "ProductionOrderRecord"))
                .Should().Be(movementCountBefore + 1);
            (await verificationContext.ProductionRecords.CountAsync(x => x.ProductionOrderId == created.Id))
                .Should().Be(1);
        }
        finally
        {
            await CleanupAsync(product.Id, warehouse.Id, location.Id, stockBefore, orderId, recordId, keys);
        }
    }

    private static ProductionCommandService CreateService(FactoryErpDbContext context)
        => new(
            context,
            new ProductCatalogService(context),
            new NoopAuditWriter(),
            new FactoryErp.Infrastructure.Persistence.EfIdempotencyStore(context));

    private static async Task CleanupAsync(
        Guid productId,
        Guid warehouseId,
        Guid locationId,
        decimal stockBefore,
        Guid? orderId,
        Guid? recordId,
        IReadOnlyCollection<string> keys)
    {
        if (!orderId.HasValue)
        {
            return;
        }

        await using var context = CreateContext();
        var stock = await context.Stocks
            .SingleAsync(x => x.ProductId == productId && x.WarehouseId == warehouseId && x.LocationId == locationId);
        stock.OnHandQtyBase = stockBefore;
        await context.SaveChangesAsync();

        await context.IdempotencyRecords
            .Where(x => keys.Contains(x.Key))
            .ExecuteDeleteAsync();
        await context.AuditLogs
            .Where(x => x.EntityId == orderId || x.EntityId == recordId)
            .ExecuteDeleteAsync();
        await context.StockMovements
            .Where(x => x.SourceEntityType == "ProductionOrderRecord" && x.SourceEntityId == orderId)
            .ExecuteDeleteAsync();
        await context.ProductionRecords
            .Where(x => x.ProductionOrderId == orderId)
            .ExecuteDeleteAsync();
        await context.ProductionOrders
            .Where(x => x.Id == orderId)
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
