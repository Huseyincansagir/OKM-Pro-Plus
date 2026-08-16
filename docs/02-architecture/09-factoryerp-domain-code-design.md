# Factory ERP — FactoryErp.Domain Kod Tasarımı

**Aşama:** ARCHITECTURE → implementation handoff

**Durum:** Kod tasarımı ve blueprint; production source tree değildir.

**Gate:** `design/implementation-ready.md` hâlâ `IMPLEMENTATION: NOT READY` olduğu için aşağıdaki C# blokları henüz `src/` altına taşınmayacaktır.

## 1. Tasarım ilkeleri

`FactoryErp.Domain` yalnızca iş kurallarını ve aggregate invariant’larını taşır. ASP.NET Core, EF Core, PostgreSQL, MediatR, logging, HTTP ve Docker referansları Domain projesine eklenmez.

Domain entity’leri şu sorumluluklara sahiptir:

- Geçerli state transition’ı korumak.
- Negatif veya precision dışı miktarı reddetmek.
- Allocation üst sınırını kendi kaynak bağlamında kontrol etmek.
- Reversal/credit gibi düzeltmeleri yeni kayıt olarak modellemek.
- Dış katmana domain event veya typed error üretmek.

Domain entity’si database projection alanlarını yeniden hesaplayabilir; fakat concurrency, row lock ve transaction orchestration Application/Infrastructure katmanında kalır.

## 2. Önerilen Domain klasörleri

```text
FactoryErp.Domain/
├─ Common/
│  ├─ Entity.cs
│  ├─ AggregateRoot.cs
│  ├─ DomainEvent.cs
│  ├─ DomainError.cs
│  ├─ DomainException.cs
│  └─ Result.cs
├─ Shared/
│  ├─ Money.cs
│  ├─ Quantity.cs
│  ├─ UomCode.cs
│  ├─ PackagingSnapshot.cs
│  └─ DocumentNumber.cs
├─ Products/
│  ├─ Product.cs
│  ├─ ProductPackaging.cs
│  └─ ProductBarcode.cs
├─ Sales/
│  ├─ SalesOrder.cs
│  ├─ SalesOrderItem.cs
│  ├─ SalesOrderStatus.cs
│  └─ Events/
├─ Shipping/
│  ├─ DeliveryNote.cs
│  ├─ DeliveryNoteItem.cs
│  ├─ DeliveryNoteItemAllocation.cs
│  ├─ Shipment.cs
│  ├─ LoadPlan.cs
│  └─ VehicleFitEvaluation.cs
└─ Invoicing/
   ├─ Invoice.cs
   ├─ InvoiceItem.cs
   └─ InvoiceItemAllocation.cs
```

## 3. Common entity ve result tasarımı

```csharp
namespace FactoryErp.Domain.Common;

public abstract class Entity
{
    private readonly List<DomainEvent> _domainEvents = [];

    public Guid Id { get; protected init; }
    public DateTimeOffset CreatedAt { get; protected init; }
    public DateTimeOffset UpdatedAt { get; protected set; }
    public long RowVersion { get; protected set; } = 1;

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents;

    protected Entity(Guid id, DateTimeOffset now)
    {
        Id = id;
        CreatedAt = now;
        UpdatedAt = now;
    }

    protected void AddDomainEvent(DomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);

    public IReadOnlyCollection<DomainEvent> DequeueDomainEvents()
    {
        var events = _domainEvents.ToArray();
        _domainEvents.Clear();
        return events;
    }
}

public abstract class AggregateRoot : Entity
{
    protected AggregateRoot(Guid id, DateTimeOffset now) : base(id, now) { }
}

public abstract record DomainEvent(
    Guid EventId,
    DateTimeOffset OccurredAt);

public sealed record DomainError(
    string Code,
    string Message,
    IReadOnlyDictionary<string, object?>? Metadata = null);

public sealed class DomainException : Exception
{
    public DomainError Error { get; }

    public DomainException(DomainError error)
        : base(error.Message)
    {
        Error = error;
    }
}
```

Production mapping’de `CreatedAt`, `UpdatedAt` ve `RowVersion` infrastructure tarafından set edilir; domain invariant’ı bozan bir değer entity method’uyla atanamaz.

