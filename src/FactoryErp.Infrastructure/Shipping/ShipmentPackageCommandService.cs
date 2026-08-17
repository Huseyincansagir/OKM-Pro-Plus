using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FactoryErp.Application.Abstractions.Persistence;
using FactoryErp.Application.Shipping;
using FactoryErp.Domain.Common;
using FactoryErp.Domain.Shipping;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.Shipping;

public sealed class ShipmentPackageCommandService(
    FactoryErpDbContext dbContext,
    IAuditWriter auditWriter,
    IIdempotencyStore idempotencyStore) : IShipmentPackageCommandService
{
    public async Task<ShipmentPackageDto> CreateShipmentPackageAsync(
        Guid shipmentId,
        CreateShipmentPackageRequest request,
        Guid actorId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        DomainGuard.AgainstEmpty(shipmentId, "SHIPMENT_REQUIRED", "Shipment zorunludur.");

        var scope = $"shipment-package:create:{actorId}:{shipmentId}";
        var payloadHash = ComputePayloadHash(new { shipmentId, request });
        var replay = await TryReplayAsync<ShipmentPackageDto>(scope, idempotencyKey, payloadHash, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var shipmentItem = await LockShipmentItemAsync(request.ShipmentItemId, cancellationToken);
        if (shipmentItem is null || shipmentItem.ShipmentId != shipmentId)
        {
            throw new DomainException(new(
                "SHIPMENT_ITEM_NOT_FOUND",
                "Shipment item belirtilen shipment'a ait değildir veya bulunamadı."));
        }

        if (!await dbContext.Shipments.AsNoTracking().AnyAsync(x => x.Id == shipmentId, cancellationToken))
        {
            throw new DomainException(new("SHIPMENT_NOT_FOUND", "Shipment bulunamadı."));
        }

        var routeStopShipmentId = request.RouteStopId is null
            ? null
            : await dbContext.RouteStops
                .AsNoTracking()
                .Where(x => x.Id == request.RouteStopId)
                .Select(x => (Guid?)x.RoutePlan.ShipmentId)
                .SingleOrDefaultAsync(cancellationToken);
        ShipmentPackage.EnsureOwnership(
            shipmentId,
            shipmentItem.ShipmentId,
            request.RouteStopId,
            routeStopShipmentId);

        var packaging = await ResolvePackagingAsync(
            shipmentItem.ProductId,
            request.PackagingId,
            cancellationToken);
        var packagingSnapshot = BuildPackagingSnapshot(shipmentItem.PackagingSnapshot, packaging);
        var physicalSnapshot = await ResolvePhysicalSnapshotAsync(
            shipmentItem.ProductId,
            request.PackagingId,
            cancellationToken);

        var packageType = ParsePackageType(request.PackageType);
        var package = ShipmentPackage.Create(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            shipmentId,
            shipmentItem.Id,
            request.PackagingId,
            request.RouteStopId,
            packageType,
            request.PackageCount,
            request.QuantityBasePerPackage,
            request.EnteredQuantity,
            request.PackageCode,
            packagingSnapshot,
            physicalSnapshot,
            request.SplitAllowed);

        var allocatedQuantity = await dbContext.ShipmentPackages
            .Where(x => x.ShipmentItemId == shipmentItem.Id && x.Status != nameof(ShipmentPackageStatus.Cancelled))
            .SumAsync(x => (decimal?)x.QuantityBase, cancellationToken) ?? 0m;
        if (allocatedQuantity + package.QuantityBase > shipmentItem.QuantityBase)
        {
            throw new DomainException(new(
                "SHIPMENT_PACKAGE_QUANTITY_CEILING_EXCEEDED",
                "Shipment package toplamı shipment item miktarını aşamaz."));
        }

        if (package.PackageCode is not null && await dbContext.ShipmentPackages.AnyAsync(
                x => x.PackageCode == package.PackageCode && x.Status != nameof(ShipmentPackageStatus.Cancelled),
                cancellationToken))
        {
            throw new DomainException(new(
                "SHIPMENT_PACKAGE_CODE_DUPLICATE",
                "Aktif package code daha önce kullanılmıştır."));
        }

        var record = ToRecord(package);
        dbContext.ShipmentPackages.Add(record);
        await auditWriter.AppendAsync(new(
            "ShipmentPackageCreated",
            nameof(ShipmentPackageRecord),
            record.Id,
            actorId,
            correlationId,
            AfterJson: JsonSerializer.Serialize(new
            {
                record.ShipmentId,
                record.ShipmentItemId,
                record.RouteStopId,
                record.PackageType,
                record.PackageCount,
                record.QuantityBase,
            })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var result = Map(record);
        await SaveIdempotencyAsync(scope, idempotencyKey, payloadHash, 201, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyCollection<ShipmentPackageDto>> GetShipmentPackagesAsync(
        Guid shipmentId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ShipmentPackages
            .AsNoTracking()
            .Where(x => x.ShipmentId == shipmentId)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Select(x => MapProjection(x))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<ShipmentPackageDto?> GetShipmentPackageAsync(
        Guid packageId,
        CancellationToken cancellationToken = default)
    {
        var record = await dbContext.ShipmentPackages
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == packageId, cancellationToken);
        return record is null ? null : Map(record);
    }

    private async Task<ShipmentItemRecord?> LockShipmentItemAsync(Guid shipmentItemId, CancellationToken cancellationToken)
        => await dbContext.ShipmentItems
            .FromSqlInterpolated($"SELECT * FROM shipment_items WHERE id = {shipmentItemId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<ProductPackagingRecord?> ResolvePackagingAsync(
        Guid productId,
        Guid? packagingId,
        CancellationToken cancellationToken)
    {
        if (packagingId is null)
        {
            return null;
        }

        var packaging = await dbContext.ProductPackagings.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == packagingId && x.ProductId == productId,
            cancellationToken);
        if (packaging is null)
        {
            throw new DomainException(new(
                "SHIPMENT_PACKAGE_PACKAGING_INVALID",
                "Packaging shipment item ürününe ait değildir veya bulunamadı."));
        }

        return packaging;
    }

    private async Task<string> ResolvePhysicalSnapshotAsync(
        Guid productId,
        Guid? packagingId,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        if (packagingId is not null)
        {
            var packagingProfile = await dbContext.PackagingPhysicalProfiles
                .AsNoTracking()
                .Where(x => x.PackagingId == packagingId
                    && x.EffectiveFrom <= now
                    && (x.EffectiveTo == null || x.EffectiveTo > now))
                .OrderByDescending(x => x.EffectiveFrom)
                .FirstOrDefaultAsync(cancellationToken);
            if (packagingProfile is not null)
            {
                return JsonSerializer.Serialize(new
                {
                    source = "PackagingPhysicalProfile",
                    profileId = packagingProfile.Id,
                    packagingId = packagingProfile.PackagingId,
                    effectiveFrom = packagingProfile.EffectiveFrom,
                    effectiveTo = packagingProfile.EffectiveTo,
                    unitsPerPackage = packagingProfile.UnitsPerPackage,
                    lengthMm = packagingProfile.LengthMm,
                    widthMm = packagingProfile.WidthMm,
                    heightMm = packagingProfile.HeightMm,
                    netWeightKg = packagingProfile.NetWeightKg,
                    tareWeightKg = packagingProfile.TareWeightKg,
                    grossWeightKg = packagingProfile.GrossWeightKg,
                    volumeM3 = packagingProfile.VolumeM3,
                    isStackable = packagingProfile.IsStackable,
                    maxStackCount = packagingProfile.MaxStackCount,
                    maxLoadAboveKg = packagingProfile.MaxLoadAboveKg,
                    keepUpright = packagingProfile.KeepUpright,
                    isFragile = packagingProfile.IsFragile,
                    compatibilityGroup = packagingProfile.CompatibilityGroup,
                    incompatibleGroups = packagingProfile.IncompatibleGroups,
                    allowedOrientations = packagingProfile.AllowedOrientations,
                    physicalPolicySnapshot = packagingProfile.PhysicalPolicySnapshot,
                });
            }
        }

        var productProfile = await dbContext.ProductPhysicalProfiles
            .AsNoTracking()
            .Where(x => x.ProductId == productId
                && x.EffectiveFrom <= now
                && (x.EffectiveTo == null || x.EffectiveTo > now))
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
        if (productProfile is null)
        {
            throw new DomainException(new(
                "PHYSICAL_PROFILE_MISSING",
                "Shipment package için geçerli ürün fiziksel profili bulunamadı."));
        }

        return JsonSerializer.Serialize(new
        {
            source = "ProductPhysicalProfile",
            profileId = productProfile.Id,
            productId = productProfile.ProductId,
            effectiveFrom = productProfile.EffectiveFrom,
            effectiveTo = productProfile.EffectiveTo,
            lengthMm = productProfile.LengthMm,
            widthMm = productProfile.WidthMm,
            heightMm = productProfile.HeightMm,
            netWeightKg = productProfile.NetWeightKg,
            volumeM3 = productProfile.VolumeM3,
            isStackable = productProfile.IsStackable,
            maxStackCount = productProfile.MaxStackCount,
            maxLoadAboveKg = productProfile.MaxLoadAboveKg,
            keepUpright = productProfile.KeepUpright,
            isFragile = productProfile.IsFragile,
            compatibilityGroup = productProfile.CompatibilityGroup,
            incompatibleGroups = productProfile.IncompatibleGroups,
            allowedOrientations = productProfile.AllowedOrientations,
            physicalPolicySnapshot = productProfile.PhysicalPolicySnapshot,
        });
    }

    private static string BuildPackagingSnapshot(string fallbackSnapshot, ProductPackagingRecord? packaging)
    {
        if (packaging is null)
        {
            return fallbackSnapshot;
        }

        return JsonSerializer.Serialize(new
        {
            packagingId = packaging.Id,
            productId = packaging.ProductId,
            level = packaging.Level,
            name = packaging.Name,
            parentPackagingId = packaging.ParentPackagingId,
            unitsPerParent = packaging.UnitsPerParent,
            quantityInBaseUom = packaging.QuantityInBaseUom,
            allowPartial = packaging.AllowPartial,
            effectiveFrom = packaging.EffectiveFrom,
            effectiveTo = packaging.EffectiveTo,
        });
    }

    private static ShipmentPackageType ParsePackageType(string value)
        => Enum.TryParse<ShipmentPackageType>(value, true, out var result)
            ? result
            : throw new DomainException(new("SHIPMENT_PACKAGE_TYPE_INVALID", $"Geçersiz package tipi: {value}."));

    private static ShipmentPackageRecord ToRecord(ShipmentPackage package)
        => new()
        {
            Id = package.Id,
            ShipmentId = package.ShipmentId,
            ShipmentItemId = package.ShipmentItemId,
            PackagingId = package.PackagingId,
            RouteStopId = package.RouteStopId,
            PackageType = package.PackageType.ToString(),
            PackageCount = package.PackageCount,
            QuantityBasePerPackage = package.QuantityBasePerPackage,
            QuantityBase = package.QuantityBase,
            EnteredQuantity = package.EnteredQuantity,
            PackageCode = package.PackageCode,
            PackagingSnapshot = package.PackagingSnapshot,
            PhysicalSnapshot = package.PhysicalSnapshot,
            SplitAllowed = package.SplitAllowed,
            Status = package.Status.ToString(),
            CreatedAt = package.CreatedAt,
            UpdatedAt = package.UpdatedAt,
            RowVersion = package.RowVersion,
        };

    private static ShipmentPackageDto Map(ShipmentPackageRecord record)
        => new(
            record.Id,
            record.ShipmentId,
            record.ShipmentItemId,
            record.PackagingId,
            record.RouteStopId,
            record.PackageType,
            record.PackageCount,
            record.QuantityBasePerPackage,
            record.QuantityBase,
            record.EnteredQuantity,
            record.PackageCode,
            record.PackagingSnapshot,
            record.PhysicalSnapshot,
            record.SplitAllowed,
            record.Status,
            record.CreatedAt,
            record.UpdatedAt,
            record.RowVersion);

    private static ShipmentPackageDto MapProjection(ShipmentPackageRecord record)
        => Map(record);

    private async Task<T?> TryReplayAsync<T>(
        string scope,
        string key,
        string payloadHash,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainException(new("MISSING_IDEMPOTENCY_KEY", "Idempotency-Key zorunludur."));
        }

        var stored = await idempotencyStore.FindAsync(scope, key, cancellationToken);
        if (stored is null)
        {
            return default;
        }

        if (!string.Equals(stored.PayloadHash, payloadHash, StringComparison.Ordinal))
        {
            throw new DomainException(new("IDEMPOTENCY_PAYLOAD_MISMATCH", "Aynı Idempotency-Key farklı payload ile kullanıldı."));
        }

        return JsonSerializer.Deserialize<T>(stored.ResponseBody)
            ?? throw new DomainException(new("IDEMPOTENCY_REPLAY_INVALID", "Idempotency replay sonucu okunamadı."));
    }

    private Task SaveIdempotencyAsync<T>(
        string scope,
        string key,
        string payloadHash,
        int statusCode,
        T result,
        CancellationToken cancellationToken)
        => idempotencyStore.SaveAsync(
            scope,
            key,
            payloadHash,
            statusCode,
            JsonSerializer.Serialize(result),
            DateTimeOffset.UtcNow.AddDays(30),
            cancellationToken);

    private static string ComputePayloadHash<T>(T payload)
    {
        var json = JsonSerializer.Serialize(payload);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }
}
