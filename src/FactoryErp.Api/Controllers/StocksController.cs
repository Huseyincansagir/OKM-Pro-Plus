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

    [Authorize(Policy = PermissionPolicies.StockRead)]
    [HttpGet("{warehouseId:guid}/locations")]
    public async Task<IActionResult> ListLocations(Guid warehouseId, CancellationToken cancellationToken)
    {
        var result = await stockQueryService.ListWarehouseLocationsAsync(warehouseId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
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

[ApiController]
[Authorize]
[Route("api/v1/stock-movements")]
public sealed class StockMovementsController(IStockQueryService stockQueryService) : ControllerBase
{
    [Authorize(Policy = PermissionPolicies.StockRead)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<StockMovementRowDto>>> List(CancellationToken cancellationToken)
        => Ok(await stockQueryService.ListStockMovementsAsync(cancellationToken));

    [Authorize(Policy = PermissionPolicies.StockRead)]
    [HttpGet("{movementId:guid}")]
    public async Task<IActionResult> Get(Guid movementId, CancellationToken cancellationToken)
    {
        var result = await stockQueryService.GetStockMovementAsync(movementId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
