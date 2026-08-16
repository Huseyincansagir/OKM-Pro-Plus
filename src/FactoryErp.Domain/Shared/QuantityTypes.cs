using FactoryErp.Domain.Common;

namespace FactoryErp.Domain.Shared;

public readonly record struct PositiveQuantity
{
    private PositiveQuantity(decimal baseValue, int scale)
    {
        BaseValue = baseValue;
        Scale = scale;
    }

    public decimal BaseValue { get; }
    public int Scale { get; }

    public static PositiveQuantity Create(decimal value, int scale)
    {
        ValidateScale(scale);

        if (value <= 0)
        {
            throw new DomainException(new(
                "QUANTITY_MUST_BE_POSITIVE",
                "İşlem miktarı sıfırdan büyük olmalıdır."));
        }

        ValidatePrecision(value, scale);
        return new PositiveQuantity(value, scale);
    }

    public PositiveQuantity Add(PositiveQuantity other)
    {
        var scale = Math.Max(Scale, other.Scale);
        return Create(BaseValue + other.BaseValue, scale);
    }

    public PositiveQuantity Subtract(PositiveQuantity other)
    {
        var scale = Math.Max(Scale, other.Scale);
        var result = BaseValue - other.BaseValue;

        if (result <= 0)
        {
            throw new DomainException(new(
                "QUANTITY_RESULT_NOT_POSITIVE",
                "Pozitif miktar çıkarma sonucu sıfır veya negatif olamaz."));
        }

        return Create(result, scale);
    }

    public bool IsGreaterThan(PositiveQuantity other) => BaseValue > other.BaseValue;

    public static bool operator <=(PositiveQuantity left, PositiveQuantity right)
        => left.BaseValue <= right.BaseValue;

    public static bool operator >=(PositiveQuantity left, PositiveQuantity right)
        => left.BaseValue >= right.BaseValue;

    public override string ToString() => BaseValue.ToString($"F{Scale}");

    internal static void ValidateScale(int scale)
    {
        if (scale is < 0 or > 6)
        {
            throw new DomainException(new(
                "UOM_SCALE_INVALID",
                "UOM precision 0 ile 6 arasında olmalıdır."));
        }
    }

    internal static void ValidatePrecision(decimal value, int scale)
    {
        if (decimal.Round(value, scale) != value)
        {
            throw new DomainException(new(
                "QUANTITY_PRECISION_EXCEEDED",
                "Miktar UOM precision sınırını aşıyor.",
                new Dictionary<string, object?>
                {
                    ["value"] = value,
                    ["scale"] = scale
                }));
        }
    }
}

public readonly record struct NonNegativeQuantity
{
    private NonNegativeQuantity(decimal baseValue, int scale)
    {
        BaseValue = baseValue;
        Scale = scale;
    }

    public decimal BaseValue { get; }
    public int Scale { get; }

    public static NonNegativeQuantity Zero(int scale)
    {
        PositiveQuantity.ValidateScale(scale);
        return new NonNegativeQuantity(0, scale);
    }

    public static NonNegativeQuantity Create(decimal value, int scale)
    {
        PositiveQuantity.ValidateScale(scale);

        if (value < 0)
        {
            throw new DomainException(new(
                "QUANTITY_MUST_BE_NON_NEGATIVE",
                "Projection miktarı negatif olamaz."));
        }

        PositiveQuantity.ValidatePrecision(value, scale);
        return new NonNegativeQuantity(value, scale);
    }

    public NonNegativeQuantity Add(PositiveQuantity other)
    {
        var scale = Math.Max(Scale, other.Scale);
        return Create(BaseValue + other.BaseValue, scale);
    }

    public NonNegativeQuantity Subtract(PositiveQuantity other)
    {
        var scale = Math.Max(Scale, other.Scale);
        var result = BaseValue - other.BaseValue;

        if (result < 0)
        {
            throw new DomainException(new(
                "QUANTITY_RESULT_NEGATIVE",
                "Projection miktarı negatif olamaz."));
        }

        return Create(result, scale);
    }

    public override string ToString() => BaseValue.ToString($"F{Scale}");
}

