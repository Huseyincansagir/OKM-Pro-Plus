using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FactoryErp.Infrastructure.Persistence;

public sealed class FactoryErpDbContextFactory : IDesignTimeDbContextFactory<FactoryErpDbContext>
{
    public FactoryErpDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=factory_erp;Username=factory_erp_app;Password=dev_only_change_me";

        var optionsBuilder = new DbContextOptionsBuilder<FactoryErpDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsqlOptions => npgsqlOptions.MigrationsAssembly(typeof(FactoryErpDbContext).Assembly.FullName));

        return new FactoryErpDbContext(optionsBuilder.Options);
    }
}
