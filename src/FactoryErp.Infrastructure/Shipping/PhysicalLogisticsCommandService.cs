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

public sealed class PhysicalLogisticsCommandService(
    FactoryErpDbContext dbContext,
    IAuditWriter auditWriter,
    IIdempotencyStore idempotencyStore) : IPhysicalLogisticsCommandService
{
    public async Task<ProductPhysicalProfileDto> CreateProductProfileAsync(CreateProductPhysicalProfileRequest request, Guid actorId, string idempotencyKey, string correlationId, CancellationToken cancellationToken = default)
    {
        var scope = $"product-physical-profile:create:{actorId}:{request.ProductId}";
        var hash = Hash(request);
        var replay = await ReplayAsync<ProductPhysicalProfileDto>(scope, idempotencyKey, hash, cancellationToken);
        if (replay is not null) return replay;

        await EnsureProductAsync(request.ProductId, cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await EnsureNoProductOverlapAsync(request.ProductId, request.EffectiveFrom, request.EffectiveTo, cancellationToken);
        var profile = ProductPhysicalProfile.Create(Guid.NewGuid(), request.ProductId, request.EffectiveFrom, request.EffectiveTo, request.LengthMm, request.WidthMm, request.HeightMm, request.NetWeightKg);
        profile.SetVolume(request.VolumeM3);
        profile.SetHandlingRules(request.IsStackable, request.MaxStackCount, request.MaxLoadAboveKg, request.KeepUpright, request.IsFragile);
        var record = new ProductPhysicalProfileRecord
        {
            Id = profile.Id, ProductId = profile.ProductId, EffectiveFrom = profile.EffectiveFrom, EffectiveTo = profile.EffectiveTo,
            LengthMm = profile.LengthMm, WidthMm = profile.WidthMm, HeightMm = profile.HeightMm, NetWeightKg = profile.NetWeightKg,
            VolumeM3 = profile.VolumeM3, IsStackable = profile.IsStackable, MaxStackCount = profile.MaxStackCount,
            MaxLoadAboveKg = profile.MaxLoadAboveKg, KeepUpright = profile.KeepUpright, IsFragile = profile.IsFragile,
            CompatibilityGroup = request.CompatibilityGroup, IncompatibleGroups = request.IncompatibleGroups, AllowedOrientations = request.AllowedOrientations,
            PhysicalPolicySnapshot = request.PhysicalPolicySnapshot, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, RowVersion = 1
        };
        dbContext.ProductPhysicalProfiles.Add(record);
        await auditWriter.AppendAsync(new("ProductPhysicalProfileCreated", nameof(ProductPhysicalProfileRecord), record.Id, actorId, correlationId, AfterJson: JsonSerializer.Serialize(new { record.ProductId, record.EffectiveFrom })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = Map(record);
        await SaveAsync(scope, idempotencyKey, hash, 201, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<ProductPhysicalProfileDto?> GetProductProfileAsync(Guid productId, DateTimeOffset asOf, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.ProductPhysicalProfiles.AsNoTracking().Where(x => x.ProductId == productId && x.EffectiveFrom <= asOf && (x.EffectiveTo == null || asOf < x.EffectiveTo)).OrderByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(cancellationToken);
        return record is null ? null : Map(record);
    }

    public async Task<PackagingPhysicalProfileDto> CreatePackagingProfileAsync(CreatePackagingPhysicalProfileRequest request, Guid actorId, string idempotencyKey, string correlationId, CancellationToken cancellationToken = default)
    {
        var scope = $"packaging-physical-profile:create:{actorId}:{request.PackagingId}";
        var hash = Hash(request);
        var replay = await ReplayAsync<PackagingPhysicalProfileDto>(scope, idempotencyKey, hash, cancellationToken);
        if (replay is not null) return replay;
        await EnsurePackagingAsync(request.PackagingId, cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await EnsureNoPackagingOverlapAsync(request.PackagingId, request.EffectiveFrom, request.EffectiveTo, cancellationToken);
        var profile = PackagingPhysicalProfile.Create(Guid.NewGuid(), request.PackagingId, request.EffectiveFrom, request.EffectiveTo, request.UnitsPerPackage, request.LengthMm, request.WidthMm, request.HeightMm, request.TareWeightKg);
        profile.SetWeights(request.NetWeightKg, request.GrossWeightKg, request.VolumeM3);
        profile.SetHandlingRules(request.IsStackable, request.MaxStackCount, request.KeepUpright);
        var record = new PackagingPhysicalProfileRecord
        {
            Id = profile.Id, PackagingId = profile.PackagingId, EffectiveFrom = profile.EffectiveFrom, EffectiveTo = profile.EffectiveTo,
            UnitsPerPackage = profile.UnitsPerPackage, LengthMm = profile.LengthMm, WidthMm = profile.WidthMm, HeightMm = profile.HeightMm,
            NetWeightKg = profile.NetWeightKg, TareWeightKg = profile.TareWeightKg, GrossWeightKg = profile.GrossWeightKg, VolumeM3 = profile.VolumeM3,
            IsStackable = profile.IsStackable, MaxStackCount = profile.MaxStackCount, KeepUpright = profile.KeepUpright,
            IsFragile = request.IsFragile, CompatibilityGroup = request.CompatibilityGroup, IncompatibleGroups = request.IncompatibleGroups,
            AllowedOrientations = request.AllowedOrientations, PhysicalPolicySnapshot = request.PhysicalPolicySnapshot,
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, RowVersion = 1
        };
        dbContext.PackagingPhysicalProfiles.Add(record);
        await auditWriter.AppendAsync(new("PackagingPhysicalProfileCreated", nameof(PackagingPhysicalProfileRecord), record.Id, actorId, correlationId, AfterJson: JsonSerializer.Serialize(new { record.PackagingId, record.EffectiveFrom })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = Map(record);
        await SaveAsync(scope, idempotencyKey, hash, 201, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<PackagingPhysicalProfileDto?> GetPackagingProfileAsync(Guid packagingId, DateTimeOffset asOf, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.PackagingPhysicalProfiles.AsNoTracking().Where(x => x.PackagingId == packagingId && x.EffectiveFrom <= asOf && (x.EffectiveTo == null || asOf < x.EffectiveTo)).OrderByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(cancellationToken);
        return record is null ? null : Map(record);
    }

    public async Task<PalletTypeDto> CreatePalletTypeAsync(CreatePalletTypeRequest request, Guid actorId, string idempotencyKey, string correlationId, CancellationToken cancellationToken = default)
    {
        var scope = $"pallet-type:create:{actorId}";
        var hash = Hash(request);
        var replay = await ReplayAsync<PalletTypeDto>(scope, idempotencyKey, hash, cancellationToken);
        if (replay is not null) return replay;
        var pallet = PalletType.Create(Guid.NewGuid(), request.Code, request.Name, request.LengthMm, request.WidthMm, request.HeightMm, request.TareWeightKg);
        pallet.SetCapacity(request.MaxGrossWeightKg, request.MaxPayloadKg, request.MaxLoadHeightMm, request.MaxStackCount, request.IsStackable);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var record = new PalletTypeRecord
        {
            Id = pallet.Id, Code = pallet.Code, Name = pallet.Name, LengthMm = pallet.LengthMm, WidthMm = pallet.WidthMm, HeightMm = pallet.HeightMm,
            TareWeightKg = pallet.TareWeightKg, MaxGrossWeightKg = pallet.MaxGrossWeightKg, MaxPayloadKg = pallet.MaxPayloadKg,
            MaxLoadHeightMm = pallet.MaxLoadHeightMm, MaxStackCount = pallet.MaxStackCount, IsStackable = pallet.IsStackable, IsActive = pallet.IsActive,
            PolicySnapshot = request.PolicySnapshot, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, RowVersion = 1
        };
        dbContext.PalletTypes.Add(record);
        await auditWriter.AppendAsync(new("PalletTypeCreated", nameof(PalletTypeRecord), record.Id, actorId, correlationId, AfterJson: JsonSerializer.Serialize(new { record.Code, record.Name })), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var result = Map(record);
        await SaveAsync(scope, idempotencyKey, hash, 201, result, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<PalletTypeDto?> GetPalletTypeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.PalletTypes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return record is null ? null : Map(record);
    }

    private async Task EnsureProductAsync(Guid id, CancellationToken ct)
    {
        if (!await dbContext.Products.AsNoTracking().AnyAsync(x => x.Id == id && x.IsActive, ct)) throw new DomainException(new("PRODUCT_NOT_FOUND", "Aktif ürün bulunamadı."));
    }

    private async Task EnsurePackagingAsync(Guid id, CancellationToken ct)
    {
        if (!await dbContext.ProductPackagings.AsNoTracking().AnyAsync(x => x.Id == id, ct)) throw new DomainException(new("PACKAGING_NOT_FOUND", "Ambalaj bulunamadı."));
    }

    private async Task EnsureNoProductOverlapAsync(Guid productId, DateTimeOffset from, DateTimeOffset? to, CancellationToken ct)
    {
        var end = to ?? DateTimeOffset.MaxValue;
        if (await dbContext.ProductPhysicalProfiles.AnyAsync(x => x.ProductId == productId && x.EffectiveFrom < end && (x.EffectiveTo ?? DateTimeOffset.MaxValue) > from, ct)) throw new DomainException(new("PHYSICAL_PROFILE_OVERLAP", "Ürün fiziksel profil aralığı çakışıyor."));
    }

    private async Task EnsureNoPackagingOverlapAsync(Guid packagingId, DateTimeOffset from, DateTimeOffset? to, CancellationToken ct)
    {
        var end = to ?? DateTimeOffset.MaxValue;
        if (await dbContext.PackagingPhysicalProfiles.AnyAsync(x => x.PackagingId == packagingId && x.EffectiveFrom < end && (x.EffectiveTo ?? DateTimeOffset.MaxValue) > from, ct)) throw new DomainException(new("PACKAGING_PHYSICAL_OVERLAP", "Ambalaj fiziksel profil aralığı çakışıyor."));
    }

    private async Task<T?> ReplayAsync<T>(string scope, string key, string hash, CancellationToken ct)
    {
        var stored = await idempotencyStore.FindAsync(scope, key, ct);
        if (stored is null) return default;
        if (!string.Equals(stored.PayloadHash, hash, StringComparison.Ordinal)) throw new DomainException(new("IDEMPOTENCY_PAYLOAD_MISMATCH", "Aynı idempotency anahtarı farklı payload ile kullanıldı."));
        return JsonSerializer.Deserialize<T>(stored.ResponseBody);
    }

    private Task SaveAsync<T>(string scope, string key, string hash, int status, T response, CancellationToken ct) => idempotencyStore.SaveAsync(scope, key, hash, status, JsonSerializer.Serialize(response), DateTimeOffset.UtcNow.AddHours(24), ct);
    private static string Hash<T>(T value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))));

    private static ProductPhysicalProfileDto Map(ProductPhysicalProfileRecord x) => new(x.Id, x.ProductId, x.EffectiveFrom, x.EffectiveTo, x.LengthMm, x.WidthMm, x.HeightMm, x.NetWeightKg, x.VolumeM3, x.IsStackable, x.MaxStackCount, x.KeepUpright, x.IsFragile, x.RowVersion);
    private static PackagingPhysicalProfileDto Map(PackagingPhysicalProfileRecord x) => new(x.Id, x.PackagingId, x.EffectiveFrom, x.EffectiveTo, x.UnitsPerPackage, x.LengthMm, x.WidthMm, x.HeightMm, x.NetWeightKg, x.TareWeightKg, x.GrossWeightKg, x.VolumeM3, x.IsStackable, x.MaxStackCount, x.KeepUpright, x.RowVersion);
    private static PalletTypeDto Map(PalletTypeRecord x) => new(x.Id, x.Code, x.Name, x.LengthMm, x.WidthMm, x.HeightMm, x.TareWeightKg, x.MaxGrossWeightKg, x.MaxPayloadKg, x.MaxLoadHeightMm, x.MaxStackCount, x.IsStackable, x.IsActive, x.RowVersion);
}
