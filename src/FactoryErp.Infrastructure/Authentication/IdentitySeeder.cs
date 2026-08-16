using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FactoryErp.Infrastructure.Authentication;

public sealed class IdentitySeeder(
    FactoryErpDbContext dbContext,
    PasswordHasher passwordHasher)
{
    public async Task SeedBootstrapAdminAsync(IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        await EnsureSystemAdminPermissionsAsync(cancellationToken);

        if (await dbContext.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var username = configuration["BOOTSTRAP_ADMIN_USERNAME"];
        var password = configuration["BOOTSTRAP_ADMIN_PASSWORD"];
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var systemAdmin = await dbContext.Roles.SingleAsync(x => x.Code == "system_admin", cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var user = new UserRecord
        {
            Id = Guid.NewGuid(),
            UserName = username.Trim(),
            DisplayName = "Factory ERP Administrator",
            PasswordHash = passwordHasher.Hash(password),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            RowVersion = 1,
        };

        dbContext.Users.Add(user);
        dbContext.UserRoles.Add(new UserRoleRecord
        {
            UserId = user.Id,
            RoleId = systemAdmin.Id,
            AssignedAt = now,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureSystemAdminPermissionsAsync(CancellationToken cancellationToken)
    {
        var systemAdmin = await dbContext.Roles.SingleAsync(x => x.Code == "system_admin", cancellationToken);
        var definitions = new[]
        {
            ("10000000-0000-0000-0000-000000000007", "order.create", "sales", "create"),
            ("10000000-0000-0000-0000-000000000008", "order.read", "sales", "read"),
            ("10000000-0000-0000-0000-000000000009", "order.submit", "sales", "submit"),
            ("10000000-0000-0000-0000-000000000010", "order.approve", "sales", "approve"),
            ("10000000-0000-0000-0000-000000000011", "order.reject", "sales", "reject"),
            ("10000000-0000-0000-0000-000000000012", "quote-request.submit", "sales", "submit"),
            ("10000000-0000-0000-0000-000000000013", "quote-request.read", "sales", "read"),
            ("10000000-0000-0000-0000-000000000014", "quote-request.review", "sales", "review"),
            ("10000000-0000-0000-0000-000000000015", "customer.create", "sales", "create"),
            ("10000000-0000-0000-0000-000000000016", "customer.read", "sales", "read"),
        };

        foreach (var definition in definitions)
        {
            var permission = await dbContext.Permissions.SingleOrDefaultAsync(x => x.Code == definition.Item2, cancellationToken);
            if (permission is null)
            {
                permission = new PermissionRecord
                {
                    Id = Guid.Parse(definition.Item1),
                    Code = definition.Item2,
                    Module = definition.Item3,
                    Action = definition.Item4,
                    IsActive = true,
                };
                dbContext.Permissions.Add(permission);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            var exists = await dbContext.RolePermissions.AnyAsync(
                x => x.RoleId == systemAdmin.Id && x.PermissionId == permission.Id,
                cancellationToken);
            if (!exists)
            {
                dbContext.RolePermissions.Add(new RolePermissionRecord
                {
                    RoleId = systemAdmin.Id,
                    PermissionId = permission.Id,
                    AssignedAt = DateTimeOffset.UtcNow,
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
