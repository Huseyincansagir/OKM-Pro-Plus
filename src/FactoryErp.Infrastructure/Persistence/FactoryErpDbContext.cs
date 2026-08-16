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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FactoryErpDbContext).Assembly);
    }
}
