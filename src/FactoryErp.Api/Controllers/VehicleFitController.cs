using FactoryErp.Api.Authorization;
using FactoryErp.Application.Shipping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Authorize]
public sealed class VehicleFitController(IVehicleFitCommandService service) : LogisticsControllerBase
{
    [Authorize(Policy = PermissionPolicies.ShipmentVehicleFit)]
    [HttpPost("api/v1/shipments/{shipmentId:guid}/vehicle-fit/evaluate")]
    public async Task<ActionResult<VehicleFitEvaluationBatchDto>> Evaluate(
        Guid shipmentId,
        [FromBody] EvaluateVehicleFitRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.EvaluateVehicleFitAsync(
            shipmentId,
            request,
            ActorId(),
            IdempotencyKey(),
            CorrelationId(),
            cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.ShipmentVehicleFit)]
    [HttpGet("api/v1/shipments/{shipmentId:guid}/vehicle-fit/candidates")]
    public async Task<IActionResult> Candidates(
        Guid shipmentId,
        [FromQuery] Guid loadPlanId,
        CancellationToken cancellationToken)
    {
        var result = await service.GetVehicleFitCandidatesAsync(shipmentId, loadPlanId, cancellationToken);
        return Ok(result);
    }
}
