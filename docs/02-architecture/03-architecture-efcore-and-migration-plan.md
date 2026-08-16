# Factory ERP — EF Core Entity ve PostgreSQL Migration Architecture

**Aşama:** ARCHITECTURE

**Durum:** Migration ve persistence tasarımı; production migration kodu değildir.

**Baseline:** O-001–O-014, 2026-08-16 tarihinde proje sahibi tarafından kabul edilmiştir.

## 1. Persistence yaklaşımı

İlk sürümde modüler monolith tek PostgreSQL database ve tek deployment ile çalışacaktır. Bounded context sınırları C# namespace, application service, aggregate root ve permission sınırıyla korunur; gereksiz schema-per-module veya mikroservis ayrımı yapılmaz.

Önerilen persistence yapısı şöyledir:

```text
FactoryErp.Api
FactoryErp.Application
  ├─ Identity
  ├─ Products
  ├─ Customers
  ├─ Sales
  ├─ Warehouse
  ├─ Shipping
  ├─ Invoicing
  ├─ CurrentAccounts
  ├─ Payments
  ├─ Production
  ├─ Employees
  └─ Reporting
FactoryErp.Domain
FactoryErp.Infrastructure
  ├─ Persistence/FactoryErpDbContext
  ├─ Persistence/Configurations
  ├─ Authentication
  ├─ Files
  ├─ Notifications
  └─ BackgroundJobs
```

PostgreSQL’de ilk sürüm için `public` schema kullanılabilir; tablo adları `snake_case`, entity/property isimleri İngilizce, UI metinleri Türkçe kalır. Module sınırları database schema’sıyla değil, aggregate ve application service sınırlarıyla korunur.

## 2. DbContext ve EF Core kuralları

`FactoryErpDbContext`, tüm aggregate’leri aynı transaction içinde birleştirebilen tek persistence boundary’dir. Her bounded context kendi `IEntityTypeConfiguration<TEntity>` sınıfını sağlar. Controller veya doğrudan repository entity’si dışarı açılmaz.

```text
FactoryErpDbContext
  ├─ DbSet<User>
  ├─ DbSet<Product>
  ├─ DbSet<ProductPackaging>
  ├─ DbSet<Customer>
  ├─ DbSet<SalesOrder>
  ├─ DbSet<DeliveryNote>
  ├─ DbSet<DeliveryNoteItemAllocation>
  ├─ DbSet<Shipment>
  ├─ DbSet<LoadPlan>
  ├─ DbSet<Invoice>
  ├─ DbSet<InvoiceItemAllocation>
  ├─ DbSet<CurrentTransaction>
  ├─ DbSet<Payment>
  ├─ DbSet<ProductionOrder>
  └─ DbSet<AuditLog>
```

### 2.1 Ortak mapping kuralları

| Domain tipi | PostgreSQL mapping |
|---|---|
| `Guid` | `uuid` |
| `DateTimeOffset` | `timestamptz` |
| `decimal quantity` | `numeric(18,6)` |
| `decimal money` | `numeric(18,2)`; şirket/para precision kararıyla genişletilebilir |
| `bool` | `boolean` |
| Enum | İlk migration’da `varchar` veya PostgreSQL enum kararı Architecture acceptance’ta sabitlenir; API string değer kullanır |
| JSON snapshot | `jsonb` |
| Long text | `text` |
| Concurrency | `row_version bigint` + update trigger veya Npgsql `xmin`; tek strateji seçilir |
| Soft delete | Sadece master data; ledger/belge için reversal/cancel |

Tüm transactional entity’lerde `id`, `created_at`, `updated_at`, `created_by`, `updated_by` ve gerektiğinde `row_version` bulunur. `created_at` ve `updated_at` UTC tutulur. `is_deleted` ledger, invoice, stock movement veya allocation tablolarında kullanılmaz.

## 3. Aggregate root haritası

