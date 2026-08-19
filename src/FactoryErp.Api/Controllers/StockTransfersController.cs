using System.Security.Claims;
using FactoryErp.Api.Authorization;
using FactoryErp.Application.Warehouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Route("api/v1/warehouse-transfers")]
public sealed class StockTransfersController(IStockTransferCommandService stockTransferCommandService) : ControllerBase
{
    [Authorize(Policy = PermissionPolicies.StockTransferCreate)]
    [HttpPost]
    public async Task<ActionResult<StockTransferDto>> Create(
        [FromBody] CreateStockTransferRequest request,
        CancellationToken cancellationToken)
    {
        var result = await stockTransferCommandService.CreateAsync(
            request,
            CurrentActorId(),
            Request.Headers["Idempotency-Key"].ToString(),
            CorrelationId(),
            cancellationToken);
        return Created($"/api/v1/warehouse-transfers/{result.Id}", result);
    }

    [Authorize(Policy = PermissionPolicies.StockTransferRead)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<StockTransferDto>>> List(CancellationToken cancellationToken)
        => Ok(await stockTransferCommandService.ListAsync(cancellationToken));

    [Authorize(Policy = PermissionPolicies.StockTransferRead)]
    [HttpGet("{transferId:guid}")]
    public async Task<IActionResult> Get(Guid transferId, CancellationToken cancellationToken)
    {
        var result = await stockTransferCommandService.GetAsync(transferId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.StockTransferComplete)]
    [HttpPost("{transferId:guid}/complete")]
    public async Task<IActionResult> Complete(Guid transferId, CancellationToken cancellationToken)
    {
        var result = await stockTransferCommandService.CompleteAsync(
            transferId,
            CurrentActorId(),
            Request.Headers["Idempotency-Key"].ToString(),
            CorrelationId(),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.StockTransferCancel)]
    [HttpPost("{transferId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid transferId, CancellationToken cancellationToken)
    {
        var result = await stockTransferCommandService.CancelAsync(
            transferId,
            CurrentActorId(),
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
