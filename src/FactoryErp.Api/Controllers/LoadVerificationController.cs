using FactoryErp.Api.Authorization;
using FactoryErp.Application.Shipping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class LoadVerificationController(ILoadVerificationCommandService service) : LogisticsControllerBase
{
    [Authorize(Policy = PermissionPolicies.ShipmentLoadVerify)]
    [HttpPost("load-plans/{loadPlanId:guid}/load-verification/sessions")]
    public async Task<ActionResult<LoadVerificationSessionDto>> StartSession(
        Guid loadPlanId,
        [FromBody] StartLoadVerificationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.StartSessionAsync(
            loadPlanId,
            request,
            ExpectedRowVersion(),
            ActorId(),
            IdempotencyKey(),
            CorrelationId(),
            cancellationToken);
        return Created($"/api/v1/load-verification/sessions/{result.Id}", result);
    }

    [Authorize(Policy = PermissionPolicies.ShipmentRead)]
    [HttpGet("load-verification/sessions/{sessionId:guid}")]
    public async Task<IActionResult> GetSession(Guid sessionId, CancellationToken cancellationToken)
    {
        var result = await service.GetSessionAsync(sessionId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.ShipmentLoadVerify)]
    [HttpPost("load-verification/sessions/{sessionId:guid}/scans")]
    public async Task<ActionResult<LoadVerificationScanDto>> Scan(
        Guid sessionId,
        [FromBody] ScanLoadVerificationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.ScanAsync(
            sessionId,
            request,
            ExpectedRowVersion(),
            ActorId(),
            IdempotencyKey(),
            CorrelationId(),
            cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.ShipmentLoadVerify)]
    [HttpPost("load-verification/sessions/{sessionId:guid}/complete")]
    public async Task<ActionResult<LoadVerificationSessionDto>> Complete(
        Guid sessionId,
        [FromBody] CompleteLoadVerificationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CompleteAsync(
            sessionId,
            request,
            ExpectedRowVersion(),
            ActorId(),
            IdempotencyKey(),
            CorrelationId(),
            cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.ShipmentLoadVerifyOverride)]
    [HttpPost("load-verification/sessions/{sessionId:guid}/close-discrepancy")]
    public async Task<ActionResult<LoadVerificationSessionDto>> CloseDiscrepancy(
        Guid sessionId,
        [FromBody] CloseLoadVerificationDiscrepancyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CloseDiscrepancyAsync(
            sessionId,
            request,
            ExpectedRowVersion(),
            ActorId(),
            IdempotencyKey(),
            CorrelationId(),
            cancellationToken);
        return Ok(result);
    }
}
