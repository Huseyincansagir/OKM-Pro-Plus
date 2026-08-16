# Factory ERP — Architecture Decision Baseline

**Karar sahibi:** Proje sahibi

**Karar tarihi:** 2026-08-16

**Durum:** Önerilen teknik kararlar proje sahibi talimatıyla kabul edilmiş Architecture baseline olarak işlenmiştir.

**Kapsam:** Quantity value object, EF Core mapping, PostgreSQL concurrency/locking, CQRS transaction, domain event/outbox, GitHub Actions runner güvenliği ve implementation gate.

> Bu belge, O-001–O-014 iş kararlarının yerine geçmez. O-001–O-014 baseline’ını teknik uygulama kararlarına bağlayan Architecture ADR paketidir.

## 1. Kabul edilen karar özeti

| ID | Karar | Kabul edilen baseline | Etki |
|---|---|---|---|
| ADR-001 | Quantity model | Immutable `Quantity` positive transaction value object + `NonNegativeQuantity`/decimal projection for zero totals | Domain, allocation, stock, invoice |
| ADR-002 | Packaging snapshot | Immutable `PackagingSnapshot` and `QuantitySnapshot`; server recalculates `quantity_base` | Mobile/API/ledger/audit |
| ADR-003 | EF Core aggregate mapping | Private backing fields, read-only collection projections, explicit Fluent API access mode | Domain/Infrastructure |
| ADR-004 | Concurrency token | Public `row_version`/ETag contract; application-managed monotonic bigint trigger; Npgsql `xmin` not exposed | EF/API/PostgreSQL |
| ADR-005 | Allocation lock | PostgreSQL Read Committed baseline + deterministic source-row `SELECT FOR UPDATE` + re-read + DB upper-bound trigger | Shipping/invoicing/stock |
| ADR-006 | CQRS transaction | One application transaction for business effects; validation/authorization before transaction; quantity/stock recheck inside transaction | Application/EF Core |
| ADR-007 | Domain events | In-process domain events for same-domain side effects; no external call inside aggregate transaction | Domain/Application |
| ADR-008 | Outbox | `outbox_messages` in same DB transaction for notifications, reports and external adapters; worker publishes after commit | Infrastructure/Worker |
| ADR-009 | Retry/conflict | `DbUpdateConcurrencyException`, deadlock/serialization and unique conflict map to typed ProblemDetails; command retry requires fresh read and idempotency | API/UX/operations |
| ADR-010 | Self-hosted runner | Production runner is private, environment-protected, runner-group restricted and release-only; PR code never runs there | GitHub Actions/security |
| ADR-011 | Implementation gate | Architecture decisions accepted; implementation gate opens only after acceptance evidence and source scaffold; first implementation slice is Domain + tests | Process/release |

## 2. ADR-001 — Quantity value object and zero totals

### Decision

`Quantity` yalnızca **pozitif işlem miktarını** temsil eder. `Quantity.Create(value, scale)` için `value > 0` zorunludur ve UOM precision dışında değer reddedilir. Allocation, stock movement, shipment, invoice allocation ve payment movement gibi fiziksel/finansal işlem kayıtları pozitif quantity/amount taşır.

Toplam, projection ve remaining alanlarında sıfır geçerli olduğu için `NonNegativeQuantity` veya typed projection tercih edilir. Production implementasyonunda önerilen ayrım:

```text
PositiveQuantity
  → allocation/movement/input command

NonNegativeQuantity
  → ordered/shipped/reserved/remaining projection

Money
  → non-negative or signed policy’ye göre ayrı value object
```

Reversal negatif quantity olarak yazılmaz; positive reversal record, `reversed_from_id` ve reason ile oluşturulur.

### Gerekçe

Value object identity taşımamalı ve immutable olmalıdır [1]. Quantity’nin pozitif işlem miktarı ile zero-capable projection miktarını aynı primitive wrapper’da tutmak, `Subtract` veya remaining hesaplarında yanlışlıkla negatif/zero transition üretme riskini artırır. Bu ayrım `QUANTITY_MUST_BE_POSITIVE`, `QUANTITY_PRECISION_EXCEEDED` ve `OVER_ALLOCATION` hatalarını daha açık hale getirir.

### Kabul testleri

- `PositiveQuantity.Create(0)` reddedilir.
- `NonNegativeQuantity.Create(0)` kabul edilir.
- `NonNegativeQuantity.Create(-1)` reddedilir.
- 3 scale UOM için `1.2345` reddedilir.
- Kısmi sevk sonrası remaining `0` olduğunda projection kabul edilir.
- Reversal negative movement oluşturmaz.

