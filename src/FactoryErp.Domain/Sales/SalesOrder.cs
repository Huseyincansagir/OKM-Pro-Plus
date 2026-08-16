using FactoryErp.Domain.Common;

namespace FactoryErp.Domain.Sales;

public sealed class SalesOrder : AggregateRoot
{
    private readonly List<SalesOrderItem> _items = [];

    private SalesOrder(Guid id, Guid customerId, DateTimeOffset now)
        : base(id, now)
    {
        DomainGuard.AgainstEmpty(customerId, "CUSTOMER_REQUIRED", "Müşteri kimliği zorunludur.");
        CustomerId = customerId;
        Status = SalesOrderStatus.Draft;
    }

    public Guid CustomerId { get; }
    public SalesOrderStatus Status { get; private set; }
    public IReadOnlyCollection<SalesOrderItem> Items => _items.AsReadOnly();

    public static SalesOrder Create(Guid id, Guid customerId, DateTimeOffset now)
        => new(id, customerId, now);

    public void AddItem(SalesOrderItem item, DateTimeOffset now)
    {
        if (Status != SalesOrderStatus.Draft)
        {
            throw new DomainException(new(
                "ORDER_NOT_EDITABLE",
                "Taslak olmayan siparişe kalem eklenemez."));
        }

        ArgumentNullException.ThrowIfNull(item);
        _items.Add(item);
        Touch(now);
    }

    public void Submit(DateTimeOffset now)
    {
        if (Status != SalesOrderStatus.Draft || _items.Count == 0)
        {
            throw new DomainException(new(
                "INVALID_ORDER_SUBMISSION",
                "Kalemsiz veya taslak olmayan sipariş gönderilemez."));
        }

        Status = SalesOrderStatus.PendingApproval;
        AddDomainEvent(new SalesOrderSubmitted(Id, now));
        Touch(now);
    }

    public void Approve(Guid actorId, DateTimeOffset now)
    {
        DomainGuard.AgainstEmpty(actorId, "APPROVER_REQUIRED", "Onaylayan kullanıcı zorunludur.");

        if (Status != SalesOrderStatus.PendingApproval)
        {
            throw new DomainException(new(
                "STATE_TRANSITION_CONFLICT",
                "Sipariş yalnızca onay beklerken onaylanabilir."));
        }

        Status = SalesOrderStatus.Approved;
        AddDomainEvent(new SalesOrderApproved(Id, actorId, now));
        Touch(now);
    }

    public void StartPreparing(DateTimeOffset now)
    {
        if (Status != SalesOrderStatus.Approved)
        {
            throw new DomainException(new(
                "ORDER_NOT_PREPARABLE",
                "Sipariş yalnızca onaydan sonra hazırlanmaya alınabilir."));
        }

        Status = SalesOrderStatus.Preparing;
        Touch(now);
    }

    public void RecordShipment(DateTimeOffset now)
    {
        if (Status is not (SalesOrderStatus.Approved
            or SalesOrderStatus.Preparing
            or SalesOrderStatus.PartiallyShipped))
        {
            throw new DomainException(new(
                "ORDER_NOT_SHIPPABLE",
                "Sipariş mevcut state'inde sevk edilemez."));
        }

        Status = _items.All(item => item.RemainingQuantity.BaseValue == 0)
            ? SalesOrderStatus.Fulfilled
            : SalesOrderStatus.PartiallyShipped;
        Touch(now);
    }
}

public sealed record SalesOrderSubmitted(
    Guid OrderId,
    DateTimeOffset OccurredAt) : DomainEvent(Guid.NewGuid(), OccurredAt);

public sealed record SalesOrderApproved(
    Guid OrderId,
    Guid ActorId,
    DateTimeOffset OccurredAt) : DomainEvent(Guid.NewGuid(), OccurredAt);
