using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FactoryErp.Application.Abstractions.Persistence;
using FactoryErp.Application.Products;
using FactoryErp.Application.Shipping;
using FactoryErp.Domain.Common;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Shipping;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.UnitTests.Idempotency;

public sealed class IdempotencyIntegrationTests
{
    [Fact]
    public async Task Central_store_replays_same_payload_and_response_through_delivery_command()
    {
        await using var context = CreateContext();
        var store = new EfIdempotencyStore(context);
        var actorId = Guid.NewGuid();
        var key = $"integration-replay-{Guid.NewGuid():N}";
        var scope = $"delivery-note:create:{actorId}";
        var request = CreateRequest(1m);
        var expected = new DeliveryNoteDto(
            Guid.NewGuid(),
            "DN-INTEGRATION-REPLAY",
            request.SalesOrderId,
            Guid.NewGuid(),
            "Draft",
            null,
            Array.Empty<DeliveryNoteItemDto>(),
            1);
        var payloadHash = ComputePayloadHash(request);

        try
        {
            await store.SaveAsync(
                scope,
                key,
                payloadHash,
                201,
                JsonSerializer.Serialize(expected),
                DateTimeOffset.UtcNow.AddMinutes(5));

            var service = CreateService(context, store);
            var replay = await service.CreateDeliveryNoteAsync(
                request,
                actorId,
                key,
                "integration-replay-correlation");

            replay.Should().BeEquivalentTo(expected);
            (await context.SalesOrders.CountAsync()).Should().BeGreaterThanOrEqualTo(0);
            (await context.IdempotencyRecords.CountAsync(x => x.Scope == scope && x.Key == key)).Should().Be(1);
        }
        finally
        {
            await DeleteRecordAsync(context, scope, key);
        }
    }

    [Fact]
    public async Task Central_store_rejects_same_key_with_different_payload_through_delivery_command()
    {
        await using var context = CreateContext();
        var store = new EfIdempotencyStore(context);
        var actorId = Guid.NewGuid();
        var key = $"integration-mismatch-{Guid.NewGuid():N}";
        var scope = $"delivery-note:create:{actorId}";
        var originalRequest = CreateRequest(1m);
        var changedRequest = CreateRequest(2m, originalRequest.SalesOrderId, originalRequest.Items.First().SalesOrderItemId);
        var payloadHash = ComputePayloadHash(originalRequest);
        var response = new DeliveryNoteDto(
            Guid.NewGuid(),
            "DN-INTEGRATION-MISMATCH",
            originalRequest.SalesOrderId,
            Guid.NewGuid(),
            "Draft",
            null,
            Array.Empty<DeliveryNoteItemDto>(),
            1);

        try
        {
            await store.SaveAsync(
                scope,
                key,
                payloadHash,
                201,
                JsonSerializer.Serialize(response),
                DateTimeOffset.UtcNow.AddMinutes(5));

            var service = CreateService(context, store);
            var action = () => service.CreateDeliveryNoteAsync(
                changedRequest,
                actorId,
                key,
                "integration-mismatch-correlation");

            var exception = await Assert.ThrowsAsync<DomainException>(action);
            exception.Error.Code.Should().Be("IDEMPOTENCY_PAYLOAD_MISMATCH");
            (await context.IdempotencyRecords.CountAsync(x => x.Scope == scope && x.Key == key)).Should().Be(1);
        }
        finally
        {
            await DeleteRecordAsync(context, scope, key);
        }
    }

    private static DeliveryInvoiceFinanceService CreateService(
        FactoryErpDbContext context,
        IIdempotencyStore store)
        => new(context, new NoopProductCatalogService(), new NoopAuditWriter(), store);

    private static CreateDeliveryNoteRequest CreateRequest(
        decimal enteredQuantity,
        Guid? salesOrderId = null,
        Guid? salesOrderItemId = null)
        => new(
            salesOrderId ?? Guid.NewGuid(),
            new[]
            {
                new CreateDeliveryNoteItemInput(
                    salesOrderItemId ?? Guid.NewGuid(),
                    enteredQuantity,
                    null,
                    "Packaging"),
            });

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

    private static async Task DeleteRecordAsync(
        FactoryErpDbContext context,
        string scope,
        string key)
        => await context.IdempotencyRecords
            .Where(x => x.Scope == scope && x.Key == key)
            .ExecuteDeleteAsync();

    private static string ComputePayloadHash(object payload)
        => Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload))))
            .ToLowerInvariant();

    private sealed class NoopAuditWriter : IAuditWriter
    {
        public Task AppendAsync(AuditEntry entry, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class NoopProductCatalogService : IProductCatalogService
    {
        public Task<ProductPage> GetPublicProductsAsync(ProductListQuery query, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PublicProductDto?> GetPublicProductBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<QuantityPreviewResult?> PreviewQuantityAsync(QuantityPreviewRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<BarcodeResolutionResult?> ResolveBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
