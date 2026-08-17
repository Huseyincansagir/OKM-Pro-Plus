using System.Net;
using System.Net.Http.Headers;
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

namespace FactoryErp.Infrastructure.UnitTests.Shipping;

public sealed class LogisticsSecurityIntegrationTests
{
    private const string TestPassword = "G6.Security.Test.Password!2026";
    private static readonly Guid DeliveryNoteId = Guid.Parse("eade528b-1cc3-42c9-8009-93066dac675f");
    private static readonly Guid ProductId = Guid.Parse("30000000-0000-0000-0000-000000000201");

    [Fact]
    public async Task Login_enforces_vehicle_driver_shipment_and_route_permission_boundary()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var fullUserId = Guid.NewGuid();
        var readUserId = Guid.NewGuid();
        var readRoleId = Guid.NewGuid();
        var fullUserName = $"g62-logistics-full-{suffix}";
        var readUserName = $"g62-logistics-read-{suffix}";
        var readRoleCode = $"g62_logistics_read_{suffix}";
        Guid? vehicleTypeId = null;
        Guid? vehicleId = null;
        Guid? driverId = null;
        Guid? shipmentId = null;
        Guid? routePlanId = null;
        Guid? physicalProfileId = null;
        Guid? palletTypeId = null;
        var connectionString = GetConnectionString();
        var previousConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__FactoryErp");
        Environment.SetEnvironmentVariable("ConnectionStrings__FactoryErp", connectionString);

        await DeleteExistingShipmentAsync();
        await SeedUsersAsync(fullUserId, fullUserName, readUserId, readUserName, readRoleId, readRoleCode);