| Aggregate root | İç varlıklar | Transaction sorumluluğu |
|---|---|---|
| `Product` | Category, Barcode, Packaging, Price, PhysicalProfile | Ürün/ambalaj/fiyat ana verisi |
| `Customer` | Address, Contact, Note, RiskProfile | Müşteri ana verisi ve public candidate bağlantısı |
| `SalesOrder` | SalesOrderItem, Approval, price snapshot | Sipariş state, approval ve reservation command’leri |
| `DeliveryNote` | DeliveryNoteItem, DeliveryNoteItemAllocation | Sevk miktarı, stock movement, reservation ve issue |
| `Shipment` | ShipmentItem, RoutePlan, RouteStop, Package | Fiziksel sevkiyat, durak ve teslim kanıtı |
| `LoadPlan` | VehicleFitEvaluation, LoadUnit, LoadUnitItem, StopAllocation | Hard/soft kapasite, FFD öneri, manuel lock/replan |
| `Invoice` | InvoiceItem, InvoiceItemAllocation | Fatura allocation, tax/price snapshot ve issue |
| `CurrentAccount` | CurrentTransaction, RiskCalculationRun | Cari ledger ve risk projection |
| `Payment` | PaymentAllocation | Tahsilat/ödeme ve cari credit |
| `ProductionOrder` | ProductionRecord, PersonnelAssignment, QualityRecord | Finished-good receipt ve üretim gerçekleşmesi |
| `Employee` | Attendance, Overtime, LeaveRequest, SalaryRecord | Personel operasyonu ve hassas veri erişimi |

Aggregate root dışından child entity state’i doğrudan değiştirilmez. Örneğin `DeliveryNoteItemAllocation` ayrı controller’dan update edilmez; `IssueDeliveryNoteCommand` veya `ReverseDeliveryNoteCommand` üzerinden yazılır.

## 4. Kritik entity tasarımları

Aşağıdaki gösterimler C# production class’ı değil, Architecture property contract’ıdır. Gerçek class, constructor, domain event ve validation implementation aşamasında üretilecektir.

### 4.1 Quantity value object

```text
QuantitySnapshot
  enteredQuantity: decimal
  enteredPackagingId: Guid?
  quantityBase: decimal
  baseUomId: Guid
  viewModeAtEntry: BaseUnit | Packaging | Breakdown?
  packagingSnapshot: jsonb
  packagingBreakdown: jsonb?
```

`quantityBase` backend tarafından hesaplanır. `enteredQuantity`, `enteredPackagingId`, `baseUomId` ve snapshot birlikte saklanır. `quantityBase > 0` ve UOM precision kontrolü tüm allocation/ledger use-case’lerinde zorunludur.

### 4.2 SalesOrder ve SalesOrderItem

```text
SalesOrder
  id: Guid
  orderNumber: string
  customerId: Guid
  status: Draft | PendingApproval | Approved | Preparing | PartiallyShipped | Fulfilled | Completed | Cancelled
  currencyCode: string
  priceSnapshotVersion: string
  approvalId: Guid?
  rowVersion: long
  items: SalesOrderItem[]

SalesOrderItem
  id: Guid
  salesOrderId: Guid
  productId: Guid
  orderedQty: decimal
  reservedQty: decimal
  shippedQty: decimal
  cancelledQty: decimal
  remainingQty: decimal [projection]
  enteredQuantity: decimal
  enteredPackagingId: Guid?
  packagingSnapshot: jsonb
  partialDeliveryAllowed: boolean
  unitPrice: decimal
  taxCodeId: Guid?
  priceSnapshot: jsonb
  rowVersion: long
```

`remainingQty = orderedQty - shippedQty - cancelledQty` projection’dır. `SalesOrderItem` satırı `rowVersion` ile concurrency kontrol edilir. Sipariş approval command’i reservation create/update ve audit’i aynı transaction’da tamamlar.

### 4.3 Stock, Reservation ve Movement

