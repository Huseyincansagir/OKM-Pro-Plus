# IMPLEMENTATION SLICE 001 — Kalan Riskler ve Migration Gereksinimleri

**Tarih:** 2026-08-16

**Kaynak rapor:** [`implementation-session-001.md`](./implementation-session-001.md)

**Amaç:** Domain scaffold’ı geçilmiş olsa da persistence/application slice’ına geçişte kapanması gereken riskleri ve PostgreSQL/EF Core migration gereksinimlerini ayrıntılandırmak.

## 1. Kapsam ve sonuç özeti

Slice 001, `FactoryErp.Domain` common/value object yapısını, quantity/allocation invariant’larını, Domain unit testlerini ve architecture dependency sınırını başarıyla doğrulamıştır. Bu sonuç, iş kurallarının saf C# seviyesinde çalıştığını gösterir; ancak aynı kuralların PostgreSQL transaction, EF Core mapping, row locking, unique index, deferred constraint, idempotency, audit ve outbox ile birlikte korunduğunu henüz kanıtlamaz.

Canonical migration planı `0001–0018` arasında küçük ve dependency sırası korunmuş migration’lar öngörür. Repository’de ise önceki G1–G5 çalışmalarından gelen timestamp tabanlı ve bazıları birden fazla canonical adımı birleştiren migration’lar bulunmaktadır. Bu nedenle sonraki migration yazımından önce **migration-baseline reconciliation** yapılmalıdır. Uygulanmış migration geçmişi yeniden adlandırılmamalı veya geriye dönük olarak yeniden yazılmamalıdır; canonical plan ile mevcut veritabanı arasındaki eşleştirme ayrı bir tablo ve integration test ile sabitlenmelidir.

> Domain testlerinin yeşil olması, PostgreSQL’de iki concurrent transaction’ın allocation üst sınırını aşamayacağını tek başına kanıtlamaz. Bu davranış gerçek PostgreSQL connection’ları, row lock, re-read ve database constraint/trigger ile ayrıca test edilmelidir.

## 2. Kalan risk kayıtları

Aşağıdaki riskler Slice 001 içinde bilinçli olarak kapatılmamıştır. Her risk için sonraki katman, etkisi ve kapanış kanıtı tanımlanmıştır.

