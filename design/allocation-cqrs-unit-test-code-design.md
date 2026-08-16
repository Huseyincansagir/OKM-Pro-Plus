# Factory ERP — Allocation ve CQRS Transaction Unit Test Kod Tasarımı

**Aşama:** ARCHITECTURE → implementation handoff

**Durum:** Unit test code blueprint; production test project’i değildir.

**Gate:** `IMPLEMENTATION: READY FOR SCAFFOLD`. Kod blokları ilk Domain + test scaffold slice’ında `tests/` altına taşınabilir; API, EF migration ve diğer feature testleri ilk slice kanıtları tamamlanmadan açılmaz.

**Accepted ADR baseline:** Positive transaction quantity, zero-capable projection, private backing fields, row-lock/re-read, atomic CQRS transaction, domain event/outbox ayrımı ve typed conflict mapping zorunludur.

## 1. Test yaklaşımı

Allocation ve CQRS testleri iki ayrı sorumluluğu ayırır:

| Katman | Ne test edilir? |
|---|---|
| Domain unit | Quantity, snapshot, state, allocation upper bound, reversal ve error code |
| Application unit | Handler’ın doğru portları çağırması, transaction orchestration, idempotency ve response mapping |
| Persistence integration | PostgreSQL lock, trigger, check, FK, unique index ve rollback |
| API integration | HTTP status, ProblemDetails, permission, correlation/idempotency header |

Bu belge ilk iki katmanın örnek C# kodunu verir. Gerçek PostgreSQL concurrency davranışı unit test ile kanıtlanamaz; ayrıca integration test gerekir.

## 2. Test project tasarımı

```text
FactoryErp.Domain.UnitTests/
├─ Shared/
│  ├─ QuantityTests.cs
│  └─ PackagingSnapshotTests.cs
├─ Sales/
│  └─ SalesOrderItemTests.cs
├─ Shipping/
│  ├─ DeliveryNoteItemAllocationTests.cs
│  └─ IssueDeliveryNoteCommandTests.cs
└─ Invoicing/
   └─ InvoiceItemAllocationTests.cs

FactoryErp.Application.UnitTests/
├─ Shipping/
│  └─ IssueDeliveryNoteHandlerTests.cs
├─ Invoicing/
│  └─ IssueInvoiceHandlerTests.cs
└─ TestDoubles/
   ├─ FakeUnitOfWork.cs
   ├─ FakeIdempotencyStore.cs
   ├─ FakeStockLedger.cs
   ├─ FakeDeliveryNoteRepository.cs
   └─ FakeInvoiceRepository.cs
```

Önerilen framework seti xUnit veya NUnit, FluentAssertions benzeri assertion library ve handler portları için hand-written fake/mock’lardan oluşur. Test business rule’ları yalnızca mock interaction sayısına indirgenmez; state/result/event assertion’ı zorunludur.

## 3. Test fixture helper’ları

Aşağıdaki helper’lar test fixture’ının okunabilirliği için tasarlanmıştır:

```csharp
public static class QuantityFixture
{
    public static Quantity Base(decimal value, int scale = 3)
        => Quantity.Create(value, scale);

    public static PackagingSnapshot Case(
        decimal quantityInBase = 2_000,
        bool allowPartial = true)
        => new(
            PackagingId: Guid.NewGuid(),
            Level: "Case",
            Name: "Koli",
            BaseUomCode: "Piece",
            QuantityInBaseUom: quantityInBase,
            AllowPartial: allowPartial,
            EffectiveVersion: "v1");

    public static QuantitySnapshot EnteredCase(
        decimal entered = 5,
        decimal quantityInBase = 2_000)
    {
        var packaging = Case(quantityInBase);
        return new QuantitySnapshot(
            entered,
            packaging.PackagingId,
            packaging.ToBaseQuantity(entered, baseScale: 0),
            "Piece",
            packaging,
            "Packaging",
            Breakdown: null);
    }
}
```

ADR-001 ile `PositiveQuantity` ve `NonNegativeQuantity` ayrımı kabul edilmiştir. Production testleri sıfır toplamları `NonNegativeQuantity.Zero(scale)` ile, yeni allocation/movement girdilerini `PositiveQuantity` ile oluşturacaktır.

