using System.Security.Claims;
using FactoryErp.Api.Authorization;
using FactoryErp.Application.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/quotes")]
public sealed class QuotesController(ISalesCommandService salesCommandService) : ControllerBase
{
    [Authorize(Policy = PermissionPolicies.QuoteRead)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<QuoteDto>>> List(CancellationToken cancellationToken)
        => Ok(await salesCommandService.ListQuotesAsync(cancellationToken));

    [Authorize(Policy = PermissionPolicies.QuoteRead)]
    [HttpGet("{quoteId:guid}")]
    public async Task<IActionResult> Get(Guid quoteId, CancellationToken cancellationToken)
    {
        var result = await salesCommandService.GetQuoteAsync(quoteId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.QuoteCreate)]
    [HttpPost]
    public async Task<ActionResult<QuoteDto>> Create(
        [FromBody] CreateQuoteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await salesCommandService.CreateQuoteAsync(
            request,
            CurrentActorId(),
            Request.Headers["Idempotency-Key"].ToString(),
            Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? HttpContext.TraceIdentifier,
            cancellationToken);
        return Created($"/api/v1/quotes/{result.Id}", result);
    }

    [Authorize(Policy = PermissionPolicies.QuoteIssue)]
    [HttpPost("{quoteId:guid}/issue")]
    public async Task<IActionResult> Issue(Guid quoteId, CancellationToken cancellationToken)
    {
        var result = await salesCommandService.IssueQuoteAsync(
            quoteId,
            CurrentActorId(),
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
}
