using FactoryErp.Application.Abstractions.Persistence;
using FactoryErp.Application.Products;
using FactoryErp.Application.Sales;
using FactoryErp.Domain.Common;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using FactoryErp.Infrastructure.Sales;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.UnitTests.Sales;

public sealed class QuoteConversionIntegrationTests
{
    [Fact]
    public async Task Issued_quote_conversion_creates_draft_order_with_source_and_no_reservation()
    {
        var fixture = await CreateQuoteAsync("issued");
        try
        {
            await using var context = CreateContext();
            var service = CreateService(context);
            var result = await service.ConvertQuoteToSalesOrderAsync(
                fixture.QuoteId,
                fixture.ActorId,
                fixture.IdempotencyKey,
                "p003-issued",
                CancellationToken.None);

            result.Should().NotBeNull();
            result!.Status.Should().Be("Draft");
            result.SourceQuoteId.Should().Be(fixture.QuoteId);
            result.SourceQuoteNumber.Should().Be(fixture.QuoteNumber);
            result.Items.Should().ContainSingle();
            result.Items.Single().OrderedQty.Should().Be(12.5m);
            result.Items.Single().ReservedQty.Should().Be(0m);
            result.TotalNet.Should().Be(125m);
            result.TotalTax.Should().Be(0m);
            result.TotalGross.Should().Be(125m);

            await using var verification = CreateContext();
            (await verification.SalesOrders.CountAsync(x => x.SourceQuoteId == fixture.QuoteId)).Should().Be(1);
            (await verification.StockReservations.AnyAsync(x => x.SalesOrderItemId == result.Items.Single().Id)).Should().BeFalse();
            (await verification.AuditLogs.AnyAsync(x =>
                x.EntityId == fixture.QuoteId && x.Action == "QuoteConvertedToOrder")).Should().BeTrue();
        }
        finally
        {
            await CleanupAsync(fixture);
        }
    }

    [Fact]
    public async Task Conversion_replays_same_key_and_returns_existing_order_for_duplicate_quote()
    {
        var fixture = await CreateQuoteAsync("duplicate");
        try
        {
            SalesOrderDto first;
            await using (var firstContext = CreateContext())
            {
                first = (await CreateService(firstContext).ConvertQuoteToSalesOrderAsync(
                    fixture.QuoteId,
                    fixture.ActorId,
                    fixture.IdempotencyKey,
                    "p003-duplicate-1",
                    CancellationToken.None))!;
            }

            await using (var replayContext = CreateContext())
            {
                var replay = await CreateService(replayContext).ConvertQuoteToSalesOrderAsync(
                    fixture.QuoteId,
                    fixture.ActorId,
                    fixture.IdempotencyKey,
                    "p003-duplicate-2",
                    CancellationToken.None);
                replay.Should().BeEquivalentTo(first);
            }

            await using (var differentKeyContext = CreateContext())
            {
                var duplicate = await CreateService(differentKeyContext).ConvertQuoteToSalesOrderAsync(
                    fixture.QuoteId,
                    fixture.ActorId,
                    $"p003-different-{Guid.NewGuid():N}",
                    "p003-duplicate-3",
                    CancellationToken.None);
                duplicate!.Id.Should().Be(first.Id);
            }

            await using var verification = CreateContext();
            (await verification.SalesOrders.CountAsync(x => x.SourceQuoteId == fixture.QuoteId)).Should().Be(1);
        }
        finally
        {
            await CleanupAsync(fixture);
        }
    }

    [Fact]
    public async Task Concurrent_conversion_of_one_quote_returns_one_order()
    {
        var fixture = await CreateQuoteAsync("concurrent");
        var keyOne = $"p003-concurrent-one-{Guid.NewGuid():N}";
        var keyTwo = $"p003-concurrent-two-{Guid.NewGuid():N}";
        try
        {
            var first = ConvertWithNewContextAsync(fixture, keyOne, "p003-concurrent-1");
            var second = ConvertWithNewContextAsync(fixture, keyTwo, "p003-concurrent-2");
            var results = await Task.WhenAll(first, second);

            results.Should().NotContainNulls();
            results.Select(x => x!.Id).Distinct().Should().ContainSingle();
            await using var verification = CreateContext();
            (await verification.SalesOrders.CountAsync(x => x.SourceQuoteId == fixture.QuoteId)).Should().Be(1);
        }
        finally
        {
            fixture = fixture with { IdempotencyKey = $"{fixture.IdempotencyKey}|{keyOne}|{keyTwo}" };
            await CleanupAsync(fixture);
        }
    }

    [Fact]
    public async Task Draft_quote_conversion_is_rejected_and_transaction_creates_no_order()
    {
        var fixture = await CreateQuoteAsync("draft", issued: false);
        try
        {
            await using var context = CreateContext();
            var action = () => CreateService(context).ConvertQuoteToSalesOrderAsync(
                fixture.QuoteId,
                fixture.ActorId,
                fixture.IdempotencyKey,
                "p003-draft",
                CancellationToken.None);

            var exception = await action.Should().ThrowAsync<DomainException>();
            exception.Which.Error.Code.Should().Be("QUOTE_NOT_ISSUED");
            (await context.SalesOrders.CountAsync(x => x.SourceQuoteId == fixture.QuoteId)).Should().Be(0);
        }
        finally
        {
            await CleanupAsync(fixture);
        }
    }