```text
Stock
  id: Guid
  productId: Guid
  warehouseId: Guid
  locationId: Guid
  onHandQtyBase: decimal
  reservedQtyBase: decimal
  availableQtyBase: decimal [projection]
  rowVersion: long

StockReservation
  id: Guid
  salesOrderItemId: Guid
  productId: Guid
  warehouseId: Guid
  quantityBase: decimal
  consumedQtyBase: decimal
  releasedQtyBase: decimal
  status: Open | PartiallyConsumed | Consumed | Released
  rowVersion: long

StockMovement
  id: Guid
  productId: Guid
  warehouseId: Guid
  locationId: Guid
  movementType: ProductionReceipt | SalesShipment | TransferOut | TransferIn | CountAdjustment | Return | Reversal
  quantityBase: decimal
  sourceEntityType: string
  sourceEntityId: Guid
  reversedFromId: Guid?
  packagingSnapshot: jsonb
  createdAt: timestamptz
```

`StockMovement` append-only ledger’dir. `Stock` ve `StockReservation` özetleri movement/use-case transaction’ı içinde güncellenir. İrsaliye issue ikinci kez stock movement üretemez.

### 4.4 DeliveryNote ve allocation

```text
DeliveryNote
  id: Guid
  documentNumber: string
  salesOrderId: Guid
  customerId: Guid
  status: Draft | Prepared | ReadyToIssue | Issued | Reversed | Closed
  issuedAt: timestamptz?
  rowVersion: long
  items: DeliveryNoteItem[]

DeliveryNoteItem
  id: Guid
  deliveryNoteId: Guid
  salesOrderItemId: Guid
  productId: Guid
  quantityBase: decimal
  enteredQuantity: decimal
  enteredPackagingId: Guid?
  packagingSnapshot: jsonb
  shippedQty: decimal
  invoicedQty: decimal
  waivedQty: decimal
  remainingToInvoice: decimal [projection]
  rowVersion: long

DeliveryNoteItemAllocation
  id: Guid
  salesOrderItemId: Guid
  deliveryNoteItemId: Guid
  quantityBase: decimal
  baseUomId: Guid
  packagingSnapshot: jsonb
  status: Active | Reversed | Voided
  idempotencyKey: string
  payloadHash: string
  reversedFromId: Guid?
  reversalReason: string?
  rowVersion: long
```

Aktif delivery allocation toplamı `orderedQty - cancelledQty` değerini aşamaz. Aynı kaynak + hedef delivery item çifti için ikinci aktif allocation unique constraint ile engellenir.

### 4.5 Invoice ve allocation

```text
Invoice
  id: Guid
  invoiceNumber: string
  customerId: Guid
  status: Draft | ReadyToIssue | Issued | PartiallyPaid | Paid | Reversed | Credited
  currencyCode: string
  subtotal: decimal
  taxTotal: decimal
  grandTotal: decimal
  taxSnapshot: jsonb
  issuedAt: timestamptz?
  rowVersion: long
  items: InvoiceItem[]

InvoiceItem
  id: Guid
  invoiceId: Guid
  productId: Guid
  quantityBase: decimal
  enteredQuantity: decimal
  enteredPackagingId: Guid?
  packagingSnapshot: jsonb
  unitPrice: decimal
  taxCodeId: Guid?
  taxSnapshot: jsonb
  lineTotal: decimal

InvoiceItemAllocation
  id: Guid
  deliveryNoteItemId: Guid
  invoiceItemId: Guid
  quantityBase: decimal
  baseUomId: Guid
  packagingSnapshot: jsonb
  priceSnapshot: jsonb
  taxSnapshot: jsonb
  status: Active | Reversed | Voided
  idempotencyKey: string
  payloadHash: string
  creditedFromId: Guid?
  creditReason: string?
  rowVersion: long
```

Aktif invoice allocation toplamı `DeliveryNoteItem.shippedQty - creditedQty` değerini aşamaz. `Invoice.Issued` command’i allocation, current debit ve audit’i aynı transaction’da yazar; stok hareketi üretmez.

