using FactoryErp.Application.Abstractions.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;

namespace FactoryErp.Infrastructure.Persistence;

public sealed class EfAuditWriter(FactoryErpDbContext dbContext) : IAuditWriter
{
    public Task AppendAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        dbContext.AuditLogs.Add(new AuditLogRecord
        {
            Id = Guid.NewGuid(),
            OccurredAt = DateTimeOffset.UtcNow,
            UserId = entry.UserId,
            Action = entry.Action,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            CorrelationId = entry.CorrelationId,
            BeforeJson = entry.BeforeJson,
            AfterJson = entry.AfterJson,
        });

        return Task.CompletedTask;
    }
}
