using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FactoryErp.Infrastructure.Authentication;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FactoryErp.Infrastructure.UnitTests.Production;

public sealed class ProductionSecurityIntegrationTests
{
    private const string TestPassword = "G6.Security.Test.Password!2026";

    [Fact]
    public async Task Login_issues_production_permissions_and_api_enforces_read_only_boundary()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var fullUserName = $"g6-full-{suffix}";
        var readUserName = $"g6-read-{suffix}";
        var readRoleCode = $"g6_read_only_{suffix}";
        var fullUserId = Guid.NewGuid();
        var readUserId = Guid.NewGuid();
        var readRoleId = Guid.NewGuid();
        var productionOrderId = (Guid?)null;
        var idempotencyKey = $"g6-security-create-{suffix}";
        var connectionString = GetConnectionString();
        var previousConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__FactoryErp");
        Environment.SetEnvironmentVariable("ConnectionStrings__FactoryErp", connectionString);

        await SeedSecurityFixtureAsync(
            fullUserId,
            fullUserName,
            readUserId,
            readUserName,
            readRoleId,
            readRoleCode);

        await using var factory = new ApiFactory(connectionString);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        try
        {
            using var anonymousGet = await client.GetAsync($"/api/v1/production/orders/{Guid.NewGuid()}");
            anonymousGet.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var fullToken = await LoginAsync(client, fullUserName);
            fullToken.Permissions.Should().Contain(new[]
            {
                "production.create",
                "production.read",
                "production.start",
                "production.record",
                "production.complete",
            });

            using var fullClient = CreateAuthenticatedClient(factory, fullToken.AccessToken);
            using var createResponse = await fullClient.PostAsJsonAsync(
                "/api/v1/production/orders",
                new
                {
                    productId = "30000000-0000-0000-0000-000000000201",
                    warehouseId = "30000000-0000-0000-0000-000000000301",
                    plannedQuantityBase = 2_000m,
                },
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                new CancellationToken());
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await ReadJsonAsync(createResponse);
            productionOrderId = created.GetProperty("id").GetGuid();

            using var readResponse = await fullClient.GetAsync($"/api/v1/production/orders/{productionOrderId}");
            readResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var readToken = await LoginAsync(client, readUserName);
            readToken.Permissions.Should().Contain("production.read");
            readToken.Permissions.Should().NotContain("production.create");
            using var readClient = CreateAuthenticatedClient(factory, readToken.AccessToken);

            using var readOnlyGet = await readClient.GetAsync($"/api/v1/production/orders/{productionOrderId}");
            readOnlyGet.StatusCode.Should().Be(HttpStatusCode.OK);

            using var forbiddenCreate = await readClient.PostAsJsonAsync(
                "/api/v1/production/orders",
                new
                {
                    productId = "30000000-0000-0000-0000-000000000201",
                    warehouseId = "30000000-0000-0000-0000-000000000301",
                    plannedQuantityBase = 2_000m,
                },
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                new CancellationToken());
            forbiddenCreate.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
        finally
        {
            await CleanupSecurityFixtureAsync(
                fullUserId,
                readUserId,
                readRoleId,
                readRoleCode,
                productionOrderId,
                idempotencyKey);
            Environment.SetEnvironmentVariable("ConnectionStrings__FactoryErp", previousConnectionString);
        }
    }

