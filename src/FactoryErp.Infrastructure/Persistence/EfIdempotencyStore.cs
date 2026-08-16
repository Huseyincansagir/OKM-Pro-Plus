using FactoryErp.Application.Abstractions.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.Persistence;

public sealed class EfIdempotencyStore(FactoryErpDbContext dbContext) : IIdempotencyStore
{
    public async Task<StoredIdempotencyResult?> FindAsync(
        string scope,
        string key,
        CancellationToken cancellationToken = default)
    {
        var record = await dbContext.IdempotencyRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Scope == scope && x.Key == key, cancellationToken);

        return record is null
            ? null
            : new StoredIdempotencyResult(record.PayloadHash, record.ResponseStatusCode, record.ResponseBody ?? string.Empty);
    }

    public async Task SaveAsync(
        string scope,
        string key,
        string payloadHash,
        int statusCode,
        string responseBody,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        dbContext.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Id = Guid.NewGuid(),
            Scope = scope,
            Key = key,
            PayloadHash = payloadHash,
            ResponseStatusCode = statusCode,
            ResponseBody = responseBody,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
