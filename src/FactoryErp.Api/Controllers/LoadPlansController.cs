using FactoryErp.Application.Shipping;
using FactoryErp.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class LoadPlansController(ILoadPlanCommandService service) : LogisticsControllerBase
{
    [Authorize(Policy = PermissionPolicies.ShipmentLoadPlan)]
    [HttpPost("shipments/{shipmentId:guid}/load-plans")]
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
    [HttpGet("load-plans/{loadPlanId:guid}")]
    public async Task<IActionResult> Get(Guid loadPlanId, CancellationToken cancellationToken)
    {
        var result = await service.GetLoadPlanAsync(loadPlanId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.ShipmentLoadPlan)]
    [HttpPost("load-plans/{loadPlanId:guid}/validate")]
    public async Task<ActionResult<LoadPlanValidationDto>> Validate(
        Guid loadPlanId,
        [FromBody] ValidateLoadPlanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.ValidateLoadPlanAsync(
            loadPlanId,
            request,
            ExpectedRowVersion(),
            ActorId(),
            IdempotencyKey(),
            CorrelationId(),
            cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.ShipmentRead)]
    [HttpGet("load-plans/{loadPlanId:guid}/validation-results")]
    public async Task<ActionResult<IReadOnlyCollection<LoadPlanValidationResultDto>>> GetValidationResults(
        Guid loadPlanId,
        CancellationToken cancellationToken)
        => Ok(await service.GetValidationResultsAsync(loadPlanId, cancellationToken));

    [Authorize(Policy = PermissionPolicies.ShipmentLoadPlan)]
    [HttpPost("load-plans/{loadPlanId:guid}/manual-changes")]
    public async Task<ActionResult<LoadPlanDto>> CreateManualChange(
        Guid loadPlanId,
        [FromBody] CreateLoadPlanManualChangeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateManualChangeAsync(
            loadPlanId,
            request,
            ExpectedRowVersion(),
            ActorId(),
            IdempotencyKey(),
            CorrelationId(),
            cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.ShipmentLoadPlan)]
    [HttpPost("load-plans/{loadPlanId:guid}/warning-resolutions")]
    public async Task<ActionResult<LoadPlanValidationResultDto>> ResolveWarning(
        Guid loadPlanId,
        [FromBody] WarningResolutionInput request,
        CancellationToken cancellationToken)
    {
        if (IsOverrideAction(request.Action) && !HasPlanOverridePermission())
        {
            return Forbid();
        }

        var result = await service.ResolveValidationResultAsync(
            loadPlanId,
            request.ValidationResultId,
            new ResolveLoadPlanValidationRequest(
                IsOverrideAction(request.Action)
                    ? nameof(FactoryErp.Domain.Shipping.LoadPlanValidationResolutionStatus.Overridden)
                    : nameof(FactoryErp.Domain.Shipping.LoadPlanValidationResolutionStatus.Resolved),
                request.Reason),
            ExpectedRowVersion(),
            ActorId(),
            IdempotencyKey(),
            CorrelationId(),
            cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.ShipmentPlanLock)]
    [HttpPost("load-plans/{loadPlanId:guid}/lock")]
    public async Task<ActionResult<LoadPlanDto>> Lock(
        Guid loadPlanId,
        [FromBody] LockLoadPlanRequest request,
        CancellationToken cancellationToken)
    {
        if ((request.WarningResolutions ?? Array.Empty<WarningResolutionInput>())
            .Any(x => IsOverrideAction(x.Action)) && !HasPlanOverridePermission())
        {
            return Forbid();
        }

        var result = await service.LockLoadPlanAsync(
            loadPlanId,
            request,
            ExpectedRowVersion(),
            ActorId(),
            IdempotencyKey(),
            CorrelationId(),
            cancellationToken);
        return Ok(result);
    }

    private bool HasPlanOverridePermission()
        => User.HasClaim("permission", "shipment.plan-override");

    private static bool IsOverrideAction(string action)
        => string.Equals(action, "override", StringComparison.OrdinalIgnoreCase)
            || string.Equals(action, "overridden", StringComparison.OrdinalIgnoreCase);
}
