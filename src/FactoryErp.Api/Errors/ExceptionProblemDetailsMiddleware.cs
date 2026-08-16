using System.Text.Json;
using FactoryErp.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Api.Errors;

public sealed class ExceptionProblemDetailsMiddleware(
    RequestDelegate next,
    ILogger<ExceptionProblemDetailsMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? $"corr-{Guid.NewGuid():N}";
        context.Response.Headers["X-Correlation-Id"] = correlationId;
        context.Items["CorrelationId"] = correlationId;

        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled request exception. CorrelationId: {CorrelationId}", correlationId);
            await WriteProblemDetailsAsync(context, exception, correlationId);
        }
    }

    private static async Task WriteProblemDetailsAsync(HttpContext context, Exception exception, string correlationId)
    {
        if (context.Response.HasStarted)
        {
            throw exception;
        }

        var problem = Map(exception, context, correlationId);
        context.Response.Clear();
        context.Response.StatusCode = problem.Status;
        context.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(context.Response.Body, problem, JsonOptions);
    }

    private static ApiProblemDetails Map(Exception exception, HttpContext context, string correlationId)
    {
        return exception switch
        {
            DomainException domainException => new ApiProblemDetails(
                "https://erp.local/problems/domain-error",
                "İş kuralı uygulanamadı",
                StatusCodes.Status422UnprocessableEntity,
                domainException.Error.Code,
                domainException.Error.Message,
                context.Request.Path,
                context.TraceIdentifier,
                correlationId,
                false),
            DbUpdateConcurrencyException => new ApiProblemDetails(
                "https://erp.local/problems/concurrency-conflict",
                "Kayıt başka bir işlem tarafından değiştirildi",
                StatusCodes.Status409Conflict,
                "QUANTITY_CONCURRENCY_CONFLICT",
                "Güncel kaydı yeniden okuyup işlemi tekrar değerlendirin.",
                context.Request.Path,
                context.TraceIdentifier,
                correlationId,
                true),
            ArgumentException => new ApiProblemDetails(
                "https://erp.local/problems/invalid-request",
                "Geçersiz istek",
                StatusCodes.Status400BadRequest,
                "INVALID_REQUEST",
                "İstek doğrulanamadı.",
                context.Request.Path,
                context.TraceIdentifier,
                correlationId,
                false),
            _ => new ApiProblemDetails(
                "https://erp.local/problems/unexpected-error",
                "Beklenmeyen bir hata oluştu",
                StatusCodes.Status500InternalServerError,
                "UNEXPECTED_ERROR",
                "İşlem tamamlanamadı. Request ID ile destek ekibine başvurun.",
                context.Request.Path,
                context.TraceIdentifier,
                correlationId,
                false),
        };
    }

    private sealed record ApiProblemDetails(
        string Type,
        string Title,
        int Status,
        string Code,
        string Detail,
        string Instance,
        string RequestId,
        string CorrelationId,
        bool Retryable)
    {
        public IReadOnlyCollection<object> Errors { get; } = [];
        public IReadOnlyCollection<object> Actions { get; } = [];
    }
}
