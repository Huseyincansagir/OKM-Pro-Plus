namespace FactoryErp.Infrastructure.Persistence.Entities;

public sealed class UnitOfMeasureRecord
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Dimension { get; set; } = string.Empty;
    public int DecimalScale { get; set; }
    public bool IsActive { get; set; }
}

public sealed class ProductCategoryRecord
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public ICollection<ProductRecord> Products { get; } = new List<ProductRecord>();
}

public sealed class ProductRecord
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SizeLabel { get; set; }
    public Guid BaseUomId { get; set; }
    public Guid? CategoryId { get; set; }
    public bool IsActive { get; set; }
    public bool IsPublic { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long RowVersion { get; set; }

    public UnitOfMeasureRecord BaseUom { get; set; } = null!;
    public ProductCategoryRecord? Category { get; set; }
    public ICollection<ProductPackagingRecord> Packagings { get; } = new List<ProductPackagingRecord>();
    public ICollection<ProductBarcodeRecord> Barcodes { get; } = new List<ProductBarcodeRecord>();
    public ICollection<ProductImageRecord> Images { get; } = new List<ProductImageRecord>();
}

public sealed class ProductPackagingRecord
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? ParentPackagingId { get; set; }
    public decimal UnitsPerParent { get; set; }
    public decimal QuantityInBaseUom { get; set; }
    public bool IsSellable { get; set; }
    public bool AllowPartial { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }

    public ProductRecord Product { get; set; } = null!;
    public ProductPackagingRecord? ParentPackaging { get; set; }
    public ICollection<ProductPackagingRecord> Children { get; } = new List<ProductPackagingRecord>();
    public ICollection<ProductBarcodeRecord> Barcodes { get; } = new List<ProductBarcodeRecord>();
}

public sealed class ProductBarcodeRecord
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid? PackagingId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public ProductRecord Product { get; set; } = null!;
    public ProductPackagingRecord? Packaging { get; set; }
}

public sealed class ProductImageRecord
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }

    public ProductRecord Product { get; set; } = null!;
}

public sealed class WarehouseRecord
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public ICollection<WarehouseLocationRecord> Locations { get; } = new List<WarehouseLocationRecord>();
}

public sealed class WarehouseLocationRecord
{
    public Guid Id { get; set; }
    public Guid WarehouseId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public WarehouseRecord Warehouse { get; set; } = null!;
}

public sealed class StockRecord
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid LocationId { get; set; }
    public decimal OnHandQtyBase { get; set; }
    public decimal ReservedQtyBase { get; set; }
    public long RowVersion { get; set; }
}

public sealed class StockMovementRecord
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid LocationId { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public decimal QuantityBase { get; set; }
    public string SourceEntityType { get; set; } = string.Empty;
    public Guid? SourceEntityId { get; set; }
    public Guid? ReversedFromId { get; set; }
    public string? PackagingSnapshot { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class ProductionOrderRecord
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal PlannedQtyBase { get; set; }
    public decimal CompletedQtyBase { get; set; }
    public string Status { get; set; } = "Planned";
    public long RowVersion { get; set; }
}

public sealed class ProductionRecord
{
    public Guid Id { get; set; }
    public Guid? ProductionOrderId { get; set; }
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid LocationId { get; set; }
    public decimal QuantityBase { get; set; }
    public decimal EnteredQuantity { get; set; }
    public Guid? EnteredPackagingId { get; set; }
    public string? PackagingSnapshot { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
}
