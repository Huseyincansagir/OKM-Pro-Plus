using FactoryErp.Api.Authorization;
using FactoryErp.Application.Shipping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/vehicle-types")]
public sealed class VehicleTypesController(ILogisticsCommandService service) : LogisticsControllerBase
{
    [Authorize(Policy = PermissionPolicies.VehicleTypeManage)]
    [HttpPost]
    public async Task<ActionResult<VehicleTypeDto>> Create(
        [FromBody] CreateVehicleTypeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateVehicleTypeAsync(request, ActorId(), IdempotencyKey(), CorrelationId(), cancellationToken);
        return Created($"/api/v1/vehicle-types/{result.Id}", result);
    }

    [Authorize(Policy = PermissionPolicies.VehicleTypeRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetVehicleTypeAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.VehicleTypeManage)]
    [HttpPost("{id:guid}/capacities")]
    public async Task<ActionResult<VehicleCapacityDto>> CreateCapacity(
        Guid id,
        [FromBody] CreateVehicleCapacityRequest request,
        CancellationToken cancellationToken)
    {
        if (request.VehicleTypeId != id)
        {
            throw new ArgumentException("VehicleTypeId path ile eşleşmelidir.");
        }

        var result = await service.CreateVehicleCapacityAsync(request, ActorId(), IdempotencyKey(), CorrelationId(), cancellationToken);
        return Created($"/api/v1/vehicle-types/{id}/capacities/{result.Id}", result);
    }
}
