using FactoryErp.Domain.Common;

namespace FactoryErp.Domain.Shipping;

public sealed class ProductPhysicalProfile : Entity
{
    private ProductPhysicalProfile(Guid id, Guid productId, DateTimeOffset effectiveFrom, DateTimeOffset? effectiveTo, decimal lengthMm, decimal widthMm, decimal heightMm, decimal netWeightKg)
        : base(id, DateTimeOffset.UtcNow)
    {
        ProductId = productId;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        LengthMm = lengthMm;
        WidthMm = widthMm;
        HeightMm = heightMm;
        NetWeightKg = netWeightKg;
        IsStackable = true;
    }

    public Guid ProductId { get; }
    public DateTimeOffset EffectiveFrom { get; }
    public DateTimeOffset? EffectiveTo { get; }
    public decimal LengthMm { get; }
    public decimal WidthMm { get; }
    public decimal HeightMm { get; }
    public decimal NetWeightKg { get; }
    public decimal? VolumeM3 { get; private set; }
    public bool IsStackable { get; private set; }
    public int? MaxStackCount { get; private set; }
    public decimal? MaxLoadAboveKg { get; private set; }
    public bool KeepUpright { get; private set; }
    public bool IsFragile { get; private set; }

    public static ProductPhysicalProfile Create(Guid id, Guid productId, DateTimeOffset effectiveFrom, DateTimeOffset? effectiveTo, decimal lengthMm, decimal widthMm, decimal heightMm, decimal netWeightKg)
    {
        DomainGuard.AgainstEmpty(id, "PHYSICAL_PROFILE_ID_REQUIRED", "Fiziksel profil kimliği zorunludur.");
        DomainGuard.AgainstEmpty(productId, "PRODUCT_REQUIRED", "Fiziksel profil ürüne bağlı olmalıdır.");
        ValidateRange(effectiveFrom, effectiveTo);
        if (lengthMm <= 0 || widthMm <= 0 || heightMm <= 0)
            throw new DomainException(new("PHYSICAL_DIMENSIONS_INVALID", "Fiziksel ölçüler sıfırdan büyük olmalıdır."));
        if (netWeightKg < 0)
            throw new DomainException(new("PHYSICAL_WEIGHT_INVALID", "Net ağırlık negatif olamaz."));
        return new ProductPhysicalProfile(id, productId, effectiveFrom, effectiveTo, lengthMm, widthMm, heightMm, netWeightKg);
    }

    public void SetHandlingRules(bool isStackable, int? maxStackCount, decimal? maxLoadAboveKg, bool keepUpright, bool isFragile)
    {
        if (maxStackCount is <= 0)
            throw new DomainException(new("STACK_COUNT_INVALID", "Maksimum istif sayısı pozitif olmalıdır."));
        if (maxLoadAboveKg is < 0)
            throw new DomainException(new("LOAD_ABOVE_INVALID", "Üst yük sınırı negatif olamaz."));
        if (!isStackable && maxStackCount is > 1)
            throw new DomainException(new("STACK_RULE_CONFLICT", "İstiflenemeyen ürün birden fazla kat taşıyamaz."));
        IsStackable = isStackable;
        MaxStackCount = maxStackCount;
        MaxLoadAboveKg = maxLoadAboveKg;
        KeepUpright = keepUpright;
        IsFragile = isFragile;
    }

    public void SetVolume(decimal? volumeM3)
    {
        if (volumeM3 is <= 0)
            throw new DomainException(new("PHYSICAL_VOLUME_INVALID", "Hacim sıfırdan büyük olmalıdır."));
        VolumeM3 = volumeM3;
    }

    private static void ValidateRange(DateTimeOffset from, DateTimeOffset? to)
    {
        if (to is not null && to <= from)
            throw new DomainException(new("PHYSICAL_EFFECTIVE_RANGE_INVALID", "Geçerlilik aralığı geçersizdir."));
    }
}

public sealed class PackagingPhysicalProfile : Entity
{
    private PackagingPhysicalProfile(Guid id, Guid packagingId, DateTimeOffset effectiveFrom, DateTimeOffset? effectiveTo, decimal unitsPerPackage, decimal lengthMm, decimal widthMm, decimal heightMm, decimal tareWeightKg)
        : base(id, DateTimeOffset.UtcNow)
    {
        PackagingId = packagingId;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        UnitsPerPackage = unitsPerPackage;
        LengthMm = lengthMm;
        WidthMm = widthMm;
        HeightMm = heightMm;
        TareWeightKg = tareWeightKg;
        IsStackable = true;
    }

    public Guid PackagingId { get; }
    public DateTimeOffset EffectiveFrom { get; }
    public DateTimeOffset? EffectiveTo { get; }
    public decimal UnitsPerPackage { get; }
    public decimal LengthMm { get; }
    public decimal WidthMm { get; }
    public decimal HeightMm { get; }
    public decimal TareWeightKg { get; }
    public decimal? NetWeightKg { get; private set; }
    public decimal? GrossWeightKg { get; private set; }
    public decimal? VolumeM3 { get; private set; }
    public bool IsStackable { get; private set; }
    public int? MaxStackCount { get; private set; }
    public bool KeepUpright { get; private set; }