    private static async Task<SalesOrderDto?> ConvertWithNewContextAsync(
        QuoteFixture fixture,
        string key,
        string correlationId)
    {
        await using var context = CreateContext();
        return await CreateService(context).ConvertQuoteToSalesOrderAsync(
            fixture.QuoteId,
            fixture.ActorId,
            key,
            correlationId,
            CancellationToken.None);
    }

    private static async Task<QuoteFixture> CreateQuoteAsync(string label, bool issued = true)
    {
        await using var context = CreateContext();
        var actorId = await context.Users.Select(x => x.Id).FirstAsync();
        var customerId = await context.Customers
            .Where(x => !x.IsDeleted && x.Status == "Active")
            .Select(x => x.Id)
            .FirstAsync();
        var productId = await context.Products.Select(x => x.Id).FirstAsync();
        var now = DateTimeOffset.UtcNow;
        var quoteRequestId = Guid.NewGuid();
        var quoteRequestItemId = Guid.NewGuid();
        var quoteRequest = new QuoteRequestRecord
        {
            Id = quoteRequestId,
            RequestNumber = $"P003-REQ-{label}-{Guid.NewGuid():N}",
            Source = "Internal",
            Status = "InReview",
            CustomerId = customerId,
            CustomerCandidateName = "P003 Test Customer",
            CreatedAt = now,
        };
        quoteRequest.Items.Add(new QuoteRequestItemRecord
        {
            Id = quoteRequestItemId,
            ProductId = productId,
            EnteredQuantity = 2.5m,
            EnteredPackagingId = null,
            QuantityBase = 12.5m,
            PackagingSnapshot = "{}",
        });
        var quoteId = Guid.NewGuid();
        var quoteNumber = $"P003-{label}-{Guid.NewGuid():N}";
        var quote = new QuoteRecord
        {
            Id = quoteId,
            QuoteNumber = quoteNumber,
            Status = issued ? "Issued" : "Draft",
            CustomerId = customerId,
            QuoteRequestId = quoteRequestId,
            CurrencyCode = "TRY",
            TotalNet = 125m,
            TotalTax = 0m,
            TotalGross = 125m,
            IssuedAt = issued ? now : null,
            IssuedBy = issued ? actorId : null,
            RowVersion = 1,
            CreatedBy = actorId,
            CreatedAt = now,
            UpdatedAt = now,
        };
        quote.Items.Add(new QuoteItemRecord
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            QuoteRequestItemId = quoteRequestItemId,
            EnteredQuantity = 2.5m,
            EnteredPackagingId = null,
            QuantityBase = 12.5m,
            PackagingSnapshot = "{\"name\":\"P003 paket\"}",
            UnitPrice = 10m,
            TaxCode = "VAT0",
            PriceSnapshot = "{\"unitPrice\":10,\"currency\":\"TRY\"}",
            LineNet = 125m,
            RowVersion = 1,
        });
        context.QuoteRequests.Add(quoteRequest);
        context.Quotes.Add(quote);
        await context.SaveChangesAsync();
        return new QuoteFixture(quoteId, quoteRequestId, quoteNumber, actorId, $"p003-{label}-{Guid.NewGuid():N}");
    }

    private static async Task CleanupAsync(QuoteFixture fixture)
    {
        await using var context = CreateContext();
        var orderIds = await context.SalesOrders
            .Where(x => x.SourceQuoteId == fixture.QuoteId)
            .Select(x => x.Id)
            .ToArrayAsync();
        if (orderIds.Length > 0)
        {
            await context.SalesOrderItems.Where(x => orderIds.Contains(x.SalesOrderId)).ExecuteDeleteAsync();
            await context.SalesOrders.Where(x => orderIds.Contains(x.Id)).ExecuteDeleteAsync();
        }

        await context.AuditLogs.Where(x => x.EntityId == fixture.QuoteId).ExecuteDeleteAsync();
        await context.IdempotencyRecords
            .Where(x => x.Scope == $"quote:convert:{fixture.ActorId}:{fixture.QuoteId}"
                || x.Key == fixture.IdempotencyKey
                || x.Key.Contains("p003-concurrent-"))
            .ExecuteDeleteAsync();
        await context.QuoteItems.Where(x => x.QuoteId == fixture.QuoteId).ExecuteDeleteAsync();
        await context.Quotes.Where(x => x.Id == fixture.QuoteId).ExecuteDeleteAsync();
        await context.QuoteRequestItems.Where(x => x.QuoteRequestId == fixture.QuoteRequestId).ExecuteDeleteAsync();
        await context.QuoteRequests.Where(x => x.Id == fixture.QuoteRequestId).ExecuteDeleteAsync();
    }

    private static SalesCommandService CreateService(FactoryErpDbContext context)
    {
        var auditWriter = new EfAuditWriter(context);
        return new SalesCommandService(
            context,
            new NoopProductCatalogService(),
            new PricingCommandService(context, auditWriter, new EfIdempotencyStore(context)),
            auditWriter,
            new EfIdempotencyStore(context));
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

    private sealed record QuoteFixture(Guid QuoteId, Guid QuoteRequestId, string QuoteNumber, Guid ActorId, string IdempotencyKey);

    private sealed class NoopProductCatalogService : IProductCatalogService
    {
        public Task<ProductPage> GetPublicProductsAsync(ProductListQuery query, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PublicProductDto?> GetPublicProductBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyCollection<StaffProductDto>> ListStaffProductsAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<StaffProductDto?> GetStaffProductAsync(Guid productId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<QuantityPreviewResult?> PreviewQuantityAsync(QuantityPreviewRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<BarcodeResolutionResult?> ResolveBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