## 4. Quantity ve packaging unit testleri

```csharp
public sealed class PackagingSnapshotTests
{
    [Fact]
    public void Five_cases_are_converted_to_ten_thousand_pieces()
    {
        var packaging = QuantityFixture.Case(quantityInBase: 2_000);

        var result = packaging.ToBaseQuantity(5, baseScale: 0);

        result.BaseValue.Should().Be(10_000);
        result.Scale.Should().Be(0);
    }

    [Fact]
    public void Client_base_quantity_is_not_used_as_the_source_of_truth()
    {
        var packaging = QuantityFixture.Case(quantityInBase: 2_000);
        var serverQuantity = packaging.ToBaseQuantity(5, baseScale: 0);
        var clientQuantity = 9_999m;

        serverQuantity.BaseValue.Should().NotBe(clientQuantity);
        // Application handler maps this mismatch to QUANTITY_BASE_MISMATCH.
    }

    [Fact]
    public void Closed_packaging_rejects_fractional_entered_quantity()
    {
        var packaging = QuantityFixture.Case(
            quantityInBase: 2_000,
            allowPartial: false);

        var action = () => packaging.ToBaseQuantity(1.5m, baseScale: 0);

        action.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("PACKAGING_PARTIAL_NOT_ALLOWED");
    }

    [Fact]
    public void Precision_outside_uom_scale_is_rejected()
    {
        var action = () => Quantity.Create(1.2345m, scale: 3);

        action.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("QUANTITY_PRECISION_EXCEEDED");
    }
}
```

## 5. SalesOrderItem allocation/ship limit testleri

```csharp
public sealed class SalesOrderItemTests
{
    [Fact]
    public void Partial_shipment_reduces_remaining_quantity()
    {
        var ordered = QuantityFixture.EnteredCase(
            entered: 10,
            quantityInBase: 2_000);
        var item = SalesOrderItem.Create(
            id: Guid.NewGuid(),
            productId: Guid.NewGuid(),
            orderedSnapshot: ordered,
            partialDeliveryAllowed: true,
            now: DateTimeOffset.UtcNow);

        item.ApplyShipment(Quantity.Create(12_000, scale: 0));

        item.ShippedQtyBase!.Value.BaseValue.Should().Be(12_000);
        item.RemainingQtyBase!.Value.BaseValue.Should().Be(8_000);
    }

    [Fact]
    public void Shipment_above_remaining_quantity_is_rejected()
    {
        var item = SalesOrderItem.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            QuantityFixture.EnteredCase(entered: 5),
            partialDeliveryAllowed: true,
            DateTimeOffset.UtcNow);

        var action = () => item.ApplyShipment(Quantity.Create(10_001, 0));

        action.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("OVER_SHIPMENT");
    }

    [Fact]
    public void Non_partial_item_requires_full_remaining_quantity()
    {
        var item = SalesOrderItem.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            QuantityFixture.EnteredCase(entered: 5),
            partialDeliveryAllowed: false,
            DateTimeOffset.UtcNow);

        var action = () => item.ApplyShipment(Quantity.Create(2_000, 0));

        action.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("PARTIAL_DELIVERY_NOT_ALLOWED");
    }
}
```

## 6. DeliveryNoteItem allocation unit testleri

