using System.Security.Claims;
using FactoryErp.Application.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FactoryErp.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    FactoryErp.Application.Identity.IAuthenticationService authenticationService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Error(
                "Geçersiz istek",
                "Kullanıcı adı ve parola zorunludur.",
                StatusCodes.Status400BadRequest,
                "https://erp.local/problems/invalid-request",
                "INVALID_REQUEST");
        }

        var tokens = await authenticationService.LoginAsync(request, cancellationToken);
        return tokens is null
            ? Error(
                "Kimlik doğrulanamadı",
                "Kullanıcı adı veya parola geçersiz.",
                StatusCodes.Status401Unauthorized,
                "https://erp.local/problems/unauthenticated",
                "UNAUTHENTICATED")
            : Ok(tokens);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Error(
                "Geçersiz refresh token",
                "Refresh token zorunludur.",
                StatusCodes.Status401Unauthorized,
                "https://erp.local/problems/token-expired",
                "TOKEN_EXPIRED");
        }

        var tokens = await authenticationService.RefreshAsync(request.RefreshToken, cancellationToken);
        return tokens is null
            ? Error(
                "Refresh token geçersiz",
                "Oturum süresi dolmuş veya iptal edilmiş.",
                StatusCodes.Status401Unauthorized,
                "https://erp.local/problems/token-expired",
                "TOKEN_EXPIRED")
            : Ok(tokens);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await authenticationService.LogoutAsync(request.RefreshToken, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).Distinct().Order().ToArray();
        var permissions = User.FindAll("permission").Select(claim => claim.Value).Distinct().Order().ToArray();

        return Ok(new
        {
            user = new
            {
                id = userId,
                userName = User.FindFirstValue("preferred_username"),
                displayName = User.Identity?.Name,
                roles,
                permissions,
            },
            company = new { code = "default", name = "Factory ERP" },
            permissionVersion = "g2",
        });
    }

    private ObjectResult Error(string title, string detail, int status, string type, string code)
    {
        var problem = new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = status,
            Type = type,
            Instance = HttpContext.Request.Path,
        };
        problem.Extensions["code"] = code;
        problem.Extensions["requestId"] = HttpContext.TraceIdentifier;
        problem.Extensions["correlationId"] = HttpContext.Response.Headers["X-Correlation-Id"].FirstOrDefault();
        problem.Extensions["retryable"] = false;
        return StatusCode(status, problem);
    }
}
