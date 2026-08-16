# Factory ERP — MVP Unit ve Integration Test Stratejisi

**Aşama:** ARCHITECTURE / QA handoff

**Durum:** Test strategy ve acceptance tasarımı; production test kodu değildir.

**Baseline:** O-001–O-014 kararları kabul edilmiş; MVP production code henüz yazılmamıştır.

## 1. Test amacı

MVP test stratejisinin amacı yalnızca endpoint’in `200 OK` dönmesini doğrulamak değildir. Stok, miktar, allocation, cari ledger, belge state, permission, public veri sınırı ve transaction bütünlüğü yanlış kullanım altında da korunmalıdır.

Öncelik sırası şöyledir:

```text
Domain invariants
→ Ledger/quantity integrity
→ State transition authorization
→ API contract
→ PostgreSQL transaction/concurrency
→ UI/mobile/public contract
→ Deployment/backup acceptance
```

MVP’de test stratejisi şu prensipleri korur:

| Prensip | Uygulama |
|---|---|
| Deterministic | Saat, UUID, current user, external provider ve risk clock testte kontrol edilir |
| Isolated | Unit test database/network kullanmaz; integration test transaction veya temiz schema ile izole edilir |
| Representative | Testler O-001–O-014 seçilmiş kararlarını doğrudan kapsar |
| Failure-first | Over-allocation, wrong state, unauthorized override, duplicate/idempotency ve concurrency senaryoları başarı kadar önemlidir |
| Evidence-based | Her kritik test test ID, karar ID, acceptance sonucu ve kanıt linkiyle raporlanır |
| No production secrets | Test fixture’larında gerçek müşteri, personel, token veya maaş verisi kullanılmaz |

## 2. Test katmanları ve MVP kapsamı

| Katman | Araç/şablon | Kapsam | Hedef |
|---|---|---|---:|
| Domain unit | xUnit veya NUnit + assertion library | Pure business rule, value object, state, quantity, risk | En geniş katman |
| Application unit | xUnit + mocks/fakes | Command handler orchestration, permission, audit/event mapping | Orta genişlik |
| Persistence integration | PostgreSQL container + EF Core | Mapping, FK, index, check, trigger, transaction, migration | Kritik sınırlı set |
| API integration | ASP.NET `WebApplicationFactory` + PostgreSQL container | Route, DTO, auth, ProblemDetails, state command | Her kritik command |
| Contract | OpenAPI schema/JSON assertions | Web/mobile/public DTO ve error compatibility | Değişiklikte zorunlu |
| E2E smoke | Browser/mobile automation veya API scenario | Login, order, shipment, invoice, public quote | MVP happy path + kritik failure |
| Deployment acceptance | Docker Compose + isolated host | Health, migration, backup/restore, LAN HTTPS, network isolation | Release öncesi |

MVP’de tam UI pixel testleri yerine API/domain/transaction güvenliği önceliklendirilir. UI testleri kritik kullanıcı yolunu ve permission görünürlüğünü doğrular; iş kuralının tek kaynağı UI değildir.

## 3. Test naming ve traceability

Test adı şu biçimde tutulur:

```text
{DecisionOrFeature}_{Scenario}_{ExpectedResult}
```

Örnekler:

```text
O002_WhenIssuingPartialDelivery_UpdatesAllocationAndRemainingQty
O003_WhenInvoicingMoreThanShipped_ReturnsOverAllocation
QTY_WhenClientBaseQuantityDiffers_ReturnsQuantityBaseMismatch
AUTH_WhenOperatorIssuesInvoice_ReturnsForbidden
MIG_0008_WhenAppliedToCleanDatabase_CreatesDeliveryAllocationSchema
```

Her test case aşağıdaki metadata’yı taşımalıdır:

| Alan | Örnek |
|---|---|
| Test ID | `O002-API-ISSUE-001` |
| Karar | `O-002` |
| Layer | Domain/API/DB/Deployment |
| Setup | Customer, product, packaging, stock, order |
| Action | Command veya HTTP request |
| Expected | State, ledger, response, audit |
| Evidence | Test result, log, schema snapshot veya report |
| Risk | Bilinen sınırlama veya yeniden açma koşulu |

## 4. Test fixture ve veri yaklaşımı

Test verisi factory/builder yaklaşımıyla üretilir. Fixture varsayılan değerleri açık olmalı; gizli global state kullanılmamalıdır.

Minimum fixture seti:

