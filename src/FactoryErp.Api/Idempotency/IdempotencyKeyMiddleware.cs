using System.Text.Json;

namespace FactoryErp.Api.Idempotency;

public sealed class IdempotencyKeyMiddleware(RequestDelegate next)
{
    private static readonly string[] CriticalRouteSegments =
    [
        "/api/v1/orders",
        "/api/v1/delivery-notes",
        "/api/v1/invoices",
        "/api/v1/payments",
        "/api/v1/shipments",
        "/api/v1/production",
        "/api/v1/quote-requests",
        "/api/v1/warehouse-transfers",
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method)
            && IsCriticalMutation(context.Request.Path)
            && string.IsNullOrWhiteSpace(context.Request.Headers["Idempotency-Key"].FirstOrDefault()))
        {
            await WriteMissingKeyAsync(context);
            return;
        }

        if (context.Request.Headers.TryGetValue("Idempotency-Key", out var key))
        {
            context.Items["Idempotency-Key"] = key.FirstOrDefault();
        }

        await next(context);
    }

    private static bool IsCriticalMutation(PathString path)
    {
        var value = path.Value ?? string.Empty;
        return CriticalRouteSegments.Any(segment => value.StartsWith(segment, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task WriteMissingKeyAsync(HttpContext context)
    {
        var correlationId = context.Items.TryGetValue("CorrelationId", out var value)
            ? value?.ToString()
            : null;
        var problem = new
        {
            type = "https://erp.local/problems/missing-idempotency-key",
            title = "Idempotency-Key zorunludur",
            status = StatusCodes.Status400BadRequest,
            code = "MISSING_IDEMPOTENCY_KEY",
            detail = "Kesinleştiren işlemler Idempotency-Key header’ı ile gönderilmelidir.",
            instance = context.Request.Path.Value,
            requestId = context.TraceIdentifier,
            correlationId,
            retryable = false,
            errors = Array.Empty<object>(),
            actions = Array.Empty<object>(),
        };

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(context.Response.Body, problem);
    }
}