### 4.6 Shipment, LoadPlan ve VehicleFitEvaluation

```text
Shipment
  id: Guid
  status: Preparing | Loaded | InTransit | PartiallyDelivered | Delivered | Exception | Returned
  customerId: Guid?
  routePlanId: Guid?
  rowVersion: long

LoadPlan
  id: Guid
  shipmentId: Guid
  status: Draft | CandidateSelection | Proposed | Validating | Valid | NeedsReview | Replanning | Locked | Loaded | Discrepancy
  version: int
  algorithmName: string
  algorithmVersion: string
  feasibilityStatus: Infeasible | FeasibleWithWarnings | Feasible
  inputSnapshotHash: string
  capacitySnapshot: jsonb
  validationSummary: jsonb
  lockedAt: timestamptz?
  lockedBy: Guid?

VehicleFitEvaluation
  id: Guid
  loadPlanId: Guid
  vehicleId: Guid
  vehicleCapacityId: Guid
  candidateStatus: Candidate | Accepted | Rejected
  rejectionCode: string?
  fitScore: decimal?
  weightRatio: decimal?
  volumeRatio: decimal?
  palletRatio: decimal?
  floorAreaRatio: decimal?
  heightRatio: decimal?
  doorCheckStatus: string
  dimensionCheckStatus: string
  stackingCheckStatus: string
  axleCheckStatus: string
  stopAccessStatus: string
  reasonText: string?
  algorithmVersion: string
  evaluatedAt: timestamptz
```

O-014 gereği `VehicleFitEvaluation` seçilmeyen adayların elenme nedenini saklar. `LoadPlan.Locked` manuel depo onayı ve hard error absence/override kanıtı olmadan yazılamaz.

### 4.7 CurrentAccount, Payment ve Production

```text
CurrentTransaction
  id: Guid
  currentAccountId: Guid
  transactionType: InvoiceDebit | PaymentCredit | Reversal | CreditNote
  debitAmount: decimal
  creditAmount: decimal
  currencyCode: string
  sourceEntityType: string
  sourceEntityId: Guid
  idempotencyKey: string
  createdAt: timestamptz

Payment
  id: Guid
  customerId: Guid
  amount: decimal
  paymentMethodId: Guid
  status: Draft | Applied | Reversed
  reference: string?
  rowVersion: long

ProductionOrder
  id: Guid
  productId: Guid
  plannedQtyBase: decimal
  completedQtyBase: decimal
  status: Planned | Released | InProgress | Paused | Completed | Cancelled
  bomEnabled: boolean = false
  rowVersion: long
```

O-004 baseline’ında `bomEnabled` false veya feature flag ile MVP dışı tutulur; `ProductionMaterial` migration’a alınmaz. Finished-good completion `StockMovement(ProductionReceipt)` üretir. O-008 salary kayıtları ayrı permission ve export audit’i ile korunur.

## 5. EF Core configuration kuralları

### 5.1 Entity configuration checklist

| Konu | Kural |
|---|---|
| Table name | `snake_case`, explicit `ToTable` |
| Required | Nullable/reference alanlar explicit `IsRequired` |
| Decimal | Quantity `numeric(18,6)`, money `numeric(18,2)` |
| JSON | Snapshot alanları `jsonb` ve immutable intent |
| FK | Delete behavior `Restrict`/`NoAction` for ledger/document |
| Cascade | Sadece draft child kayıtları için dikkatli; ledger/allocation cascade delete yok |
| Index | Source/target/status, document number, active barcode, idempotency |
| Concurrency | `row_version` concurrency token; API ETag/If-Match mapping |
| Enum | Stable string code veya controlled PostgreSQL enum; API string values |
| Query filter | Sadece master data soft-delete; belge/ledger global filter ile gizlenmez |

### 5.2 Aggregate write rule

