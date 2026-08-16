# Factory ERP — Kodlama Öncesi Readiness ve Test Planı

**Aşama:** Architecture kabulü → Implementation hazırlığı

**Tarih:** 2026-08-16

**Karar sahibi:** Proje sahibi

**İnceleme sonucu:** `READY FOR SCAFFOLD — CONDITIONAL`

## 1. Yönetici özeti

Factory ERP-Lite için iş kararları `O-001–O-014`, teknik Architecture kararları `ADR-001–ADR-011` olarak kabul edilmiştir. API, EF Core, PostgreSQL migration, Docker Compose, CI/CD, Domain ve test blueprint’leri hazırdır. Repository’de henüz `.sln`, `.csproj`, `src/`, `tests/`, Dockerfile, Compose veya aktif GitHub Actions workflow dosyası bulunmadığı için gerçek uygulama henüz başlamamıştır.

Bu nedenle doğrudan tüm modülleri kodlamak yerine **kanıt üreten küçük bir ilk implementation slice** ile ilerlemek gerekir. İlk slice’ın amacı yalnızca Domain kurallarının ve test altyapısının çalıştığını kanıtlamaktır. İlk slice yeşil olmadan API, EF migration, web, mobil, Worker, rapor, personel, dış e-belge veya production deployment başlatılmamalıdır.

> Sonuç: Karar ve Architecture blokajı yoktur; implementation başlangıcı koşulludur. Koşul, ilk scaffold’un kurulması ve aşağıdaki test/quality gate’lerinin geçilmesidir.

## 2. Mevcut durum kontrolü

| Alan | Durum | Kodlama öncesi anlamı |
|---|---|---|
| O-001–O-014 | `DECIDED` | İş kapsamı ve MVP sınırları kabul edilmiş |
| ADR-001–ADR-011 | Kabul edilmiş | Quantity, EF mapping, locking, transaction, outbox, runner ve gate kararları sabit |
| Domain/API/database tasarımı | Hazır | Implementation contract olarak kullanılabilir |
| PostgreSQL migration SQL | Tasarım hazır | Executable EF migration henüz yok |
| Docker Compose | Tasarım hazır | Çalıştırılabilir deployment dosyası henüz yok |
| GitHub Actions | Tasarım hazır | Aktif workflow dosyaları henüz yok |
| Source scaffold | Yok | İlk implementation adımı solution/project yapısıdır |
| Test source tree | Yok | Test project’leri scaffold ile birlikte oluşturulmalıdır |
| Production deployment | Kapalı | İlk slice kanıtları ve release gate olmadan açılamaz |

## 3. MVP kapsamı ve sınırları

### 3.1 İlk implementation slice’ı

İlk slice, ERP’nin en riskli ve diğer modüllere temel olan quantity/allocation/domain sınırını doğrular:

```text
Solution scaffold
→ FactoryErp.Domain
→ FactoryErp.Domain.UnitTests
→ FactoryErp.ArchitectureTests
→ PositiveQuantity / NonNegativeQuantity
→ PackagingSnapshot / QuantitySnapshot
→ SalesOrderItem invariants
→ DeliveryNoteItem allocation invariants
→ Domain events and typed errors
→ CI build + unit + architecture test
```

Bu slice’ta gerçek PostgreSQL, HTTP endpoint veya Docker deployment zorunlu değildir; fakat Domain API’si sonraki Application/Infrastructure katmanlarının ihtiyaç duyacağı sözleşmeyi net biçimde sağlamalıdır.

### 3.2 İkinci implementation slice’ı

İlk slice kanıtları alındıktan sonra persistence ve transaction davranışı eklenir:

```text
FactoryErp.Infrastructure
→ FactoryErp.Application
→ FactoryErp.Migrator
→ PostgreSQL 0001–0008
→ EF mapping/backing field
→ row_version trigger
→ allocation SQL constraint
→ IssueDeliveryNote handler
→ PostgreSQL integration tests
```

Bu aşamada O-002 kısmi sevkiyat, quantity mismatch, idempotency ve concurrency ilk kez gerçek database üzerinde kanıtlanır.

### 3.3 Üçüncü implementation slice’ı

```text
Invoice/current account/payment
→ IssueInvoice handler
→ no-stock-movement assertion
→ outbox_messages + Worker
→ API integration and ProblemDetails contract
→ security/permission tests
```