## 4. Quantity value object

Quantity, UI’daki görünümden bağımsız olarak temel UOM miktarını temsil eder. Ambalaj snapshot’ı ayrı value object’tir.

```csharp
namespace FactoryErp.Domain.Shared;

public readonly record struct Quantity
{
    public decimal BaseValue { get; }
    public int Scale { get; }

    private Quantity(decimal baseValue, int scale)
    {
        BaseValue = baseValue;
        Scale = scale;
    }

    public static Quantity Create(decimal value, int scale)
    {
        if (scale is < 0 or > 6)
            throw new DomainException(new(
                "UOM_SCALE_INVALID",
                "UOM precision 0 ile 6 arasında olmalıdır."));

        if (value <= 0)
            throw new DomainException(new(
                "QUANTITY_MUST_BE_POSITIVE",
                "Miktar sıfırdan büyük olmalıdır."));

        if (decimal.Round(value, scale) != value)
            throw new DomainException(new(
                "QUANTITY_PRECISION_EXCEEDED",
                "Miktar UOM precision sınırını aşıyor.",
                new Dictionary<string, object?>
                {
                    ["value"] = value,
                    ["scale"] = scale
                }));

        return new Quantity(value, scale);
    }

    public Quantity Add(Quantity other)
    {
        var scale = Math.Max(Scale, other.Scale);
        return Create(BaseValue + other.BaseValue, scale);
    }

    public Quantity Subtract(Quantity other)
    {
        var result = BaseValue - other.BaseValue;
        return Create(result, Math.Max(Scale, other.Scale));
    }

    public bool IsGreaterThan(Quantity other) => BaseValue > other.BaseValue;
    public static bool operator <=(Quantity left, Quantity right)
        => left.BaseValue <= right.BaseValue;
    public static bool operator >=(Quantity left, Quantity right)
        => left.BaseValue >= right.BaseValue;
}
```

`Quantity.Create` içindeki negative sonucu `QUANTITY_MUST_BE_POSITIVE` verir. Reversal hesaplarında negatif kayıt oluşturulmaz; reversal ayrı positive movement ve `reversed_from_id` ile modellenir.

## 5. PackagingSnapshot value object

```csharp
public sealed record PackagingSnapshot(
    Guid? PackagingId,
    string Level,
    string Name,
    string BaseUomCode,
    decimal QuantityInBaseUom,
    bool AllowPartial,
    string EffectiveVersion)
{
    public Quantity ToBaseQuantity(decimal enteredQuantity, int baseScale)
    {
        if (enteredQuantity <= 0)
            throw new DomainException(new(
                "QUANTITY_MUST_BE_POSITIVE",
                "Girilen miktar sıfırdan büyük olmalıdır."));

        if (!AllowPartial && decimal.Truncate(enteredQuantity) != enteredQuantity)
            throw new DomainException(new(
                "PACKAGING_PARTIAL_NOT_ALLOWED",
                "Bu ambalaj seviyesi parçalı kullanılamaz."));

        var baseValue = enteredQuantity * QuantityInBaseUom;
        return Quantity.Create(baseValue, baseScale);
    }
}

public sealed record QuantitySnapshot(
    decimal EnteredQuantity,
    Guid? EnteredPackagingId,
    Quantity QuantityBase,
    string BaseUomCode,
    PackagingSnapshot Packaging,
    string ViewModeAtEntry,
    IReadOnlyDictionary<string, decimal>? Breakdown);
```

Snapshot immutable intent taşır. `ProductPackaging.quantity_in_base_uom` değiştiğinde geçmiş snapshot yeniden yorumlanmaz.

## 6. SalesOrder aggregate