        await using var factory = new ApiFactory(connectionString);
        using var anonymousClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        try
        {
            using var anonymousResponse = await anonymousClient.GetAsync($"/api/v1/vehicles/{Guid.NewGuid()}");
            anonymousResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var fullToken = await LoginAsync(anonymousClient, fullUserName);
            fullToken.Permissions.Should().Contain(new[]
            {
                "vehicle-type.manage",
                "vehicle.manage",
                "driver.manage",
                "shipment.create",
                "shipment.route-manage",
                "shipment.route-lock",
                "shipment.plan-replan",
                "physical-profile.manage",
                "pallet-type.manage",
                "shipment.package-read",
                "shipment.package-manage",
            });
            using var fullClient = CreateAuthenticatedClient(factory, fullToken.AccessToken, suffix);

            using var typeResponse = await fullClient.PostAsJsonAsync(
                "/api/v1/vehicle-types",
                new { code = $"SEC-{suffix[..8]}", name = "Security Test Vehicle Type" });
            typeResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            vehicleTypeId = (await ReadJsonAsync(typeResponse)).GetProperty("id").GetGuid();

            using var vehicleResponse = await fullClient.PostAsJsonAsync(
                "/api/v1/vehicles",
                new { vehicleTypeId, plateNumber = $"99 SEC {suffix[..3].ToUpperInvariant()}", maintenanceUntil = (DateTimeOffset?)null, lastKnownLocationText = "Security test" });
            vehicleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var vehicleJson = await ReadJsonAsync(vehicleResponse);
            vehicleId = vehicleJson.GetProperty("id").GetGuid();

            using var driverResponse = await fullClient.PostAsJsonAsync(
                "/api/v1/drivers",
                new
                {
                    employeeId = (Guid?)null,
                    fullName = "Security Test Driver",
                    phone = (string?)null,
                    licenseNumber = $"SEC-{suffix}",
                    licenseExpiry = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
                });
            driverResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            driverId = (await ReadJsonAsync(driverResponse)).GetProperty("id").GetGuid();

            using var shipmentResponse = await fullClient.PostAsJsonAsync(
                "/api/v1/shipments",
                new { deliveryNoteId = DeliveryNoteId, expectedDeliveryNoteRowVersion = 1L });
            shipmentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var shipmentJson = await ReadJsonAsync(shipmentResponse);
            shipmentId = shipmentJson.GetProperty("id").GetGuid();
            var shipmentRowVersion = shipmentJson.GetProperty("rowVersion").GetInt64();

            var start = DateTimeOffset.UtcNow.AddDays(2);
            using var routeResponse = await fullClient.PostAsJsonAsync(
                $"/api/v1/shipments/{shipmentId}/route-plans",
                new
                {
                    plannedStartAt = start,
                    plannedEndAt = start.AddHours(2),
                    expectedShipmentRowVersion = shipmentRowVersion,
                });
            routeResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var routeJson = await ReadJsonAsync(routeResponse);
            routePlanId = routeJson.GetProperty("id").GetGuid();

            using var physicalProfileResponse = await fullClient.PostAsJsonAsync(
                $"/api/v1/physical-logistics/products/{ProductId}/profiles",
                new
                {
                    productId = ProductId,
                    effectiveFrom = DateTimeOffset.UtcNow,
                    effectiveTo = DateTimeOffset.UtcNow.AddDays(1),
                    lengthMm = 600m,
                    widthMm = 400m,
                    heightMm = 300m,
                    netWeightKg = 12m,
                    volumeM3 = 0.072m,
                    isStackable = true,
                    maxStackCount = 5,
                    maxLoadAboveKg = 200m,
                    keepUpright = false,
                    isFragile = false,
                    compatibilityGroup = "SEC",
                    incompatibleGroups = "[]",
                    allowedOrientations = "[\"LWH\"]",
                    physicalPolicySnapshot = "{\"source\":\"security\"}",
                });
            physicalProfileResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            physicalProfileId = (await ReadJsonAsync(physicalProfileResponse)).GetProperty("id").GetGuid();

            using var packageResponse = await fullClient.PostAsJsonAsync(
                $"/api/v1/shipments/{shipmentId}/packages",
                new
                {
                    shipmentItemId = shipmentJson.GetProperty("items")[0].GetProperty("id").GetGuid(),
                    packagingId = Guid.Parse("30000000-0000-0000-0000-000000000213"),
                    routeStopId = (Guid?)null,
                    packageType = "Case",
                    packageCount = 2m,
                    quantityBasePerPackage = 100m,
                    enteredQuantity = 2m,
                    packageCode = $"SEC-PKG-{suffix[..8]}",
                    splitAllowed = false,
                });
            packageResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            using var palletTypeResponse = await fullClient.PostAsJsonAsync(
                "/api/v1/physical-logistics/pallet-types",
                new
                {
                    code = $"SEC-PAL-{suffix[..8]}",
                    name = "Security Test Pallet",
                    lengthMm = 1200m,
                    widthMm = 800m,
                    heightMm = 150m,
                    tareWeightKg = 25m,
                    maxGrossWeightKg = 1025m,
                    maxPayloadKg = 1000m,
                    maxLoadHeightMm = 1800m,
                    maxStackCount = 1,
                    isStackable = false,
                    policySnapshot = "{\"source\":\"security\"}",
                });
            palletTypeResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            palletTypeId = (await ReadJsonAsync(palletTypeResponse)).GetProperty("id").GetGuid();

            var readToken = await LoginAsync(anonymousClient, readUserName);
            readToken.Permissions.Should().Contain("vehicle.read");
            readToken.Permissions.Should().Contain("shipment.read");
            readToken.Permissions.Should().Contain("physical-profile.read");
            readToken.Permissions.Should().Contain("pallet-type.read");
            readToken.Permissions.Should().Contain("shipment.package-read");
            readToken.Permissions.Should().NotContain("shipment.package-manage");
            readToken.Permissions.Should().NotContain("vehicle.manage");
            using var readClient = CreateAuthenticatedClient(factory, readToken.AccessToken, suffix);

            using var readVehicle = await readClient.GetAsync($"/api/v1/vehicles/{vehicleId}");
            readVehicle.StatusCode.Should().Be(HttpStatusCode.OK);
            using var forbiddenVehicleCreate = await readClient.PostAsJsonAsync(
                "/api/v1/vehicles",
                new { vehicleTypeId, plateNumber = "99 READ 001", maintenanceUntil = (DateTimeOffset?)null, lastKnownLocationText = "" });
            forbiddenVehicleCreate.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            using var readShipment = await readClient.GetAsync($"/api/v1/shipments/{shipmentId}");
            readShipment.StatusCode.Should().Be(HttpStatusCode.OK);
            using var forbiddenRouteCreate = await readClient.PostAsJsonAsync(
                $"/api/v1/shipments/{shipmentId}/route-plans",
                new { plannedStartAt = start, plannedEndAt = start.AddHours(1), expectedShipmentRowVersion = shipmentRowVersion });
            forbiddenRouteCreate.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            using var readPackages = await readClient.GetAsync($"/api/v1/shipments/{shipmentId}/packages");
            readPackages.StatusCode.Should().Be(HttpStatusCode.OK);
            using var forbiddenPackageCreate = await readClient.PostAsJsonAsync(
                $"/api/v1/shipments/{shipmentId}/packages",
                new
                {
                    shipmentItemId = shipmentJson.GetProperty("items")[0].GetProperty("id").GetGuid(),
                    packagingId = Guid.Parse("30000000-0000-0000-0000-000000000213"),
                    routeStopId = (Guid?)null,
                    packageType = "Case",
                    packageCount = 1m,
                    quantityBasePerPackage = 100m,
                    enteredQuantity = 1m,
                    packageCode = $"READ-PKG-{suffix[..8]}",
                    splitAllowed = false,
                });
            forbiddenPackageCreate.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            using var readPhysicalProfile = await readClient.GetAsync($"/api/v1/physical-logistics/products/{ProductId}/profile");
            readPhysicalProfile.StatusCode.Should().Be(HttpStatusCode.OK);
            using var forbiddenPalletCreate = await readClient.PostAsJsonAsync(
                "/api/v1/physical-logistics/pallet-types",
                new { code = $"READ-PAL-{suffix[..8]}", name = "Read Only Pallet", lengthMm = 1200m, widthMm = 800m, heightMm = 150m, tareWeightKg = 25m, maxGrossWeightKg = 1025m, maxPayloadKg = 1000m, maxLoadHeightMm = 1800m, maxStackCount = 1, isStackable = false, policySnapshot = "{}" });
            forbiddenPalletCreate.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
        finally
        {
            await CleanupAsync(fullUserId, readUserId, readRoleId, readRoleCode, vehicleTypeId, vehicleId, driverId, shipmentId, routePlanId, physicalProfileId, palletTypeId);
            Environment.SetEnvironmentVariable("ConnectionStrings__FactoryErp", previousConnectionString);
        }
    }

