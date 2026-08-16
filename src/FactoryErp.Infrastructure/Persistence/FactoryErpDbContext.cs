using FactoryErp.Application.Abstractions.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.Persistence;

public sealed class FactoryErpDbContext(DbContextOptions<FactoryErpDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<UserRecord> Users => Set<UserRecord>();
    public DbSet<RoleRecord> Roles => Set<RoleRecord>();
    public DbSet<PermissionRecord> Permissions => Set<PermissionRecord>();
    public DbSet<UserRoleRecord> UserRoles => Set<UserRoleRecord>();
    public DbSet<RolePermissionRecord> RolePermissions => Set<RolePermissionRecord>();
    public DbSet<RefreshTokenRecord> RefreshTokens => Set<RefreshTokenRecord>();
    public DbSet<AuditLogRecord> AuditLogs => Set<AuditLogRecord>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<SystemSettingRecord> SystemSettings => Set<SystemSettingRecord>();
    public DbSet<DocumentSequenceRecord> DocumentSequences => Set<DocumentSequenceRecord>();
    public DbSet<OutboxMessageRecord> OutboxMessages => Set<OutboxMessageRecord>();
    public DbSet<UnitOfMeasureRecord> UnitsOfMeasure => Set<UnitOfMeasureRecord>();
    public DbSet<ProductCategoryRecord> ProductCategories => Set<ProductCategoryRecord>();
    public DbSet<ProductRecord> Products => Set<ProductRecord>();
    public DbSet<ProductPackagingRecord> ProductPackagings => Set<ProductPackagingRecord>();
    public DbSet<ProductBarcodeRecord> ProductBarcodes => Set<ProductBarcodeRecord>();
    public DbSet<ProductImageRecord> ProductImages => Set<ProductImageRecord>();
    public DbSet<WarehouseRecord> Warehouses => Set<WarehouseRecord>();
    public DbSet<WarehouseLocationRecord> WarehouseLocations => Set<WarehouseLocationRecord>();
    public DbSet<StockRecord> Stocks => Set<StockRecord>();
    public DbSet<StockMovementRecord> StockMovements => Set<StockMovementRecord>();
    public DbSet<ProductionOrderRecord> ProductionOrders => Set<ProductionOrderRecord>();
    public DbSet<ProductionRecord> ProductionRecords => Set<ProductionRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FactoryErpDbContext).Assembly);
    }
}
