using FactoryErp.Api.Authorization;
using FactoryErp.Application.Shipping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/drivers")]
public sealed class DriversController(ILogisticsCommandService service) : LogisticsControllerBase
{
    [Authorize(Policy = PermissionPolicies.DriverRead)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<DriverDto>>> List(CancellationToken cancellationToken)
        => Ok(await service.ListDriversAsync(cancellationToken));

    [Authorize(Policy = PermissionPolicies.DriverManage)]
    [HttpPost]
    public async Task<ActionResult<DriverDto>> Create(
        [FromBody] CreateDriverRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateDriverAsync(request, ActorId(), IdempotencyKey(), CorrelationId(), cancellationToken);
        return Created($"/api/v1/drivers/{result.Id}", result);
    }

    [Authorize(Policy = PermissionPolicies.DriverRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetDriverAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.DriverManage)]
    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(
        Guid id,
        [FromBody] ChangeDriverStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.ChangeDriverStatusAsync(
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