| ID | Risk | Neden Slice 001’de kapanmadı? | Öncelik | Kapanış kanıtı |
|---|---|---|---|---|
| R-001 | Domain–database invariant drift | Domain projection’ları ve allocation toplamları database’de henüz gerçek transaction/constraint ile doğrulanmadı | P0 | PostgreSQL constraint + integration test |
| R-002 | Cross-row allocation upper bound | PostgreSQL `CHECK` başka satırların toplamını doğrudan kontrol edemez | P0 | `SELECT FOR UPDATE` + re-read + deferred trigger/constraint testi |
| R-003 | Duplicate request/idempotency açığı | Domain yalnızca aynı allocation entity ID’sini tekrar eklemeyi engeller; farklı ID’li aynı command payload’ı application sınırındadır | P0 | `(company_scope, endpoint, idempotency_key)` unique index, payload hash ve replay testi |
| R-004 | Concurrent partial shipment/invoice yarışı | Unit test tek process içindedir; iki gerçek transaction connection’ı yoktur | P0 | İki connection race testi, tek commit ve kontrollü 409 sonucu |
| R-005 | Row version/ETag uyumsuzluğu | Domain `RowVersion` alanı public HTTP concurrency sözleşmesini uygulamaz | P0 | `row_version` trigger, EF concurrency token, `ETag`/`If-Match` ve typed ProblemDetails testi |
| R-006 | Atomic transaction sınırı | Stock movement, reservation, document state, ledger, audit ve outbox birlikte commit edilmedi | P0 | Commit/rollback integration testi; başarısız command’da sıfır yan etki |
| R-007 | Allocation granularity/index çelişkisi | Domain mevcut testinde aynı source/target için farklı ID’li allocation’ların toplamı sınanıyor; canonical SQL ise aktif source/target çiftini unique kılıyor | P0 | Tasarım kararı: tek aktif source/target mı, çoklu allocation mı; ardından Domain/EF/SQL uyum testi |
| R-008 | Private collection mapping | EF materialization’ın private backing field’a, mutation’ın aggregate method’una bağlı olduğu henüz kanıtlanmadı | P1 | `HasField`, field access ve read-only projection integration testi |
| R-009 | Snapshot ve precision kaybı | JSONB snapshot schema/version, `numeric(18,6)` ve money precision gerçek DB’de doğrulanmadı | P1 | Round-trip snapshot, precision boundary ve timezone testi |
| R-010 | Ledger mutation/deletion | Domain reversal semantiği var; FK `RESTRICT`, no-cascade ve append-only davranış henüz DB’de kanıtlanmadı | P0 | Delete/reversal integration testi |
| R-011 | Migration order/FK drift | Canonical 0001–0018 ile mevcut timestamp migration’larının birebir eşleştiği varsayılamaz | P0 | Clean DB apply, schema inventory ve baseline mapping |
| R-012 | Seed duplicate/state drift | Seed’lerin ikinci ve üçüncü çalışmada aynı sonucu üretmesi tüm canonical reference data için kanıtlanmadı | P1 | Idempotent seed testleri ve stable code/UUID assertion’ları |
| R-013 | Migration rollback/backfill riski | Belge/ledger verisi silen `Down` migration production’da güvenli değildir; backfill stratejisi yazılmadı | P0 | Staging rollback/forward-fix prosedürü ve backup restore kanıtı |
| R-014 | Typed error mapping | Domain error kodları var; SQL/EF exception’larının güvenli `ProblemDetails` response’una mapping’i bu slice’ta yok | P1 | 422/409 error contract integration testleri |
| R-015 | Outbox delivery consistency | Domain event collection mevcut; transactional outbox ve post-commit worker davranışı henüz yok | P1 | Aynı DB transaction’ında outbox insert, commit sonrası publish ve retry/dead-letter testi |
| R-016 | Migration runtime operasyonu | Migration’ın API startup’ında değil controlled Migrator’da çalışması ve health gate’leri sonraki deployment/persistence slice’ında kanıtlanmalı | P1 | Migrator clean-run, API startup migration yok assertion’ı |
| R-017 | Skill paketi senkronizasyonu | Repository `.claude/skills/factory-erp-implementation/SKILL.md` yolu eksik; archive kopyası mevcut | P1 | Runtime skill directory’nin repository’ye eklenmesi veya resmi senkronizasyon kararı |

### 2.1 En kritik teknik tutarsızlık: allocation granularity

Canonical PostgreSQL taslağı `delivery_note_item_allocations` için aşağıdaki mantığı öngörür:

```sql
CREATE UNIQUE INDEX ux_delivery_allocation_active_target
    ON delivery_note_item_allocations(sales_order_item_id, delivery_note_item_id)
    WHERE status = 'Active';
```

Bu kural aynı source/target çiftinde ikinci aktif allocation’ı engeller. Mevcut Domain testleri ise önce 7.000, sonra aynı delivery item’a farklı ID ile 4.000 allocation eklemeyi deneyerek toplamın planned miktarı aşması halinde `OVER_ALLOCATION` beklemektedir. Database unique index’i uygulanırsa bu testteki ikinci kayıt `OVER_ALLOCATION` yerine unique violation ile reddedilebilir.

Migration başlamadan önce bu teknik ayrım çözülmelidir:

| Seçenek | Model | Sonuç |
|---|---|---|
| A | Bir source/target çifti için tek aktif allocation | Domain ikinci eklemeyi duplicate/over-allocation olarak reddeder; unique index canonical tasarımla uyumlu kalır |
| B | Aynı source/target için birden fazla aktif allocation | Partial unique index kaldırılır veya daha geniş business key kullanılır; toplam üst sınırı trigger/re-read korur |

