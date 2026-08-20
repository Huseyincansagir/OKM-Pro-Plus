using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FactoryErp.Api;
using FactoryErp.Infrastructure.Authentication;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FactoryErp.Infrastructure.UnitTests.Sales;

public sealed class QuoteConversionSecurityIntegrationTests
{
    private const string TestPassword = "P003.Security.Test.Password!2026";

    [Fact]
    public async Task Login_user_without_quote_convert_permission_gets_forbidden_on_conversion_endpoint()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var userName = $"p003-quote-read-{suffix}";
        var roleCode = $"p003_quote_read_{suffix}";
        var connectionString = GetConnectionString();
        var previousConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__FactoryErp");
        Environment.SetEnvironmentVariable("ConnectionStrings__FactoryErp", connectionString);

        await SeedReadOnlyUserAsync(userId, userName, roleId, roleCode);
        try
        {
            await using var factory = new ApiFactory(connectionString);
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            using var loginResponse = await client.PostAsJsonAsync(
                "/api/v1/auth/login",
                new { userName, password = TestPassword });
            loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var loginJson = JsonSerializer.Deserialize<JsonElement>(await loginResponse.Content.ReadAsStringAsync());
            var accessToken = loginJson.GetProperty("accessToken").GetString();
            accessToken.Should().NotBeNullOrWhiteSpace();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.Add("Idempotency-Key", $"p003-security-{Guid.NewGuid():N}");
            using var conversionResponse = await client.PostAsJsonAsync(
                $"/api/v1/quotes/{Guid.NewGuid()}/convert",
                new { });
            conversionResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
        finally
        {
            await CleanupAsync(userId, roleId, roleCode);
            Environment.SetEnvironmentVariable("ConnectionStrings__FactoryErp", previousConnectionString);
        }
    }

    private static async Task SeedReadOnlyUserAsync(Guid userId, string userName, Guid roleId, string roleCode)
    {
        await using var context = CreateContext();
        var readPermission = await context.Permissions.SingleAsync(x => x.Code == "quote.read");
        var now = DateTimeOffset.UtcNow;
        var hasher = new PasswordHasher();
        context.Users.Add(new UserRecord
        {
            Id = userId,
            UserName = userName,
            Email = $"{userName}@test.local",
            DisplayName = userName,
            PasswordHash = hasher.Hash(TestPassword),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            RowVersion = 1,
        });
        context.Roles.Add(new RoleRecord
        {
            Id = roleId,
            Code = roleCode,
            Name = "P-003 Quote Read Test",
            IsSystemRole = false,
            IsActive = true,
        });
        context.UserRoles.Add(new UserRoleRecord { UserId = userId, RoleId = roleId, AssignedAt = now });
        context.RolePermissions.Add(new RolePermissionRecord
        {
            RoleId = roleId,
            PermissionId = readPermission.Id,
            AssignedAt = now,
        });
        await context.SaveChangesAsync();
    }

    private static async Task CleanupAsync(Guid userId, Guid roleId, string roleCode)
    {
        await using var context = CreateContext();
        await context.RefreshTokens.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        await context.UserRoles.Where(x => x.UserId == userId || x.RoleId == roleId).ExecuteDeleteAsync();
        await context.RolePermissions.Where(x => x.RoleId == roleId).ExecuteDeleteAsync();
        await context.Users.Where(x => x.Id == userId).ExecuteDeleteAsync();
        await context.Roles.Where(x => x.Id == roleId && x.Code == roleCode).ExecuteDeleteAsync();
    }

    private static FactoryErpDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FactoryErpDbContext>()
            .UseNpgsql(GetConnectionString())
            .Options;
        return new FactoryErpDbContext(options);
    }

    private static string GetConnectionString()
        => Environment.GetEnvironmentVariable("FactoryErpTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__FactoryErp")
            ?? "Host=127.0.0.1;Port=5432;Database=factory_erp_g1;Username=factory_erp;Password=dev_only_change_me";

    private sealed class ApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:FactoryErp"] = connectionString,
                ["Authentication:SigningKey"] = "development-only-signing-key-change-before-production-2026",
                ["Authentication:Issuer"] = "factory-erp",
                ["Authentication:Audience"] = "factory-erp-clients",
            }));
        }
    }
}
