using FactoryErp.Api.Authorization;
using FactoryErp.Application.Warehouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/warehouses")]
public sealed class WarehousesController(IStockQueryService stockQueryService) : ControllerBase
{
    [Authorize(Policy = PermissionPolicies.StockRead)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<WarehouseDto>>> List(CancellationToken cancellationToken)
        => Ok(await stockQueryService.ListWarehousesAsync(cancellationToken));
}

[ApiController]
[Authorize]
[Route("api/v1/stocks")]
public sealed class StocksController(IStockQueryService stockQueryService) : ControllerBase
{
    [Authorize(Policy = PermissionPolicies.StockRead)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<StockRowDto>>> List(CancellationToken cancellationToken)
        => Ok(await stockQueryService.ListStocksAsync(cancellationToken));

    [Authorize(Policy = PermissionPolicies.StockRead)]
    [HttpGet("{stockId:guid}")]
    public async Task<IActionResult> Get(Guid stockId, CancellationToken cancellationToken)
    {
        var result = await stockQueryService.GetStockAsync(stockId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
