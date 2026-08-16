using FactoryErp.Application.Abstractions.Persistence;
using FactoryErp.Application.Products;
using FactoryErp.Application.Shipping;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using FactoryErp.Infrastructure.Shipping;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FactoryErp.Infrastructure.UnitTests.Idempotency;

public sealed class CurrentAccountConcurrencyIntegrationTests
{
    [Fact]
    public async Task Concurrent_payment_apply_creates_one_current_account_and_one_transaction()
    {
        var customerId = Guid.NewGuid();
        var customerCode = $"CONC-{Guid.NewGuid():N}";
        var paymentMethodId = Guid.Empty;
        var actorOne = Guid.Empty;
        var actorTwo = Guid.Empty;
        var keyOne = $"integration-concurrency-one-{Guid.NewGuid():N}";
        var keyTwo = $"integration-concurrency-two-{Guid.NewGuid():N}";

        await using (var setupContext = CreateContext())
        {
            var paymentMethod = await setupContext.PaymentMethods
                .SingleAsync(x => x.Code == "CASH" && x.IsActive);
            paymentMethodId = paymentMethod.Id;
            actorOne = await setupContext.Users
                .Where(x => x.IsActive)
                .Select(x => x.Id)
                .FirstAsync();
            actorTwo = actorOne;
            setupContext.Customers.Add(new CustomerRecord
            {
                Id = customerId,
                CustomerCode = customerCode,
                LegalName = "Concurrency Integration Fixture",
                Status = "Active",
                IsDeleted = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
            await setupContext.SaveChangesAsync();
        }

        try
        {
            await using var contextOne = CreateContext();
            await using var contextTwo = CreateContext();
            var serviceOne = CreateService(contextOne);
            var serviceTwo = CreateService(contextTwo);
            var startGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var first = ApplyAfterGateAsync(
                startGate.Task,
                serviceOne,
                customerId,
                paymentMethodId,
                actorOne,
                keyOne,
                "concurrency-one");
            var second = ApplyAfterGateAsync(
                startGate.Task,
                serviceTwo,
                customerId,
                paymentMethodId,
                actorTwo,
                keyTwo,
                "concurrency-two");

            startGate.SetResult(true);
            var all = Task.WhenAll(first, second);
            var completed = await Task.WhenAny(all, Task.Delay(TimeSpan.FromSeconds(30)));
            completed.Should().Be(all, "the two database transactions must resolve without hanging");
            var outcomes = await all;

            var outcomeDetails = string.Join(
                Environment.NewLine,
                outcomes.Select(x => x.Success ? "SUCCESS" : x.Exception?.ToString() ?? "FAILURE WITHOUT EXCEPTION"));
            outcomes.Count(x => x.Success).Should().Be(1, "outcomes were:{0}{1}", Environment.NewLine, outcomeDetails);
            outcomes.Count(x => !x.Success).Should().Be(1);
            var failed = outcomes.Single(x => !x.Success).Exception;
            failed.Should().NotBeNull();
            FindPostgresException(failed!).SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);

            await using var verificationContext = CreateContext();
            var account = await verificationContext.CurrentAccounts
                .SingleAsync(x => x.CustomerId == customerId);
            account.CurrencyCode.Should().Be("TRY");
            account.CreditTotal.Should().Be(10m);
            account.Balance.Should().Be(-10m);
            (await verificationContext.Payments.CountAsync(x => x.CustomerId == customerId)).Should().Be(1);
            (await verificationContext.CurrentTransactions.CountAsync(x => x.CurrentAccountId == account.Id)).Should().Be(1);
        }
        finally
        {
            await CleanupFixtureAsync(customerId, customerCode, keyOne, keyTwo, actorOne, actorTwo);
        }
    }

    private static async Task<RaceOutcome> ApplyAfterGateAsync(
        Task startSignal,
        DeliveryInvoiceFinanceService service,
        Guid customerId,
        Guid paymentMethodId,
        Guid actorId,
        string idempotencyKey,
        string correlationId)
    {
        await startSignal;
        try
        {
            await service.ApplyPaymentAsync(
                new ApplyPaymentRequest(customerId, 10m, paymentMethodId, null, correlationId),
                actorId,
                idempotencyKey,
                correlationId);
            return new RaceOutcome(true, null);
        }
        catch (Exception exception)
        {
            return new RaceOutcome(false, exception);
        }
    }

    private static async Task CleanupFixtureAsync(
        Guid customerId,
        string customerCode,
        string keyOne,
        string keyTwo,
        Guid actorOne,
        Guid actorTwo)
    {
        await using var context = CreateContext();
        await context.IdempotencyRecords
            .Where(x => x.Key == keyOne || x.Key == keyTwo)
            .ExecuteDeleteAsync();
        await context.CurrentTransactions
            .Where(x => x.IdempotencyKey == keyOne || x.IdempotencyKey == keyTwo)
            .ExecuteDeleteAsync();
        await context.Payments
            .Where(x => x.CustomerId == customerId)
            .ExecuteDeleteAsync();
        await context.CurrentAccounts
            .Where(x => x.CustomerId == customerId)
            .ExecuteDeleteAsync();
        await context.Customers
            .Where(x => x.Id == customerId && x.CustomerCode == customerCode)
            .ExecuteDeleteAsync();
    }

    private static DeliveryInvoiceFinanceService CreateService(FactoryErpDbContext context)
        => new(context, new NoopProductCatalogService(), new NoopAuditWriter(), new EfIdempotencyStore(context));

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

    private static PostgresException FindPostgresException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgresException)
            {
                return postgresException;
            }
        }

        throw new InvalidOperationException("The expected PostgreSQL exception was not found.", exception);
    }

    private sealed record RaceOutcome(bool Success, Exception? Exception);

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