## 3. ADR-002 — Immutable packaging and quantity snapshots

`PackagingSnapshot` şu alanları immutable saklar:

```text
packaging_id
level/name
base_uom_code
quantity_in_base_uom
allow_partial
effective_version
```

`QuantitySnapshot` ise entered quantity, operation packaging, calculated base quantity, view mode, breakdown ve packaging snapshot’ı birlikte taşır. Server `quantity_base` değerini yeniden hesaplar; istemci değeri yalnızca mismatch kontrolünde kullanılır.

Bu karar, mobil `Temel Birim / Ambalaj / Kırılım` toggle’ının yalnızca görünüm değiştirmesini ve operation packaging’in ayrı tutulmasını garanti eder. Geçmiş işlem snapshot’ı, master packaging katsayısı değiştiğinde yeniden yorumlanmaz.

### Kabul testleri

- Beş koli, snapshot katsayısına göre temel birime çevrilir.
- Client ve server base quantity farklıysa `422 QUANTITY_BASE_MISMATCH` oluşur.
- `allow_partial=false` ambalaj fractional input kabul etmez.
- Master packaging version değişince eski allocation snapshot değişmez.

## 4. ADR-003 — EF Core backing fields ve aggregate encapsulation

Aggregate child collection’ları private backing field ile tutulur; dışarıya yalnızca `IReadOnlyCollection<T>` projection verilir. EF Core backing field’lar, domain encapsulation’ını korurken persistence’ın field üzerinden çalışmasına izin verir [2].

Önerilen mapping biçimi:

```csharp
private readonly List<DeliveryNoteItemAllocation> _allocations = [];
public IReadOnlyCollection<DeliveryNoteItemAllocation> Allocations => _allocations;

builder.Metadata
    .FindNavigation(nameof(DeliveryNoteItem.Allocations))!
    .SetPropertyAccessMode(PropertyAccessMode.Field);
```

Gerçek configuration’da `HasField`, `UsePropertyAccessMode` ve private field mapping açıkça yazılır. Allocation child entity’nin public setter’ı olmaz. Controller veya query DTO doğrudan aggregate collection’ı mutate edemez.

### Kabul testleri

- Domain assembly’den allocation collection’a public `Add` yoktur.
- EF materialization private field’ı doldurabilir.
- Aggregate method’u olmadan state değiştirilemez.
- Query projection read-only DTO üretir.
- `ArchitectureTests` API’nin `DbContext` veya EF entity expose etmediğini doğrular.

## 5. ADR-004 — Concurrency token ve ETag

PostgreSQL için dış API sözleşmesi `row_version`/ETag olarak korunur. EF Core optimistic concurrency token’ı query sırasında yükler ve SaveChanges sırasında original değerle karşılaştırır; mismatch `DbUpdateConcurrencyException` üretebilir [3]. PostgreSQL’de SQL Server `rowversion` bulunmadığı için Npgsql `xmin` system column’ını alternatif olarak sunar [4].

Bu proje için `xmin` public contract veya domain alanı olarak kullanılmayacaktır. Önerilen baseline:

```text
Database: bigint row_version NOT NULL DEFAULT 1
Update: trigger row_version = old + 1
EF: IsConcurrencyToken()
HTTP: ETag + If-Match
Conflict: 409 QUANTITY_CONCURRENCY_CONFLICT / RESOURCE_VERSION_CONFLICT
```

`xmin` yalnızca provider-level alternatif olarak documented risk kaydıdır; `row_version` ve `xmin` aynı entity’de eşzamanlı concurrency source olarak kullanılmaz.

### Kabul testleri

- Stale `If-Match` ile update `409` döner.
- EF concurrency exception typed ProblemDetails’a map edilir.
- Yeni version response ETag olarak döner.
- Same idempotency key replay ilk response’u döndürür.
- Farklı payload aynı key ile gönderilirse yeni transaction başlamadan `IDEMPOTENCY_PAYLOAD_MISMATCH` döner.

## 6. ADR-005 — Allocation locking and isolation

Allocation command’leri PostgreSQL default Read Committed ile başlar. Source row deterministic sırayla `SELECT FOR UPDATE` ile kilitlenir; PostgreSQL bu satırları transaction sonuna kadar concurrent writer/locker işlemlerine karşı bloklar [5]. Lock alındıktan sonra current source quantity, active allocation toplamı ve remaining quantity yeniden okunur.

