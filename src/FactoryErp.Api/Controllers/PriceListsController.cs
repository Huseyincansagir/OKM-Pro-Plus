using System.Security.Claims;
using FactoryErp.Api.Authorization;
using FactoryErp.Application.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/price-lists")]
public sealed class PriceListsController(ISalesPricingService pricingService) : ControllerBase
{
    [Authorize(Policy = PermissionPolicies.PriceRead)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<PriceListDto>>> List(CancellationToken cancellationToken)
        => Ok(await pricingService.ListPriceListsAsync(cancellationToken));

    [Authorize(Policy = PermissionPolicies.PriceRead)]
    [HttpGet("{priceListId:guid}")]
    public async Task<IActionResult> Get(Guid priceListId, CancellationToken cancellationToken)
    {
        var result = await pricingService.GetPriceListAsync(priceListId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.PriceManage)]
    [HttpPost]
    public async Task<ActionResult<PriceListDto>> Create(
        [FromBody] CreatePriceListRequest request,
        CancellationToken cancellationToken)
    {
        var result = await pricingService.CreatePriceListAsync(
            request,
            CurrentActorId(),
            Request.Headers["Idempotency-Key"].ToString(),
            Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? HttpContext.TraceIdentifier,
            cancellationToken);
        return Created($"/api/v1/price-lists/{result.Id}", result);
    }

    private Guid CurrentActorId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var actorId)
            ? actorId
            : throw new UnauthorizedAccessException("Authenticated actor id is missing.");
    }
}

[ApiController]
[Authorize]
[Route("api/v1/products")]
public sealed class ProductPricesController(ISalesPricingService pricingService) : ControllerBase
{
    [Authorize(Policy = PermissionPolicies.PriceManage)]
    [HttpPost("{productId:guid}/prices")]
    public async Task<ActionResult<ProductPriceDto>> Create(
        Guid productId,
        [FromBody] CreateProductPriceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await pricingService.AddProductPriceAsync(
            productId,
            request,
            CurrentActorId(),
            Request.Headers["Idempotency-Key"].ToString(),
            Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? HttpContext.TraceIdentifier,
            cancellationToken);
        return Created($"/api/v1/price-lists/{result.PriceListId}", result);
    }

    private Guid CurrentActorId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var actorId)
            ? actorId
            : throw new UnauthorizedAccessException("Authenticated actor id is missing.");
    }
}

[ApiController]
[Authorize]
[Route("api/v1/customer-price-groups")]
public sealed class CustomerPriceGroupsController(ISalesPricingService pricingService) : ControllerBase
{
    [Authorize(Policy = PermissionPolicies.PriceRead)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<CustomerPriceGroupDto>>> List(CancellationToken cancellationToken)
        => Ok(await pricingService.ListCustomerPriceGroupsAsync(cancellationToken));

    [Authorize(Policy = PermissionPolicies.PriceManage)]
    [HttpPost]
    public async Task<ActionResult<CustomerPriceGroupDto>> Create(
        [FromBody] CreateCustomerPriceGroupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await pricingService.CreateCustomerPriceGroupAsync(
            request,
            CurrentActorId(),
            Request.Headers["Idempotency-Key"].ToString(),
            Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? HttpContext.TraceIdentifier,
            cancellationToken);
        return Created($"/api/v1/customer-price-groups/{result.Id}", result);
    }

    private Guid CurrentActorId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var actorId)
            ? actorId
            : throw new UnauthorizedAccessException("Authenticated actor id is missing.");
    }
}
