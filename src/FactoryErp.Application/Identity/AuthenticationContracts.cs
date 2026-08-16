namespace FactoryErp.Application.Identity;

public sealed record LoginRequest(string UserName, string Password);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record UserSummary(
    Guid Id,
    string UserName,
    string DisplayName,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions,
    long RowVersion);

public sealed record AuthTokens(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    UserSummary User);

public interface IAuthenticationService
{
    Task<AuthTokens?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<AuthTokens?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
}

public interface ICurrentUserAccessor
{
    Guid? UserId { get; }

    IReadOnlySet<string> Permissions { get; }
}
