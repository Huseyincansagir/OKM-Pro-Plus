using System.Security.Claims;
using FactoryErp.Application.Shipping;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

public abstract class ShippingFinanceControllerBase(IShippingFinanceCommandService service) : ControllerBase
{
    protected IShippingFinanceCommandService Service { get; } = service;

    protected Guid ActorId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var actorId)
            ? actorId
            : throw new UnauthorizedAccessException("Authenticated actor id is missing.");
    }

    protected string IdempotencyKey() => Request.Headers["Idempotency-Key"].ToString();

    protected string CorrelationId() => Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
}