```csharp
public sealed class DeliveryNoteItemAllocationTests
{
    [Fact]
    public void Active_allocations_cannot_exceed_planned_quantity()
    {
        var item = DeliveryNoteItemFixture.Planned(10_000);
        var first = DeliveryNoteItemFixture.Allocation(6_000);
        var second = DeliveryNoteItemFixture.Allocation(4_000);

        item.AddAllocation(first, Quantity.Create(10_000, 0));
        item.AddAllocation(second, Quantity.Create(4_000, 0));

        item.ActiveAllocatedQty().BaseValue.Should().Be(10_000);
    }

    [Fact]
    public void Over_allocation_is_rejected_before_persistence()
    {
        var item = DeliveryNoteItemFixture.Planned(10_000);
        item.AddAllocation(
            DeliveryNoteItemFixture.Allocation(8_000),
            Quantity.Create(10_000, 0));

        var action = () => item.AddAllocation(
            DeliveryNoteItemFixture.Allocation(2_001),
            Quantity.Create(2_000, 0));

        action.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("OVER_ALLOCATION");
    }

    [Fact]
    public void Reversal_requires_reason_and_does_not_delete_source_allocation()
    {
        var allocation = DeliveryNoteItemFixture.Allocation(2_000);

        var action = () => allocation.Reverse(
            reversalId: Guid.NewGuid(),
            reason: "Yanlış depo seçildi",
            now: DateTimeOffset.UtcNow);

        action.Should().NotThrow();
        allocation.Status.Should().Be(AllocationStatus.Reversed);
        allocation.ReversalReason.Should().Be("Yanlış depo seçildi");
    }

    [Fact]
    public void Inactive_allocation_cannot_be_reversed_twice()
    {
        var allocation = DeliveryNoteItemFixture.Allocation(2_000);
        allocation.Reverse(Guid.NewGuid(), "İlk reversal", DateTimeOffset.UtcNow);

        var action = () => allocation.Reverse(
            Guid.NewGuid(),
            "İkinci reversal",
            DateTimeOffset.UtcNow);

        action.Should().Throw<DomainException>()
            .Which.Error.Code.Should().Be("ALLOCATION_NOT_ACTIVE");
    }
}
```

## 7. Fake ports for handler tests

Handler unit testinde database lock veya PostgreSQL trigger taklit edilmez. Bunun yerine port davranışı kontrollü fake ile verilir; gerçek lock/trigger integration testinde çalıştırılır.

```csharp
public sealed class FakeIdempotencyStore : IIdempotencyStore
{
    private readonly Dictionary<string, StoredIdempotencyResult> _items = [];

    public Task<StoredIdempotencyResult?> TryGetAsync(
        string endpoint,
        string key,
        CancellationToken cancellationToken)
    {
        _items.TryGetValue($"{endpoint}:{key}", out var value);
        return Task.FromResult(value);
    }

    public Task StoreAsync<T>(
        string endpoint,
        string key,
        string payloadHash,
        T result,
        CancellationToken cancellationToken)
    {
        _items[$"{endpoint}:{key}"] =
            StoredIdempotencyResult.From(payloadHash, result!);
        return Task.CompletedTask;
    }
}

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }
    public int CommitCount { get; private set; }
    public int RollbackCount { get; private set; }

    public Task BeginTransactionAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.FromResult(1);
    }

    public Task CommitAsync(CancellationToken cancellationToken)
    {
        CommitCount++;
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken)
    {
        RollbackCount++;
        return Task.CompletedTask;
    }
}
```

## 8. IssueDeliveryNote handler testleri

