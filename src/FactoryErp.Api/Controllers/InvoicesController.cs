using FactoryErp.Api.Authorization;
using FactoryErp.Application.Shipping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/invoices")]
public sealed class InvoicesController(IShippingFinanceCommandService service)
    : ShippingFinanceControllerBase(service)
{
    [Authorize(Policy = PermissionPolicies.InvoiceCreate)]
    [HttpPost]
    public async Task<ActionResult<InvoiceDto>> Create(
        [FromBody] CreateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Service.CreateInvoiceAsync(request, ActorId(), IdempotencyKey(), CorrelationId(), cancellationToken);
        return Created($"/api/v1/invoices/{result.Id}", result);
    }

    [Authorize(Policy = PermissionPolicies.InvoiceRead)]
    [HttpGet("{invoiceId:guid}")]
    public async Task<IActionResult> Get(Guid invoiceId, CancellationToken cancellationToken)
    {
        var result = await Service.GetInvoiceAsync(invoiceId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.InvoiceIssue)]
    [HttpPost("{invoiceId:guid}/issue")]
    public async Task<IActionResult> Issue(Guid invoiceId, CancellationToken cancellationToken)
    {
        var result = await Service.IssueInvoiceAsync(invoiceId, ActorId(), IdempotencyKey(), CorrelationId(), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
