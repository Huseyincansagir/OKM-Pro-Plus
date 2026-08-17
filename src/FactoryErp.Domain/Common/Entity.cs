namespace FactoryErp.Domain.Common;

public abstract class Entity
{
    private readonly List<DomainEvent> _domainEvents = [];

    protected Entity(Guid id, DateTimeOffset now)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException(new(
                "ENTITY_ID_REQUIRED",
                "Domain entity kimliği boş olamaz."));
        }

        Id = id;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public long RowVersion { get; private set; } = 1;
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Touch(DateTimeOffset now) => UpdatedAt = now;

    protected void RestoreUpdatedAt(DateTimeOffset updatedAt) => UpdatedAt = updatedAt;

    protected void AddDomainEvent(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public IReadOnlyCollection<DomainEvent> DequeueDomainEvents()
    {
        var events = _domainEvents.ToArray();
        _domainEvents.Clear();
        return events;
    }
}
