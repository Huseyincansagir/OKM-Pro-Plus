using System.Security.Claims;
using FactoryErp.Api.Authorization;
using FactoryErp.Application.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/quote-requests")]
public sealed class QuoteRequestsController(ISalesCommandService salesCommandService) : ControllerBase
{
    [Authorize(Policy = PermissionPolicies.QuoteRequestRead)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<QuoteRequestDto>>> List(CancellationToken cancellationToken)
        => Ok(await salesCommandService.ListQuoteRequestsAsync(cancellationToken));

    [Authorize(Policy = PermissionPolicies.QuoteRequestRead)]
    [HttpGet("{quoteRequestId:guid}")]
    public async Task<IActionResult> Get(Guid quoteRequestId, CancellationToken cancellationToken)
    {
        var result = await salesCommandService.GetQuoteRequestAsync(quoteRequestId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.QuoteRequestReview)]
    [HttpPost("{quoteRequestId:guid}/review")]
    public async Task<IActionResult> Review(
        Guid quoteRequestId,
        [FromBody] ReviewQuoteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await salesCommandService.ReviewQuoteRequestAsync(
            quoteRequestId,
            CurrentActorId(),
            request.CustomerId,
            Request.Headers["Idempotency-Key"].ToString(),
            Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? HttpContext.TraceIdentifier,
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    private Guid CurrentActorId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var actorId)
            ? actorId
            : throw new UnauthorizedAccessException("Authenticated actor id is missing.");
    }

    public sealed record ReviewQuoteRequest(Guid? CustomerId);
}