```csharp
public enum SalesOrderStatus
{
    Draft,
    PendingApproval,
    Approved,
    Preparing,
    PartiallyShipped,
    Fulfilled,
    Completed,
    Cancelled
}

public sealed class SalesOrder : AggregateRoot
{
    private readonly List<SalesOrderItem> _items = [];

    public Guid CustomerId { get; private set; }
    public SalesOrderStatus Status { get; private set; } = SalesOrderStatus.Draft;
    public string CurrencyCode { get; private set; } = "TRY";
    public IReadOnlyCollection<SalesOrderItem> Items => _items;

    private SalesOrder(Guid id, Guid customerId, DateTimeOffset now)
        : base(id, now)
    {
        CustomerId = customerId;
    }

    public static SalesOrder Create(Guid id, Guid customerId, DateTimeOffset now)
    {
        if (customerId == Guid.Empty)
            throw new DomainException(new("CUSTOMER_REQUIRED", "Müşteri zorunludur."));

        return new SalesOrder(id, customerId, now);
    }

    public void AddItem(SalesOrderItem item)
    {
        if (Status != SalesOrderStatus.Draft)
            throw new DomainException(new(
                "ORDER_NOT_EDITABLE",
                "Taslak olmayan siparişe kalem eklenemez."));

        _items.Add(item);
    }

    public void Submit()
    {
        if (Status != SalesOrderStatus.Draft || _items.Count == 0)
            throw new DomainException(new(
                "INVALID_ORDER_SUBMISSION",
                "Kalemsiz veya taslak olmayan sipariş gönderilemez."));

        Status = SalesOrderStatus.PendingApproval;
        AddDomainEvent(new SalesOrderSubmitted(Id, DateTimeOffset.UtcNow));
    }

    public void Approve(Guid actorId)
    {
        if (Status != SalesOrderStatus.PendingApproval)
            throw new DomainException(new(
                "STATE_TRANSITION_CONFLICT",
                "Sipariş yalnızca onay beklerken onaylanabilir."));

        Status = SalesOrderStatus.Approved;
        AddDomainEvent(new SalesOrderApproved(Id, actorId, DateTimeOffset.UtcNow));
    }

    public void RecordShipment(Quantity shippedTotal)
    {
        if (Status is not (SalesOrderStatus.Approved
            or SalesOrderStatus.Preparing
            or SalesOrderStatus.PartiallyShipped))
            throw new DomainException(new(
                "ORDER_NOT_SHIPPABLE",
                "Sipariş mevcut state’inde sevk edilemez."));

        if (Items.All(x => x.RemainingQtyBase.BaseValue == 0))
            Status = SalesOrderStatus.Fulfilled;
        else
            Status = SalesOrderStatus.PartiallyShipped;
    }
}

public sealed record SalesOrderSubmitted(
    Guid OrderId,
    DateTimeOffset OccurredAt) : DomainEvent(Guid.NewGuid(), OccurredAt);

public sealed record SalesOrderApproved(
    Guid OrderId,
    Guid ActorId,
    DateTimeOffset OccurredAt) : DomainEvent(Guid.NewGuid(), OccurredAt);
```

## 7. SalesOrderItem miktar invariant’ı

```csharp
public sealed class SalesOrderItem : Entity
{
    public Guid ProductId { get; private set; }
    public Quantity OrderedQtyBase { get; private set; }
    public Quantity ReservedQtyBase { get; private set; }
    public Quantity? ShippedQtyBase { get; private set; }
    public Quantity? CancelledQtyBase { get; private set; }
    public Quantity? RemainingQtyBase { get; private set; }
    public bool PartialDeliveryAllowed { get; private set; }
    public QuantitySnapshot OrderedSnapshot { get; private set; }

    private SalesOrderItem(
        Guid id,
        Guid productId,
        QuantitySnapshot orderedSnapshot,
        bool partialDeliveryAllowed,
        DateTimeOffset now)
        : base(id, now)
    {
        ProductId = productId;
        OrderedSnapshot = orderedSnapshot;
        OrderedQtyBase = orderedSnapshot.QuantityBase;
        ReservedQtyBase = Quantity.Create(0.000001m, orderedSnapshot.QuantityBase.Scale);
        ShippedQtyBase = null;
        CancelledQtyBase = null;
        RemainingQtyBase = orderedSnapshot.QuantityBase;
        PartialDeliveryAllowed = partialDeliveryAllowed;
    }

    public static SalesOrderItem Create(
        Guid id,
        Guid productId,
        QuantitySnapshot orderedSnapshot,
        bool partialDeliveryAllowed,
        DateTimeOffset now)
    {
        if (productId == Guid.Empty)
            throw new DomainException(new("PRODUCT_REQUIRED", "Ürün zorunludur."));

        return new SalesOrderItem(
            id, productId, orderedSnapshot, partialDeliveryAllowed, now);
    }

    public Quantity GetShipLimit()
        => RemainingQtyBase ?? throw new InvalidOperationException("Remaining quantity missing.");

    public void ApplyShipment(Quantity quantity)
    {
        if (!PartialDeliveryAllowed && quantity.BaseValue != GetShipLimit().BaseValue)
            throw new DomainException(new(
                "PARTIAL_DELIVERY_NOT_ALLOWED",
                "Bu sipariş kalemi kısmi sevke izin vermiyor."));

        if (quantity > GetShipLimit())
            throw new DomainException(new(
                "OVER_SHIPMENT",
                "Sevk miktarı kalan sipariş miktarını aşamaz."));

        var shipped = ShippedQtyBase is null
            ? quantity
            : ShippedQtyBase.Value.Add(quantity);
        var remaining = Quantity.Create(
            OrderedQtyBase.BaseValue
            - shipped.BaseValue
            - (CancelledQtyBase?.BaseValue ?? 0),
            OrderedQtyBase.Scale);

        ShippedQtyBase = shipped;
        RemainingQtyBase = remaining;
    }
}
```

