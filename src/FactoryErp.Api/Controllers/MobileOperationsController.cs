using FactoryErp.Api.Authorization;
using FactoryErp.Application.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/mobile")]
public sealed class MobileOperationsController(IProductCatalogService productCatalogService) : ControllerBase
{
    [Authorize(Policy = PermissionPolicies.BarcodeResolve)]
    [HttpPost("barcodes/resolve")]
    public async Task<IActionResult> ResolveBarcode([FromBody] BarcodeRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Barcode))
        {
            return Problem(
                title: "Barkod zorunludur",
                detail: "Okunan barkod değeri boş olamaz.",
                statusCode: StatusCodes.Status400BadRequest,
                type: "https://erp.local/problems/invalid-request");
        }

        var result = await productCatalogService.ResolveBarcodeAsync(request.Barcode.Trim(), cancellationToken);
        return result is null
            ? NotFound(new
            {
                type = "https://erp.local/problems/resource-not-found",
                title = "Barkod bulunamadı",
                status = StatusCodes.Status404NotFound,
                code = "RESOURCE_NOT_FOUND",
                requestId = HttpContext.TraceIdentifier,
            })
            : Ok(result);
    }

    [HttpPost("quantity-previews")]
    public async Task<IActionResult> PreviewQuantity(
        [FromBody] QuantityPreviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await productCatalogService.PreviewQuantityAsync(request, cancellationToken);
        return result is null
            ? NotFound(new
            {
                type = "https://erp.local/problems/resource-not-found",
                title = "Ürün veya ambalaj bulunamadı",
                status = StatusCodes.Status404NotFound,
                code = "RESOURCE_NOT_FOUND",
                requestId = HttpContext.TraceIdentifier,
            })
            : Ok(result);
    }

    public sealed record BarcodeRequest(string Barcode);
}
