using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FactoryErp.Infrastructure.Persistence.Configurations;

public sealed class UnitOfMeasureRecordConfiguration : IEntityTypeConfiguration<UnitOfMeasureRecord>
{
    public void Configure(EntityTypeBuilder<UnitOfMeasureRecord> builder)
    {
        builder.ToTable("units_of_measure", table =>
        {
            table.HasCheckConstraint("ck_units_of_measure_decimal_scale", "decimal_scale between 0 and 6");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(40).IsRequired();
        builder.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(80).IsRequired();
        builder.Property(x => x.Dimension).HasColumnName("dimension").HasMaxLength(40).IsRequired();
        builder.Property(x => x.DecimalScale).HasColumnName("decimal_scale");
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class ProductCategoryRecordConfiguration : IEntityTypeConfiguration<ProductCategoryRecord>
{
    public void Configure(EntityTypeBuilder<ProductCategoryRecord> builder)
    {
        builder.ToTable("product_categories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(80).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(160).IsRequired();
        builder.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(180).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.Slug).IsUnique();
    }
}

public sealed class ProductRecordConfiguration : IEntityTypeConfiguration<ProductRecord>
{
    public void Configure(EntityTypeBuilder<ProductRecord> builder)
    {
        builder.ToTable("products");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(80).IsRequired();
        builder.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(180).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(240).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text");
        builder.Property(x => x.SizeLabel).HasColumnName("size_label").HasMaxLength(80);
        builder.Property(x => x.BaseUomId).HasColumnName("base_uom_id");
        builder.Property(x => x.CategoryId).HasColumnName("category_id");
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(x => x.IsPublic).HasColumnName("is_public").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasColumnType("bigint").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasDefaultValue(1L);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => new { x.IsActive, x.IsPublic, x.CategoryId });
        builder.HasOne(x => x.BaseUom).WithMany().HasForeignKey(x => x.BaseUomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductPackagingRecordConfiguration : IEntityTypeConfiguration<ProductPackagingRecord>
{
    public void Configure(EntityTypeBuilder<ProductPackagingRecord> builder)
    {
        builder.ToTable("product_packagings", table =>
        {
            table.HasCheckConstraint("ck_product_packagings_quantity_positive", "quantity_in_base_uom > 0");
            table.HasCheckConstraint("ck_product_packagings_units_positive", "units_per_parent > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.Level).HasColumnName("level").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.ParentPackagingId).HasColumnName("parent_packaging_id");
        builder.Property(x => x.UnitsPerParent).HasColumnName("units_per_parent").HasPrecision(18, 6);
        builder.Property(x => x.QuantityInBaseUom).HasColumnName("quantity_in_base_uom").HasPrecision(18, 6);
        builder.Property(x => x.IsSellable).HasColumnName("is_sellable").IsRequired();
        builder.Property(x => x.AllowPartial).HasColumnName("allow_partial").IsRequired();
        builder.Property(x => x.EffectiveFrom).HasColumnName("effective_from").HasColumnType("timestamptz");
        builder.Property(x => x.EffectiveTo).HasColumnName("effective_to").HasColumnType("timestamptz");
        builder.HasIndex(x => new { x.ProductId, x.Level, x.EffectiveFrom }).IsUnique();
        builder.HasIndex(x => new { x.ProductId, x.IsSellable, x.EffectiveTo });
        builder.HasOne(x => x.Product).WithMany(x => x.Packagings).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ParentPackaging).WithMany(x => x.Children).HasForeignKey(x => x.ParentPackagingId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductBarcodeRecordConfiguration : IEntityTypeConfiguration<ProductBarcodeRecord>
{
    public void Configure(EntityTypeBuilder<ProductBarcodeRecord> builder)
    {
        builder.ToTable("product_barcodes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.PackagingId).HasColumnName("packaging_id");
        builder.Property(x => x.Barcode).HasColumnName("barcode").HasMaxLength(80).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        builder.HasIndex(x => x.Barcode).IsUnique().HasFilter("is_active = true");
        builder.HasOne(x => x.Product).WithMany(x => x.Barcodes).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Packaging).WithMany(x => x.Barcodes).HasForeignKey(x => x.PackagingId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductImageRecordConfiguration : IEntityTypeConfiguration<ProductImageRecord>
{
    public void Configure(EntityTypeBuilder<ProductImageRecord> builder)
    {
        builder.ToTable("product_images");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.Url).HasColumnName("url").HasMaxLength(1000).IsRequired();
        builder.Property(x => x.AltText).HasColumnName("alt_text").HasMaxLength(240);
        builder.Property(x => x.SortOrder).HasColumnName("sort_order");
        builder.Property(x => x.IsPrimary).HasColumnName("is_primary").IsRequired();
        builder.HasIndex(x => new { x.ProductId, x.SortOrder });
        builder.HasOne(x => x.Product).WithMany(x => x.Images).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class WarehouseRecordConfiguration : IEntityTypeConfiguration<WarehouseRecord>
{
    public void Configure(EntityTypeBuilder<WarehouseRecord> builder)
    {
        builder.ToTable("warehouses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(80).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(160).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class WarehouseLocationRecordConfiguration : IEntityTypeConfiguration<WarehouseLocationRecord>
{
    public void Configure(EntityTypeBuilder<WarehouseLocationRecord> builder)
    {
        builder.ToTable("warehouse_locations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.WarehouseId).HasColumnName("warehouse_id");
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(80).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(160).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        builder.HasIndex(x => new { x.WarehouseId, x.Code }).IsUnique();
        builder.HasOne(x => x.Warehouse).WithMany(x => x.Locations).HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StockRecordConfiguration : IEntityTypeConfiguration<StockRecord>
{
    public void Configure(EntityTypeBuilder<StockRecord> builder)
    {
        builder.ToTable("stocks", table =>
        {
            table.HasCheckConstraint("ck_stocks_on_hand_non_negative", "on_hand_qty_base >= 0");
            table.HasCheckConstraint("ck_stocks_reserved_non_negative", "reserved_qty_base >= 0");
            table.HasCheckConstraint("ck_stocks_reserved_not_above_on_hand", "reserved_qty_base <= on_hand_qty_base");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.WarehouseId).HasColumnName("warehouse_id");
        builder.Property(x => x.LocationId).HasColumnName("location_id");
        builder.Property(x => x.OnHandQtyBase).HasColumnName("on_hand_qty_base").HasPrecision(18, 6);
        builder.Property(x => x.ReservedQtyBase).HasColumnName("reserved_qty_base").HasPrecision(18, 6);
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasColumnType("bigint").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasDefaultValue(1L);
        builder.HasIndex(x => new { x.ProductId, x.WarehouseId, x.LocationId }).IsUnique();
    }
}

public sealed class StockMovementRecordConfiguration : IEntityTypeConfiguration<StockMovementRecord>
{
    public void Configure(EntityTypeBuilder<StockMovementRecord> builder)
    {
        builder.ToTable("stock_movements", table =>
        {
            table.HasCheckConstraint("ck_stock_movements_quantity_positive", "quantity_base > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.WarehouseId).HasColumnName("warehouse_id");
        builder.Property(x => x.LocationId).HasColumnName("location_id");
        builder.Property(x => x.MovementType).HasColumnName("movement_type").HasMaxLength(60).IsRequired();
        builder.Property(x => x.QuantityBase).HasColumnName("quantity_base").HasPrecision(18, 6);
        builder.Property(x => x.SourceEntityType).HasColumnName("source_entity_type").HasMaxLength(120).IsRequired();
        builder.Property(x => x.SourceEntityId).HasColumnName("source_entity_id");
        builder.Property(x => x.ReversedFromId).HasColumnName("reversed_from_id");
        builder.Property(x => x.PackagingSnapshot).HasColumnName("packaging_snapshot").HasColumnType("jsonb");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.HasIndex(x => new { x.ProductId, x.WarehouseId, x.CreatedAt });
        builder.HasIndex(x => new { x.SourceEntityType, x.SourceEntityId });
    }
}

public sealed class ProductionOrderRecordConfiguration : IEntityTypeConfiguration<ProductionOrderRecord>
{
    public void Configure(EntityTypeBuilder<ProductionOrderRecord> builder)
    {
        builder.ToTable("production_orders", table =>
        {
            table.HasCheckConstraint("ck_production_orders_planned_positive", "planned_qty_base > 0");
            table.HasCheckConstraint("ck_production_orders_completed_valid", "completed_qty_base >= 0 and completed_qty_base <= planned_qty_base");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.WarehouseId).HasColumnName("warehouse_id");
        builder.Property(x => x.PlannedQtyBase).HasColumnName("planned_qty_base").HasPrecision(18, 6);
        builder.Property(x => x.CompletedQtyBase).HasColumnName("completed_qty_base").HasPrecision(18, 6);
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(40).IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasColumnType("bigint").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasDefaultValue(1L);
        builder.HasIndex(x => new { x.Status, x.ProductId });
    }
}

public sealed class ProductionRecordConfiguration : IEntityTypeConfiguration<ProductionRecord>
{
    public void Configure(EntityTypeBuilder<ProductionRecord> builder)
    {
        builder.ToTable("production_records", table =>
        {
            table.HasCheckConstraint("ck_production_records_quantity_positive", "quantity_base > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ProductionOrderId).HasColumnName("production_order_id");
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.WarehouseId).HasColumnName("warehouse_id");
        builder.Property(x => x.LocationId).HasColumnName("location_id");
        builder.Property(x => x.QuantityBase).HasColumnName("quantity_base").HasPrecision(18, 6);
        builder.Property(x => x.EnteredQuantity).HasColumnName("entered_quantity").HasPrecision(18, 6);
        builder.Property(x => x.EnteredPackagingId).HasColumnName("entered_packaging_id");
        builder.Property(x => x.PackagingSnapshot).HasColumnName("packaging_snapshot").HasColumnType("jsonb");
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamptz");
        builder.HasIndex(x => new { x.ProductId, x.CompletedAt });
        builder.HasOne<ProductionOrderRecord>().WithMany().HasForeignKey(x => x.ProductionOrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