EF Core `SaveChanges` her command için application transaction ile çağrılır. `IssueDeliveryNoteCommandHandler`, `IssueInvoiceCommandHandler`, `ApplyPaymentCommandHandler` ve `CompleteProductionCommandHandler` aggregate + ledger + audit etkilerini tek commit içinde yönetir. Query tarafı `AsNoTracking`, projection ve server-side pagination kullanır.

## 6. PostgreSQL migration stratejisi

Migration’lar küçük, geri izlenebilir ve dependency sırasına göre uygulanır. Her migration tek sorumluluğa sahip olmalı; büyük tabloya index ekleme ve data backfill ayrı migration olarak yürütülmelidir.

| Sıra | Migration | İçerik | Kabul kontrolü |
|---:|---|---|---|
| 0001 | `InitialIdentityAndAudit` | users, roles, permissions, refresh_tokens, audit_logs | Login seed ve audit insert |
| 0002 | `SystemSettingsAndSequences` | system_settings, document_sequences, idempotency_records | Sequence duplicate yok |
| 0003 | `UnitsProductsAndPackaging` | UOM, products, categories, packaging, barcode, images | Packaging conversion test |
| 0004 | `CustomersAndAddresses` | customers, addresses, contacts, notes | Customer FK/duplicate test |
| 0005 | `PricingAndQuoteRequests` | price lists, prices, customer groups, public quote requests | Effective-date price test |
| 0006 | `WarehouseStockLedger` | warehouses, locations, stocks, stock_movements, reservations | Available quantity invariant |
| 0007 | `SalesOrdersAndApprovals` | sales_orders, sales_order_items, approvals | Approval/reservation transaction |
| 0008 | `DeliveryNotesAndShipmentAllocations` | delivery_notes/items, delivery allocations, quantity snapshots | Upper-bound and idempotency test |
| 0009 | `VehiclesRoutesAndLoadPlans` | vehicle types/capacity, vehicles, shipments, routes, load plans, fit evaluations | FFD/vehicle candidate test |
| 0010 | `InvoicesAndInvoiceAllocations` | invoices/items, invoice allocations, tax/price snapshots | Issued delivery + overinvoice test |
| 0011 | `CurrentAccountsAndPayments` | current_accounts, transactions, payment methods/payments/allocations | Debit/credit ledger test |
| 0012 | `ProductionAndMachines` | production orders/records, machines, downtime, personnel | Finished-good receipt test |
| 0013 | `EmployeesAndAttendance` | employees, attendance, overtime, leaves, salary records | Permission/masking test |
| 0014 | `NotificationsAndFiles` | notifications, recipients, files/references | File metadata and notification test |
| 0015 | `IndexesAndConstraints` | Composite/filtered indexes, non-negative checks, unique active constraints | `EXPLAIN`, constraint violation tests |
| 0016 | `TriggersAndConcurrency` | row_version update trigger, allocation upper-bound/deferred checks | Concurrent command test |
| 0017 | `SeedBaselinePermissions` | Accepted permission catalog, system settings, default roles | RBAC matrix test |
| 0018 | `SeedReferenceData` | UOM, tax code placeholders/config, payment methods, packaging levels | Idempotent seed test |

### 6.1 Migration dependency rules

`0001–0002` önce kimlik ve system foundation’ı kurar. Product/UOM/packaging olmadan stock/order migration’ı çalıştırılmaz. SalesOrder’dan önce customer/product gerekir. Delivery allocation, sales order ve delivery note tablolarından sonra gelir. Invoice allocation, delivery note ve invoice tablolarından sonra gelir. Current account tables, invoice/payment source FK’leri kurulmadan seeded transaction kabul etmez.

### 6.2 Constraint ve trigger seti

| Kural | Uygulama |
|---|---|
| Negative quantity | PostgreSQL `CHECK` |
| `shipped + cancelled ≤ ordered` | PostgreSQL `CHECK` + command re-read |
| `invoiced + waived ≤ shipped` | PostgreSQL `CHECK` + invoice allocation sum |
| Allocation upper bound | `SELECT ... FOR UPDATE` + deferred constraint trigger kararı |
| Active source/target duplicate | Partial unique index |
| Idempotency | `company_id + endpoint + idempotency_key` unique |
| Document number | Sequence + document type/year unique |
| Ledger delete | FK `RESTRICT`, application reversal only |
| Row concurrency | `row_version` trigger + EF concurrency token |
| Public exposure | API query allowlist; database view/projection tercih |

