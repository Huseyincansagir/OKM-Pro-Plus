using FactoryErp.Infrastructure;
using FactoryErp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);

using var host = builder.Build();
using var scope = host.Services.CreateScope();

var dbContext = scope.ServiceProvider.GetRequiredService<FactoryErpDbContext>();
await dbContext.Database.MigrateAsync();

var identitySeeder = scope.ServiceProvider.GetRequiredService<FactoryErp.Infrastructure.Authentication.IdentitySeeder>();
await identitySeeder.SeedBootstrapAdminAsync(builder.Configuration);

var catalogSeeder = scope.ServiceProvider.GetRequiredService<FactoryErp.Infrastructure.Products.CatalogSeeder>();
await catalogSeeder.SeedAsync();

var salesSeeder = scope.ServiceProvider.GetRequiredService<FactoryErp.Infrastructure.Sales.SalesSeeder>();
await salesSeeder.SeedAsync();

var financeSeeder = scope.ServiceProvider.GetRequiredService<FactoryErp.Infrastructure.Shipping.FinanceSeeder>();
await financeSeeder.SeedAsync();

Console.WriteLine("Factory ERP database migration and optional foundation/catalog/sales/finance seed completed.");