Daha sonra web, mobil, public katalog, üretim, sevkiyat planlama, raporlama ve personel modülleri ayrı vertical slice’lar olarak ilerletilir. Böylece büyük bir batch merge yerine her modül çalışan ve test edilmiş bir dilim olarak eklenir.

## 4. Kodlama öncesi kalan riskler

Karar blokajı bulunmamakla birlikte implementation sırasında aşağıdaki teknik riskler kontrol edilmelidir:

| Risk | Önlem | İlk kanıt |
|---|---|---|
| Domain blueprint’inde eski `Quantity` adı ile kabul edilen `PositiveQuantity`/`NonNegativeQuantity` ayrımının karışması | İlk slice’ta tek canonical type seti seçilir; eski örnekler derlenebilir source’a taşınmaz | Domain compile + unit test |
| Sıfır remaining miktarının yanlışlıkla transaction quantity’si olarak kullanılması | Positive input, non-negative projection ayrımı | Quantity unit tests |
| Aggregate child collection’larının bypass edilmesi | Private backing field, read-only projection ve architecture test | Architecture test |
| EF Core mapping’in Domain kararlarını bozması | Mapping-only persistence testleri | EF integration test |
| Allocation’ın stoktan veya faturadan bağımsız yazılması | Tek command transaction ve source-row re-read | PostgreSQL integration test |
| İki kullanıcının aynı kalan miktarı aşması | `SELECT FOR UPDATE`, row version, deferred guard | Two-connection concurrency test |
| Idempotency replay’in ikinci hareket üretmesi | Key + payload hash + stored response | API integration test |
| Outbox kaydının business commit’ten ayrılması | Aynı DB transaction’ında outbox insert | Transaction integration test |
| CI tasarımının gerçek path’lerle uyuşmaması | Scaffold sonrası workflow’ları hemen çalıştırmak | PR CI run |
| Docker/backup planının yalnızca dokümanda kalması | Restore edilmiş database üzerinde smoke ve RPO/RTO evidence | Deployment acceptance |

## 5. Test stratejisi

### 5.1 Test piramidi

Testlerin büyük bölümü hızlı ve deterministik Domain/Application unit testlerinden oluşur. PostgreSQL transaction ve concurrency gibi davranışlar gerçek PostgreSQL integration testine bırakılır. E2E testleri yalnızca kritik kullanıcı yolunu ve katmanların birlikte çalıştığını kanıtlar; iş kuralının tek doğrulama kaynağı E2E değildir.

| Katman | Amaç | Araç/ortam | İlk zorunluluk |
|---|---|---|---|
| Domain unit | Value object, invariant, state ve error | xUnit/NUnit, in-memory olmayan saf test | İlk slice |
| Application unit | Handler orchestration, authorization, idempotency port mapping | xUnit + fake/mock port | İkinci slice |
| Architecture test | Dependency direction, public API/EF leakage, naming | NetArchTest veya eşdeğer | İlk slice |
| Persistence integration | EF mapping, FK, check, trigger, index, migration | Gerçek PostgreSQL container | İkinci slice |
| API integration | Route, DTO, auth, ProblemDetails, transaction | ASP.NET `WebApplicationFactory` + PostgreSQL | İkinci/üçüncü slice |
| Contract test | OpenAPI, web/mobile/public response compatibility | OpenAPI schema assertions | API slice |
| Security test | IDOR/BOLA, permission, masking, rate limit, secret leakage | API integration + static checks | API/public slice |
| Concurrency test | Lock, re-read, row version, idempotency | İki veya daha fazla PostgreSQL connection | Allocation slice |
| E2E smoke | Kritik uçtan uca akış | API/browser/mobile smoke | MVP release candidate |
| Deployment acceptance | Health, migration, Compose, backup/restore, LAN HTTPS | Isolated host/container | Release öncesi |

### 5.2 Domain unit test kapsamı

İlk test grubu hiçbir database veya HTTP bağımlılığı olmadan çalışmalıdır. Her test tek bir kuralı doğrulamalı ve failure durumunda hangi invariant’ın bozulduğunu göstermelidir.

