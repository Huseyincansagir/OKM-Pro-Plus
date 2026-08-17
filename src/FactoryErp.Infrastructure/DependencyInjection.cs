using FactoryErp.Application.Abstractions.Persistence;
using FactoryErp.Application.Identity;
using FactoryErp.Application.Products;
using FactoryErp.Application.Production;
using FactoryErp.Application.Sales;
using FactoryErp.Application.Shipping;
using FactoryErp.Application.Warehouse;
using FactoryErp.Infrastructure.Authentication;
using FactoryErp.Infrastructure.Health;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Products;
using FactoryErp.Infrastructure.Production;
using FactoryErp.Infrastructure.Sales;
using FactoryErp.Infrastructure.Shipping;
using FactoryErp.Infrastructure.Warehouse;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FactoryErp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("FactoryErp")
            ?? configuration["POSTGRES_CONNECTION_STRING"]
            ?? throw new InvalidOperationException("FactoryErp PostgreSQL connection string is not configured.");

        services.Configure<AuthOptions>(options =>
        {
            options.Issuer = configuration["Authentication:Issuer"] ?? options.Issuer;
            options.Audience = configuration["Authentication:Audience"] ?? options.Audience;
            options.SigningKey = configuration["Authentication:SigningKey"] ?? options.SigningKey;
            if (int.TryParse(configuration["Authentication:AccessTokenMinutes"], out var accessTokenMinutes))
            {
                options.AccessTokenMinutes = accessTokenMinutes;
            }

            if (int.TryParse(configuration["Authentication:RefreshTokenDays"], out var refreshTokenDays))
            {
                options.RefreshTokenDays = refreshTokenDays;
            }
        });
        services.AddSingleton<PasswordHasher>();
        services.AddScoped<IdentitySeeder>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IIdempotencyStore, EfIdempotencyStore>();
        services.AddScoped<IAuditWriter, EfAuditWriter>();
        services.AddScoped<IProductCatalogService, ProductCatalogService>();
        services.AddScoped<CatalogSeeder>();
        services.AddScoped<ISalesCommandService, SalesCommandService>();
        services.AddScoped<IProductionCommandService, ProductionCommandService>();
        services.AddScoped<IStockTransferCommandService, StockTransferCommandService>();
        services.AddScoped<SalesSeeder>();
        services.AddScoped<IShippingFinanceCommandService, DeliveryInvoiceFinanceService>();
        services.AddScoped<ILogisticsCommandService, LogisticsCommandService>();
        services.AddScoped<IPhysicalLogisticsCommandService, PhysicalLogisticsCommandService>();
        services.AddScoped<IShipmentPackageCommandService, ShipmentPackageCommandService>();
        services.AddScoped<ILoadPlanCommandService, LoadPlanCommandService>();
        services.AddScoped<IVehicleFitCommandService, VehicleFitCommandService>();
        services.AddScoped<FinanceSeeder>();

        services.AddDbContext<FactoryErpDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.MigrationsAssembly(typeof(FactoryErpDbContext).Assembly.FullName)));

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("postgres", tags: ["ready", "database"]);

        return services;
    }
}
