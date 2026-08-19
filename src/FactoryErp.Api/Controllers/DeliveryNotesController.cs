using FactoryErp.Api.Authorization;
using FactoryErp.Application.Shipping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/delivery-notes")]
public sealed class DeliveryNotesController(IShippingFinanceCommandService service)
    : ShippingFinanceControllerBase(service)
{
    [Authorize(Policy = PermissionPolicies.DeliveryNoteCreate)]
    [HttpPost]
    public async Task<ActionResult<DeliveryNoteDto>> Create(
        [FromBody] CreateDeliveryNoteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Service.CreateDeliveryNoteAsync(request, ActorId(), IdempotencyKey(), CorrelationId(), cancellationToken);
        return Created($"/api/v1/delivery-notes/{result.Id}", result);
    }

    [Authorize(Policy = PermissionPolicies.DeliveryNoteRead)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<DeliveryNoteDto>>> List(CancellationToken cancellationToken)
        => Ok(await Service.ListDeliveryNotesAsync(cancellationToken));

    [Authorize(Policy = PermissionPolicies.DeliveryNoteRead)]
    [HttpGet("{deliveryNoteId:guid}")]
    public async Task<IActionResult> Get(Guid deliveryNoteId, CancellationToken cancellationToken)
    {
        var result = await Service.GetDeliveryNoteAsync(deliveryNoteId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.DeliveryNoteIssue)]
    [HttpPost("{deliveryNoteId:guid}/issue")]
    public async Task<IActionResult> Issue(Guid deliveryNoteId, CancellationToken cancellationToken)
    {
        var result = await Service.IssueDeliveryNoteAsync(deliveryNoteId, ActorId(), IdempotencyKey(), CorrelationId(), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