| Test ID | Senaryo | Beklenen sonuç |
|---|---|---|
| `QTY-001` | `PositiveQuantity.Create(0)` | `QUANTITY_MUST_BE_POSITIVE` |
| `QTY-002` | Negatif positive quantity | Domain error |
| `QTY-003` | `NonNegativeQuantity.Create(0)` | Başarılı |
| `QTY-004` | UOM precision aşımı | `QUANTITY_PRECISION_EXCEEDED` |
| `QTY-005` | Kapalı ambalajda fractional giriş | `PACKAGING_PARTIAL_NOT_ALLOWED` |
| `QTY-006` | Packaging katsayısıyla base conversion | Doğru `quantityBase` |
| `QTY-007` | Snapshot master katsayı değişiminden etkilenmiyor | Eski snapshot aynı kalır |
| `ALLOC-001` | Allocation source miktarını aşmıyor | Başarılı sınır veya domain error |
| `ALLOC-002` | Over-shipment | `OVER_ALLOCATION` |
| `ALLOC-003` | Reversal negative record üretmiyor | Pozitif reversal + referans |
| `STATE-001` | Approved order partial shipment | `PartiallyShipped` |
| `STATE-002` | Remaining sıfıra geldiğinde fulfillment | `Fulfilled/Completed` policy |
| `STATE-003` | Issued delivery doğrudan Draft’a dönmüyor | Transition error |
| `EVENT-001` | Aggregate domain event üretiyor | Event collection’da mevcut |
| `ERROR-001` | Domain error API mapping input’u | Typed code korunur |

### 5.3 Application/CQRS unit test kapsamı

Handler unit testlerinde database yerine port/fake kullanılır. Amaç, handler’ın doğru sırayı ve doğru transaction sınırını yönettiğini göstermektir.

| Handler | Zorunlu testler |
|---|---|
| `IssueDeliveryNoteHandler` | Quantity yeniden hesaplama, source lock port’u, allocation, stock movement, reservation consume/release, audit, outbox ve rollback sırası |
| `IssueInvoiceHandler` | Yalnızca `DeliveryNote.Issued`, remaining invoice limit, current debit, no stock movement, audit, outbox ve idempotency |
| `ApproveOrderHandler` | Risk soft/hard block, override permission, reservation ve approval audit |
| `ApplyPaymentHandler` | Payment allocation, current credit, duplicate key ve reversal |
| `CompleteProductionHandler` | Finished-good receipt, machine/personnel record, no BOM/lot side effect |

Her handler için başarısız bir adımın önceki allocation, stock, current transaction, audit ve outbox etkilerini transaction abstraction üzerinden geri aldırdığı test edilir.

### 5.4 PostgreSQL integration test kapsamı

Persistence testleri SQLite ile ikame edilmemelidir; allocation trigger’ı, row lock, PostgreSQL `numeric`, JSONB, deferred constraint ve transaction isolation gerçek PostgreSQL üzerinde test edilmelidir.

Zorunlu senaryolar şunlardır:

| Test ID | Senaryo | Beklenen sonuç |
|---|---|---|
| `PG-001` | Temiz database’e migration 0001–0001 uygulanır | Schema ve seed başarılı |
| `PG-002` | Migration 0001–0018 sıralı uygulanır | Version/checksum doğru |
| `PG-003` | FK/delete restriction | Ledger ve allocation fiziksel silinemez |
| `PG-004` | Deferred allocation upper-bound | Commit aşımda rollback olur |
| `PG-005` | Row version trigger | Update sonrası version artar |
| `PG-006` | Same idempotency key/same payload | İlk response tekrar döner |
| `PG-007` | Same key/different payload | `IDEMPOTENCY_PAYLOAD_MISMATCH` |
| `PG-008` | İki connection aynı 600 miktara 400’er ister | Yalnızca biri commit eder |
| `PG-009` | Deadlock/serialization retry | Fresh transaction ile sınırlı retry |
| `PG-010` | Invoice allocation shipped quantity’i aşar | Rollback + `OVER_ALLOCATION` |
| `PG-011` | Invoice issue | Current debit oluşur, stock movement oluşmaz |
| `PG-012` | Outbox aynı transaction’da | Business rollback’te outbox da rollback olur |

### 5.5 API, security ve contract testleri

API testleri status code kadar response body, ProblemDetails code, correlation ID, retryable flag, ETag ve permission davranışını doğrular. Her kritik endpoint için pozitif ve negatif test birlikte yazılır.

Gerekli güvenlik senaryoları arasında başka müşterinin sipariş, rota, paket veya teslim kanıtına IDOR/BOLA ile erişememe; maaş alanlarının role göre maskelenmesi; public API’nin iç stok/cari/risk/personel alanlarını döndürmemesi; refresh token ve secret’ların loglanmaması; public quote request’in rate limit/bot/consent sınırlarını koruması bulunur.

### 5.6 E2E smoke senaryoları

