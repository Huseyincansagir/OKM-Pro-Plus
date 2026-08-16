namespace FactoryErp.Application.Abstractions.Persistence;

public sealed record AuditEntry(
    string Action,
    string EntityType,
    Guid? EntityId,
    Guid? UserId,
    string CorrelationId,
    string? BeforeJson = null,
    string? AfterJson = null);

public interface IAuditWriter
{
    Task AppendAsync(AuditEntry entry, CancellationToken cancellationToken = default);
}
