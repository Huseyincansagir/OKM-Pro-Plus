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
}
