using FactoryErp.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Route("api/v1/system")]
public sealed class SystemController : ControllerBase
{
    [Authorize(Policy = PermissionPolicies.SystemRead)]
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "operational",
            service = "FactoryErp.Api",
            permission = "system.read",
            requestId = HttpContext.TraceIdentifier,
        });
    }
}
