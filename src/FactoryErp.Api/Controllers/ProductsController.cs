using FactoryErp.Api.Authorization;
using FactoryErp.Application.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/products")]
public sealed class ProductsController(IProductCatalogService productCatalogService) : ControllerBase
{
    [Authorize(Policy = PermissionPolicies.ProductRead)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<StaffProductDto>>> List(CancellationToken cancellationToken)
        => Ok(await productCatalogService.ListStaffProductsAsync(cancellationToken));

    [Authorize(Policy = PermissionPolicies.ProductRead)]
    [HttpGet("{productId:guid}")]
    public async Task<IActionResult> Get(Guid productId, CancellationToken cancellationToken)
    {
        var result = await productCatalogService.GetStaffProductAsync(productId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
