using FactoryErp.Api.Authorization;
using FactoryErp.Application.Shipping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/payments")]
public sealed class PaymentsController(IShippingFinanceCommandService service)
    : ShippingFinanceControllerBase(service)
{
    [Authorize(Policy = PermissionPolicies.PaymentApply)]
    [HttpPost]
    public async Task<ActionResult<PaymentDto>> Apply(
        [FromBody] ApplyPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Service.ApplyPaymentAsync(request, ActorId(), IdempotencyKey(), CorrelationId(), cancellationToken);
        return Ok(result);
    }
}
