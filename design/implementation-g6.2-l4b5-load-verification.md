# Implementation Evidence — G6.2 L4-B5 Actual Load Verification

**Tarih:** 2026-08-17  
**Slice:** L4-B5 — Locked LoadPlan Actual Load Verification  
**Baseline:** L4-B4 commit `2f16e38` — validation, manual change, warning resolution ve LockLoadPlan  
**Durum:** Implementation tamamlandı; release gate PASS

## 1. Amaç ve kapsam

L4-B5, yalnızca `Locked` durumundaki bir `LoadPlan` üzerinden depo yükleme doğrulamasını sağlar. Depo personeli `ShipmentPackage.PackageCode` barkodunu okur; sistem aynı Shipment ve locked LoadPlan içindeki allocation’ı doğrular, actual scan sonucunu audit edilebilir biçimde saklar ve kabul edilen paketlerin state’ini ilerletir.

İlk B5 implementation’ında barkod çözümleme **ShipmentPackage seviyesinde tam paket** olarak sınırlandırılmıştır. Parent palet/koli barcode çözümlemesi, partial quantity, offline queue, stock ledger, vehicle reservation/status, departure, route arrival, delivery proof, invoice/cari etkisi ve replan bu slice’a dahil edilmemiştir.

## 2. Uygulanan domain davranışları

`LoadVerificationSession` için `Draft → InProgress → Completed` veya `Discrepancy` lifecycle’ı uygulanmıştır. Aynı LoadPlan için aynı anda yalnızca tek aktif session açılabilir. Tamamlanmış session yeniden açılamaz; discrepancy kapanışı gerekçe olmadan yapılamaz.

`LoadVerificationScan` için `Accepted`, `Duplicate`, `Unexpected`, `WrongUnit`, `CancelledPackage` ve `Discrepancy` sonuçları eklenmiştir. Barkod, audit key ve pozitif `quantityBase` guard’ları domain seviyesinde uygulanmıştır. `LoadVerificationPolicy`, yalnızca locked plan, InProgress session, aynı LoadPlan ownership’i, cancel edilmiş olmayan package ve eşleşen LoadUnit ile accepted scan’e izin verir.

L4-B5 actual-load geçişi için ShipmentPackage domain davranışı `Available/Allocated → Loaded` olacak şekilde genişletilmiştir. Accepted package ikinci kez state değiştirmez. Tüm beklenen package’lar kabul edilince ilgili LoadUnit `Loaded`; `complete` komutu başarıyla işlendiğinde session ve Shipment `Loaded` olur. Shipment `InTransit` geçişi bu slice’ta yapılmaz.

## 3. Persistence ve migration

Eklenen persistence kayıtları şunlardır:

| Kayıt | Tablo | Amaç |
|---|---|---|
| `LoadVerificationSessionRecord` | `load_verification_sessions` | Session lifecycle, ownership, completion/discrepancy audit ve ETag |
| `LoadVerificationScanRecord` | `load_verification_scans` | Her barcode command sonucunun immutable audit kaydı |

Eklenen migration’lar:

| Migration | İçerik |
|---|---|
| `20260817120932_AddLoadVerification` | Session/scan tabloları, FK’ler, CHECK’ler, index’ler |
| `20260817122347_FixLoadVerificationRowVersionConcurrency` | Manual row-version increment standardına geçiş için forward-fix |

Canlı PostgreSQL doğrulamasında iki tablo, FK’ler, lifecycle CHECK’leri, completion/discrepancy pair CHECK’leri, accepted package filtered unique index’i, aktif session filtered unique index’i ve scan idempotency unique index’i görülmüştür.

## 4. Application, transaction ve idempotency

`ILoadVerificationCommandService` ve `LoadVerificationCommandService` aşağıdaki command/query sınırlarını sağlar:

| İşlem | Davranış |
|---|---|
| `StartSessionAsync` | Plan row lock, Shipment lock, locked-plan ve active-session guard, audit/idempotency |
| `GetSessionAsync` | Session ve deterministic scan projection |
| `ScanAsync` | Barcode normalize/resolve, ownership, LoadUnit, package state ve actual-load transition |
| `CompleteAsync` | Tüm expected package set’i kabul edilmeden kapanışa izin vermez; LoadUnit/Shipment state’lerini ilerletir |
| `CloseDiscrepancyAsync` | Yalnızca gerekçeli ve dedicated override policy ile discrepancy kapanışı |

Scan, complete ve discrepancy işlemlerinde lock sırası `LoadPlan → Shipment → LoadVerificationSession → ShipmentPackage → LoadUnit → LoadUnitItem` olarak uygulanmıştır. `SKIP LOCKED` kullanılmamıştır. Session, package, LoadUnit ve Shipment row version değerleri mutation sırasında aynı transaction içinde manuel ilerletilir. Stale ETag `RESOURCE_VERSION_CONFLICT` ile reddedilir.

