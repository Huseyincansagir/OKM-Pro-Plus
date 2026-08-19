using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FactoryErp.Infrastructure.Persistence.Configurations;

public sealed class EmployeeRecordConfiguration : IEntityTypeConfiguration<EmployeeRecord>
{
    public void Configure(EntityTypeBuilder<EmployeeRecord> builder)
    {
        builder.ToTable("employees", table =>
        {
            table.HasCheckConstraint("ck_employees_status", "status in ('Active', 'Inactive')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(40).IsRequired();
        builder.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(160).IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(120);
        builder.Property(x => x.Department).HasColumnName("department").HasMaxLength(120);
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(x => x.HiredOn).HasColumnName("hired_on");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class StockCountRecordConfiguration : IEntityTypeConfiguration<StockCountRecord>
{
    public void Configure(EntityTypeBuilder<StockCountRecord> builder)
    {
        builder.ToTable("stock_counts", table =>
        {
            table.HasCheckConstraint("ck_stock_counts_status", "status in ('Draft', 'Completed')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.DocumentNumber).HasColumnName("document_number").HasMaxLength(40).IsRequired();
        builder.Property(x => x.WarehouseId).HasColumnName("warehouse_id");
        builder.Property(x => x.LocationId).HasColumnName("location_id");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamptz");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.HasIndex(x => x.DocumentNumber).IsUnique();
        builder.HasOne<WarehouseRecord>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WarehouseLocationRecord>().WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserRecord>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StockCountItemRecordConfiguration : IEntityTypeConfiguration<StockCountItemRecord>
{
    public void Configure(EntityTypeBuilder<StockCountItemRecord> builder)
    {
        builder.ToTable("stock_count_items", table =>
        {
            table.HasCheckConstraint("ck_stock_count_items_counted_non_negative", "counted_qty_base >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.StockCountId).HasColumnName("stock_count_id");
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.CountedQtyBase).HasColumnName("counted_qty_base").HasPrecision(18, 6);
        builder.Property(x => x.SystemOnHandQtyBase).HasColumnName("system_on_hand_qty_base").HasPrecision(18, 6);
        builder.Property(x => x.VarianceQtyBase).HasColumnName("variance_qty_base").HasPrecision(18, 6);
        builder.HasIndex(x => new { x.StockCountId, x.ProductId }).IsUnique();
        builder.HasOne(x => x.StockCount).WithMany(x => x.Items).HasForeignKey(x => x.StockCountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductRecord>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