Bu konu yeni bir business kararından çok, mevcut Domain test modeli ile canonical SQL constraint’in aynı allocation granülerliğini kullanıp kullanmadığının netleştirilmesidir. Persistence slice başlamadan önce owner, test ve migration mapping ile kapatılmalıdır.

## 3. Migration baseline reconciliation

Mevcut repository’de G1–G5 ile gelen timestamp migration’ları bulunurken canonical doküman `0001–0018` sorumluluk sırasını kullanır. Bu iki adlandırma sisteminin karıştırılmaması gerekir.

| Canonical sıra | Sorumluluk | Mevcut repository ile beklenen eşleşme | Reconciliation gereksinimi |
|---:|---|---|---|
| 0001 | Identity and Audit | Initial identity migration ve authentication alanları | Gerçek tablo/kolon/index listesi çıkarılmalı |
| 0002 | Settings, sequences, idempotency | Foundation seed/settings/idempotency parçaları | Idempotency key scope ve response replay alanları doğrulanmalı |
| 0003 | UOM, products, packaging | Catalog/packaging migration | Packaging version/effective date ve snapshot alanları karşılaştırılmalı |
| 0004 | Customers and addresses | Sales foundation içindeki customer yapısı | Candidate/active customer ayrımı ve FK’ler doğrulanmalı |
| 0005 | Pricing and quote requests | Sales/quote migration | Effective-date price ve quote source/status karşılaştırılmalı |
| 0006 | Warehouse, stock, reservations | Catalog/stock migration ve sales reservation | `available = on_hand - reserved` constraint/trigger kontrol edilmeli |
| 0007 | Sales orders and approvals | Sales orders/approval migration | `remaining_qty` projection ve approval FK’leri doğrulanmalı |
| 0008 | Delivery notes and shipment allocations | G5 delivery migration’ın delivery bölümü | Allocation granularity, idempotency ve upper-bound mapping çözülmeli |
| 0009 | Vehicles, routes and load plans | Henüz sonraki logistics slice kapsamı | O-014 alanları ve migration bağımlılıkları tanımlanmalı |
| 0010 | Invoices and invoice allocations | G5 invoice migration bölümü | Issued delivery source, tax/price snapshot ve active target kuralı doğrulanmalı |
| 0011 | Current accounts and payments | G5 finance migration bölümü | Debit/credit one-sided check, payment allocation ve currency kapsamı doğrulanmalı |
| 0012 | Production and machines | Henüz sonraki production slice | Finished-good receipt ve StockMovement FK’leri planlanmalı |
| 0013 | Employees and attendance | Henüz sonraki HR slice | Salary masking/export permission ve sensitive data sınırı korunmalı |
| 0014 | Notifications and files | Henüz sonraki adapter/file slice | Outbox/file reference ve fiziksel dosya lifecycle’ı ayrıştırılmalı |
| 0015 | Indexes and constraints | Bazıları ilgili migration’larda dağınık olabilir | Consolidation migration yalnızca mevcut şemaya göre yazılmalı; duplicate constraint üretilmemeli |
| 0016 | Triggers and concurrency | Foundation/G5’te kısmen mevcut olabilir | `row_version`, allocation deferred guard ve projection trigger’ları inventory ile doğrulanmalı |
| 0017 | Permission and system seeds | Identity seed’leri | Stable permission code, role assignment ve repeatability testi |
| 0018 | Reference seeds | UOM, tax, payment method, packaging reference data | Stable code/UUID, idempotent upsert, no destructive overwrite |

### 3.1 Reconciliation çıktısı

P persistence/application slice başlamadan önce aşağıdaki dosya veya test artifact’ı üretilmelidir:

```text
migration-baseline-map.md
```

Bu artifact her canonical migration için mevcut migration dosyasını, uygulandığı schema version’ı, tablo/kolon/index/trigger kapsamını, eksik alanları ve forward-fix gereksinimini gösterir. Temiz database’de `dotnet ef database update` veya controlled Migrator çalıştırıldığında bu mapping’in beklenen son state ile eşleştiği schema inventory sorgusuyla doğrulanmalıdır.