Not: `Quantity` value object’inin sıfırı temsil etme ihtiyacı olduğu için production tasarımında `Quantity` ile `NonNegativeQuantity` ayrımı veya `decimal` projection alanı ayrıca kararlaştırılmalıdır. Yukarıdaki snippet kavramsal tasarımdır; gerçek implementation’da bu ayrım açıkça çözülmelidir.

## 8. DeliveryNoteItem ve allocation

```csharp
public enum AllocationStatus
{
    Active,
    Reversed,
    Voided
}

public sealed class DeliveryNoteItemAllocation : Entity
{
    public Guid SalesOrderItemId { get; private set; }
    public Guid DeliveryNoteItemId { get; private set; }
    public Quantity QuantityBase { get; private set; }
    public AllocationStatus Status { get; private set; } = AllocationStatus.Active;
    public string IdempotencyKey { get; private set; }
    public string PayloadHash { get; private set; }
    public Guid? ReversedFromId { get; private set; }
    public string? ReversalReason { get; private set; }

    private DeliveryNoteItemAllocation(
        Guid id,
        Guid salesOrderItemId,
        Guid deliveryNoteItemId,
        Quantity quantityBase,
        string idempotencyKey,
        string payloadHash,
        DateTimeOffset now)
        : base(id, now)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new DomainException(new("IDEMPOTENCY_KEY_REQUIRED", "Idempotency key zorunludur."));

        if (string.IsNullOrWhiteSpace(payloadHash))
            throw new DomainException(new("PAYLOAD_HASH_REQUIRED", "Payload hash zorunludur."));

        SalesOrderItemId = salesOrderItemId;
        DeliveryNoteItemId = deliveryNoteItemId;
        QuantityBase = quantityBase;
        IdempotencyKey = idempotencyKey;
        PayloadHash = payloadHash;
    }

    public static DeliveryNoteItemAllocation Create(
        Guid id,
        Guid salesOrderItemId,
        Guid deliveryNoteItemId,
        Quantity quantityBase,
        string idempotencyKey,
        string payloadHash,
        DateTimeOffset now)
        => new(
            id,
            salesOrderItemId,
            deliveryNoteItemId,
            quantityBase,
            idempotencyKey,
            payloadHash,
            now);

    public void Reverse(Guid reversalId, string reason, DateTimeOffset now)
    {
        if (Status != AllocationStatus.Active)
            throw new DomainException(new(
                "ALLOCATION_NOT_ACTIVE",
                "Yalnızca aktif allocation terslenebilir."));

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException(new(
                "REVERSAL_REASON_REQUIRED",
                "Reversal gerekçesi zorunludur."));

        Status = AllocationStatus.Reversed;
        ReversedFromId = reversalId;
        ReversalReason = reason;
        UpdatedAt = now;
    }
}

public sealed class DeliveryNoteItem : Entity
{
    private readonly List<DeliveryNoteItemAllocation> _allocations = [];

    public Guid SalesOrderItemId { get; private set; }
    public Quantity PlannedQtyBase { get; private set; }
    public IReadOnlyCollection<DeliveryNoteItemAllocation> Allocations => _allocations;

    public Quantity ActiveAllocatedQty()
    {
        var total = _allocations
            .Where(x => x.Status == AllocationStatus.Active)
            .Select(x => x.QuantityBase.BaseValue)
            .DefaultIfEmpty(0)
            .Sum();

        // Production implementation must use a non-negative quantity type for zero totals.
        return Quantity.Create(Math.Max(total, 0.000001m), PlannedQtyBase.Scale);
    }

    public void AddAllocation(DeliveryNoteItemAllocation allocation, Quantity sourceRemaining)
    {
        if (allocation.QuantityBase > sourceRemaining)
            throw new DomainException(new(
                "OVER_ALLOCATION",
                "Allocation kaynak kalan miktarını aşamaz."));

        var newTotal = ActiveAllocatedQty().BaseValue + allocation.QuantityBase.BaseValue;
        if (newTotal > PlannedQtyBase.BaseValue)
            throw new DomainException(new(
                "OVER_ALLOCATION",
                "Allocation toplamı planlanan miktarı aşamaz."));

        _allocations.Add(allocation);
    }
}
```