```sql
BEGIN;

SELECT id, ordered_qty, shipped_qty, cancelled_qty, row_version
FROM sales_order_items
WHERE id = :source_item_id
FOR UPDATE;

SELECT COALESCE(SUM(quantity_base), 0)
FROM delivery_note_item_allocations
WHERE sales_order_item_id = :source_item_id
  AND status = 'Active';

-- current remaining and requested quantity are checked here.
-- allocation, movement and projection update commit atomically.

COMMIT;
```

Çoklu source item command’lerinde ID veya line sequence artan sırada lock alınır. Deadlock/serialization/unique conflict oluşursa transaction rollback olur; command yeni read ve aynı idempotency policy ile kontrollü retry edebilir.

Her command’ı Serializable yapmak MVP baseline’ı değildir. PostgreSQL Read Committed’de iki SELECT farklı snapshot görebileceği için source row lock + transaction içi re-read zorunludur [6].

### Kabul testleri

- Aynı kalan 600 için iki transaction 400’er istediğinde yalnızca biri commit eder.
- İkinci transaction güncel remaining değerini görür ve `QUANTITY_CONCURRENCY_CONFLICT` veya `OVER_ALLOCATION` üretir.
- Multi-item lock sırası deterministic olduğu için deadlock oluşmaz veya kontrollü retry ile tamamlanır.
- Allocation upper-bound deferred constraint commit öncesi çalışır.
- Lock transaction sonunda serbest kalır.

## 7. ADR-006 — CQRS transaction sınırı

CQRS command pipeline şu sırayla çalışır:

```text
Correlation
→ validation
→ authorization
→ idempotency lookup
→ transaction begin
→ source lock/re-read
→ domain command
→ ledger/allocation/reservation/current transaction
→ audit/outbox record
→ SaveChanges
→ commit
→ post-commit worker/notification
```

EF Core transaction’lar birden fazla database operation’ının atomik işlenmesini sağlar; commit tümünü uygular, rollback hiçbirini uygulamaz [7]. EF Core mevcut transaction içinde SaveChanges öncesi savepoint oluşturabilir [7].

MVP command’leri:

| Command | Aynı transaction’da kalan etkiler |
|---|---|
| `IssueDeliveryNote` | Delivery state, delivery allocation, stock movement, reservation consume/release, order projection, audit, outbox record |
| `IssueInvoice` | Invoice state, invoice allocation, current debit, tax/price snapshot, audit, outbox record; stock movement yok |
| `ApplyPayment` | Payment state, payment allocation, current credit, audit, outbox record |
| `CompleteProduction` | Production record, finished-good stock receipt, production projection, audit, outbox record |
| `LockLoadPlan` | Plan state, candidate acceptance, manual-change audit, outbox record |

External HTTP, SMTP, e-invoice provider veya push notification transaction’ın ortasında çağrılmaz. Bu side effect’ler outbox’a kaydedilir ve commit sonrasında worker tarafından yürütülür.

## 8. ADR-007/ADR-008 — Domain event and transactional outbox

Domain event aynı domain içinde in-process side effect’i açıkça ifade eder. Microsoft .NET rehberinde domain event ile integration event’in ayrılması; integration event’in ancak transaction persist edildikten sonra asynchronous yayınlanması önerilir [8].

Kabul edilen desen:

```text
Aggregate raises DomainEvent
→ Application collects event
→ local same-domain handler, if needed
→ OutboxMessage written in same DB transaction
→ Commit
→ Worker reads pending outbox
→ external adapter/notification
→ retry/backoff
→ processed/dead-letter status
```

MVP’de message broker kurulmaz. PostgreSQL `outbox_messages` tablosu yeterlidir:

```text
id
message_type
aggregate_type
aggregate_id
payload_json
payload_hash
status: Pending | Processing | Published | Failed | DeadLetter
attempt_count
available_at
processed_at
last_error
created_at
```

Outbox consumer duplicate delivery’ye dayanıklı olur; `message_id` ve external idempotency key kullanır. External provider’a çağrı başarılı olup response kaybolursa aynı message tekrar denenebilir; provider adapter duplicate-safe olmalıdır.

## 9. ADR-009 — Conflict mapping and retry policy

