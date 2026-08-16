namespace FactoryErp.Domain.Common;

public abstract class AggregateRoot : Entity
{
    protected AggregateRoot(Guid id, DateTimeOffset now)
        : base(id, now)
    {
    }
}
