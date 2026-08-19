using System.Security.Claims;
using FactoryErp.Api.Authorization;
using FactoryErp.Application.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/customers")]
public sealed class CustomersController(
    ISalesCommandService salesCommandService,
    ICustomerDirectoryService directoryService,
    ISalesPricingService pricingService) : ControllerBase
{
    [Authorize(Policy = PermissionPolicies.CustomerRead)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<CustomerDto>>> List(CancellationToken cancellationToken)
        => Ok(await salesCommandService.ListCustomersAsync(cancellationToken));

    [Authorize(Policy = PermissionPolicies.CustomerRead)]
    [HttpGet("{customerId:guid}")]
    public async Task<IActionResult> Get(Guid customerId, CancellationToken cancellationToken)
    {
        var result = await directoryService.GetCustomerCardAsync(customerId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.PriceResolve)]
    [HttpGet("{customerId:guid}/price-context")]
    public async Task<IActionResult> PriceContext(
        Guid customerId,
        [FromQuery] Guid? productId,
        [FromQuery] Guid? packagingId,
        CancellationToken cancellationToken)
    {
        var result = await pricingService.GetCustomerPriceContextAsync(
            customerId,
            productId,
            packagingId,
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.CustomerCreate)]
    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await salesCommandService.CreateCustomerAsync(
            request,
            CurrentActorId(),
            Request.Headers["Idempotency-Key"].ToString(),
            Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? HttpContext.TraceIdentifier,
            cancellationToken);
        return Created($"/api/v1/customers/{result.Id}", result);
    }

    [Authorize(Policy = PermissionPolicies.CustomerUpdate)]
    [HttpPost("{customerId:guid}/contacts")]
    public async Task<ActionResult<CustomerContactDto>> CreateContact(
        Guid customerId,
        [FromBody] CreateCustomerContactRequest request,
        CancellationToken cancellationToken)
    {
        var result = await directoryService.CreateContactAsync(
            customerId,
            request,
            CurrentActorId(),
            Request.Headers["Idempotency-Key"].ToString(),
            Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? HttpContext.TraceIdentifier,
            cancellationToken);
        return Created($"/api/v1/customers/{customerId}", result);
    }

    [Authorize(Policy = PermissionPolicies.CustomerUpdate)]
    [HttpPost("{customerId:guid}/price-group")]
    public async Task<IActionResult> AssignPriceGroup(
        Guid customerId,
        [FromBody] AssignCustomerPriceGroupRequest request,
        CancellationToken cancellationToken)
    {
        await pricingService.AssignCustomerPriceGroupAsync(
            customerId,
            request,
            CurrentActorId(),
            Request.Headers["Idempotency-Key"].ToString(),
            Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? HttpContext.TraceIdentifier,
            cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = PermissionPolicies.CustomerMessage)]
    [HttpGet("{customerId:guid}/outbound-emails")]
    public async Task<ActionResult<IReadOnlyCollection<CustomerOutboundEmailDto>>> ListEmails(
        Guid customerId,
        CancellationToken cancellationToken)
        => Ok(await directoryService.ListOutboundEmailsAsync(customerId, cancellationToken));

    [Authorize(Policy = PermissionPolicies.CustomerMessage)]
    [HttpPost("{customerId:guid}/outbound-emails")]
    public async Task<ActionResult<CustomerOutboundEmailDto>> SendEmail(
        Guid customerId,
        [FromBody] SendCustomerEmailRequest request,
        CancellationToken cancellationToken)
    {
        var result = await directoryService.SendOutboundEmailAsync(
            customerId,
            request,
            CurrentActorId(),
            Request.Headers["Idempotency-Key"].ToString(),
            Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? HttpContext.TraceIdentifier,
            cancellationToken);
        return Created($"/api/v1/customers/{customerId}/outbound-emails/{result.Id}", result);
    }

    private Guid CurrentActorId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var actorId)
            ? actorId
            : throw new UnauthorizedAccessException("Authenticated actor id is missing.");
    }
}
