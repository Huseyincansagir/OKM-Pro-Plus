# L4-B5 Bounded Slice Planı — Locked LoadPlan Actual Load Verification

**Tarih:** 2026-08-17

**Durum:** Implementation tamamlandı; evidence ve release gate aşaması

**Ön koşul:** L4-B4 `ValidateLoadPlan`, warning resolution, manual-change audit ve `LockLoadPlan` gate’i tamamlanmış olmalıdır.

## 1. Amaç

L4-B5’in amacı, yalnızca `Locked` durumundaki bir LoadPlan üzerinden depo yükleme doğrulamasını gerçekleştirmektir. Depo personeli araç yüklerken ShipmentPackage barkodunu okur; sistem barkodu aynı shipment ve locked LoadPlan içindeki beklenen LoadUnit/LoadUnitItem allocation’ı ile karşılaştırır, gerçek yükleme sonucunu audit edilebilir biçimde kaydeder ve kabul edilen paket/LoadUnit/Shipment state’lerini ilerletir.

Bu bounded slice’ın ana çıktısı **planned load ile actual load arasındaki farkın transaction-safe ve idempotent biçimde görünür hale getirilmesidir**. Barkod okuma tekrarında fiziksel veya stok hareketi iki kez üretilmeyecektir.

## 2. Önerilen kapsam

| Alan | L4-B5 kapsamı |
|---|---|
| Girdi | ShipmentPackage `PackageCode`/barkod; opsiyonel beklenen LoadUnit bağlamı |
| Ön koşul | LoadPlan `Locked`, bağlı Shipment ve package allocation aktif |
| Ana işlem | Barkodu çöz, aynı shipment/plan ownership’ini doğrula, beklenen LoadUnit’i bul, actual scan kaydı oluştur |
| Kabul edilen sonuç | Paket `Loaded`; ilgili LoadUnit içindeki tüm beklenen paketler yüklüyse LoadUnit `Loaded` |
| Session sonucu | Tüm beklenen paketler yüklüyse `Completed`; eksik/fazla/yanlış paket varsa `Discrepancy` veya kontrollü kapanış engeli |
| Shipment sonucu | Session başarıyla tamamlandığında Shipment `Loaded`; `InTransit` geçişi bu slice’ta yok |
| Audit | Her accepted, duplicate, unexpected, wrong-unit ve discrepancy sonucu actor/correlation/time ile saklanır |
| Concurrency | Plan → Shipment → session → package → LoadUnit/LoadUnitItem deterministic row lock; `SKIP LOCKED` yok |
| Idempotency | Aynı `Idempotency-Key` + payload replay; farklı payload mismatch; aynı barkodun yeni key ile tekrar okutulması state’i ikinci kez ilerletmez |

### 2.1 Barkod ve miktar sınırı

İlk B5 uygulamasında barkod okuma **ShipmentPackage düzeyinde tam paket kabulü** olarak ele alınacaktır. `SplitAllowed` paket için kısmi temel miktar, koli içi adet veya paket açma işlemi bu slice’a dahil edilmeyecek; bunlar ayrı bir quantity/partial-load kararı olarak açılacaktır. Barkodun gerçek değeri mevcut `ShipmentPackage.PackageCode` alanından alınır; sonraki barcode master-data entegrasyonu ayrı bounded sınırdır.

### 2.2 Discrepancy politikası

Sistemde plan dışı barkod, başka LoadPlan’a ait paket, yanlış LoadUnit, iptal paket ve tamamlanmış session’a tekrar scan ayrı hata kodlarıyla saklanacaktır. Başarılı kapanış için tüm beklenen paketlerin kabul edilmiş olması gerekir. Eksik/fazla durumu açıklamasız biçimde `Shipment.Loaded` yapamaz; discrepancy gerekçesi ve yetkili kapanış politikası ayrı permission ile korunacaktır.

## 3. State geçişleri

