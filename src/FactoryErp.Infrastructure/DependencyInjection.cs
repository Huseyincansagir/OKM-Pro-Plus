using FactoryErp.Infrastructure.Health;
using FactoryErp.Infrastructure.Persistence;
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

        services.AddDbContext<FactoryErpDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.MigrationsAssembly(typeof(FactoryErpDbContext).Assembly.FullName)));

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("postgres", tags: ["ready", "database"]);

        return services;
    }
}