### 6.3 Seed ve backfill

Seed’ler idempotent `code` veya stable UUID ile çalışır. Tax code ve price data örnek/placeholder olarak seed edilebilir; gerçek mali müşavir ve satış verileri import job’ı ile ayrıca alınır. Packaging conversion değişirse mevcut row update edilmez; `effective_to` kapanır ve yeni version eklenir.

## 7. Migration çalışma ve rollback prosedürü

Production’a yakın ortamda migration şu sırayla uygulanır:

```text
Backup doğrulama
→ Readiness/health check
→ Migration dry-run veya staging apply
→ Schema version kontrolü
→ Constraint/index validation
→ Smoke test
→ Application deployment
→ Post-deploy acceptance
```

Migration’ın geri alınması için her migration’da `Down` yolu bulunabilir; ancak ledger/document veri silen destructive down migration production’da otomatik çalıştırılmaz. Hatalı schema değişikliği için forward-fix migration tercih edilir. Büyük backfill’ler batch ve resumable job olarak planlanır.

## 8. Architecture acceptance checklist

- Tüm aggregate root’ların owner ve transaction sınırı yazılıdır.
- O-002/O-003 allocation toplamları ve concurrency guard’ları EF/domain/PostgreSQL katmanlarında tutarlıdır.
- O-001 tax/price snapshot ve adapter sınırı migration planına yansımıştır.
- O-004 BOM/hammadde ve O-005 lot/seri MVP dışı tablolar migration listesine yanlışlıkla eklenmemiştir.
- O-012 price list/customer group/snapshot tabloları migration sırasındadır.
- O-014 vehicle-fit/load-plan tabloları algorithm/version/manual approval alanlarını içerir.
- Tüm document/ledger FK’leri fiziksel delete’i engeller.
- `row_version`, `Idempotency-Key`, ProblemDetails ve audit correlation id birlikte izlenebilir.
- Migration sırası temiz bir database üzerinde çalıştırılabilir.
- Seed işlemleri ikinci kez çalıştırıldığında duplicate üretmez.
- Backup ve restore prosedürü migration öncesi ve sonrası test edilmiştir.

Bu belge Architecture persistence tasarımıdır; gerçek `DbContext`, entity class, configuration class ve migration dosyaları implementation/architecture acceptance sonrasında üretilecektir.


## 9. Accepted Architecture ADR overlay

The EF Core design now consumes ADR-001–ADR-009. Positive movement/allocation inputs use the positive quantity type; ordered/shipped/reserved/remaining projections use a zero-capable non-negative type. Packaging and quantity snapshots are immutable JSONB/complex-value mappings with an explicit schema version.

Aggregate child collections use private backing fields and explicit Fluent API field access. `DeliveryNoteItemAllocation`, `InvoiceItemAllocation`, stock movements and current-account transactions cannot be modified through public collection setters.

The PostgreSQL schema exposes `row_version bigint NOT NULL` for API/ETag semantics. A database trigger increments it on update. Npgsql `xmin` is not exposed as the public resource version. EF maps `row_version` as a concurrency token and integration tests must verify stale updates produce the expected conflict mapping.

Allocation writes run under Read Committed with deterministic `SELECT FOR UPDATE` source-row locks, transaction-local re-read and deferred upper-bound validation. Multi-source commands lock rows in ascending stable ID/line order. Deadlock, serialization and unique conflicts rollback the transaction and map to the API contract.

`outbox_messages` is included in the infrastructure persistence model and migration sequence. Outbox records are written in the same transaction as the domain business effects; Worker publication happens only after commit. External adapters are not called from `SaveChanges` or aggregate methods.
