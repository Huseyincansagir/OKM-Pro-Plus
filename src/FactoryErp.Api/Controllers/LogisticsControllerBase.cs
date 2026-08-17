using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

public abstract class LogisticsControllerBase : ControllerBase
{
    protected Guid ActorId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var actorId)
            ? actorId
            : throw new UnauthorizedAccessException("Authenticated actor id is missing.");
    }

    protected string IdempotencyKey() => Request.Headers["Idempotency-Key"].ToString();

    protected string CorrelationId()
        => Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? HttpContext.TraceIdentifier;

    protected long ExpectedRowVersion()
    {
        var value = Request.Headers["If-Match"].FirstOrDefault()?.Trim().Trim('"');
        return long.TryParse(value, out var rowVersion)
            ? rowVersion
            : throw new ArgumentException("If-Match header geçerli bir row_version içermelidir.");
    }
}
