using System.Security.Claims;
using FactoryErp.Api.Authorization;
using FactoryErp.Application.Production;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Route("api/v1/production/orders")]
public sealed class ProductionController(IProductionCommandService productionCommandService) : ControllerBase
{
    [Authorize(Policy = PermissionPolicies.ProductionCreate)]
    [HttpPost]
    public async Task<ActionResult<ProductionOrderDto>> Create(
        [FromBody] CreateProductionOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await productionCommandService.CreateProductionOrderAsync(
            request,
            CurrentActorId(),
            Request.Headers["Idempotency-Key"].ToString(),
            CorrelationId(),
            cancellationToken);
        return Created($"/api/v1/production/orders/{result.Id}", result);
    }

    [Authorize(Policy = PermissionPolicies.ProductionRead)]
    [HttpGet("{productionOrderId:guid}")]
    public async Task<IActionResult> Get(Guid productionOrderId, CancellationToken cancellationToken)
    {
        var result = await productionCommandService.GetProductionOrderAsync(productionOrderId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.ProductionStart)]
    [HttpPost("{productionOrderId:guid}/release")]
    public async Task<IActionResult> Release(Guid productionOrderId, CancellationToken cancellationToken)
    {
        var result = await productionCommandService.ReleaseProductionOrderAsync(
            productionOrderId,
            CurrentActorId(),
            Request.Headers["Idempotency-Key"].ToString(),
            CorrelationId(),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.ProductionStart)]
    [HttpPost("{productionOrderId:guid}/start")]
    public async Task<IActionResult> Start(Guid productionOrderId, CancellationToken cancellationToken)
    {
        var result = await productionCommandService.StartProductionOrderAsync(
            productionOrderId,
            CurrentActorId(),
            Request.Headers["Idempotency-Key"].ToString(),
            CorrelationId(),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.ProductionRecord)]
    [HttpPost("{productionOrderId:guid}/records")]
    public async Task<IActionResult> AddRecord(
        Guid productionOrderId,
        [FromBody] AddProductionRecordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await productionCommandService.AddProductionRecordAsync(
            productionOrderId,
            request,
            CurrentActorId(),
            Request.Headers["Idempotency-Key"].ToString(),
            CorrelationId(),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.ProductionComplete)]
    [HttpPost("{productionOrderId:guid}/complete")]
    public async Task<IActionResult> Complete(
        Guid productionOrderId,
        [FromBody] CompleteProductionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await productionCommandService.CompleteProductionOrderAsync(
            productionOrderId,
            request,
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
