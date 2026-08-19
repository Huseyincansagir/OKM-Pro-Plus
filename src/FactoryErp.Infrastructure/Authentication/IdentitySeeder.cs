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
            ("10000000-0000-0000-0000-000000000062", "quote.read", "sales", "read"),
            ("10000000-0000-0000-0000-000000000063", "quote.create", "sales", "create"),
            ("10000000-0000-0000-0000-000000000064", "quote.issue", "sales", "issue"),
            ("10000000-0000-0000-0000-000000000065", "price.read", "sales", "read"),
            ("10000000-0000-0000-0000-000000000066", "price.manage", "sales", "manage"),
            ("10000000-0000-0000-0000-000000000067", "price.resolve", "sales", "resolve"),
            ("10000000-0000-0000-0000-000000000068", "customer.update", "sales", "update"),
            ("10000000-0000-0000-0000-000000000069", "customer.message", "sales", "message"),
            ("10000000-0000-0000-0000-000000000070", "product.read", "products", "read"),
            ("10000000-0000-0000-0000-000000000071", "stock.read", "warehouse", "read"),
            ("10000000-0000-0000-0000-000000000017", "delivery-note.create", "shipping", "create"),
            ("10000000-0000-0000-0000-000000000018", "delivery-note.read", "shipping", "read"),
            ("10000000-0000-0000-0000-000000000019", "delivery-note.issue", "shipping", "issue"),
            ("10000000-0000-0000-0000-000000000020", "invoice.create", "finance", "create"),
            ("10000000-0000-0000-0000-000000000021", "invoice.read", "finance", "read"),
            ("10000000-0000-0000-0000-000000000022", "invoice.issue", "finance", "issue"),
            ("10000000-0000-0000-0000-000000000023", "payment.apply", "finance", "apply"),
            ("10000000-0000-0000-0000-000000000024", "current-account.read", "finance", "read"),
            ("10000000-0000-0000-0000-000000000025", "production.create", "production", "create"),
            ("10000000-0000-0000-0000-000000000026", "production.read", "production", "read"),
            ("10000000-0000-0000-0000-000000000027", "production.start", "production", "start"),
            ("10000000-0000-0000-0000-000000000028", "production.record", "production", "record"),
            ("10000000-0000-0000-0000-000000000029", "production.complete", "production", "complete"),
            ("10000000-0000-0000-0000-000000000030", "stock-transfer.create", "warehouse", "create"),
            ("10000000-0000-0000-0000-000000000031", "stock-transfer.read", "warehouse", "read"),
            ("10000000-0000-0000-0000-000000000032", "stock-transfer.complete", "warehouse", "complete"),
            ("10000000-0000-0000-0000-000000000033", "stock-transfer.cancel", "warehouse", "cancel"),
            ("10000000-0000-0000-0000-000000000034", "vehicle-type.read", "shipping", "read"),
            ("10000000-0000-0000-0000-000000000035", "vehicle-type.manage", "shipping", "manage"),
            ("10000000-0000-0000-0000-000000000036", "vehicle.read", "shipping", "read"),
            ("10000000-0000-0000-0000-000000000037", "vehicle.manage", "shipping", "manage"),
            ("10000000-0000-0000-0000-000000000038", "vehicle.status-update", "shipping", "status-update"),
            ("10000000-0000-0000-0000-000000000039", "driver.read", "shipping", "read"),
            ("10000000-0000-0000-0000-000000000040", "driver.manage", "shipping", "manage"),
            ("10000000-0000-0000-0000-000000000041", "shipment.create", "shipping", "create"),
            ("10000000-0000-0000-0000-000000000042", "shipment.read", "shipping", "read"),
            ("10000000-0000-0000-0000-000000000043", "shipment.route-manage", "shipping", "route-manage"),
            ("10000000-0000-0000-0000-000000000044", "shipment.route-lock", "shipping", "route-lock"),
            ("10000000-0000-0000-0000-000000000045", "shipment.plan-replan", "shipping", "plan-replan"),
            ("10000000-0000-0000-0000-000000000046", "physical-profile.read", "shipping", "read"),
            ("10000000-0000-0000-0000-000000000047", "physical-profile.manage", "shipping", "manage"),
            ("10000000-0000-0000-0000-000000000048", "pallet-type.read", "shipping", "read"),
            ("10000000-0000-0000-0000-000000000049", "pallet-type.manage", "shipping", "manage"),
            ("10000000-0000-0000-0000-000000000050", "shipment.package-read", "shipping", "read"),
            ("10000000-0000-0000-0000-000000000051", "shipment.package-manage", "shipping", "manage"),
            ("10000000-0000-0000-0000-000000000052", "shipment.load-plan", "shipping", "manage"),
            ("10000000-0000-0000-0000-000000000053", "shipment.vehicle-fit", "shipping", "evaluate"),
            ("10000000-0000-0000-0000-000000000054", "shipment.plan-lock", "shipping", "lock"),
            ("10000000-0000-0000-0000-000000000055", "shipment.plan-override", "shipping", "override"),
            ("10000000-0000-0000-0000-000000000056", "shipment.load-verify", "shipping", "load-verify"),
            ("10000000-0000-0000-0000-000000000057", "shipment.load-verify-override", "shipping", "load-verify-override"),
            ("10000000-0000-0000-0000-000000000058", "shipment.dispatch", "shipping", "dispatch"),
            ("10000000-0000-0000-0000-000000000059", "shipment.depart", "shipping", "depart"),
            ("10000000-0000-0000-0000-000000000060", "shipment.route-execute", "shipping", "route-execute"),
            ("10000000-0000-0000-0000-000000000061", "shipment.route-exception", "shipping", "route-exception"),
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