```text
User: Admin, SalesManager, WarehouseOperator, Accounting, ProductionManager, HR, ReportViewer
Customer: ActiveCustomer, CandidateCustomer, BlockedCustomer
Product: PieceProduct, PackagedProduct, NonPartialPackagingProduct
Warehouse: MainWarehouse + LocationA + LocationB
Order: ApprovedFullOrder, ApprovedPartialOrder, CancelledOrder
Delivery: DraftDelivery, IssuedPartialDelivery
Invoice: DraftInvoice, IssuedPartialInvoice
Vehicle: FeasibleVehicle, WeightExceededVehicle, DoorMismatchVehicle
```

Tüm miktar fixture’ları aynı zamanda şu değerleri açıkça taşır:

```text
enteredQuantity
enteredPackagingId
quantityBase
orderedQty
reservedQty
shippedQty
cancelledQty
invoicedQty
remainingToInvoice
```

Test database’de gerçek müşteri veya personel kişisel verisi kullanılmaz. Maaş alanları masking ve export audit testleri için sentetik değerlerle doldurulur.

## 5. Domain unit test stratejisi

Domain unit testleri database, HTTP ve framework olmadan çalışır. Her aggregate invariant’i küçük ve hızlı testlerle doğrulanır.

### 5.1 Quantity ve packaging

| Test grubu | Örnek davranış |
|---|---|
| Conversion | `5 Koli × 2.000 = 10.000 Piece` |
| Snapshot | Packaging katsayısı sonradan değişse bile eski snapshot değişmez |
| Precision | UOM scale dışındaki değer `QUANTITY_PRECISION_EXCEEDED` üretir |
| Partial packaging | `allow_partial=false` ise kapalı koli parçalanamaz |
| Base mismatch | Client quantityBase ile server conversion farklıysa `QUANTITY_BASE_MISMATCH` |
| Breakdown | `4 Koli + 6 Paket` base miktarıyla eşleşmiyorsa reddedilir |
| Zero/negative | Sıfır veya negatif quantity commit edilemez |

### 5.2 SalesOrder ve partial shipment

```text
orderedQty = 20.000
reservedQty = 20.000
shippedQty = 0
cancelledQty = 0
remainingQty = 20.000
```

Testler:

- Approved order’dan partial delivery hazırlanabilir.
- `newShipmentQty <= remainingQty` kuralı çalışır.
- `newShipmentQty <= availableStock` kuralı çalışır.
- İlk 12.000 sevkten sonra `remainingQty = 8.000` olur.
- Son sevkten sonra state `Fulfilled/Completed` seçilmiş policy’ye göre oluşur.
- Cancelled/rejected order’dan issue yapılamaz.
- Reversal delete yerine reverse movement üretir.

### 5.3 Invoice ve partial invoicing

- Sadece `DeliveryNote.Issued` source invoice olabilir.
- `newInvoiceQty <= remainingToInvoice` kuralı çalışır.
- Aynı delivery note iki invoice’a bölünebilir.
- Invoice issue stok movement oluşturmaz.
- Invoice issue current debit ve audit üretir.
- Reversal/credit aktif allocation’ı doğru düşürür.
- Invoice allocation over limit ise domain error ve transaction failure oluşur.

### 5.4 State machine

Her transition için başarılı, yanlış source state, eksik permission, duplicate command ve reversal testleri bulunur.

| Aggregate | Kritik transition |
|---|---|
| SalesOrder | Draft → PendingApproval → Approved → Preparing → PartiallyShipped → Fulfilled |
| DeliveryNote | Draft → Prepared → ReadyToIssue → Issued → Reversed |
| Invoice | Draft → ReadyToIssue → Issued → PartiallyPaid → Paid |
| Shipment | Preparing → Loaded → InTransit → PartiallyDelivered → Delivered |
| LoadPlan | Draft → Proposed → Valid/NeedsReview → Locked |
| Payment | Draft → Applied → Reversed |
| ProductionOrder | Planned → Released → InProgress → Completed |

## 6. Application/handler unit test stratejisi

Application testleri aggregate rule’larını tekrar yazmaz; command’ın doğru repository, policy, transaction, audit, notification ve response mapping çağrılarını yaptığını doğrular.

Örnek command handler testleri:

