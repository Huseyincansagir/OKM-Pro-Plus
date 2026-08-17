using FactoryErp.Application.Shipping;
using FactoryErp.Domain.Common;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using FactoryErp.Infrastructure.Shipping;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.UnitTests.Shipping;

public sealed class ShipmentPackageIntegrationTests
{
    private static readonly Guid DeliveryNoteId = Guid.Parse("eade528b-1cc3-42c9-8009-93066dac675f");
    private static readonly Guid ProductId = Guid.Parse("30000000-0000-0000-0000-000000000201");
    private static readonly Guid PackagingId = Guid.Parse("30000000-0000-0000-0000-000000000213");

    [Fact]
    public async Task Create_package_calculates_quantity_and_replays_idempotently()
    {
        var actorId = await GetAdminIdAsync();
        var profileId = Guid.NewGuid();
        var shipmentId = Guid.Empty;
        var packageCode = "L4B-" + Guid.NewGuid().ToString("N");

        try
        {
            await InsertPhysicalProfileAsync(profileId);
            await using var setupContext = CreateContext();
            var logistics = new LogisticsCommandService(
                setupContext,
                new EfAuditWriter(setupContext),
                new EfIdempotencyStore(setupContext));
            var shipment = await logistics.CreateShipmentAsync(
                new CreateShipmentRequest(DeliveryNoteId, 1),
                actorId,
                "l4b-shipment-" + Guid.NewGuid(),
                "l4b-integration",
                CancellationToken.None);
            shipmentId = shipment.Id;
            var shipmentItemId = shipment.Items.Single().Id;

            await using var context = CreateContext();
            var service = CreateService(context);
            var request = new CreateShipmentPackageRequest(
                shipmentItemId,
                PackagingId,
                null,
                "Case",
                2,
                100,
                2,
                packageCode,
                false);
            var key = "l4b-package-" + Guid.NewGuid();

            var created = await service.CreateShipmentPackageAsync(
                shipmentId,
                request,
                actorId,
                key,
                "l4b-integration",
                CancellationToken.None);
            var replay = await service.CreateShipmentPackageAsync(
                shipmentId,
                request,
                actorId,
                key,
                "l4b-integration",
                CancellationToken.None);

            created.QuantityBase.Should().Be(200);
            created.PhysicalSnapshot.Should().Contain("ProductPhysicalProfile");
            replay.Id.Should().Be(created.Id);
            replay.QuantityBase.Should().Be(created.QuantityBase);
        }
        finally
        {
            await CleanupAsync(shipmentId, profileId);
        }
    }

    [Fact]
    public async Task Create_package_rejects_over_allocation_at_exact_shipment_item_ceiling()
    {
        var actorId = await GetAdminIdAsync();
        var profileId = Guid.NewGuid();
        var shipmentId = Guid.Empty;

        try
        {
            await InsertPhysicalProfileAsync(profileId);
            await using var setupContext = CreateContext();
            var logistics = new LogisticsCommandService(
                setupContext,
                new EfAuditWriter(setupContext),
                new EfIdempotencyStore(setupContext));
            var shipment = await logistics.CreateShipmentAsync(
                new CreateShipmentRequest(DeliveryNoteId, 1),
                actorId,
                "l4b-shipment-ceiling-" + Guid.NewGuid(),
                "l4b-ceiling",
                CancellationToken.None);
            shipmentId = shipment.Id;
            var shipmentItemId = shipment.Items.Single().Id;

            await using var context = CreateContext();
            var service = CreateService(context);
            var exact = await service.CreateShipmentPackageAsync(
                shipmentId,
                new CreateShipmentPackageRequest(
                    shipmentItemId, PackagingId, null, "Case", 40, 100, 40,
                    "L4B-EXACT-" + Guid.NewGuid().ToString("N"), false),
                actorId,
                "l4b-exact-" + Guid.NewGuid(),
                "l4b-ceiling",
                CancellationToken.None);
            exact.QuantityBase.Should().Be(4000);

            var action = () => service.CreateShipmentPackageAsync(
                shipmentId,
                new CreateShipmentPackageRequest(
                    shipmentItemId, PackagingId, null, "Case", 1, 100, 1,
                    "L4B-OVER-" + Guid.NewGuid().ToString("N"), false),
                actorId,
                "l4b-over-" + Guid.NewGuid(),
                "l4b-ceiling",
                CancellationToken.None);

            await action.Should().ThrowAsync<DomainException>()
                .WithMessage("*shipment item miktarını aşamaz*");
        }
        finally
        {
            await CleanupAsync(shipmentId, profileId);
        }
    }

    private static ShipmentPackageCommandService CreateService(FactoryErpDbContext context)
        => new(context, new EfAuditWriter(context), new EfIdempotencyStore(context));

    private static FactoryErpDbContext CreateContext()
    {
        var connectionString = Environment.GetEnvironmentVariable("FactoryErpTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__FactoryErp")
            ?? "Host=127.0.0.1;Port=5432;Database=factory_erp_g1;Username=factory_erp;Password=dev_only_change_me";
        return new FactoryErpDbContext(new DbContextOptionsBuilder<FactoryErpDbContext>().UseNpgsql(connectionString).Options);
    }

    private static async Task<Guid> GetAdminIdAsync()
    {
        await using var context = CreateContext();
        return await context.Users.Where(x => x.UserName == "admin").Select(x => x.Id).SingleAsync();
    }

    private static async Task InsertPhysicalProfileAsync(Guid profileId)
    {
        await using var context = CreateContext();
        context.ProductPhysicalProfiles.Add(new ProductPhysicalProfileRecord
        {
            Id = profileId,
            ProductId = ProductId,
            EffectiveFrom = DateTimeOffset.UtcNow.AddMinutes(-5),
            LengthMm = 100,
            WidthMm = 100,
            HeightMm = 100,
            NetWeightKg = 1,
            VolumeM3 = 0.001m,
            IsStackable = true,
            MaxStackCount = 5,
            PhysicalPolicySnapshot = "{}",
            IncompatibleGroups = "[]",
            AllowedOrientations = "[\"LWH\"]",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            RowVersion = 1,
        });
        await context.SaveChangesAsync();
    }

    private static async Task CleanupAsync(Guid shipmentId, Guid profileId)
    {
        await using var context = CreateContext();
        if (shipmentId != Guid.Empty)
        {
            context.ShipmentPackages.RemoveRange(context.ShipmentPackages.Where(x => x.ShipmentId == shipmentId));
            context.ShipmentItems.RemoveRange(context.ShipmentItems.Where(x => x.ShipmentId == shipmentId));
            var shipment = await context.Shipments.FindAsync(shipmentId);
            if (shipment is not null)
            {
                context.Shipments.Remove(shipment);
            }
        }

        var profile = await context.ProductPhysicalProfiles.FindAsync(profileId);
        if (profile is not null)
        {
            context.ProductPhysicalProfiles.Remove(profile);
        }

        context.AuditLogs.RemoveRange(context.AuditLogs.Where(x => x.CorrelationId == "l4b-integration" || x.CorrelationId == "l4b-ceiling"));
        context.IdempotencyRecords.RemoveRange(context.IdempotencyRecords.Where(x => x.Scope.StartsWith("shipment-package:create:") || x.Scope.StartsWith("shipment:create:")));
        await context.SaveChangesAsync();
    }
}
