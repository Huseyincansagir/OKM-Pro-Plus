using FactoryErp.Application.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/public/catalog")]
public sealed class PublicCatalogController(IProductCatalogService productCatalogService) : ControllerBase
{
    [HttpGet("products")]
    public async Task<ActionResult<ProductPage>> Products([FromQuery] ProductListQuery query, CancellationToken cancellationToken)
    {
        var result = await productCatalogService.GetPublicProductsAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("products/{slug}")]
    public async Task<IActionResult> Product(string slug, CancellationToken cancellationToken)
    {
        var result = await productCatalogService.GetPublicProductBySlugAsync(slug, cancellationToken);
        return result is null
            ? NotFound(new
            {
                type = "https://erp.local/problems/resource-not-found",
                title = "Ürün bulunamadı",
                status = StatusCodes.Status404NotFound,
                code = "RESOURCE_NOT_FOUND",
                requestId = HttpContext.TraceIdentifier,
            })
            : Ok(result);
    }
}
