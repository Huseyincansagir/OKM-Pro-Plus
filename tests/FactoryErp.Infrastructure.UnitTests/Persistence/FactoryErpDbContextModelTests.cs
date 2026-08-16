using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.UnitTests.Persistence;

public sealed class FactoryErpDbContextModelTests
{
    [Fact]
    public void Foundation_entities_use_explicit_snake_case_tables()
    {
        using var context = CreateContext();

        context.Model.FindEntityType(typeof(UserRecord))!.GetTableName().Should().Be("users");
        context.Model.FindEntityType(typeof(RoleRecord))!.GetTableName().Should().Be("roles");
        context.Model.FindEntityType(typeof(AuditLogRecord))!.GetTableName().Should().Be("audit_logs");
        context.Model.FindEntityType(typeof(IdempotencyRecord))!.GetTableName().Should().Be("idempotency_records");
        context.Model.FindEntityType(typeof(OutboxMessageRecord))!.GetTableName().Should().Be("outbox_messages");
    }

    [Fact]
    public void User_row_version_is_an_ef_concurrency_token()
    {
        using var context = CreateContext();
        var property = context.Model.FindEntityType(typeof(UserRecord))!.FindProperty(nameof(UserRecord.RowVersion));

        property.Should().NotBeNull();
        property!.IsConcurrencyToken.Should().BeTrue();
        property.ValueGenerated.Should().Be(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate);
    }

    [Fact]
    public void Idempotency_scope_and_key_are_unique()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(IdempotencyRecord))!;

        entity.GetIndexes()
            .Should()
            .Contain(index => index.IsUnique && index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(IdempotencyRecord.Scope), nameof(IdempotencyRecord.Key) }));
    }

    [Fact]
    public void User_role_and_role_permission_use_composite_keys()
    {
        using var context = CreateContext();

        context.Model.FindEntityType(typeof(UserRoleRecord))!.FindPrimaryKey()!.Properties
            .Select(property => property.Name)
            .Should()
            .Equal(nameof(UserRoleRecord.UserId), nameof(UserRoleRecord.RoleId));

        context.Model.FindEntityType(typeof(RolePermissionRecord))!.FindPrimaryKey()!.Properties
            .Select(property => property.Name)
            .Should()
            .Equal(nameof(RolePermissionRecord.RoleId), nameof(RolePermissionRecord.PermissionId));
    }

    private static FactoryErpDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FactoryErpDbContext>()
            .UseNpgsql("Host=localhost;Database=factory_erp_g1;Username=factory_erp;Password=dev_only_change_me")
            .Options;

        return new FactoryErpDbContext(options);
    }
}
