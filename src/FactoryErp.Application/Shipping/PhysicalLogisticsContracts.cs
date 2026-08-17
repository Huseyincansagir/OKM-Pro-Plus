namespace FactoryErp.Application.Shipping;

public sealed record CreateProductPhysicalProfileRequest(
    Guid ProductId,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    decimal LengthMm,
    decimal WidthMm,
    decimal HeightMm,
    decimal NetWeightKg,
    decimal? VolumeM3,
    bool IsStackable,
    int? MaxStackCount,
    decimal? MaxLoadAboveKg,
    bool KeepUpright,
    bool IsFragile,
    string? CompatibilityGroup,
    string IncompatibleGroups,
    string AllowedOrientations,
    string PhysicalPolicySnapshot);

public sealed record CreatePackagingPhysicalProfileRequest(
    Guid PackagingId,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    decimal UnitsPerPackage,
    decimal LengthMm,
    decimal WidthMm,
    decimal HeightMm,
    decimal? NetWeightKg,
    decimal TareWeightKg,
    decimal? GrossWeightKg,
    decimal? VolumeM3,
    bool IsStackable,
    int? MaxStackCount,
    decimal? MaxLoadAboveKg,
    bool KeepUpright,
    bool IsFragile,
    string? CompatibilityGroup,
    string IncompatibleGroups,
    string AllowedOrientations,
    string PhysicalPolicySnapshot);

public sealed record CreatePalletTypeRequest(
    string Code,
    string Name,
    decimal LengthMm,
    decimal WidthMm,
    decimal HeightMm,
    decimal TareWeightKg,
    decimal? MaxGrossWeightKg,
    decimal? MaxPayloadKg,
    decimal? MaxLoadHeightMm,
    int? MaxStackCount,
    bool IsStackable,
    string PolicySnapshot);

public sealed record ProductPhysicalProfileDto(
    Guid Id,
    Guid ProductId,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    decimal LengthMm,
    decimal WidthMm,
    decimal HeightMm,
    decimal NetWeightKg,
    decimal? VolumeM3,
    bool IsStackable,
    int? MaxStackCount,
    bool KeepUpright,
    bool IsFragile,
    long RowVersion);

public sealed record PackagingPhysicalProfileDto(
    Guid Id,
    Guid PackagingId,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    decimal UnitsPerPackage,
    decimal LengthMm,
    decimal WidthMm,
    decimal HeightMm,
    decimal? NetWeightKg,
    decimal TareWeightKg,
    decimal? GrossWeightKg,
    decimal? VolumeM3,
    bool IsStackable,
    int? MaxStackCount,
    bool KeepUpright,
    long RowVersion);

public sealed record PalletTypeDto(
    Guid Id,
    string Code,
    string Name,
    decimal LengthMm,
    decimal WidthMm,
    decimal HeightMm,
    decimal TareWeightKg,
    decimal? MaxGrossWeightKg,
    decimal? MaxPayloadKg,
    decimal? MaxLoadHeightMm,
    int? MaxStackCount,
    bool IsStackable,
    bool IsActive,
    long RowVersion);

public interface IPhysicalLogisticsCommandService
{
    Task<ProductPhysicalProfileDto> CreateProductProfileAsync(CreateProductPhysicalProfileRequest request, Guid actorId, string idempotencyKey, string correlationId, CancellationToken cancellationToken = default);
    Task<ProductPhysicalProfileDto?> GetProductProfileAsync(Guid productId, DateTimeOffset asOf, CancellationToken cancellationToken = default);
    Task<PackagingPhysicalProfileDto> CreatePackagingProfileAsync(CreatePackagingPhysicalProfileRequest request, Guid actorId, string idempotencyKey, string correlationId, CancellationToken cancellationToken = default);
    Task<PackagingPhysicalProfileDto?> GetPackagingProfileAsync(Guid packagingId, DateTimeOffset asOf, CancellationToken cancellationToken = default);
    Task<PalletTypeDto> CreatePalletTypeAsync(CreatePalletTypeRequest request, Guid actorId, string idempotencyKey, string correlationId, CancellationToken cancellationToken = default);
    Task<PalletTypeDto?> GetPalletTypeAsync(Guid id, CancellationToken cancellationToken = default);
}
