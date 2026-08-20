using FactoryErp.Api.Authorization;
using FactoryErp.Application.Shipping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class RoutePlansController(ILogisticsCommandService service) : LogisticsControllerBase
{
    [Authorize(Policy = PermissionPolicies.ShipmentRouteManage)]
    [HttpPost("shipments/{shipmentId:guid}/route-plans")]
    public async Task<ActionResult<RoutePlanDto>> Create(
        Guid shipmentId,
        [FromBody] CreateRoutePlanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateRoutePlanAsync(
            shipmentId,
            request,
            ActorId(),
            IdempotencyKey(),
            CorrelationId(),
            cancellationToken);
        return Created($"/api/v1/route-plans/{result.Id}", result);
    }

    [Authorize(Policy = PermissionPolicies.ShipmentRead)]
    [HttpGet("shipments/{shipmentId:guid}/route-plans")]
    public async Task<ActionResult<IReadOnlyCollection<RoutePlanDto>>> ListByShipment(
        Guid shipmentId,
        CancellationToken cancellationToken)
        => Ok(await service.ListRoutePlansByShipmentAsync(shipmentId, cancellationToken));

    [Authorize(Policy = PermissionPolicies.ShipmentRouteManage)]
    [HttpGet("route-plans/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetRoutePlanAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.ShipmentRouteManage)]
    [HttpPost("route-plans/{id:guid}/stops/replace")]
    public async Task<IActionResult> ReplaceStops(
        Guid id,
        [FromBody] ReplaceRouteStopsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.ReplaceStopsAsync(
            id,
            request,
            ExpectedRowVersion(),
            ActorId(),
            IdempotencyKey(),
            CorrelationId(),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.ShipmentRouteManage)]
    [HttpPost("route-plans/{id:guid}/assign-resources")]
    public async Task<IActionResult> AssignResources(
        Guid id,
        [FromBody] AssignRouteResourcesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.AssignResourcesAsync(
            id,
            request,
            ExpectedRowVersion(),
            ActorId(),
            IdempotencyKey(),
            CorrelationId(),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.ShipmentRouteManage)]
    [HttpPost("route-plans/{id:guid}/plan")]
    public async Task<IActionResult> Plan(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.PlanRouteAsync(
            id,
            ExpectedRowVersion(),
            ActorId(),
            IdempotencyKey(),
            CorrelationId(),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.ShipmentRouteLock)]
    [HttpPost("route-plans/{id:guid}/lock")]
    public async Task<IActionResult> Lock(
        Guid id,
        [FromBody] RouteConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.LockRouteAsync(
            id,
            request,
            ExpectedRowVersion(),
            ActorId(),
            IdempotencyKey(),
            CorrelationId(),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.ShipmentPlanReplan)]
    [HttpPost("route-plans/{id:guid}/replan")]
    public async Task<IActionResult> Replan(
        Guid id,
        [FromBody] ReplanRouteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.ReplanRouteAsync(
            id,
            request,
            ExpectedRowVersion(),
            ActorId(),
            IdempotencyKey(),
            CorrelationId(),
            cancellationToken);
        return result is null ? NotFound() : Created($"/api/v1/route-plans/{result.Id}", result);
    }
}