| Handler | Başarı testi | Failure testi |
|---|---|---|
| `ApproveOrderHandler` | Approval + reservation + audit + notification | Risk hard block veya stok yetersiz |
| `IssueDeliveryNoteHandler` | Allocation + movement + reservation + projection | Base mismatch, over shipment, state conflict |
| `IssueInvoiceHandler` | Invoice + allocation + current debit + audit | Issued olmayan delivery, over invoice |
| `ApplyPaymentHandler` | Payment + credit + allocation | Open balance aşımı, duplicate key |
| `CompleteProductionHandler` | Finished-good receipt + machine stat | Negative quantity veya invalid machine |
| `LockLoadPlanHandler` | Hard error yok + manual approval | Hard error veya override permission eksik |
| `ReviewQuoteRequestHandler` | Candidate/customer binding audit | Public request’in doğrudan active customer olması |
| `ExportSalaryHandler` | Masked export + audit | `salary.export` yok |

Handler unit testlerinde fake clock, deterministic ID provider, fake current-user, mock notification ve controlled idempotency store kullanılır. Gerçek transaction davranışı persistence integration testine bırakılır.

## 7. Persistence integration test stratejisi

Persistence integration testleri gerçek PostgreSQL major sürümüne yakın container üzerinde çalışır. SQLite kullanılmaz; PostgreSQL `jsonb`, partial index, `numeric`, deferred constraint, trigger, timezone ve FK davranışları SQLite ile eşdeğer değildir.

### 7.1 Database lifecycle

```text
Test run start
→ PostgreSQL container
→ Apply EF migrations 0001–0018
→ Apply deterministic seeds
→ Schema/constraint smoke check
→ Test collection execution
→ Database cleanup/container dispose
```

Her test collection ayrı schema/database veya transaction rollback kullanır. Concurrency testleri aynı transaction rollback yaklaşımı yerine iki gerçek connection kullanır.

### 7.2 Migration testleri

Her migration için:

- Clean database üzerinde migration sırası başarılı.
- Expected table, column, PK, FK, check, index ve trigger vardır.
- Migration ikinci kez çalıştırıldığında duplicate veya state drift üretmez.
- 0017 ve 0018 seed’leri idempotenttir.
- Schema snapshot beklenen sürümle eşleşir.
- Destructive migration production’da otomatik down yapmaz.
- Baseline test data 0001–0018 sonrasında kullanılabilir.

### 7.3 Constraint ve ledger testleri

- Negative stock/money/quantity database tarafından reddedilir.
- `shipped + cancelled > ordered` commit edilemez.
- `invoiced + waived > shipped` commit edilemez.
- Active source/target allocation duplicate unique index’e takılır.
- Idempotency key aynı payload ile aynı sonucu döndürür.
- Aynı key farklı payload ile `IDEMPOTENCY_PAYLOAD_MISMATCH` oluşur.
- Stock movement silinemez veya FK restriction ile engellenir.
- Invoice issue sonrasında stock movement sayısı değişmez.
- Payment allocation toplamı payment amount ve invoice open balance sınırını aşamaz.

## 8. API integration test stratejisi

API integration testleri gerçek ASP.NET host pipeline’ını `WebApplicationFactory` benzeri host ile çalıştırır. Authentication testte gerçek password hashing/JWT validation ile veya güvenli test issuer ile yapılır; controller bypass edilmez.

### 8.1 API happy path

```text
Login
→ Product/packaging resolve
→ Public veya internal customer/quote
→ Order create
→ Order submit
→ Order approve + reservation
→ Partial delivery draft
→ Delivery issue
→ Shipment/load-plan suggestion
→ Invoice create from issued delivery
→ Invoice issue + current debit
→ Payment apply + current credit
```

Her adım response DTO, database state, audit event, permission ve idempotency sonucuyla birlikte assertion edilir.

### 8.2 API error matrix

| Senaryo | HTTP | Code |
|---|---:|---|
| Client/server base quantity farklı | 422 | `QUANTITY_BASE_MISMATCH` |
| Aynı kalan miktara eşzamanlı iki işlem | 409 | `QUANTITY_CONCURRENCY_CONFLICT` |
| Issued olmayan delivery’den invoice | 409/422 | `INVALID_INVOICE_SOURCE_STATE` |
| Allocation üst sınırı aşımı | 422 | `OVER_ALLOCATION` |
| Aynı idempotency key farklı payload | 409 | `IDEMPOTENCY_PAYLOAD_MISMATCH` |
| Eksik header | 400 | `MISSING_IDEMPOTENCY_KEY` |
| Yetkisiz issue | 403 | `FORBIDDEN` |
| Public API iç ERP alanını ister | 403/404 | `PUBLIC_FIELD_NOT_AVAILABLE` |
| Expired JWT | 401 | `TOKEN_EXPIRED` |
| Rate limit | 429 | `RATE_LIMITED` |