| Aggregate/kayıt | İzin verilen geçiş | Yasaklanan geçiş |
|---|---|---|
| LoadPlan | `Locked` kalır | `Draft`, `Valid`, `NeedsReview` plan üzerinde scan |
| LoadUnit | `Locked → Loaded` yalnızca tüm beklenen package scan’leri kabul edilince | `Draft/Validated → Loaded`; `Cancelled → Loaded` |
| ShipmentPackage | `Available/Allocated → Loaded` | `Cancelled → Loaded`; başka shipment/plan package’ı yükleme |
| LoadVerificationSession | `Draft → InProgress → Completed` veya `Discrepancy` | `Completed → InProgress`; ikinci aktif session |
| Shipment | `Preparing → Loaded` yalnızca session tam ve discrepancy’siz kapanınca | `Loaded → InTransit` bu slice’ta |
| RoutePlan/RouteStop | Değişmez | departure, arrival veya teslim state’i B5 dışında |

## 4. Önerilen persistence modeli

### 4.1 `load_verification_sessions`

| Alan | Açıklama |
|---|---|
| `id` | Session kimliği |
| `load_plan_id`, `shipment_id` | Locked plan ve shipment ownership |
| `status` | `Draft`, `InProgress`, `Completed`, `Discrepancy`, `Cancelled` |
| `started_by`, `started_at` | Session başlatan kullanıcı ve zaman |
| `completed_by`, `completed_at` | Kapanış bilgisi |
| `completion_reason` | Discrepancy kapanış gerekçesi; normal kapanışta nullable |
| `row_version` | ETag/concurrency |
| `created_at`, `updated_at` | Audit zamanı |

Kısıtlar: `load_plan_id` ve `shipment_id` foreign key; `shipment_id` planla aynı olmak zorunda application transaction’da doğrulanır; aynı LoadPlan için yalnızca bir aktif session filtered unique index ile korunur.

### 4.2 `load_verification_scans`

| Alan | Açıklama |
|---|---|
| `id` | Scan command sonucu |
| `session_id`, `load_plan_id`, `shipment_id` | Ownership ve sorgu kapsamı |
| `shipment_package_id` | Çözülen package; beklenmeyen barkodda nullable olabilir |
| `expected_load_unit_id`, `actual_load_unit_id` | Planlanan ve fiziksel bağlam karşılaştırması |
| `barcode` | Okunan normalize barkod |
| `scan_status` | `Accepted`, `Duplicate`, `Unexpected`, `WrongUnit`, `CancelledPackage`, `Discrepancy` |
| `quantity_base` | Bu slice’ta tam package miktarı |
| `reason_code`, `reason_text` | Sonuç ve kullanıcıya açıklama |
| `scanned_by`, `scanned_at` | Actor/time |
| `idempotency_key`, `correlation_id` | Command audit ve replay bağlantısı |

Kısıtlar: session/package ilişkisi; accepted kayıtlar için aynı package’ın aynı session’da tekrar accepted olmamasını sağlayan unique partial index; `(session_id, scanned_at, id)` deterministik liste index’i; barcode normalize ve boş olmama CHECK’i; `quantity_base > 0` CHECK’i.

## 5. API taslağı

| Method | Endpoint | Permission | Amaç |
|---|---|---|---|
| `POST` | `/api/v1/load-plans/{loadPlanId}/load-verification/sessions` | `shipment.load-verify` | Locked plan için session başlatır/replay eder |
| `GET` | `/api/v1/load-verification/sessions/{sessionId}` | `shipment.read` | Session summary, scan listesi ve discrepancy özeti |
| `POST` | `/api/v1/load-verification/sessions/{sessionId}/scans` | `shipment.load-verify` | Barkod scan ve actual load kabulü |
| `POST` | `/api/v1/load-verification/sessions/{sessionId}/complete` | `shipment.load-verify` | Eksiksiz session kapanışı |
| `POST` | `/api/v1/load-verification/sessions/{sessionId}/close-discrepancy` | `shipment.load-verify-override` | Gerekçeli discrepancy kapanışı; Shipment `Loaded` geçişi policy ile sınırlandırılır |

Tüm POST command’leri `Idempotency-Key`, `X-Correlation-Id` ve kaynak `If-Match`/row-version kontrolü kullanacaktır. `close-discrepancy` hard ownership/state hatalarını gizleyemez; yalnızca açıklanmış operasyonel discrepancy’nin yetkili kapanışını yönetebilir.

Önerilen request alanları:

```text
StartLoadVerificationRequest
  ExpectedLoadPlanRowVersion

ScanLoadVerificationRequest
  Barcode
  ExpectedLoadUnitId?
  ScanMode: Pallet | Case | Package | BaseUnit

CompleteLoadVerificationRequest
  ExpectedSessionRowVersion

CloseLoadVerificationDiscrepancyRequest
  Reason
  ExpectedSessionRowVersion
```

