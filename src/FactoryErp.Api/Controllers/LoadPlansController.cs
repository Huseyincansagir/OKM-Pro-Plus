using FactoryErp.Api.Authorization;
using FactoryErp.Application.Shipping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Authorize]
public sealed class LoadPlansController(ILoadPlanCommandService service) : LogisticsControllerBase
{
    [Authorize(Policy = PermissionPolicies.ShipmentLoadPlan)]
    [HttpPost("api/v1/shipments/{shipmentId:guid}/load-plans")]
    public async Task<ActionResult<LoadPlanDto>> Create(
        Guid shipmentId,
        [FromBody] CreateLoadPlanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateLoadPlanAsync(
            shipmentId,
            request,
            ActorId(),
            IdempotencyKey(),
            CorrelationId(),
            cancellationToken);
        return Created($"/api/v1/load-plans/{result.Id}", result);
    }

    [Authorize(Policy = PermissionPolicies.ShipmentRead)]
    [HttpGet("api/v1/load-plans/{loadPlanId:guid}")]
    public async Task<IActionResult> Get(Guid loadPlanId, CancellationToken cancellationToken)
    {
        var result = await service.GetLoadPlanAsync(loadPlanId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
