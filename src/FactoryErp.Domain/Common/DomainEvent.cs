namespace FactoryErp.Domain.Common;

public abstract record DomainEvent(
    Guid EventId,
    DateTimeOffset OccurredAt);
