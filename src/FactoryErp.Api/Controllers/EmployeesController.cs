using System.Security.Claims;
using FactoryErp.Api.Authorization;
using FactoryErp.Application.Hr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/employees")]
public sealed class EmployeesController(IEmployeeDirectoryService employees) : ControllerBase
{
    [Authorize(Policy = PermissionPolicies.EmployeeRead)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<EmployeeDto>>> List(CancellationToken cancellationToken)
        => Ok(await employees.ListAsync(cancellationToken));

    [Authorize(Policy = PermissionPolicies.EmployeeRead)]
    [HttpGet("{employeeId:guid}")]
    public async Task<IActionResult> Get(Guid employeeId, CancellationToken cancellationToken)
    {
        var result = await employees.GetAsync(employeeId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = PermissionPolicies.EmployeeCreate)]
    [HttpPost]
    public async Task<ActionResult<EmployeeDto>> Create(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await employees.CreateAsync(request, ActorId(), IdempotencyKey(), HttpContext.TraceIdentifier, cancellationToken);
        return Created($"/api/v1/employees/{result.Id}", result);
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