Her hata response’unda status, code, requestId, correlationId, retryable ve action alanlarının schema’ya uygunluğu test edilir. SQL, stack trace, token veya maaş detayı response’a sızmamalıdır.

## 9. Concurrency ve idempotency testleri

### 9.1 Partial shipment race

```text
Given remainingQty = 600
When connection A requests 400
And connection B requests 400
Then exactly one request commits
And the other returns 409 QUANTITY_CONCURRENCY_CONFLICT
And final shippedQty = initial + 400
And stock movement count increases exactly once
```

### 9.2 Partial invoice race

```text
Given remainingToInvoice = 600
When two connections request 400
Then exactly one invoice allocation commits
And the other returns 409 or controlled over-allocation conflict
And current debit is produced exactly once
And stock movement count does not change
```

### 9.3 Duplicate request

Aynı endpoint, aynı `Idempotency-Key` ve aynı payload iki kez gönderildiğinde ikinci response ilk committed result ile eşleşmelidir. Aynı key farklı payload ile gönderildiğinde hiçbir yeni ledger/allocation yazılmamalıdır.

## 10. Authorization ve security testleri

| Alan | Test kapsamı |
|---|---|
| Authentication | Missing/expired/invalid JWT, refresh rotation, logout invalidation |
| Permission | Role permission, user override, deny precedence, endpoint policy |
| State authorization | Operator draft oluşturabilir ama issue/override yapamaz |
| Financial | Invoice issue, payment, salary ve export permission’ları ayrıdır |
| Public isolation | Public catalog cari, cost, risk, stock detail ve employee alanlarını döndürmez |
| Input | SQL injection string, oversized upload, invalid JSON, malformed UUID, precision overflow |
| Audit | Approval, issue, reversal, override, export, consent, backup failure audit edilir |
| Rate limit | Public quote, login ve barcode resolve abuse senaryoları |

Security testleri yalnızca UI button gizli mi diye bakmaz; doğrudan API çağrısı ve farklı role token’ı ile backend policy’yi sınar.

## 11. O-001–O-014 karar coverage matrisi

| Karar | MVP test kapsamı |
|---|---|
| O-001 | Tax code/rate snapshot, rounding, adapter/stub boundary, invoice totals |
| O-002 | Partial delivery allocation, remainder, reservation consume/release, concurrency |
| O-003 | Issued delivery source, partial invoice, over-allocation, current debit, no stock movement |
| O-004 | Finished-good receipt; no mandatory production material/movement |
| O-005 | Lot/serial tables/endpoints absent from MVP migration; no accidental references |
| O-006 | Public request review; no automatic active customer; duplicate candidate |
| O-007 | Soft/hard risk block, override permission, reason and audit |
| O-008 | Salary masking/export permission/audit; no payroll engine expectation |
| O-009 | Public minimum data, consent, rate limit, bot control and API isolation |
| O-010 | Backup file, checksum, retention, restore and RPO/RTO evidence |
| O-011 | Compose health, LAN HTTPS, internal network and PostgreSQL non-public binding |
| O-012 | Price list/customer group/effective price and snapshot immutability |
| O-013 | Single brand token/asset manifest and no placeholder production asset |
| O-014 | Hard constraint, FFD suggestion, candidate rejection, manual lock/override and algorithm snapshot |

## 12. Critical MVP scenario catalogue

MVP release için aşağıdaki scenario grupları geçmeden release gate açılmaz:

```text
MVP-AUTH-001   Login, refresh, logout
MVP-MASTER-001 Product, barcode, packaging and price setup
MVP-ORDER-001 Order create/submit/approve/reject
MVP-STOCK-001 Reservation and available stock invariant
MVP-SHIP-001 Partial shipment and reversal
MVP-INVOICE-001 Partial invoice and current account debit
MVP-PAY-001 Payment, allocation and current credit
MVP-PROD-001 Production completion and finished-good receipt
MVP-LOAD-001 Vehicle fit, FFD suggestion and manual load lock
MVP-PUBLIC-001 Public catalog and quote request isolation
MVP-HR-001 Attendance/leave and salary masking/export
MVP-REPORT-001 Date range/timezone and permission-sensitive reports
MVP-MIG-001 0001–0018 clean database migration
MVP-BACKUP-001 Backup/restore/RPO/RTO
MVP-SEC-001 Authorization, rate limit, audit and no data leakage
```