| Low-level durum | API problem code | Retry |
|---|---|---|
| EF `DbUpdateConcurrencyException` | `RESOURCE_VERSION_CONFLICT` veya quantity context’inde `QUANTITY_CONCURRENCY_CONFLICT` | Fresh read + kullanıcı/command policy |
| PostgreSQL deadlock | `TRANSACTION_DEADLOCK` | Kısa jitter ile sınırlı command retry; idempotent olmalı |
| Serialization failure | `TRANSACTION_SERIALIZATION_FAILURE` | Fresh transaction ile sınırlı retry |
| Unique idempotency conflict | `IDEMPOTENCY_PAYLOAD_MISMATCH` | Retry yok |
| Allocation trigger/check | `OVER_ALLOCATION` | Retry yok; current remaining göster |
| Packaging calculation mismatch | `QUANTITY_BASE_MISMATCH` | Retry yok; preview refresh |

API response `application/problem+json`, request/correlation ID, retryable flag, current version/remaining quantity ve user action içerir. SQL exception, connection string, stack trace ve secret response’a konulmaz.

## 10. ADR-010 — GitHub Actions and self-hosted runner

GitHub, self-hosted runner’ların GitHub-hosted runner’lar gibi temiz/ephemeral ortam garantisi vermediğini ve untrusted workflow code ile kalıcı biçimde compromise edilebileceğini belirtir [9].

Bu nedenle:

- PR ve untrusted branch testleri GitHub-hosted runner’da çalışır.
- Production self-hosted runner yalnızca protected release job’ında kullanılır.
- Runner ayrı group, private repository, environment approval ve required reviewer ile sınırlandırılır.
- Production runner’da source checkout ile arbitrary PR code çalıştırılmaz.
- `GITHUB_TOKEN` least privilege olur; workflow permission’ları job bazında yazılır.
- Production secret’ları runner host secret store’da tutulur.
- Runner’ın PostgreSQL ve Compose host erişimi yalnızca release/backup job’ına verilir.

## 11. ADR-011 — Implementation gate

Bu araştırma ve karar kabulüyle Architecture karar blokajı kapanmıştır. Aşağıdaki geçiş kabul edilmiştir:

```text
Architecture decisions: ACCEPTED
Architecture artefacts: COMPLETE FOR MVP HANDOFF
IMPLEMENTATION: READY FOR SCAFFOLD
NEXT: factory-erp-implementation
```

Implementation’ın ilk slice’ı yalnızca şunlardır:

```text
FactoryErp.Domain common types
→ Quantity/PackagingSnapshot
→ SalesOrderItem/DeliveryNoteItem allocation invariants
→ Domain unit tests
→ Architecture test project
```

İlk slice’ın başarı kriterleri geçmeden API, EF migration, web, mobile, payroll, report ve external e-document feature’ları başlatılmaz. Karar değişirse implementation durdurulur ve ilgili ADR/design gate yeniden açılır.

## 12. Evidence ve risk kaydı

| Risk | Mitigation | Evidence |
|---|---|---|
| Zero quantity yanlışlıkla transaction’a girmesi | Positive/non-negative type ayrımı | Domain unit tests |
| Concurrent allocation overrun | `FOR UPDATE`, re-read, trigger, concurrency test | PostgreSQL integration |
| Event external çağrı ile transaction’ın bozulması | Outbox after commit | Outbox retry test |
| EF aggregate bypass | Backing field + architecture test | Architecture test report |
| Self-hosted runner compromise | Protected release runner/group/environment | CI security review |
| Deadlock | Deterministic lock order + bounded retry | Two-connection integration test |
| Provider token drift | Explicit row_version + Npgsql integration test | EF mapping/schema snapshot |

## References

[1]: https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/implement-value-objects "Implementing value objects - .NET"

[2]: https://learn.microsoft.com/en-us/ef/core/modeling/backing-field "Backing Fields - EF Core"

[3]: https://learn.microsoft.com/en-us/ef/core/saving/concurrency "Handling Concurrency Conflicts - EF Core"

[4]: https://www.npgsql.org/efcore/modeling/concurrency.html "Concurrency Tokens - Npgsql Documentation"

[5]: https://www.postgresql.org/docs/current/explicit-locking.html "PostgreSQL Explicit Locking"

[6]: https://www.postgresql.org/docs/current/transaction-iso.html "PostgreSQL Transaction Isolation"

[7]: https://learn.microsoft.com/en-us/ef/core/saving/transactions "Transactions - EF Core"

[8]: https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation "Domain events: Design and implementation - .NET"

[9]: https://docs.github.com/en/actions/reference/security/secure-use "Secure use reference - GitHub Docs"
