using System.Security.Claims;
using FactoryErp.Api.Authorization;
using FactoryErp.Application.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/customers")]
public sealed class CustomersController(ISalesCommandService salesCommandService) : ControllerBase
{
    [Authorize(Policy = PermissionPolicies.CustomerRead)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<CustomerDto>>> List(CancellationToken cancellationToken)
        => Ok(await salesCommandService.ListCustomersAsync(cancellationToken));

    [Authorize(Policy = PermissionPolicies.CustomerRead)]
    [HttpGet("{customerId:guid}")]
    public async Task<IActionResult> Get(Guid customerId, CancellationToken cancellationToken)
    {
        var result = await salesCommandService.GetCustomerAsync(customerId, cancellationToken);
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

    private Guid CurrentActorId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var actorId)
            ? actorId
            : throw new UnauthorizedAccessException("Authenticated actor id is missing.");
    }
}