MVP release candidate için minimum senaryo şöyledir:

```text
Admin/login
→ Product + Packaging
→ Customer
→ Quote Request review
→ SalesOrder create/approve
→ Stock reservation
→ Partial DeliveryNote issue
→ Shipment/LoadPlan manual lock
→ Invoice issue
→ Payment apply
→ Current statement
```

Üretim smoke senaryosu ayrı tutulur:

```text
ProductionOrder
→ machine/personnel record
→ finished-good completion
→ stock receipt
→ report projection
```

Her E2E senaryosunun sonunda state, allocation, stock ledger, current ledger, audit ve outbox sonuçları API/database assertion ile doğrulanır.

## 6. CI/CD quality gate’leri

| Gate | Pull Request | Main | Release |
|---|---:|---:|---:|
| Format/static analysis | Zorunlu | Zorunlu | Zorunlu |
| Domain/Application unit | Zorunlu | Zorunlu | Zorunlu |
| Architecture dependency test | Zorunlu | Zorunlu | Zorunlu |
| PostgreSQL migration/integration | Zorunlu | Zorunlu | Zorunlu |
| API contract/security | API varsa zorunlu | Zorunlu | Zorunlu |
| Docker image/SBOM/vulnerability | Önerilen | Zorunlu | Zorunlu |
| Backup freshness | Hayır | Hayır | Zorunlu |
| Restore/RPO/RTO evidence | Hayır | Schedule/manual | Zorunlu |
| Production deployment approval | Hayır | Hayır | Zorunlu |

Boş scaffold döneminde CI’nin sessizce başarılı görünmemesi gerekir. Solution ve project dosyaları oluşturulduktan sonra geçiş amaçlı `hashFiles` koşulları kaldırılmalı, gerçek test project path’leri zorunlu hale getirilmelidir.

## 7. Definition of Done

Bir implementation slice ancak aşağıdaki şartlarla tamamlanmış sayılır:

1. Scope, ilgili O-ID/ADR ve canonical tasarım belgesiyle eşleşir.
2. Domain invariant’ları unit test ile doğrulanır.
3. Transaction veya persistence etkileri gerekiyorsa PostgreSQL integration test bulunur.
4. API varsa DTO, ProblemDetails, permission, idempotency ve ETag testleri bulunur.
5. Concurrency etkisi varsa en az iki connection senaryosu bulunur.
6. Audit ve outbox davranışı test edilmiştir.
7. Migration forward uygulama ve temiz database testinden geçmiştir.
8. `git diff --check`, build, test, static analysis ve security gate’leri yeşildir.
9. Dokümantasyon ve numbered mirror günceldir.
10. Release/deployment etkisi varsa backup, restore, health ve rollback kanıtı eklenmiştir.

## 8. Kodlama sırası ve kontrol noktaları

Önerilen sıra şöyledir:

```text
1. Solution/project scaffold
2. Domain common types
3. Domain unit tests
4. Architecture dependency tests
5. CI PR workflow
6. Infrastructure DbContext/configuration skeleton
7. PostgreSQL migration 0001–0008
8. Persistence integration tests
9. Application command/query ports
10. IssueDeliveryNote vertical slice
11. API integration/contract tests
12. Invoice/current account vertical slice
13. Outbox/Worker integration tests
14. Web/mobile/public slices
15. Deployment/backup/restore acceptance
```

Her kontrol noktasında önce test ve acceptance kanıtı üretilir, ardından sonraki modüle geçilir. Büyük bir modülü testleri sona bırakarak geliştirmek bu proje için kabul edilen yöntem değildir.

## 9. Implementation başlamadan önce son onay listesi

- `PositiveQuantity` ve `NonNegativeQuantity` için tek gerçek C# API’si seçildi.
- Solution adı, target framework, nullable/implicit usings ve analyzer policy sabitlendi.
- Test framework’ü ve assertion/mock yaklaşımı sabitlendi.
- PostgreSQL test container sürümü production major sürümüyle uyumlu hale getirildi.
- CI gerçek project path’lerini kullanacak şekilde scaffold edildi.
- Secret ve environment naming sözleşmesi `.env.example` ile yazıldı.
- Test data factory’leri sentetik müşteri/personel verisiyle tanımlandı.
- İlk slice dışındaki feature’lar backlog sınırında tutuldu.

Bu onay listesi tamamlandığında `factory-erp-implementation` skill’i çalıştırılabilir ve yalnızca ilk Domain/test slice’ı başlatılabilir.