Allocation entity’si doğrudan controller’dan update edilmez. `IssueDeliveryNoteCommand`, `ReverseDeliveryNoteCommand` veya ilgili aggregate method’u aracılığıyla oluşturulur.

## 9. Invoice allocation tasarımı

```csharp
public sealed class InvoiceItemAllocation : Entity
{
    public Guid DeliveryNoteItemId { get; private set; }
    public Guid InvoiceItemId { get; private set; }
    public Quantity QuantityBase { get; private set; }
    public AllocationStatus Status { get; private set; } = AllocationStatus.Active;
    public string PriceSnapshotJson { get; private set; }
    public string TaxSnapshotJson { get; private set; }
    public Guid? CreditedFromId { get; private set; }
    public string? CreditReason { get; private set; }

    public bool IsActive => Status == AllocationStatus.Active;

    public void Credit(Guid creditId, string reason, DateTimeOffset now)
    {
        if (!IsActive)
            throw new DomainException(new(
                "INVOICE_ALLOCATION_NOT_ACTIVE",
                "Yalnızca aktif invoice allocation credit edilebilir."));

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException(new(
                "CREDIT_REASON_REQUIRED",
                "Credit gerekçesi zorunludur."));

        Status = AllocationStatus.Reversed;
        CreditedFromId = creditId;
        CreditReason = reason;
        UpdatedAt = now;
    }
}
```

`InvoiceItemAllocation` için source `DeliveryNoteItem` state’inin `Issued` olması Application handler ve persistence query tarafından birlikte doğrulanır. Domain, kendisine verilen source snapshot’ın issued/remaining invariant’ını kontrol eder; database lock ve transaction Application/Infrastructure katmanında kalır.

## 10. Domain code implementation checklist

Implementation gate açılmadan önce aşağıdaki noktalar netleştirilmelidir:

| Konu | Karar/iş |
|---|---|
| Zero quantity | `Quantity` veya `NonNegativeQuantity` value object ayrımı |
| Time | Domain event’ler fake clock üzerinden üretilecek |
| Error | `DomainException` → ProblemDetails mapping contract’ı |
| State | Her aggregate transition için exhaustive test |
| Snapshot | JSON serialization schema/version ve immutable type |
| Concurrency | Domain limit + EF row version + PostgreSQL lock üçlü davranışı |
| Events | Outbox mı, transaction sonrası notification mı |
| EF mapping | Private fields/backing collections ve owned/complex types |
| Aggregate loading | Allocation child graph’ının hangi command’da yüklenmesi |
| Reversal | Reversal entity/movement’in source record’dan ayrımı |

Bu belge implementation’a geçiş için temel sınıf tasarımını inceler; henüz `src/FactoryErp.Domain` altında üretim kodu oluşturulmadı.