    private static async Task<AuthResponse> LoginAsync(HttpClient client, string userName)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { userName, password = TestPassword },
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            new CancellationToken());
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ReadJsonAsync(response);
        var accessToken = json.GetProperty("accessToken").GetString();
        var refreshToken = json.GetProperty("refreshToken").GetString();
        accessToken.Should().NotBeNullOrWhiteSpace();
        refreshToken.Should().NotBeNullOrWhiteSpace();
        var permissions = json
            .GetProperty("user")
            .GetProperty("permissions")
            .EnumerateArray()
            .Select(x => x.GetString()!)
            .ToArray();
        return new AuthResponse(accessToken!, refreshToken!, permissions);
    }

    private static HttpClient CreateAuthenticatedClient(WebApplicationFactory<Program> factory, string accessToken)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
        client.DefaultRequestHeaders.Add("X-Correlation-Id", $"g6-security-{Guid.NewGuid():N}");
        client.DefaultRequestHeaders.Add("Idempotency-Key", $"g6-security-{Guid.NewGuid():N}");
        return client;
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(body);
    }

    private static async Task SeedSecurityFixtureAsync(
        Guid fullUserId,
        string fullUserName,
        Guid readUserId,
        string readUserName,
        Guid readRoleId,
        string readRoleCode)
    {
        await using var context = CreateContext();
        var systemAdmin = await context.Roles.SingleAsync(x => x.Code == "system_admin");
        var readPermission = await context.Permissions.SingleAsync(x => x.Code == "production.read");
        var now = DateTimeOffset.UtcNow;
        var hasher = new PasswordHasher();

        context.Users.AddRange(
            new UserRecord
            {
                Id = fullUserId,
                UserName = fullUserName,
                Email = $"{fullUserName}@test.local",
                DisplayName = "G6 Full Security Test",
                PasswordHash = hasher.Hash(TestPassword),
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
                RowVersion = 1,
            },
            new UserRecord
            {
                Id = readUserId,
                UserName = readUserName,
                Email = $"{readUserName}@test.local",
                DisplayName = "G6 Read Security Test",
                PasswordHash = hasher.Hash(TestPassword),
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
                RowVersion = 1,
            });
        context.Roles.Add(new RoleRecord
        {
            Id = readRoleId,
            Code = readRoleCode,
            Name = "G6 Production Read Test",
            IsSystemRole = false,
            IsActive = true,
        });
        context.UserRoles.AddRange(
            new UserRoleRecord
            {
                UserId = fullUserId,
                RoleId = systemAdmin.Id,
                AssignedAt = now,
            },
            new UserRoleRecord
            {
                UserId = readUserId,
                RoleId = readRoleId,
                AssignedAt = now,
            });
        context.RolePermissions.Add(new RolePermissionRecord
        {
            RoleId = readRoleId,
            PermissionId = readPermission.Id,
            AssignedAt = now,
        });
        await context.SaveChangesAsync();
    }

    private static async Task CleanupSecurityFixtureAsync(
        Guid fullUserId,
        Guid readUserId,
        Guid readRoleId,
        string readRoleCode,
        Guid? productionOrderId,
        string idempotencyKey)
    {
        await using var context = CreateContext();
        if (productionOrderId.HasValue)
        {
            await context.AuditLogs
                .Where(x => x.EntityId == productionOrderId.Value)
                .ExecuteDeleteAsync();
            await context.IdempotencyRecords
                .Where(x => x.Key == idempotencyKey)
                .ExecuteDeleteAsync();
            await context.ProductionOrders
                .Where(x => x.Id == productionOrderId.Value)
                .ExecuteDeleteAsync();
        }

        await context.RefreshTokens
            .Where(x => x.UserId == fullUserId || x.UserId == readUserId)
            .ExecuteDeleteAsync();
        await context.UserRoles
            .Where(x => x.UserId == fullUserId || x.UserId == readUserId)
            .ExecuteDeleteAsync();
        await context.RolePermissions
            .Where(x => x.RoleId == readRoleId)
            .ExecuteDeleteAsync();
        await context.Users
            .Where(x => x.Id == fullUserId || x.Id == readUserId)
            .ExecuteDeleteAsync();
        await context.Roles
            .Where(x => x.Id == readRoleId && x.Code == readRoleCode)
            .ExecuteDeleteAsync();
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

    private sealed record AuthResponse(string AccessToken, string RefreshToken, IReadOnlyCollection<string> Permissions);

    private sealed class ApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:FactoryErp"] = connectionString,
                    ["Authentication:SigningKey"] = "development-only-signing-key-change-before-production-2026",
                    ["Authentication:Issuer"] = "factory-erp",
                    ["Authentication:Audience"] = "factory-erp-clients",
                });
            });
        }
    }
}
