using System.Security.Claims;
using FactoryErp.Api.Authorization;
using FactoryErp.Application.Warehouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/stock-counts")]
public sealed class StockCountsController(IStockCountCommandService stockCounts) : ControllerBase
{
    [Authorize(Policy = PermissionPolicies.StockCountRead)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<StockCountDto>>> List(CancellationToken cancellationToken)
        => Ok(await stockCounts.ListAsync(cancellationToken));

    [Authorize(Policy = PermissionPolicies.StockCountRead)]
    [HttpGet("{countId:guid}")]
    public async Task<IActionResult> Get(Guid countId, CancellationToken cancellationToken)
    {
        var result = await stockCounts.GetAsync(countId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.StockCountManage)]
    [HttpPost]
    public async Task<ActionResult<StockCountDto>> Create(
        [FromBody] CreateStockCountRequest request,
        CancellationToken cancellationToken)
    {
        var result = await stockCounts.CreateAsync(request, ActorId(), IdempotencyKey(), HttpContext.TraceIdentifier, cancellationToken);
        return Created($"/api/v1/stock-counts/{result.Id}", result);
    }

    [Authorize(Policy = PermissionPolicies.StockCountManage)]
    [HttpPost("{countId:guid}/items")]
    public async Task<IActionResult> AddItem(
        Guid countId,
        [FromBody] AddStockCountItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await stockCounts.AddItemAsync(countId, request, ActorId(), IdempotencyKey(), HttpContext.TraceIdentifier, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.StockCountComplete)]
    [HttpPost("{countId:guid}/complete")]
    public async Task<IActionResult> Complete(Guid countId, CancellationToken cancellationToken)
    {
        var result = await stockCounts.CompleteAsync(countId, ActorId(), IdempotencyKey(), HttpContext.TraceIdentifier, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    private Guid ActorId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var actorId)
            ? actorId
            : throw new UnauthorizedAccessException("Authenticated actor id is missing.");
    }

    private string IdempotencyKey()
        => Request.Headers["Idempotency-Key"].ToString();
}
