using FactoryErp.Api.Authorization;
using FactoryErp.Application.Shipping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Authorize]
public sealed class ShipmentPackagesController(IShipmentPackageCommandService service) : LogisticsControllerBase
{
    [Authorize(Policy = PermissionPolicies.ShipmentPackageManage)]
    [HttpPost("api/v1/shipments/{shipmentId:guid}/packages")]
    public async Task<ActionResult<ShipmentPackageDto>> Create(
        Guid shipmentId,
        [FromBody] CreateShipmentPackageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateShipmentPackageAsync(
            shipmentId,
            request,
            ActorId(),
            IdempotencyKey(),
            CorrelationId(),
            cancellationToken);
        return Created($"/api/v1/shipment-packages/{result.Id}", result);
    }

    [Authorize(Policy = PermissionPolicies.ShipmentPackageRead)]
    [HttpGet("api/v1/shipments/{shipmentId:guid}/packages")]
    public async Task<IActionResult> GetByShipment(Guid shipmentId, CancellationToken cancellationToken)
        => Ok(await service.GetShipmentPackagesAsync(shipmentId, cancellationToken));

    [Authorize(Policy = PermissionPolicies.ShipmentPackageRead)]
    [HttpGet("api/v1/shipment-packages/{packageId:guid}")]
    public async Task<IActionResult> Get(Guid packageId, CancellationToken cancellationToken)
    {
        var result = await service.GetShipmentPackageAsync(packageId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