public readonly record struct UomCode
{
    private UomCode(string value) => Value = value;

    public string Value { get; }

    public static UomCode Create(string value)
    {
        DomainGuard.AgainstBlank(value, "UOM_CODE_REQUIRED", "UOM kodu zorunludur.");
        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length > 32)
        {
            throw new DomainException(new(
                "UOM_CODE_TOO_LONG",
                "UOM kodu 32 karakteri geçemez."));
        }

        return new UomCode(normalized);
    }

    public override string ToString() => Value;
}

public sealed record PackagingSnapshot
{
    private PackagingSnapshot(
        Guid? packagingId,
        string level,
        string name,
        UomCode baseUomCode,
        decimal quantityInBaseUom,
        bool allowPartial,
        string effectiveVersion)
    {
        PackagingId = packagingId;
        Level = level;
        Name = name;
        BaseUomCode = baseUomCode;
        QuantityInBaseUom = quantityInBaseUom;
        AllowPartial = allowPartial;
        EffectiveVersion = effectiveVersion;
    }

    public Guid? PackagingId { get; }
    public string Level { get; }
    public string Name { get; }
    public UomCode BaseUomCode { get; }
    public decimal QuantityInBaseUom { get; }
    public bool AllowPartial { get; }
    public string EffectiveVersion { get; }

    public static PackagingSnapshot Create(
        Guid? packagingId,
        string level,
        string name,
        UomCode baseUomCode,
        decimal quantityInBaseUom,
        bool allowPartial,
        string effectiveVersion)
    {
        DomainGuard.AgainstBlank(level, "PACKAGING_LEVEL_REQUIRED", "Ambalaj seviyesi zorunludur.");
        DomainGuard.AgainstBlank(name, "PACKAGING_NAME_REQUIRED", "Ambalaj adı zorunludur.");
        DomainGuard.AgainstBlank(effectiveVersion, "PACKAGING_VERSION_REQUIRED", "Ambalaj effective version zorunludur.");

        if (quantityInBaseUom <= 0)
        {
            throw new DomainException(new(
                "PACKAGING_CONVERSION_INVALID",
                "Ambalaj temel birim katsayısı sıfırdan büyük olmalıdır."));
        }

        return new PackagingSnapshot(
            packagingId,
            level.Trim(),
            name.Trim(),
            baseUomCode,
            quantityInBaseUom,
            allowPartial,
            effectiveVersion.Trim());
    }

    public PositiveQuantity ToBaseQuantity(decimal enteredQuantity, int baseScale)
    {
        if (enteredQuantity <= 0)
        {
            throw new DomainException(new(
                "QUANTITY_MUST_BE_POSITIVE",
                "Girilen miktar sıfırdan büyük olmalıdır."));
        }

        if (!AllowPartial && decimal.Truncate(enteredQuantity) != enteredQuantity)
        {
            throw new DomainException(new(
                "PACKAGING_PARTIAL_NOT_ALLOWED",
                "Bu ambalaj seviyesi parçalı miktarı kabul etmiyor."));
        }

        var baseValue = enteredQuantity * QuantityInBaseUom;
        return PositiveQuantity.Create(baseValue, baseScale);
    }
}

public sealed record QuantitySnapshot(
    decimal EnteredQuantity,
    Guid? EnteredPackagingId,
    PositiveQuantity QuantityBase,
    UomCode BaseUomCode,
    string ViewMode,
    PackagingSnapshot PackagingSnapshot,
    IReadOnlyCollection<PackagingBreakdown> Breakdown);

public sealed record PackagingBreakdown(
    Guid PackagingId,
    decimal EnteredQuantity,
    PositiveQuantity QuantityBase);