`ScanMode`, telefon barkod akışındaki palet/koli/paket/birim görünüm bağlamını taşır; ilk B5’te server quantity hesabını değiştirmez. `Pallet` veya `Case` barkodu için parent-child barcode çözümlemesi ayrı catalog/barcode bounded slice’ında kesinleştirilecektir.

## 6. Uygulama görev listesi

### A. Tasarım ve domain hazırlığı

1. Barkod kaynağını `ShipmentPackage.PackageCode` olarak kesinleştirmek ve parent packaging barcode’larının B5 dışında olduğunu karar kaydına geçirmek.
2. `LoadVerificationSession` ve `LoadVerificationScan` domain tiplerini, enum’larını ve state transition guard’larını eklemek.
3. `LoadVerificationPolicy` ile locked-plan, package ownership, duplicate, wrong-unit, cancelled-package ve complete/discrepancy kurallarını tanımlamak.
4. Accepted scan sonrası LoadUnit ve Shipment state’lerinin türetim kurallarını aggregate seviyesinde test edilebilir hale getirmek.
5. Error code kataloğunu oluşturmak: `LOAD_PLAN_NOT_LOCKED`, `LOAD_VERIFICATION_ACTIVE_SESSION`, `PACKAGE_BARCODE_NOT_FOUND`, `PACKAGE_NOT_IN_LOAD_PLAN`, `PACKAGE_ALREADY_LOADED`, `LOAD_UNIT_MISMATCH`, `PACKAGE_CANCELLED`, `LOAD_VERIFICATION_INCOMPLETE`, `LOAD_VERIFICATION_DISCREPANCY_REASON_REQUIRED`, `LOAD_VERIFICATION_COMPLETED`.

### B. Persistence ve migration

6. `LoadVerificationSessionRecord` ve `LoadVerificationScanRecord` persistence kayıtlarını eklemek.
7. EF Core configuration’larını, PostgreSQL `jsonb`/varchar/check/FK/filtered unique index mapping’lerini yazmak.
8. `FactoryErpDbContext` DbSet’lerini eklemek.
9. `AddLoadVerification` forward migration’ını üretmek; mevcut migration’ları değiştirmemek.
10. Aktif session unique index’i ve accepted package unique index’i için EF model testlerini yazmak.
11. Migration apply sonrası canlı PostgreSQL’de tablo, FK, CHECK, unique index ve rollback/forward-fix davranışını doğrulamak.

### C. Application service ve transaction

12. `StartLoadVerificationRequest`, `ScanLoadVerificationRequest`, `CompleteLoadVerificationRequest`, discrepancy request/DTO’ları ve `ILoadVerificationCommandService` sözleşmesini eklemek.
13. Session başlatma transaction’ında LoadPlan’ın `Locked` olduğunu, shipment ownership’ini ve aktif session olmadığını doğrulamak.
14. Scan transaction’ında canonical lock order uygulamak: LoadPlan → Shipment → Session → ShipmentPackage → LoadUnit → LoadUnitItem.
15. Barkod normalize/resolve, package ownership, expected LoadUnit ve package state guard’larını uygulamak.
16. Accepted scan’de aynı package’ın ikinci kez state değiştirmesini engellemek; duplicate scan’i deterministic response/audit olarak döndürmek.
17. Tüm beklenen paketler kabul edilince LoadUnit ve session/Shipment state’lerini transaction içinde ilerletmek.
18. Complete ve discrepancy-close komutlarında eksik paket, duplicate, unexpected scan ve gerekçe kontrollerini yapmak.
19. Audit ve merkezi idempotency kayıtlarını aynı transaction sınırında üretmek.

### D. API ve security

20. Load verification controller’ını, ETag/idempotency/correlation helper desenleriyle eklemek.
21. `shipment.load-verify` permission ID `56`, `shipment.load-verify-override` permission ID `57` olarak seed etmek; system-admin rolüne idempotent grant eklemek.
22. Normal scan/complete ile discrepancy override sınırını ayırmak; override hard state/ownership hatalarını bastıramamalı.
23. ProblemDetails mapping’lerini hata kodları, `retryable` ve `actions` alanlarıyla güncellemek.
24. Gerçek `/api/v1/auth/login` üzerinden scan yetkili, read-only ve override’sız kullanıcı sınırlarını doğrulamak.