Idempotency replay aynı `Idempotency-Key` ve aynı payload hash için önceki sonucu döndürür. Aynı key ile farklı payload `IDEMPOTENCY_PAYLOAD_MISMATCH` üretir. Aynı package’ın yeni key ile tekrar okunması package state’ini ikinci kez ilerletmez; eşzamanlı ikinci command stale session row version ile conflict alır.

## 5. API ve authorization

Eklenen endpoint’ler:

| Method | Endpoint | Permission |
|---|---|---|
| POST | `/api/v1/load-plans/{loadPlanId}/load-verification/sessions` | `shipment.load-verify` |
| GET | `/api/v1/load-verification/sessions/{sessionId}` | `shipment.read` |
| POST | `/api/v1/load-verification/sessions/{sessionId}/scans` | `shipment.load-verify` |
| POST | `/api/v1/load-verification/sessions/{sessionId}/complete` | `shipment.load-verify` |
| POST | `/api/v1/load-verification/sessions/{sessionId}/close-discrepancy` | `shipment.load-verify-override` |

Permission seed’leri stable ID’lerle eklenmiştir:

| ID son eki | Permission |
|---:|---|
| 56 | `shipment.load-verify` |
| 57 | `shipment.load-verify-override` |

`IdempotencyKeyMiddleware`, `/api/v1/load-verification` POST route’larını kritik mutation olarak zorunlu header kontrolüne dahil eder. Real-login security testinde `/api/v1/auth/login` üzerinden full permission kullanıcısının B5 permission’larını aldığı ve read-only kullanıcının session başlatma ile discrepancy override endpoint’lerinden `403 Forbidden` aldığı doğrulanmıştır.

## 6. Test sonuçları

Release gate öncesi tam test çalıştırması aşağıdaki sonuçları vermiştir:

| Proje | Sonuç |
|---|---:|
| `FactoryErp.Domain.UnitTests` | **114/114 PASS** |
| `FactoryErp.ArchitectureTests` | **5/5 PASS** |
| `FactoryErp.Infrastructure.UnitTests` | **69/69 PASS** |
| **Toplam** | **188/188 PASS** |

B5’e özgü test kapsamı şunları içerir: session start state guard’ları, valid scan, empty barcode, zero quantity, accepted scan package zorunluluğu, wrong unit, cancelled package, duplicate package, incomplete completion, discrepancy reason, EF CHECK/index model gate’leri, locked prerequisite, accepted scan, idempotent replay, unexpected barcode, stale session ETag, discrepancy close, eşzamanlı scan yarışı ve real-login authorization sınırları.

Full gate komutları:

```text
dotnet restore FactoryErp.sln
dotnet build FactoryErp.sln --configuration Release -p:UseSharedCompilation=false
dotnet test FactoryErp.sln --configuration Release --no-build --no-restore -p:UseSharedCompilation=false
git diff --check
```

Sonuç: restore **PASS**, Release build **0 warning / 0 error**, full test **188/188 PASS**, architecture tests **5/5 PASS**, live PostgreSQL migration/schema check **PASS**.

## 7. Değiştirilen başlıca dosyalar

Domain tarafında `LoadVerification.cs` ve `ShipmentPackage.cs`; application tarafında `LogisticsContracts.cs`; infrastructure tarafında session/scan records, EF configurations, DbContext DbSet’leri, iki migration, `LoadVerificationCommandService.cs`, DI ve permission seeder; API tarafında controller, policies, Program authorization registration ve idempotency middleware; test tarafında domain, model, PostgreSQL integration ve real-login security testleri güncellenmiştir.

Canonical belgeler de güncellenmiştir: `implementation-g6.2-l4b5-load-verification-plan.md`, `architecture-api-contracts.md` ve `postgresql-18-migration-sql-specification.md`.

## 8. Kalan riskler ve sonraki boundary

L4-B5, package-level online actual-load verification sınırında tamamlanmıştır. Parent packaging barcode çözümleme, partial/split quantity, offline mobile queue, stock ledger movement, vehicle reservation/status, departure/in-transit, route arrival ve delivery proof sonraki bounded slice’ların konusudur.

Ayrıca `ShipmentPackage.Load()` davranışının Available durumundan yüklemeye izin vermesi bilinçli bir B5 kararıdır; sonraki stock/warehouse slice’ında package allocation ile physical load arasındaki ayrım daha ayrıntılı quantity contract ile yeniden ele alınmalıdır. Bu durum mevcut B5 testlerinde açıkça korunmuştur.
