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

Console.WriteLine("Factory ERP database migration completed.");
