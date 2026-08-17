using FactoryErp.Api.Authorization;
using FactoryErp.Application.Shipping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class DispatchRunsController(IDispatchRunCommandHandler handler) : LogisticsControllerBase
{
    [Authorize(Policy = PermissionPolicies.ShipmentDispatch)]
    [HttpPost("route-plans/{routePlanId:guid}/dispatch")]
    public async Task<ActionResult<DispatchRunDto>> Prepare(
        Guid routePlanId,
        [FromBody] PrepareDispatchRunRequest request,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new PrepareDispatchRunCommand(
                request.ShipmentId,
                request.LoadPlanId,
                routePlanId,
                request.VehicleId,
                request.DriverId,
                request.PlannedDepartureAt,
                request.Stops,
                request.ExpectedLoadPlanRowVersion,
                request.ExpectedShipmentRowVersion,
                request.ExpectedRoutePlanRowVersion,
                ActorId(),
                IdempotencyKey(),
                CorrelationId()),
            cancellationToken);
        return Created($"/api/v1/dispatch-runs/{result.Id}", result);
    }

    [Authorize(Policy = PermissionPolicies.ShipmentDispatch)]
    [HttpPost("dispatch-runs/{dispatchRunId:guid}/confirm")]
    public async Task<ActionResult<DispatchRunDto>> Confirm(Guid dispatchRunId, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(
            new ConfirmDispatchCommand(
                dispatchRunId,
                ExpectedRowVersion(),
                ActorId(),
                IdempotencyKey(),
                CorrelationId()),
            cancellationToken));

    [Authorize(Policy = PermissionPolicies.ShipmentDepart)]
    [HttpPost("dispatch-runs/{dispatchRunId:guid}/depart")]
    public async Task<ActionResult<DispatchRunDto>> Depart(
        Guid dispatchRunId,
        [FromBody] DepartDispatchRunRequest request,
        CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(
            new DepartDispatchRunCommand(
                dispatchRunId,
                request.OccurredAt,
                request.LocationText,
                request.Latitude,
                request.Longitude,
                ExpectedRowVersion(),
                ActorId(),
                IdempotencyKey(),
                CorrelationId()),
            cancellationToken));

    [Authorize(Policy = PermissionPolicies.ShipmentRouteExecute)]
    [HttpPost("dispatch-runs/{dispatchRunId:guid}/stops/{routeStopId:guid}/arrive")]
    public async Task<ActionResult<DispatchRunDto>> Arrive(
        Guid dispatchRunId,
        Guid routeStopId,
        [FromBody] RouteExecutionLocationRequest request,
        CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(
            new ArriveAtStopCommand(
                dispatchRunId,
                routeStopId,
                request.OccurredAt,
                request.LocationText,
                request.Latitude,
                request.Longitude,
                ExpectedRowVersion(),
                ActorId(),
                IdempotencyKey(),
                CorrelationId()),
            cancellationToken));

    [Authorize(Policy = PermissionPolicies.ShipmentRouteExecute)]
    [HttpPost("dispatch-runs/{dispatchRunId:guid}/stops/{routeStopId:guid}/depart")]
    public async Task<ActionResult<DispatchRunDto>> DepartStop(
        Guid dispatchRunId,
        Guid routeStopId,
        [FromBody] RouteExecutionLocationRequest request,
        CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(
            new DepartStopCommand(
                dispatchRunId,
                routeStopId,
                request.OccurredAt,
                request.LocationText,
                request.Latitude,
                request.Longitude,
                ExpectedRowVersion(),
                ActorId(),
                IdempotencyKey(),
                CorrelationId()),
            cancellationToken));

    [Authorize(Policy = PermissionPolicies.ShipmentRouteException)]
    [HttpPost("dispatch-runs/{dispatchRunId:guid}/stops/{routeStopId:guid}/skip")]
    public async Task<ActionResult<DispatchRunDto>> Skip(
        Guid dispatchRunId,
        Guid routeStopId,
        [FromBody] SkipRouteStopRequest request,
        CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(
            new SkipStopCommand(
                dispatchRunId,
                routeStopId,
                request.OccurredAt,
                request.Reason,
                ExpectedRowVersion(),
                ActorId(),
                IdempotencyKey(),
                CorrelationId()),
            cancellationToken));

    [Authorize(Policy = PermissionPolicies.ShipmentRouteExecute)]
    [HttpPost("dispatch-runs/{dispatchRunId:guid}/complete")]
    public async Task<ActionResult<DispatchRunDto>> Complete(
        Guid dispatchRunId,
        [FromBody] CompleteRouteRequest request,
        CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(
            new CompleteRouteCommand(
                dispatchRunId,
                request.OccurredAt,
                ExpectedRowVersion(),
                ActorId(),
                IdempotencyKey(),
                CorrelationId()),
            cancellationToken));

    [Authorize(Policy = PermissionPolicies.ShipmentRouteException)]
    [HttpPost("dispatch-runs/{dispatchRunId:guid}/cancel")]
    public async Task<ActionResult<DispatchRunDto>> Cancel(
        Guid dispatchRunId,
        [FromBody] CancelDispatchRunRequest request,
        CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(
            new CancelDispatchRunCommand(
                dispatchRunId,
                request.OccurredAt,
                request.Reason,
                ExpectedRowVersion(),
                ActorId(),
                IdempotencyKey(),
                CorrelationId()),
            cancellationToken));
}

public sealed record PrepareDispatchRunRequest(
    Guid ShipmentId,
    Guid LoadPlanId,
    Guid VehicleId,
    Guid DriverId,
    DateTimeOffset? PlannedDepartureAt,
    IReadOnlyCollection<DispatchStopInput> Stops,
    long ExpectedLoadPlanRowVersion,
    long ExpectedShipmentRowVersion,
    long ExpectedRoutePlanRowVersion);

public sealed record DepartDispatchRunRequest(
    DateTimeOffset OccurredAt,
    string? LocationText,
    decimal? Latitude,
    decimal? Longitude);

public sealed record RouteExecutionLocationRequest(
    DateTimeOffset OccurredAt,
    string? LocationText,
    decimal? Latitude,
    decimal? Longitude);

public sealed record SkipRouteStopRequest(
    DateTimeOffset OccurredAt,
    string Reason);

public sealed record CompleteRouteRequest(DateTimeOffset OccurredAt);

public sealed record CancelDispatchRunRequest(
    DateTimeOffset OccurredAt,
    string Reason);
