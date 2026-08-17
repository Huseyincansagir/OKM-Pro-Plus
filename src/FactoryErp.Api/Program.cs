using System.Text;
using FactoryErp.Application.Abstractions.Persistence;
using FactoryErp.Infrastructure;
using FactoryErp.Infrastructure.Authentication;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Api.Authorization;
using FactoryErp.Api.Errors;
using FactoryErp.Api.Idempotency;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IUnitOfWork>(serviceProvider =>
    serviceProvider.GetRequiredService<FactoryErpDbContext>());
builder.Services.AddControllers();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = authOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PermissionPolicies.SystemRead, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "system.read"));
    options.AddPolicy(PermissionPolicies.OrderCreate, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "order.create"));
    options.AddPolicy(PermissionPolicies.OrderRead, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "order.read"));
    options.AddPolicy(PermissionPolicies.OrderSubmit, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "order.submit"));
    options.AddPolicy(PermissionPolicies.OrderApprove, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "order.approve"));
    options.AddPolicy(PermissionPolicies.OrderReject, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "order.reject"));
    options.AddPolicy(PermissionPolicies.QuoteRequestRead, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "quote-request.read"));
    options.AddPolicy(PermissionPolicies.QuoteRequestReview, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "quote-request.review"));
    options.AddPolicy(PermissionPolicies.DeliveryNoteCreate, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "delivery-note.create"));
    options.AddPolicy(PermissionPolicies.DeliveryNoteRead, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "delivery-note.read"));
    options.AddPolicy(PermissionPolicies.DeliveryNoteIssue, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "delivery-note.issue"));
    options.AddPolicy(PermissionPolicies.InvoiceCreate, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "invoice.create"));
    options.AddPolicy(PermissionPolicies.InvoiceRead, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "invoice.read"));
    options.AddPolicy(PermissionPolicies.InvoiceIssue, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "invoice.issue"));
    options.AddPolicy(PermissionPolicies.PaymentApply, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "payment.apply"));
    options.AddPolicy(PermissionPolicies.CurrentAccountRead, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "current-account.read"));
    options.AddPolicy(PermissionPolicies.ProductionCreate, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "production.create"));
    options.AddPolicy(PermissionPolicies.ProductionRead, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "production.read"));
    options.AddPolicy(PermissionPolicies.ProductionStart, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "production.start"));
    options.AddPolicy(PermissionPolicies.ProductionRecord, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "production.record"));
    options.AddPolicy(PermissionPolicies.ProductionComplete, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "production.complete"));
    options.AddPolicy(PermissionPolicies.StockTransferCreate, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "stock-transfer.create"));
    options.AddPolicy(PermissionPolicies.StockTransferRead, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "stock-transfer.read"));
    options.AddPolicy(PermissionPolicies.StockTransferComplete, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "stock-transfer.complete"));
    options.AddPolicy(PermissionPolicies.StockTransferCancel, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "stock-transfer.cancel"));
    options.AddPolicy(PermissionPolicies.VehicleTypeRead, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "vehicle-type.read"));
    options.AddPolicy(PermissionPolicies.VehicleTypeManage, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "vehicle-type.manage"));
    options.AddPolicy(PermissionPolicies.VehicleRead, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "vehicle.read"));
    options.AddPolicy(PermissionPolicies.VehicleManage, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "vehicle.manage"));
    options.AddPolicy(PermissionPolicies.VehicleStatusUpdate, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "vehicle.status-update"));
    options.AddPolicy(PermissionPolicies.DriverRead, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "driver.read"));
    options.AddPolicy(PermissionPolicies.DriverManage, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "driver.manage"));
    options.AddPolicy(PermissionPolicies.ShipmentCreate, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "shipment.create"));
    options.AddPolicy(PermissionPolicies.ShipmentRead, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "shipment.read"));
    options.AddPolicy(PermissionPolicies.ShipmentRouteManage, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "shipment.route-manage"));
    options.AddPolicy(PermissionPolicies.ShipmentRouteLock, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "shipment.route-lock"));
    options.AddPolicy(PermissionPolicies.ShipmentPlanReplan, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "shipment.plan-replan"));
    options.AddPolicy(PermissionPolicies.ShipmentPackageRead, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "shipment.package-read"));
    options.AddPolicy(PermissionPolicies.ShipmentPackageManage, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "shipment.package-manage"));
    options.AddPolicy(PermissionPolicies.ShipmentLoadPlan, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "shipment.load-plan"));
    options.AddPolicy(PermissionPolicies.ShipmentVehicleFit, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "shipment.vehicle-fit"));
    options.AddPolicy(PermissionPolicies.ShipmentPlanLock, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "shipment.plan-lock"));
    options.AddPolicy(PermissionPolicies.ShipmentPlanOverride, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "shipment.plan-override"));
    options.AddPolicy(PermissionPolicies.ShipmentLoadVerify, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "shipment.load-verify"));
    options.AddPolicy(PermissionPolicies.ShipmentLoadVerifyOverride, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "shipment.load-verify-override"));
    options.AddPolicy(PermissionPolicies.ShipmentDispatch, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "shipment.dispatch"));
    options.AddPolicy(PermissionPolicies.ShipmentDepart, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "shipment.depart"));
    options.AddPolicy(PermissionPolicies.ShipmentRouteExecute, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "shipment.route-execute"));
    options.AddPolicy(PermissionPolicies.ShipmentRouteException, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "shipment.route-exception"));
    options.AddPolicy(PermissionPolicies.PhysicalProfileRead, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "physical-profile.read"));
    options.AddPolicy(PermissionPolicies.PhysicalProfileManage, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "physical-profile.manage"));
    options.AddPolicy(PermissionPolicies.PalletTypeRead, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "pallet-type.read"));
    options.AddPolicy(PermissionPolicies.PalletTypeManage, policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "pallet-type.manage"));
});

var app = builder.Build();

app.UseMiddleware<ExceptionProblemDetailsMiddleware>();
app.UseMiddleware<IdempotencyKeyMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

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
