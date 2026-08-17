# L4-B4 Implementation Evidence — Validation, Manual Change, Warning Resolution ve LockLoadPlan

**Tarih:** 2026-08-17

**Slice:** L4-B4 — LoadPlan validation, manual change audit, warning resolution ve LockLoadPlan

**Durum:** Implementation gate PASS

## 1. Amaç ve bounded kapsam

L4-B4, L4-B3’ün deterministik FFD ve `VehicleFitEvaluation` baseline’ı üzerine LoadPlan’ın server-side yeniden doğrulanmasını, validation lifecycle’ını, manuel değişiklik audit kaydını, warning resolution/override sınırını ve depo sorumlusu onayıyla `Locked` durumuna geçişi ekler.

Bu slice içinde stok hareketi, araç rezervasyonu, araç ana durum değişikliği, sevkiyat departure veya replan ile yeni LoadPlan version üretimi yapılmaz. Lock yalnızca plan state’ini, bağlı LoadUnit state’lerini, approval/audit kayıtlarını ve idempotency sonucunu etkiler.

## 2. Domain davranışları

`LoadPlanValidationResult` validation key, severity, code, mesaj, hedef entity ve resolution lifecycle bilgilerini taşır. Severity değerleri `HardError`, `Warning` ve `Info`; resolution değerleri `Open`, `Resolved`, `Overridden` ve `NotApplicable` olarak sınırlandırılmıştır. Bir validation yalnızca `Open` durumundan kapanabilir; resolver, zaman ve gerekçe zorunludur.

`LoadPlanManualChange`, değişiklik tipini, actor kullanıcıyı, entity kimliğini, before/after JSON snapshot’larını ve gerekçeyi immutable audit kaydı olarak tutar. Geçerli değişiklik tipleri `AddLoadUnit`, `RemoveLoadUnit`, `MovePackage`, `ChangeQuantity`, `ChangeStopAllocation`, `ChangeVehicle`, `ChangeCapacity` ve `Other` değerleriyle sınırlıdır.

`LoadPlanLockPolicy` aşağıdaki kuralları uygular:

| Kural | Sonuç |
|---|---|
| Plan yalnızca `Valid` veya `NeedsReview` durumundan lock edilebilir | Geçersiz state transition reddedilir |
| `Approval=true` zorunludur | `LOAD_PLAN_APPROVAL_REQUIRED` |
| Açık hard error veya `Infeasible` feasibility | `LOAD_PLAN_INFEASIBLE` |
| Açık warning | Warning resolve/override edilmeden lock edilemez |
| Override | Yalnızca warning için kullanılabilir; hard error override edilemez |
| Vehicle, capacity ve input snapshot hash | Lock öncesi zorunlu prerequisite |
| LoadUnit | `Loaded` veya `Cancelled` state’inden lock edilemez; başarıda tümü `Locked` olur |

`LoadPlan.Rehydrate` sırası da düzeltilmiştir: nested LoadUnit kayıtları aggregate henüz mutable durumdayken eklenir, kaynak state en son atanır. Böylece `Valid`/`NeedsReview` planların domain `Lock` çağrısı sırasında `LOAD_PLAN_IMMUTABLE` kaynaklı yanlış hata vermesi engellenmiştir.

## 3. Validation ve idempotency davranışı

`ValidateLoadPlanAsync` planı `FOR UPDATE` ile alır, bağlı shipment/route/package/load-unit/vehicle kaynaklarını canonical sırada kilitler, mevcut allocation’ları tekrar değerlendirir ve `(load_plan_id, validation_key)` unique anahtarı üzerinden sonuçları upsert eder. Aynı command payload ve `Idempotency-Key` tekrarlandığında ilk response replay edilir; payload değişirse `IDEMPOTENCY_PAYLOAD_MISMATCH` üretilir.

Validation sırasında şu kurallar server-side yeniden kontrol edilir:

- LoadPlan’ın boş olması ve FFD feasibility durumunun `Infeasible` olması.
- Vehicle, effective capacity veya input snapshot hash eksikliği.
- Loaded/Cancelled LoadUnit kullanımı.
- Aynı ShipmentPackage’ın aynı LoadUnit içinde duplicate allocation’ı.
- Stop allocation toplamının LoadUnitItem miktarını aşması.
- Shipment item allocation miktarının üst sınıra çıkması veya kalan miktar bilgisi.
- Seçilen vehicle için route zaman çakışması.
- Stop allocation bulunmaması warning olarak saklanır.

Validation summary; açık hard error, açık warning, info ve validation zamanını JSON olarak LoadPlan üzerinde saklar. Hard error planı `NeedsReview`/`Infeasible` konumunda tutar; warning’siz uygulanabilir plan `Valid` olur.

## 4. Persistence ve migration

Yeni tablolar aşağıdaki migration ile PostgreSQL’e uygulanmıştır:

```text
20260817103404_AddLoadPlanValidationAndManualChanges
```

| Tablo | İçerik | Ana kısıtlar/indexler |
|---|---|---|
| `load_plan_validation_results` | Validation sonucu ve resolution lifecycle | `ux_load_plan_validation_key`, severity CHECK, resolution CHECK, resolution pair CHECK |
| `load_plan_manual_changes` | Before/after snapshot ve kullanıcı gerekçeli manuel değişiklik audit’i | Change type CHECK, non-empty entity CHECK, zaman/entity lookup indexleri |

Live PostgreSQL doğrulamasında migration history içinde `20260817103404_AddLoadPlanValidationAndManualChanges` bulunmuş; tablolar, unique validation key index’i ve bütün CHECK constraint’leri okunmuştur.