### E. Test ve dokümantasyon

25. Domain unit testleri: valid scan, zero/empty barcode, invalid state, duplicate, wrong unit, cancelled package, invalid transition ve discrepancy reason.
26. EF model testleri: session active unique index, accepted package unique index, CHECK constraint’ler, FK delete behavior ve row-version concurrency.
27. PostgreSQL integration testleri: locked prerequisite, session idempotency, accepted scan, duplicate scan, unexpected barcode, wrong plan, wrong unit, cancelled package, complete state ve stale ETag.
28. İki ayrı PostgreSQL context ile aynı package’ın eşzamanlı scan yarışını test etmek; yalnızca bir kabul ve tek state transition kanıtlamak.
29. Real-login security testleri: `shipment.load-verify` ile scan, read-only `403`, discrepancy override izni olmayan kullanıcı `403`.
30. `design/implementation-g6.2-l4b5-load-verification.md` evidence dokümanını ve canonical API/PostgreSQL tasarım notlarını güncellemek.
31. Full gate: `dotnet restore`, Release build, tüm testler, architecture tests, live migration check ve `git diff --check`.

## 7. L4-B5 dışı kapsam

Bu slice’a stock movement/stock decrement, vehicle reservation, vehicle status `InTransit`, departure, route arrival, teslimat kanıtı, müşteri imzası/fotoğrafı, invoice/cari etkisi, locked plan’dan replan ile yeni version üretimi, optimal packing/traffic optimization ve parent barcode master-data çözümlemesi dahil değildir.

Mobil Flutter ekranı ve Next.js operasyon ekranı bu planın API acceptance’ından sonra ayrı client slice’ında uygulanmalıdır. B5 backend’i `ScanMode` bağlamını kabul edebilir; ancak gerçek kamera UX’i ve offline queue bu bounded slice’ın tamamlanma kriteri değildir.

## 8. Riskler ve alınması gereken kararlar

| Risk/karar | Önerilen L4-B5 kararı |
|---|---|
| Barkodun package mı parent packaging mi olduğu | B5’te PackageCode; parent barcode ayrı slice |
| SplitAllowed paket | İlk B5’te full package scan; partial quantity ayrı karar |
| Duplicate scan | State ikinci kez değişmez; audit sonucu `Duplicate` döner |
| Eksik/fazla yük | Açıklamasız `Shipment.Loaded` yok; discrepancy session sonucu |
| Offline mobil scan | B5 dışında; merkezi idempotency ile online command |
| Aynı plan için birden fazla yükleme session’ı | Tek aktif session; filtered unique index |
| Locked plan değişikliği | Scan planı değiştirmez; yalnızca actual verification kayıtlarını ilerletir |
| Stock etkisi | Bu slice’ta yok; stock ledger ayrı command sınırı |

## 9. Completion gate

L4-B5 ancak aşağıdaki koşulların tümü sağlanırsa tamamlanmış sayılacaktır:

| Gate | Beklenen sonuç |
|---|---|
| Domain implementation | Session/scan state guard’ları PASS |
| Quantity/package invariants | Duplicate, ownership, split ve positive quantity kuralları PASS |
| Persistence model | Index, CHECK, FK ve concurrency mapping PASS |
| PostgreSQL migration | Forward migration apply ve live schema inspection PASS |
| Application transaction | Deterministic lock order, idempotency ve rollback PASS |
| API/security | Endpoint contract, ETag, real-login `403` sınırları PASS |
| Unit/integration tests | Tüm yeni testler PASS |
| Architecture tests | Dependency violation yok |
| Documentation | Evidence ve canonical design güncel |
| Release gate | `dotnet restore`, build 0 warning/0 error, full `dotnet test`, `git diff --check` PASS |

## 10. Önerilen uygulama sırası

Önce barkod/quantity/discrepancy kararları kullanıcı tarafından onaylanmalı; ardından domain ve EF model slice’ı yazılmalıdır. Migration ve model testleri geçmeden service/API geliştirilmemeli, gerçek PostgreSQL integration ve security testleri yeşil olmadan mobil/web client’a geçilmemelidir. L4-B5 tamamlanmadan departure veya delivery-proof bounded slice’ı başlatılmamalıdır.
