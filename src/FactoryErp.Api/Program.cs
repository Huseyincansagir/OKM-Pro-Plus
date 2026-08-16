using FactoryErp.Application.Abstractions.Persistence;
using FactoryErp.Infrastructure;
using FactoryErp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IUnitOfWork>(serviceProvider =>
    serviceProvider.GetRequiredService<FactoryErpDbContext>());

var app = builder.Build();

app.UseExceptionHandler();

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }))
    .AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready", StringComparer.OrdinalIgnoreCase),
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
    },
}).AllowAnonymous();

app.MapHealthChecks("/health/startup", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("database", StringComparer.OrdinalIgnoreCase),
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
    },
}).AllowAnonymous();

app.MapGet("/", () => Results.Ok(new
{
    service = "FactoryErp.Api",
    status = "running",
    version = "g1",
})).AllowAnonymous();

app.Run();

public partial class Program;