## 4. Ortak SQL ve EF Core gereksinimleri

### 4.1 Tip ve ortak alanlar

| Alan | PostgreSQL | EF Core gereksinimi | Kabul testi |
|---|---|---|---|
| Identity | `uuid` | `Guid`, generated identity veya application ID policy | Empty ID reddi, FK round-trip |
| UTC time | `timestamptz` | `DateTimeOffset`, UTC normalization | Timezone round-trip |
| Quantity | `numeric(18,6)` | Explicit precision `(18,6)` | Scale 0–6 boundary |
| Money | `numeric(18,2)` | Explicit precision `(18,2)` | Rounding/tax total |
| Snapshot | `jsonb` | Owned/complex JSON mapping veya converter | Immutable round-trip + version |
| Status | `varchar`/controlled enum | Stable string conversion | Unknown/invalid status rejection |
| Concurrency | `bigint NOT NULL DEFAULT 1` | `.IsConcurrencyToken()` | Stale update conflict |
| Ledger delete | FK `RESTRICT`/`NO ACTION` | DeleteBehavior.Restrict | Physical delete blocked |

Her transactional entity için `id`, `created_at`, `updated_at`, `created_by`, gerektiğinde `updated_by` ve `row_version` standardı uygulanmalıdır. Ledger, invoice, allocation ve stock movement tablolarında soft-delete query filter kullanılmamalıdır; reversal/status modeli kullanılmalıdır.

### 4.2 Allocation ve idempotency

Delivery ve invoice allocation tablolarında en az şu alanlar bulunmalıdır:

```text
source entity id
source/target line id
quantity_base > 0
base_uom_id
entered_quantity > 0
entered_packaging_id nullable
packaging_snapshot jsonb
status
idempotency_key
payload_hash
reversal/credit reference
created_by / created_at
row_version
```

İdempotency anahtarı yalnızca `allocation.id` değildir. Command endpoint’i, company scope ve payload hash ile birlikte değerlendirilmelidir. Aynı key aynı payload ile ilk committed response’u replay eder; farklı payload aynı key ile gönderilirse yeni business transaction başlamadan `IDEMPOTENCY_PAYLOAD_MISMATCH` üretilir.

### 4.3 Cross-row allocation upper bound

`CHECK (quantity_base > 0)` tek satır pozitifliğini korur; fakat `SUM(active allocations) <= source remaining` kuralını koruyamaz. Bu nedenle aşağıdaki çift koruma zorunludur:

1. Application command source row’ı deterministic `SELECT ... FOR UPDATE` ile kilitler.
2. Lock sonrasında source quantity, active allocation toplamı ve remaining projection yeniden okunur.
3. Domain guard güncel değerlerle çalıştırılır.
4. Allocation, stock/reservation/projection etkileri aynı transaction’da yazılır.
5. Deferred constraint trigger veya eşdeğer database son kontrolü commit öncesinde çalışır.
6. Üst sınır ihlalinde transaction rollback olur ve `OVER_ALLOCATION` veya quantity context’inde `QUANTITY_CONCURRENCY_CONFLICT` döner.

### 4.4 Projection constraints

Aşağıdaki ilişkiler hem application/domain hesaplarında hem de database tarafında korunmalıdır:

```sql
shipped_qty + cancelled_qty <= ordered_qty
invoiced_qty + waived_qty <= shipped_qty
reserved_qty <= on_hand_qty
available_qty = on_hand_qty - reserved_qty
consumed_qty + released_qty <= reservation.quantity_base
payment_allocated_total <= payment.amount
payment_allocated_total <= invoice.open_balance
```

Başka satır toplamlarına bağlı kurallar için yalnızca `CHECK` kullanılmamalıdır. Trigger, deferred constraint veya transaction lock/re-read kombinasyonu belirlenmelidir.

## 5. Migration sırası ve kapsamı

Canonical sıra küçük, geri izlenebilir migration’lar öngörür. Her migration tek sorumlulukta tutulmalı; büyük index/backfill işlemleri tablo oluşturma migration’ından ayrılmalıdır.