```csharp
public sealed class IssueDeliveryNoteHandlerTests
{
    [Fact]
    public async Task Base_quantity_mismatch_does_not_post_stock_or_allocation()
    {
        var fixture = IssueDeliveryFixture.ApprovedDelivery();
        fixture.QuantityCalculator.ServerResult = Quantity.Create(12_000, 0);
        fixture.Command = fixture.Command with { ClientQuantityBase = 11_999 };

        var action = () => fixture.Handler.Handle(
            fixture.Command,
            CancellationToken.None);

        var error = await action.Should().ThrowAsync<ProblemException>();
        error.Which.Code.Should().Be("QUANTITY_BASE_MISMATCH");
        fixture.StockLedger.PostCount.Should().Be(0);
        fixture.DeliveryRepository.ActiveAllocationCount.Should().Be(0);
        fixture.UnitOfWork.CommitCount.Should().Be(0);
    }

    [Fact]
    public async Task Successful_issue_posts_allocation_stock_movement_and_reservation_update()
    {
        var fixture = IssueDeliveryFixture.ApprovedDelivery();
        fixture.QuantityCalculator.ServerResult = Quantity.Create(12_000, 0);
        fixture.Command = fixture.Command with { ClientQuantityBase = 12_000 };

        var result = await fixture.Handler.Handle(
            fixture.Command,
            CancellationToken.None);

        result.DeliveryNoteStatus.Should().Be("Issued");
        fixture.StockLedger.PostCount.Should().Be(1);
        fixture.Reservations.ConsumeCount.Should().Be(1);
        fixture.Audit.Events.Should().ContainSingle(x =>
            x.Code == "DELIVERY_NOTE_ISSUED");
    }

    [Fact]
    public async Task Same_idempotency_key_and_payload_replays_first_result()
    {
        var fixture = IssueDeliveryFixture.ApprovedDelivery();
        var first = await fixture.Handler.Handle(
            fixture.Command,
            CancellationToken.None);

        var second = await fixture.Handler.Handle(
            fixture.Command,
            CancellationToken.None);

        second.Should().BeEquivalentTo(first);
        fixture.StockLedger.PostCount.Should().Be(1);
        fixture.DeliveryRepository.ActiveAllocationCount.Should().Be(1);
    }

    [Fact]
    public async Task Same_idempotency_key_with_different_payload_is_rejected()
    {
        var fixture = IssueDeliveryFixture.ApprovedDelivery();
        await fixture.Handler.Handle(fixture.Command, CancellationToken.None);
        var changed = fixture.Command with
        {
            PayloadHash = "different-payload-hash"
        };

        var action = () => fixture.Handler.Handle(
            changed,
            CancellationToken.None);

        var error = await action.Should().ThrowAsync<ProblemException>();
        error.Which.Code.Should().Be("IDEMPOTENCY_PAYLOAD_MISMATCH");
        fixture.StockLedger.PostCount.Should().Be(1);
    }

    [Fact]
    public async Task Handler_does_not_commit_when_stock_port_rejects_quantity()
    {
        var fixture = IssueDeliveryFixture.ApprovedDelivery();
        fixture.StockLedger.ThrowInsufficientStock = true;

        var action = () => fixture.Handler.Handle(
            fixture.Command,
            CancellationToken.None);

        var error = await action.Should().ThrowAsync<ProblemException>();
        error.Which.Code.Should().Be("INSUFFICIENT_STOCK");
        fixture.UnitOfWork.CommitCount.Should().Be(0);
        fixture.UnitOfWork.RollbackCount.Should().Be(1);
        fixture.DeliveryRepository.ActiveAllocationCount.Should().Be(0);
    }
}
```

`ProblemException`, `IssueDeliveryFixture` ve fake port’lar implementation scaffold’unda oluşturulacak test support kodudur. Handler transaction behavior ile test fixture’ın rollback sayacı ayrı katmanlar olarak test edilmelidir.

## 9. IssueInvoice handler testleri

```csharp
public sealed class IssueInvoiceHandlerTests
{
    [Fact]
    public async Task Invoice_issue_creates_current_debit_but_no_stock_movement()
    {
        var fixture = IssueInvoiceFixture.FromIssuedDelivery();
        var stockBefore = fixture.StockLedger.MovementCount;

        var result = await fixture.Handler.Handle(
            fixture.Command,
            CancellationToken.None);

        result.Status.Should().Be("Issued");
        fixture.CurrentAccount.DebitCount.Should().Be(1);
        fixture.StockLedger.MovementCount.Should().Be(stockBefore);
        fixture.InvoiceRepository.ActiveAllocationCount.Should().Be(1);
    }

    [Fact]
    public async Task Invoice_from_non_issued_delivery_is_rejected()
    {
        var fixture = IssueInvoiceFixture.FromPreparedDelivery();

        var action = () => fixture.Handler.Handle(
            fixture.Command,
            CancellationToken.None);

        var error = await action.Should().ThrowAsync<ProblemException>();
        error.Which.Code.Should().Be("INVALID_INVOICE_SOURCE_STATE");
        fixture.CurrentAccount.DebitCount.Should().Be(0);
    }

    [Fact]
    public async Task Invoice_over_remaining_delivery_quantity_is_rejected()
    {
        var fixture = IssueInvoiceFixture.FromIssuedDelivery();
        fixture.InvoiceRepository.InvoiceableQty = Quantity.Create(5_000, 0);
        fixture.Command = fixture.Command with
        {
            RequestedQuantityBase = 5_001
        };

        var action = () => fixture.Handler.Handle(
            fixture.Command,
            CancellationToken.None);

        var error = await action.Should().ThrowAsync<ProblemException>();
        error.Which.Code.Should().Be("OVER_ALLOCATION");
        fixture.CurrentAccount.DebitCount.Should().Be(0);
        fixture.StockLedger.MovementCount.Should().Be(0);
    }
}
```

