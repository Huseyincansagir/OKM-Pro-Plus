using FactoryErp.Application.Shipping;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Shipping;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.UnitTests.Shipping;

public sealed class PhysicalLogisticsIntegrationTests
{
    private static readonly Guid ProductId = Guid.Parse("30000000-0000-0000-0000-000000000201");
    private static readonly Guid PackagingId = Guid.Parse("30000000-0000-0000-0000-000000000213");
    private static readonly Guid AdminId = Guid.Parse("98d0a7dc-5c7a-4ecf-8477-54deb4f61c27");

    [Fact]
    public async Task Physical_master_creates_profiles_and_replays_idempotently()
    {
        await using var context = CreateContext();
        var service = new PhysicalLogisticsCommandService(
            context,
            new EfAuditWriter(context),
            new EfIdempotencyStore(context));
        var correlationId = Guid.NewGuid().ToString("N");
        var createdProfileIds = new List<Guid>();
        var createdPalletIds = new List<Guid>();
        var idempotencyKeys = new[] { $"l4a-product-{Guid.NewGuid():N}", $"l4a-packaging-{Guid.NewGuid():N}", $"l4a-pallet-{Guid.NewGuid():N}" };

        try
        {
            var now = DateTimeOffset.UtcNow.AddMinutes(-1);
            var productRequest = new CreateProductPhysicalProfileRequest(
                ProductId, now, now.AddDays(1), 600, 400, 300, 12, 0.072m, true, 5, 200, false, false, "NAP", "[]", "[\"LWH\"]", "{\"source\":\"integration\"}");
            var firstProduct = await service.CreateProductProfileAsync(productRequest, AdminId, idempotencyKeys[0], correlationId);
            var replayProduct = await service.CreateProductProfileAsync(productRequest, AdminId, idempotencyKeys[0], correlationId);
            createdProfileIds.Add(firstProduct.Id);

            replayProduct.Id.Should().Be(firstProduct.Id);
            replayProduct.ProductId.Should().Be(ProductId);
            (await service.GetProductProfileAsync(ProductId, now.AddHours(1))).Should().NotBeNull();

            var packagingRequest = new CreatePackagingPhysicalProfileRequest(
                PackagingId, now, now.AddDays(1), 2000, 600, 400, 300, 12, 0.5m, 12.5m, 0.072m, true, 5, 200, false, false, "NAP", "[]", "[\"LWH\"]", "{\"source\":\"integration\"}");
            var packaging = await service.CreatePackagingProfileAsync(packagingRequest, AdminId, idempotencyKeys[1], correlationId);
            createdProfileIds.Add(packaging.Id);
            packaging.GrossWeightKg.Should().Be(12.5m);

            var pallet = await service.CreatePalletTypeAsync(
                new CreatePalletTypeRequest("L4A-TEST", "L4A Test Palet", 1200, 800, 150, 25, 1025, 1000, 1800, 1, false, "{\"source\":\"integration\"}"),
                AdminId,
                idempotencyKeys[2],
                correlationId);
            createdPalletIds.Add(pallet.Id);
            var replayPallet = await service.CreatePalletTypeAsync(
                new CreatePalletTypeRequest("L4A-TEST", "L4A Test Palet", 1200, 800, 150, 25, 1025, 1000, 1800, 1, false, "{\"source\":\"integration\"}"),
                AdminId,
                idempotencyKeys[2],
                correlationId);

            replayPallet.Id.Should().Be(pallet.Id);
            replayPallet.Code.Should().Be("L4A-TEST");
        }
        finally
        {
            await context.Database.ExecuteSqlInterpolatedAsync($"delete from product_physical_profiles where id in ({createdProfileIds.FirstOrDefault()})", CancellationToken.None);
            await context.Database.ExecuteSqlInterpolatedAsync($"delete from packaging_physical_profiles where id in ({createdProfileIds.Skip(1).FirstOrDefault()})", CancellationToken.None);
            await context.Database.ExecuteSqlInterpolatedAsync($"delete from pallet_types where id in ({createdPalletIds.FirstOrDefault()})", CancellationToken.None);
            await context.Database.ExecuteSqlInterpolatedAsync($"delete from idempotency_records where key in ({idempotencyKeys[0]}) or key in ({idempotencyKeys[1]}) or key in ({idempotencyKeys[2]})", CancellationToken.None);
        }
    }

    private static FactoryErpDbContext CreateContext()
    {
        var connectionString = Environment.GetEnvironmentVariable("FactoryErpTestConnectionString")
            ?? "Host=127.0.0.1;Port=5432;Database=factory_erp_g1;Username=factory_erp;Password=dev_only_change_me";
        return new FactoryErpDbContext(new DbContextOptionsBuilder<FactoryErpDbContext>().UseNpgsql(connectionString).Options);
    }
}
