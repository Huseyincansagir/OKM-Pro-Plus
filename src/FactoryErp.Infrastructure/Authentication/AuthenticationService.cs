using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FactoryErp.Application.Identity;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FactoryErp.Infrastructure.Authentication;

public sealed class AuthenticationService(
    FactoryErpDbContext dbContext,
    PasswordHasher passwordHasher,
    IOptions<AuthOptions> options) : IAuthenticationService
{
    private readonly AuthOptions _options = options.Value;

    public async Task<AuthTokens?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await FindUserAsync(request.UserName, cancellationToken);
        if (user is null || !user.IsActive || !passwordHasher.Verify(request.Password, user.PasswordHash ?? string.Empty))
        {
            return null;
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthTokens?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashRefreshToken(refreshToken);
        var token = await dbContext.RefreshTokens
            .Include(x => x.User)
                .ThenInclude(x => x.UserRoles)
                    .ThenInclude(x => x.Role)
                        .ThenInclude(x => x.RolePermissions)
                            .ThenInclude(x => x.Permission)
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (token is null || token.RevokedAt is not null || token.ExpiresAt <= DateTimeOffset.UtcNow || !token.User.IsActive)
        {
            return null;
        }

        token.RevokedAt = DateTimeOffset.UtcNow;
        return await IssueTokensAsync(token.User, cancellationToken);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var tokenHash = HashRefreshToken(refreshToken);
        var token = await dbContext.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        if (token is null || token.RevokedAt is not null)
        {
            return;
        }

        token.RevokedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<UserRecord?> FindUserAsync(string userName, CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                    .ThenInclude(x => x.RolePermissions)
                        .ThenInclude(x => x.Permission)
            .SingleOrDefaultAsync(x => x.UserName == userName, cancellationToken);
    }

    private async Task<AuthTokens> IssueTokensAsync(UserRecord user, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var accessExpiresAt = now.AddMinutes(_options.AccessTokenMinutes);
        var refreshExpiresAt = now.AddDays(_options.RefreshTokenDays);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

        dbContext.RefreshTokens.Add(new RefreshTokenRecord
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashRefreshToken(refreshToken),
            CreatedAt = now,
            ExpiresAt = refreshExpiresAt,
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var summary = BuildSummary(user);
        return new AuthTokens(
            CreateAccessToken(user, summary, accessExpiresAt),
            accessExpiresAt,
            refreshToken,
            refreshExpiresAt,
            summary);
    }

    private string CreateAccessToken(UserRecord user, UserSummary summary, DateTimeOffset expiresAt)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("preferred_username", user.UserName),
        };

        claims.AddRange(summary.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(summary.Permissions.Select(permission => new Claim("permission", permission)));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserSummary BuildSummary(UserRecord user)
    {
        var roles = user.UserRoles
            .Where(x => x.Role.IsActive)
            .Select(x => x.Role.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var permissions = user.UserRoles
            .Where(x => x.Role.IsActive)
            .SelectMany(x => x.Role.RolePermissions)
            .Where(x => x.Permission.IsActive)
            .Select(x => x.Permission.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new UserSummary(user.Id, user.UserName, user.DisplayName, roles, permissions, user.RowVersion);
    }

    private static string HashRefreshToken(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
