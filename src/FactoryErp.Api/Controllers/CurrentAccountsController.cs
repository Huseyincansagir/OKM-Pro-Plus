using FactoryErp.Api.Authorization;
using FactoryErp.Application.Shipping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/current-accounts")]
public sealed class CurrentAccountsController(IShippingFinanceCommandService service)
    : ShippingFinanceControllerBase(service)
{
    [Authorize(Policy = PermissionPolicies.CurrentAccountRead)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<CurrentAccountDto>>> List(CancellationToken cancellationToken)
        => Ok(await Service.ListCurrentAccountsAsync(cancellationToken));

    [Authorize(Policy = PermissionPolicies.CurrentAccountRead)]
    [HttpGet("{customerId:guid}")]
    public async Task<IActionResult> Get(Guid customerId, CancellationToken cancellationToken)
    {
        var result = await Service.GetCurrentAccountAsync(customerId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