EF model testleri `GetTableName`, check constraint adları, unique index property sırası ve manual-change lookup indexlerini doğrular. JSON snapshot alanları PostgreSQL `jsonb` olarak map edilmiştir. İlişkiler LoadPlan/User kayıtlarına `Restrict` delete behavior ile bağlanmıştır.

## 5. Transaction ve concurrency sırası

Validation ve lock komutlarında kaynak erişim sırası aşağıdaki şekilde uygulanır:

```text
LoadPlan FOR UPDATE
→ Shipment FOR UPDATE
→ RoutePlan FOR UPDATE
→ RouteStops sequence_no, id sırasıyla FOR UPDATE
→ ShipmentPackages id sırasıyla FOR UPDATE
→ LoadUnits unit_code, id sırasıyla FOR UPDATE
→ selected Vehicle FOR UPDATE
→ selected VehicleCapacity FOR UPDATE
→ validation/route-overlap/hard-warning guard
→ domain Lock + approval + LoadUnit locks + audit + idempotency
→ commit
```

`If-Match` değeri command servislerine ayrı `expectedRowVersion` parametresi olarak aktarılır. Stale değer `RESOURCE_VERSION_CONFLICT` ile reddedilir. `Idempotency-Key` middleware’ine doğrudan `/api/v1/load-plans` mutasyon prefix’i eklenmiştir; servis katmanında da replay ve payload mismatch kontrolü korunmuştur. Lock sırasında `SKIP LOCKED` kullanılmaz.

## 6. API ve permission sınırları

| Method | Endpoint | Policy | Davranış |
|---|---|---|---|
| `POST` | `/api/v1/load-plans/{id}/validate` | `shipment.load-plan` | Server-side validation ve result batch |
| `GET` | `/api/v1/load-plans/{id}/validation-results` | `shipment.read` | Validation projection |
| `POST` | `/api/v1/load-plans/{id}/manual-changes` | `shipment.load-plan` | ManualChange audit + `NeedsReview` |
| `POST` | `/api/v1/load-plans/{id}/warning-resolutions` | `shipment.load-plan` + override action check | Warning resolve veya yetkili override |
| `POST` | `/api/v1/load-plans/{id}/lock` | `shipment.plan-lock` | Approval guard ve domain lock |

Yeni permission seedleri sabit kimliklerle eklenmiştir:

| ID son eki | Code | Action |
|---:|---|---|
| `54` | `shipment.plan-lock` | `lock` |
| `55` | `shipment.plan-override` | `override` |

Override action isteyen command, JWT içindeki `permission=shipment.plan-override` claim’i olmadan `403 Forbidden` döner. Hard error için warning endpoint’i veya lock payload’ı kullanılarak override yapılamaz.

## 7. Test kapsamı ve sonuçları

| Test grubu | Sonuç |
|---|---:|
| L4-B4 domain validation/lock tests | 16/16 PASS |
| Full Domain unit suite | 102/102 PASS |
| Architecture dependency tests | 5/5 PASS |
| Infrastructure model + integration + real-login security suite | 64/64 PASS |
| Full solution test gate | 171/171 PASS |

L4-B4 PostgreSQL integration coverage şu senaryoları doğrular:

- Validation result batch oluşturma ve aynı key ile deterministic replay.
- Açık hard error varken approval verilse dahi LockLoadPlan reddi.
- Vehicle, capacity ve input snapshot hash mevcutken approval ile başarılı lock.
- Başarılı lock sonrası bağlı LoadUnit’lerin `Locked`, approval actor ve lock metadata’sının yazılması.
- Warning resolution/override, manual-change audit kaydı ve `IDEMPOTENCY_PAYLOAD_MISMATCH`.
- Stale ETag için `RESOURCE_VERSION_CONFLICT`.
- EF modelinde unique validation key ve CHECK constraint’ler.
- Gerçek `/api/v1/auth/login` üzerinden full user’ın plan-lock erişimi, read-only kullanıcının `403` alması ve override permission yokluğunda erişimin kesilmesi.

## 8. Verification gate

| Gate | Sonuç |
|---|---|
| PostgreSQL yeniden kurulum ve bootstrap seed | PASS |
| L4-B4 migration apply | PASS |
| `dotnet restore` | PASS |
| `dotnet build FactoryErp.sln --configuration Release` | PASS — 0 warning, 0 error |
| `dotnet test FactoryErp.sln --configuration Release` | PASS — 171/171 |
| Architecture dependency rule | PASS — Domain → Infrastructure/API ihlali yok |
| `git diff --check` | PASS |

## 9. Kalan riskler ve sonraki boundary

O-014 sınırı korunmuştur: FFD ve validation optimal 3D bin-packing, trafik optimizasyonu veya gerçek aks yükü garantisi vermez. Door opening, axle load ve ileri stop-access kuralları mevcut evaluation modelindeki `NotChecked`/policy seviyesinden daha ileri taşınmamıştır.

`VehicleFitEvaluation` henüz final vehicle/capacity seçimini otomatik yapmaz; bu bilinçli olarak depo sorumlusu onayına bırakılmıştır. L4-B4 lock, seçilmiş `vehicle_id`, `vehicle_capacity_id` ve `input_snapshot_hash` değerlerini prerequisite olarak kontrol eder; araç rezervasyonu veya vehicle status transition üretmez.

L4-B4 dışında kalan sonraki sınır; locked plan üzerinden actual load verification, barcode scan, departure, teslimat kanıtı ve gerektiğinde yeni version üreten replan akışıdır.

## 10. Sonuç

L4-B4 validation, manual change, warning resolution, permission boundary, idempotent command ve LockLoadPlan state transition’ı; migration, EF model, PostgreSQL integration, real-login security, architecture, build ve full test gate’leriyle tamamlanmıştır. Bu slice commit ve remote push için hazırdır.