    private static async Task<AuthResponse> LoginAsync(HttpClient client, string userName)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { userName, password = TestPassword });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ReadJsonAsync(response);
        return new AuthResponse(
            json.GetProperty("accessToken").GetString()!,
            json.GetProperty("user").GetProperty("permissions").EnumerateArray().Select(x => x.GetString()!).ToArray());
    }

    private static HttpClient CreateAuthenticatedClient(WebApplicationFactory<Program> factory, string token, string suffix)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Correlation-Id", $"g62-security-{suffix}");
        client.DefaultRequestHeaders.Add("Idempotency-Key", $"g62-security-{suffix}-{Guid.NewGuid():N}");
        return client;
    }

    private static async Task SeedUsersAsync(
        Guid fullUserId,
        string fullUserName,
        Guid readUserId,
        string readUserName,
        Guid readRoleId,
        string readRoleCode)
    {
        await using var context = CreateContext();
        var systemAdmin = await context.Roles.SingleAsync(x => x.Code == "system_admin");
                    var readPermissions = await context.Permissions
            .Where(x => x.Code == "vehicle.read" || x.Code == "shipment.read" || x.Code == "physical-profile.read" || x.Code == "pallet-type.read" || x.Code == "shipment.package-read")

            .ToArrayAsync();
        var now = DateTimeOffset.UtcNow;
        var hasher = new PasswordHasher();
        context.Users.AddRange(
            NewUser(fullUserId, fullUserName, hasher, now),
            NewUser(readUserId, readUserName, hasher, now));
        context.Roles.Add(new RoleRecord
        {
            Id = readRoleId,
            Code = readRoleCode,
            Name = "G6.2 Logistics Read Test",
            IsSystemRole = false,
            IsActive = true,
        });
        context.UserRoles.AddRange(
            new UserRoleRecord { UserId = fullUserId, RoleId = systemAdmin.Id, AssignedAt = now },
            new UserRoleRecord { UserId = readUserId, RoleId = readRoleId, AssignedAt = now });
        context.RolePermissions.AddRange(readPermissions.Select(x => new RolePermissionRecord
        {
            RoleId = readRoleId,
            PermissionId = x.Id,
            AssignedAt = now,
        }));
        await context.SaveChangesAsync();
    }

    private static UserRecord NewUser(Guid id, string userName, PasswordHasher hasher, DateTimeOffset now)
        => new()
        {
            Id = id,
            UserName = userName,
            Email = $"{userName}@test.local",
            DisplayName = userName,
            PasswordHash = hasher.Hash(TestPassword),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            RowVersion = 1,
        };

    private static async Task DeleteExistingShipmentAsync()
    {
        await using var context = CreateContext();
        var shipmentIds = await context.Shipments.Where(x => x.DeliveryNoteId == DeliveryNoteId).Select(x => x.Id).ToArrayAsync();
        foreach (var shipmentId in shipmentIds)
        {
            var routeIds = await context.RoutePlans.Where(x => x.ShipmentId == shipmentId).Select(x => x.Id).ToArrayAsync();
            context.RouteStops.RemoveRange(context.RouteStops.Where(x => routeIds.Contains(x.RoutePlanId)));
            context.RoutePlans.RemoveRange(context.RoutePlans.Where(x => routeIds.Contains(x.Id)));
            context.ShipmentPackages.RemoveRange(context.ShipmentPackages.Where(x => x.ShipmentId == shipmentId));
            context.ShipmentItems.RemoveRange(context.ShipmentItems.Where(x => x.ShipmentId == shipmentId));
            context.Shipments.RemoveRange(context.Shipments.Where(x => x.Id == shipmentId));
        }
        await context.SaveChangesAsync();
    }

    private static async Task CleanupAsync(
        Guid fullUserId,
        Guid readUserId,
        Guid readRoleId,
        string readRoleCode,
        Guid? vehicleTypeId,
        Guid? vehicleId,
        Guid? driverId,
        Guid? shipmentId,
        Guid? routePlanId,
        Guid? physicalProfileId,
        Guid? palletTypeId)
    {
        await using var context = CreateContext();
        if (physicalProfileId.HasValue)
        {
            context.ProductPhysicalProfiles.RemoveRange(context.ProductPhysicalProfiles.Where(x => x.Id == physicalProfileId.Value));
        }
        if (palletTypeId.HasValue)
        {
            context.PalletTypes.RemoveRange(context.PalletTypes.Where(x => x.Id == palletTypeId.Value));
        }
        if (routePlanId.HasValue)
        {
            context.RouteStops.RemoveRange(context.RouteStops.Where(x => x.RoutePlanId == routePlanId.Value));
            context.RoutePlans.RemoveRange(context.RoutePlans.Where(x => x.Id == routePlanId.Value));
        }
        if (shipmentId.HasValue)
        {
            context.ShipmentPackages.RemoveRange(context.ShipmentPackages.Where(x => x.ShipmentId == shipmentId.Value));
            context.ShipmentItems.RemoveRange(context.ShipmentItems.Where(x => x.ShipmentId == shipmentId.Value));
            context.Shipments.RemoveRange(context.Shipments.Where(x => x.Id == shipmentId.Value));
        }
        if (vehicleId.HasValue)
        {
            context.Vehicles.RemoveRange(context.Vehicles.Where(x => x.Id == vehicleId.Value));
        }
        if (driverId.HasValue)
        {
            context.Drivers.RemoveRange(context.Drivers.Where(x => x.Id == driverId.Value));
        }
        if (vehicleTypeId.HasValue)
        {
            context.VehicleCapacities.RemoveRange(context.VehicleCapacities.Where(x => x.VehicleTypeId == vehicleTypeId.Value));
            context.VehicleTypes.RemoveRange(context.VehicleTypes.Where(x => x.Id == vehicleTypeId.Value));
        }
        await context.SaveChangesAsync();
        await context.RefreshTokens.Where(x => x.UserId == fullUserId || x.UserId == readUserId).ExecuteDeleteAsync();
        await context.UserRoles.Where(x => x.UserId == fullUserId || x.UserId == readUserId).ExecuteDeleteAsync();
        await context.RolePermissions.Where(x => x.RoleId == readRoleId).ExecuteDeleteAsync();
        await context.Users.Where(x => x.Id == fullUserId || x.Id == readUserId).ExecuteDeleteAsync();
        await context.Roles.Where(x => x.Id == readRoleId && x.Code == readRoleCode).ExecuteDeleteAsync();
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
        => JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());

    private static FactoryErpDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FactoryErpDbContext>().UseNpgsql(GetConnectionString()).Options;
        return new FactoryErpDbContext(options);
    }

    private static string GetConnectionString()
        => Environment.GetEnvironmentVariable("FactoryErpTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__FactoryErp")
            ?? "Host=127.0.0.1;Port=5432;Database=factory_erp_g1;Username=factory_erp;Password=dev_only_change_me";

    private sealed record AuthResponse(string AccessToken, IReadOnlyCollection<string> Permissions);

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
