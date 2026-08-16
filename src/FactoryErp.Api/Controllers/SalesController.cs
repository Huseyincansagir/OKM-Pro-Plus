using System.Security.Claims;
using FactoryErp.Api.Authorization;
using FactoryErp.Application.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
public sealed class SalesController(ISalesCommandService salesCommandService) : ControllerBase
{
    [Authorize(Policy = PermissionPolicies.OrderCreate)]
    [HttpPost]
    public async Task<ActionResult<SalesOrderDto>> Create(
        [FromBody] CreateSalesOrderRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = CurrentActorId();
        var result = await salesCommandService.CreateSalesOrderAsync(
            request,
            actorId,
            Request.Headers["Idempotency-Key"].ToString(),
            CorrelationId(),
            cancellationToken);
        return Created($"/api/v1/orders/{result.Id}", result);
    }

    [Authorize(Policy = PermissionPolicies.OrderRead)]
    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> Get(Guid orderId, CancellationToken cancellationToken)
    {
        var result = await salesCommandService.GetSalesOrderAsync(orderId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.OrderSubmit)]
    [HttpPost("{orderId:guid}/submit")]
    public async Task<IActionResult> Submit(Guid orderId, CancellationToken cancellationToken)
    {
        var result = await salesCommandService.SubmitSalesOrderAsync(
            orderId,
            CurrentActorId(),
            Request.Headers["Idempotency-Key"].ToString(),
            CorrelationId(),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.OrderApprove)]
    [HttpPost("{orderId:guid}/approve")]
    public async Task<IActionResult> Approve(
        Guid orderId,
        [FromBody] ApproveOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await salesCommandService.ApproveSalesOrderAsync(
            orderId,
            CurrentActorId(),
            request.Comment,
            Request.Headers["Idempotency-Key"].ToString(),
            CorrelationId(),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.OrderReject)]
    [HttpPost("{orderId:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid orderId,
        [FromBody] RejectOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await salesCommandService.RejectSalesOrderAsync(
            orderId,
            CurrentActorId(),
            request.Comment,
            Request.Headers["Idempotency-Key"].ToString(),
            CorrelationId(),
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

    private string CorrelationId()
        => Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? HttpContext.TraceIdentifier;
}

[ApiController]
[AllowAnonymous]
[Route("api/v1/public/quote-requests")]
public sealed class PublicQuoteRequestsController(ISalesCommandService salesCommandService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<QuoteRequestDto>> Create(
        [FromBody] CreatePublicQuoteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await salesCommandService.CreatePublicQuoteRequestAsync(
            request,
            Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? HttpContext.TraceIdentifier,
            cancellationToken);
        return Created($"/api/v1/public/quote-requests/{result.Id}", result);
    }
}