| Sıra | Migration kapsamı | Bağımlılıklar | Kabul kriteri |
|---:|---|---|---|
| 0001 | users, roles, permissions, role/user joins, refresh tokens, audit logs | Extensions | Duplicate username/email reddi, audit insert |
| 0002 | settings, document sequences, idempotency records | 0001 users | Sequence uniqueness; same-key replay/mismatch |
| 0003 | UOM, products, categories, packaging, barcode, images, physical profiles | 0001/0002 | `scale 0–6`, positive conversion, effective version |
| 0004 | customers, addresses, contacts, notes | 0001 | Customer FK and candidate/active status |
| 0005 | price lists, customer groups, product prices, quote requests | 0003/0004 | Effective-date price and quote isolation |
| 0006 | warehouses, locations, stocks, stock movements, reservations, quantity snapshots | 0003/0004 | Available quantity, append-only movement, reservation bounds |
| 0007 | sales orders, items, approvals | 0003/0004/0005/0006 | Approval/reservation transaction; remaining projection |
| 0008 | delivery notes, items, delivery allocations | 0007/0006 | Issued state, source/target allocation, quantity upper bound |
| 0009 | vehicles, capacity, drivers, shipments, routes, load plans, fit evaluations | 0008 + master data | Hard capacity, FFD candidate, manual lock fields |
| 0010 | invoices, items, invoice allocations, tax/price snapshots | 0008/0009 optional shipment link | Issued delivery source; no stock movement |
| 0011 | current accounts, transactions, payment methods, payments, allocations | 0004/0010 | One-sided debit/credit, payment/open balance bounds |
| 0012 | production orders, machines, records, downtime, personnel assignment | 0003/0006 | Finished-good receipt movement |
| 0013 | employees, attendance, overtime, leave, salary summary | 0001 | Permission/masking/export audit |
| 0014 | notifications, recipients, file references | 0001/0017/outbox | Metadata/reference only, no unsafe external call |
| 0015 | composite/partial indexes and cross-table constraint helpers | 0001–0014 | `EXPLAIN`, unique active, index existence |
| 0016 | row-version triggers, deferred allocation/projection checks | tables affected by triggers | Stale ETag and concurrent race tests |
| 0017 | permission catalog, roles, settings | 0001/0002 | Idempotent stable code seed |
| 0018 | UOM/tax/payment/packaging reference seeds | 0003/0011 | Repeat seed has no duplicate or destructive overwrite |

### 5.1 MVP dışı migration sınırları

O-004 nedeniyle BOM/hammadde tüketimini zorunlu kılan `production_materials` tabloları bu migration planına eklenmemelidir. O-005 nedeniyle lot/serial tabloları da MVP migration’ına eklenmemelidir. Bu alanlar yalnızca yeni karar ve yeni migration setiyle açılabilir. Production order ve finished-good receipt kapsamı ise ayrı persistence slice’ında, `StockMovement(ProductionReceipt)` ile planlanmalıdır.

## 6. Migration çalışma, rollback ve seed gereksinimleri

Migration execution controlled Migrator üzerinden yapılmalıdır; API startup’ında otomatik migration çalıştırılmamalıdır. Uygulama sırası aşağıdaki gibi olmalıdır:

```text
Backup doğrulama
→ health/readiness kontrolü
→ clean/staging migration apply
→ schema version ve migration history kontrolü
→ table/column/FK/check/index/trigger inventory
→ seed çalıştırma ve ikinci seed replay
→ constraint/ledger smoke test
→ API veya worker deployment
→ post-deploy acceptance
```

Production’da ledger/document satırlarını silen destructive `Down` migration otomatik çalıştırılmamalıdır. Hatalı migration için forward-fix veya backup restore tercih edilir. Büyük backfill gerekiyorsa ayrı, batch, resumable ve progress/audit bilgili job olarak tasarlanmalıdır.

