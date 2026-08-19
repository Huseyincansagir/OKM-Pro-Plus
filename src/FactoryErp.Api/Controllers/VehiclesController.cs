using FactoryErp.Api.Authorization;
using FactoryErp.Application.Shipping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/vehicles")]
public sealed class VehiclesController(ILogisticsCommandService service) : LogisticsControllerBase
{
    [Authorize(Policy = PermissionPolicies.VehicleRead)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<VehicleDto>>> List(CancellationToken cancellationToken)
        => Ok(await service.ListVehiclesAsync(cancellationToken));

    [Authorize(Policy = PermissionPolicies.VehicleManage)]
    [HttpPost]
    public async Task<ActionResult<VehicleDto>> Create(
        [FromBody] CreateVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateVehicleAsync(request, ActorId(), IdempotencyKey(), CorrelationId(), cancellationToken);
        return Created($"/api/v1/vehicles/{result.Id}", result);
    }

    [Authorize(Policy = PermissionPolicies.VehicleRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetVehicleAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.VehicleStatusUpdate)]
    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(
        Guid id,
        [FromBody] ChangeVehicleStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.ChangeVehicleStatusAsync(
            id,
            request,
            ExpectedRowVersion(),
            ActorId(),
            IdempotencyKey(),
            CorrelationId(),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
