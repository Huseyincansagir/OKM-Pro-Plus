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
}