## 10. CQRS transaction behavior testleri

```csharp
public sealed class TransactionBehaviorTests
{
    [Fact]
    public async Task Successful_handler_saves_and_commits_once()
    {
        var unitOfWork = new FakeUnitOfWork();
        var behavior = new TransactionBehavior<TestCommand, TestResult>(unitOfWork);

        var result = await behavior.Handle(
            new TestCommand(),
            CancellationToken.None,
            () => Task.FromResult(new TestResult("ok")));

        result.Value.Should().Be("ok");
        unitOfWork.SaveCount.Should().Be(1);
        unitOfWork.CommitCount.Should().Be(1);
        unitOfWork.RollbackCount.Should().Be(0);
    }

    [Fact]
    public async Task Handler_exception_rolls_back_and_does_not_commit()
    {
        var unitOfWork = new FakeUnitOfWork();
        var behavior = new TransactionBehavior<TestCommand, TestResult>(unitOfWork);

        var action = () => behavior.Handle(
            new TestCommand(),
            CancellationToken.None,
            () => throw new ProblemException("OVER_ALLOCATION"));

        await action.Should().ThrowAsync<ProblemException>();
        unitOfWork.CommitCount.Should().Be(0);
        unitOfWork.RollbackCount.Should().Be(1);
    }
}
```

## 11. Unit test ile integration test sınırı

Aşağıdaki davranışlar unit testte fake ile temsil edilse de gerçek kanıt için PostgreSQL integration test şarttır:

| Davranış | Unit test | Integration test |
|---|---|---|
| Quantity conversion | Evet | Seed/mapping ile tekrar |
| Allocation domain upper bound | Evet | Check/trigger + `FOR UPDATE` |
| Row-version conflict | Hayır, fake exception mapping | İki gerçek DB connection |
| Deferred trigger | Hayır | Commit öncesi PostgreSQL constraint |
| Idempotency unique | Fake store | Unique index + transaction |
| Stock movement rollback | Fake ledger | PostgreSQL transaction rollback |
| Invoice no-stock effect | Evet | Delivery/invoice/stock tables birlikte |
| HTTP ProblemDetails | Hayır | API integration |

## 12. Beklenen test coverage alanı

MVP release öncesi özellikle şu branch’ler kırmızı olmamalıdır:

```text
QUANTITY_BASE_MISMATCH
QUANTITY_PRECISION_EXCEEDED
PACKAGING_PARTIAL_NOT_ALLOWED
OVER_SHIPMENT
OVER_ALLOCATION
QUANTITY_CONCURRENCY_CONFLICT
IDEMPOTENCY_PAYLOAD_MISMATCH
INVALID_INVOICE_SOURCE_STATE
INSUFFICIENT_STOCK
STATE_TRANSITION_CONFLICT
FORBIDDEN
RISK_HARD_BLOCK
```

Coverage yüzdesi tek başına kabul ölçütü değildir. Her error code için en az bir domain/application/API veya database evidence kaydı tutulur.

## 13. Implementation başlangıç sırası

Gate açıldıktan sonra test ve domain implementation şu sırayla başlatılır:

```text
1. FactoryErp.Domain common/result/error types
2. Quantity, PackagingSnapshot ve QuantitySnapshot
3. SalesOrderItem ve DeliveryNoteItem allocation invariants
4. Domain unit test project ve fixture’lar
5. Application ports ve IssueDeliveryNote/IssueInvoice commands
6. CQRS transaction/idempotency behavior
7. Application unit tests
8. EF Core persistence integration testleri
9. PostgreSQL concurrency ve API integration testleri
```

Bu belge test project’i için blueprint’tir. `IMPLEMENTATION: READY FOR SCAFFOLD` kararıyla ilk Domain ve test source tree eklenebilir; ancak ilk slice kanıtları tamamlanmadan API, EF migration, web, mobile, worker veya external adapter testleri açılmaz.
