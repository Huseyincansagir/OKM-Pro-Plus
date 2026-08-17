using FactoryErp.Api.Authorization;
using FactoryErp.Application.Shipping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/physical-logistics")]
public sealed class PhysicalLogisticsController(IPhysicalLogisticsCommandService service) : LogisticsControllerBase
{
    [Authorize(Policy = PermissionPolicies.PhysicalProfileManage)]
    [HttpPost("products/{productId:guid}/profiles")]
    public async Task<ActionResult<ProductPhysicalProfileDto>> CreateProductProfile(Guid productId, [FromBody] CreateProductPhysicalProfileRequest request, CancellationToken cancellationToken)
    {
        if (request.ProductId != productId) throw new ArgumentException("ProductId path ile eşleşmelidir.");
        var result = await service.CreateProductProfileAsync(request, ActorId(), IdempotencyKey(), CorrelationId(), cancellationToken);
        return Created($"/api/v1/physical-logistics/products/{productId}/profiles/{result.Id}", result);
    }

    [Authorize(Policy = PermissionPolicies.PhysicalProfileRead)]
    [HttpGet("products/{productId:guid}/profile")]
    public async Task<IActionResult> GetProductProfile(Guid productId, [FromQuery] DateTimeOffset? asOf, CancellationToken cancellationToken)
    {
        var result = await service.GetProductProfileAsync(productId, asOf ?? DateTimeOffset.UtcNow, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.PhysicalProfileManage)]
    [HttpPost("packagings/{packagingId:guid}/profiles")]
    public async Task<ActionResult<PackagingPhysicalProfileDto>> CreatePackagingProfile(Guid packagingId, [FromBody] CreatePackagingPhysicalProfileRequest request, CancellationToken cancellationToken)
    {
        if (request.PackagingId != packagingId) throw new ArgumentException("PackagingId path ile eşleşmelidir.");
        var result = await service.CreatePackagingProfileAsync(request, ActorId(), IdempotencyKey(), CorrelationId(), cancellationToken);
        return Created($"/api/v1/physical-logistics/packagings/{packagingId}/profiles/{result.Id}", result);
    }

    [Authorize(Policy = PermissionPolicies.PhysicalProfileRead)]
    [HttpGet("packagings/{packagingId:guid}/profile")]
    public async Task<IActionResult> GetPackagingProfile(Guid packagingId, [FromQuery] DateTimeOffset? asOf, CancellationToken cancellationToken)
    {
        var result = await service.GetPackagingProfileAsync(packagingId, asOf ?? DateTimeOffset.UtcNow, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.PalletTypeManage)]
    [HttpPost("pallet-types")]
    public async Task<ActionResult<PalletTypeDto>> CreatePalletType([FromBody] CreatePalletTypeRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreatePalletTypeAsync(request, ActorId(), IdempotencyKey(), CorrelationId(), cancellationToken);
        return Created($"/api/v1/physical-logistics/pallet-types/{result.Id}", result);
    }

    [Authorize(Policy = PermissionPolicies.PalletTypeRead)]
    [HttpGet("pallet-types/{id:guid}")]
    public async Task<IActionResult> GetPalletType(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetPalletTypeAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