Seed’ler stable `code` veya UUID ile idempotent olmalıdır. Tax code, payment method, UOM ve permission seed’leri ikinci çalıştırmada duplicate üretmemeli, canlı fiyat/snapshot veya ledger verisini sessizce overwrite etmemelidir. Packaging conversion değişiminde mevcut version update edilmez; eski version `effective_to` ile kapanır ve yeni version eklenir.

## 7. Persistence migration acceptance checklist

Migration/application slice tamamlanmadan aşağıdaki maddelerin tümü kanıtlanmalıdır:

| Gate | Kabul koşulu |
|---|---|
| Clean database | 0001–0018 veya mevcut baseline mapping sırası boş PostgreSQL database’de çalışır |
| Repeatability | Migration/seed ikinci çalıştırmada duplicate veya state drift üretmez |
| Schema inventory | Beklenen tablo, kolon, type, PK, FK, check, index ve trigger’lar bulunur |
| Quantity | Negative ve precision dışı miktar database tarafından reddedilir |
| Allocation | Exact boundary kabul, over-allocation rollback ve duplicate policy doğrulanır |
| Concurrency | İki gerçek PostgreSQL connection race testinde yalnızca biri commit eder |
| Row version | Stale `If-Match` 409 typed ProblemDetails’a map edilir |
| Idempotency | Same key/same payload replay; same key/different payload mismatch; ikinci ledger yok |
| Transaction | Delivery, invoice, payment command başarısız olduğunda kısmi yan etki kalmaz |
| Ledger | Stock/current transaction/invoice allocation fiziksel delete ile silinemez |
| Snapshot | Packaging, quantity, price ve tax snapshot’ları round-trip immutable kalır |
| Error mapping | `QUANTITY_BASE_MISMATCH` 422, concurrency conflict 409, safe response schema |
| Audit/outbox | Kritik state transition audit’lenir; outbox aynı transaction’da yazılır |
| Restore | Backup, restore ve schema verification kanıtı bulunur |

## 8. Önerilen sonraki implementation sırası

Bir sonraki implementation slice’ı doğrudan bütün 0001–0018 tablolarını bir kerede yazmamalıdır. Önce mevcut timestamp migration geçmişi ile canonical plan arasında mapping çıkarılmalı, ardından aşağıdaki küçük aşamalarla ilerlenmelidir:

1. **Persistence baseline reconciliation:** mevcut DbContext, migrations, applied schema ve canonical plan karşılaştırması.
2. **EF mapping hardening:** private backing field, owned/JSON snapshot, decimal precision, FK restrict, row version ve stable status mapping’leri.
3. **Foundation constraints:** idempotency, document sequence, common audit, quantity checks ve version trigger.
4. **Allocation persistence:** delivery/invoice allocation tables, duplicate policy kararı, partial unique indexes ve deferred upper-bound guard.
5. **Application transaction slice:** issue delivery, issue invoice ve apply payment command’lerinin lock/re-read, ledger, audit/outbox atomikliği.
6. **PostgreSQL integration tests:** clean migration, constraint, rollback, two-connection concurrency, idempotency ve current-account ledger testleri.
7. **Only after evidence:** API contract/error mapping ve sonraki UI/mobile integration.

Bu sıra tamamlanmadan web, mobile, production feature veya external adapter çalışmasına geçilmemelidir.

## References

[1]: ./implementation-session-001.md "IMPLEMENTATION SLICE 001 — Domain Scaffold"
[2]: ./implementation-ready.md "Factory ERP Implementation Ready Gate"
[3]: ./architecture-efcore-and-migration-plan.md "Factory ERP EF Core Entity ve PostgreSQL Migration Architecture"
[4]: ./postgresql-18-migration-sql-specification.md "PostgreSQL 0001–0018 Migration SQL Specification"
[5]: ./architecture-decision-baseline.md "Accepted Architecture Decision Baseline"
[6]: ./quantity-error-handling-and-allocation-sql.md "Quantity Error Handling and Allocation SQL"
[7]: ./mvp-test-strategy.md "MVP Test Strategy"
[8]: ./decision-log.md "Factory ERP Design Decision Log"
