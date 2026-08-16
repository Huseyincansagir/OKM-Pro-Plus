namespace FactoryErp.Application.Abstractions.Persistence;

public sealed record StoredIdempotencyResult(
    string PayloadHash,
    int StatusCode,
    string ResponseBody);

public interface IIdempotencyStore
{
    Task<StoredIdempotencyResult?> FindAsync(
        string scope,
        string key,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        string scope,
        string key,
        string payloadHash,
        int statusCode,
        string responseBody,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);
}