    public static PackagingPhysicalProfile Create(Guid id, Guid packagingId, DateTimeOffset effectiveFrom, DateTimeOffset? effectiveTo, decimal unitsPerPackage, decimal lengthMm, decimal widthMm, decimal heightMm, decimal tareWeightKg)
    {
        DomainGuard.AgainstEmpty(id, "PACKAGING_PHYSICAL_PROFILE_ID_REQUIRED", "Ambalaj fiziksel profil kimliği zorunludur.");
        DomainGuard.AgainstEmpty(packagingId, "PACKAGING_REQUIRED", "Fiziksel profil ambalaja bağlı olmalıdır.");
        if (unitsPerPackage <= 0 || lengthMm <= 0 || widthMm <= 0 || heightMm <= 0)
            throw new DomainException(new("PACKAGING_PHYSICAL_VALUES_INVALID", "Ambalaj fiziksel değerleri geçerli olmalıdır."));
        if (tareWeightKg < 0)
            throw new DomainException(new("PACKAGING_TARE_INVALID", "Ambalaj darası negatif olamaz."));
        if (effectiveTo is not null && effectiveTo <= effectiveFrom)
            throw new DomainException(new("PACKAGING_EFFECTIVE_RANGE_INVALID", "Ambalaj geçerlilik aralığı geçersizdir."));
        return new PackagingPhysicalProfile(id, packagingId, effectiveFrom, effectiveTo, unitsPerPackage, lengthMm, widthMm, heightMm, tareWeightKg);
    }

    public void SetWeights(decimal? netWeightKg, decimal? grossWeightKg, decimal? volumeM3)
    {
        if (netWeightKg is < 0 || grossWeightKg is < 0 || volumeM3 is <= 0)
            throw new DomainException(new("PACKAGING_WEIGHT_VOLUME_INVALID", "Ambalaj ağırlık ve hacim değerleri geçerli olmalıdır."));
        if (grossWeightKg is not null && netWeightKg is not null && grossWeightKg < netWeightKg + TareWeightKg)
            throw new DomainException(new("PACKAGING_GROSS_WEIGHT_INVALID", "Brüt ağırlık net ağırlık ve daradan küçük olamaz."));
        NetWeightKg = netWeightKg;
        GrossWeightKg = grossWeightKg;
        VolumeM3 = volumeM3;
    }

    public void SetHandlingRules(bool isStackable, int? maxStackCount, bool keepUpright)
    {
        if (maxStackCount is <= 0 || (!isStackable && maxStackCount is > 1))
            throw new DomainException(new("PACKAGING_STACK_RULE_INVALID", "Ambalaj istifleme kuralları geçersizdir."));
        IsStackable = isStackable;
        MaxStackCount = maxStackCount;
        KeepUpright = keepUpright;
    }
}

public sealed class PalletType : Entity
{
    private PalletType(Guid id, string code, string name, decimal lengthMm, decimal widthMm, decimal heightMm, decimal tareWeightKg)
        : base(id, DateTimeOffset.UtcNow)
    {
        Code = code;
        Name = name;
        LengthMm = lengthMm;
        WidthMm = widthMm;
        HeightMm = heightMm;
        TareWeightKg = tareWeightKg;
        IsActive = true;
    }

    public string Code { get; }
    public string Name { get; }
    public decimal LengthMm { get; }
    public decimal WidthMm { get; }
    public decimal HeightMm { get; }
    public decimal TareWeightKg { get; }
    public decimal? MaxGrossWeightKg { get; private set; }
    public decimal? MaxPayloadKg { get; private set; }
    public decimal? MaxLoadHeightMm { get; private set; }
    public int? MaxStackCount { get; private set; }
    public bool IsStackable { get; private set; }
    public bool IsActive { get; private set; }

    public static PalletType Create(Guid id, string code, string name, decimal lengthMm, decimal widthMm, decimal heightMm, decimal tareWeightKg)
    {
        DomainGuard.AgainstEmpty(id, "PALLET_TYPE_ID_REQUIRED", "Palet tipi kimliği zorunludur.");
        DomainGuard.AgainstBlank(code, "PALLET_TYPE_CODE_REQUIRED", "Palet tipi kodu zorunludur.");
        DomainGuard.AgainstBlank(name, "PALLET_TYPE_NAME_REQUIRED", "Palet tipi adı zorunludur.");
        if (lengthMm <= 0 || widthMm <= 0 || heightMm <= 0 || tareWeightKg < 0)
            throw new DomainException(new("PALLET_TYPE_VALUES_INVALID", "Palet tipi fiziksel değerleri geçerli olmalıdır."));
        return new PalletType(id, code.Trim(), name.Trim(), lengthMm, widthMm, heightMm, tareWeightKg);
    }

    public void SetCapacity(decimal? maxGrossWeightKg, decimal? maxPayloadKg, decimal? maxLoadHeightMm, int? maxStackCount, bool isStackable)
    {
        if (maxGrossWeightKg is < 0 || maxPayloadKg is < 0 || maxLoadHeightMm is <= 0 || maxStackCount is <= 0)
            throw new DomainException(new("PALLET_CAPACITY_INVALID", "Palet kapasite değerleri geçerli olmalıdır."));
        if (maxPayloadKg is not null && maxGrossWeightKg is not null && maxPayloadKg > maxGrossWeightKg)
            throw new DomainException(new("PALLET_PAYLOAD_OVER_GROSS", "Palet payload değeri brüt kapasiteyi aşamaz."));
        MaxGrossWeightKg = maxGrossWeightKg;
        MaxPayloadKg = maxPayloadKg;
        MaxLoadHeightMm = maxLoadHeightMm;
        MaxStackCount = maxStackCount;
        IsStackable = isStackable;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