Her senaryo en az bir API integration veya persistence integration kanıtına sahip olmalıdır. Kritik finans/stok senaryoları yalnızca UI E2E ile kanıtlanamaz.

## 13. Test data reset ve flakiness kuralları

Testler gerçek zaman, random GUID, unordered collection veya network timeout’a bağımlı yazılmaz. Fake clock ve deterministic ID sağlayıcı kullanılır. Test sıralamasına bağımlılık yasaktır. Integration testinde fixture cleanup başarısız olursa test suite kırmızı kalır; sessiz kirli database ile devam edilmez.

Flaky test üç kez art arda geçse bile otomatik olarak “başarılı” sayılmaz. Test quarantine kaydı, root cause, owner ve düzeltme tarihi gerekir. Concurrency testleri ayrı, daha uzun çalışan pipeline kategorisinde tutulabilir; ancak release öncesi çalıştırılması zorunludur.

## 14. Pipeline ve kalite kapıları

```text
Pull Request
→ format/lint/static analysis
→ domain unit tests
→ application unit tests
→ API contract tests
→ PostgreSQL migration/persistence tests
→ security/authorization tests

Main branch
→ full integration tests
→ concurrency/idempotency tests
→ public/mobile smoke
→ Docker Compose health/smoke

Release candidate
→ backup/restore test
→ RPO/RTO evidence
→ critical scenario catalogue
→ QA/security approval
```

Önerilen minimum gate’ler:

| Gate | Minimum koşul |
|---|---|
| Unit gate | Domain ve application unit testleri geçer; kritik invariant coverage eksik değildir |
| Integration gate | Migration, PostgreSQL constraint, transaction ve API critical path testleri geçer |
| Security gate | Auth, permission, public isolation, rate limit ve audit testleri geçer |
| Deployment gate | Compose health, LAN HTTPS, backup ve restore testi geçer |
| Release gate | Kritik scenario catalogue, açık P0/P1 yok, known risk owner’ı var |

Coverage yüzdesi tek başına kalite ölçüsü değildir. Coverage raporu; özellikle allocation, ledger, state transition, error mapping, permission ve reversal branch’lerini göstermelidir.

## 15. MVP acceptance checklist

- `QUANTITY_BASE_MISMATCH` ve `QUANTITY_CONCURRENCY_CONFLICT` API testleri mevcut.
- Kısmi sevkiyat iki ayrı irsaliye ile doğru remaining/reservation/stock sonucunu üretiyor.
- Kısmi fatura iki ayrı fatura ile doğru invoice allocation/current debit sonucunu üretiyor.
- Fatura issue stok movement üretmiyor.
- Aynı idempotency request ikinci ledger hareketini üretmiyor.
- Allocation üst sınırı hem application hem PostgreSQL testinde korunuyor.
- Clean database’de 0001–0018 migration sırası başarılı.
- Seed’ler ikinci çalıştırmada duplicate üretmiyor.
- Public API internal financial/risk/employee/stock detail sızdırmıyor.
- O-014 hard constraint, FFD öneri, manual approval ve algorithm snapshot testleri geçiyor.
- O-010 backup restore ve RPO/RTO kanıtı var.
- Docker Compose PostgreSQL’i public interface’e açmıyor.
- Salary export permission ve audit ile korunuyor.
- QA/security acceptance raporu ve Architecture decision traceability kaydı hazırlanmış.

Bu belge test framework configuration veya test implementation dosyası değildir. Architecture ve implementation ekiplerinin üreteceği test projeleri, fixture’lar ve pipeline adımları için kabul edilen MVP stratejisidir.


## 13. Accepted ADR test overlay

ADR-001–ADR-011 is accepted for MVP handoff. The first implementation slice must prove positive/non-negative quantity separation, immutable packaging snapshots, private aggregate backing fields, row_version/ETag conflicts, deterministic source-row locking, transaction rollback, outbox-after-commit behavior and typed ProblemDetails mapping.

Unit tests must not pretend to prove PostgreSQL locking. The persistence integration suite uses two real PostgreSQL connections to prove that two concurrent allocations cannot exceed the source remaining quantity. It also verifies the deferred upper-bound trigger, idempotency unique behavior, row-version conflict and deterministic multi-item lock ordering.

Release readiness remains separate from Architecture acceptance. A green Domain/test scaffold allows the next implementation slice only after build, unit, architecture dependency and documentation evidence are archived. API, migration, web, mobile, Worker, external adapter, backup and deployment release gates remain individually required.
