using FactoryErp.Api.Authorization;
using FactoryErp.Application.Shipping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/shipments")]
public sealed class ShipmentsController(ILogisticsCommandService service) : LogisticsControllerBase
{
    [Authorize(Policy = PermissionPolicies.ShipmentCreate)]
    [HttpPost]
    public async Task<ActionResult<ShipmentDto>> Create(
        [FromBody] CreateShipmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateShipmentAsync(request, ActorId(), IdempotencyKey(), CorrelationId(), cancellationToken);
        return Created($"/api/v1/shipments/{result.Id}", result);
    }

    [Authorize(Policy = PermissionPolicies.ShipmentRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetShipmentAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
