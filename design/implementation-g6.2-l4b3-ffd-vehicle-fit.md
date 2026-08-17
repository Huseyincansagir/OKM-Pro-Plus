# G6.2 L4-B3 Implementation Evidence

**Slice:** PlanningItem normalization, deterministik FFD engine ve VehicleFitEvaluation

**Tarih:** 2026-08-17

**Durum:** Implementation gate PASS

## 1. Amaç ve bounded kapsam

L4-B3, L4-B2 LoadPlan Draft baseline’ının üzerine server-side `PlanningItem` normalization, deterministik First Fit Decreasing benzeri FFD engine ve araç adaylarının `VehicleFitEvaluation` snapshot’ı olarak saklanmasını ekler. Bu slice, L4-B2’nin Draft üretim sınırını korur.

Bu slice içinde `suggest`, `validate`, manual change, approval, `LockLoadPlan` ve `replan` komutları uygulanmamıştır. Araç rezervasyonu, stok hareketi ve vehicle status değişikliği de üretilmez. Bu davranışlar L4-B4 approval/lock gate’inin kapsamındadır.

## 2. Uygulanan domain davranışları

`PlanningItem` server-side oluşturulur ve shipment package physical snapshot’ından normalize edilir. Normalized alanlar arasında shipment/package/item/product/packaging kimlikleri, base quantity, package count, net/tare/gross weight, volume, dimensions, compatibility, orientation, fragility, split policy, route-stop sequence ve stable sort key bulunur.

Stable sort key aşağıdaki sırayı korur:

| Sıra | Alan | Yön |
|---:|---|---|
| 1 | Compatibility group | ASC |
| 2 | Keep upright | DESC |
| 3 | Fragile | DESC |
| 4 | Gross weight | DESC |
| 5 | Volume | DESC |
| 6 | Floor footprint | DESC |
| 7 | Route-stop sequence | ASC |
| 8 | Shipment item ID | ASC |
| 9 | Packaging ID | ASC |
| 10 | Shipment package ID | ASC |

UUID veya database insertion order tek başına sıralama anahtarı değildir. Aynı normalized input, algorithm version ve parameter set aynı item sırasını üretir.

FFD hard constraint sırası quantity, physical profile, gross weight, volume, dimensions, compatibility, stacking, orientation ve stop ownership/access kontrolleri olarak uygulanmıştır. Split-enabled item’lar için partial capacity tüketimi yapılır; unsplittable item tam quantity ile test edilir. Sığmayan item’lar deterministic rejection code ile döner.

## 3. VehicleFitEvaluation persistence

Migration:

```text
20260817094755_AddVehicleFitEvaluations
```

`vehicle_fit_evaluations` tablosu aşağıdaki bilgileri snapshot olarak saklar:

| Alan grubu | İçerik |
|---|---|
| Candidate | CandidateStatus, rejection code, reason |
| Ratios | Weight, volume, pallet, floor-area, height ve fit score |
| Checks | Door, dimension, stacking, axle ve stop-access status |
| Determinism | AlgorithmVersion, InputSnapshotHash, ParameterSet bilgisi LoadPlan üzerinde |
| Capacity | Effective vehicle capacity ve zone snapshot’ı |
| Audit zamanı | EvaluatedAt |

Nullable `vehicle_capacity_id` için PostgreSQL expression unique index uygulanmıştır:

```sql
(load_plan_id,
 vehicle_id,
 COALESCE(vehicle_capacity_id, '00000000-0000-0000-0000-000000000000'::uuid),
 input_snapshot_hash)
```

Input snapshot hash; shipment, LoadPlan route/version bilgileri, normalized package snapshot’ları, seçilen vehicles, effective capacity/zone snapshot’ları, algorithm version ve parameter set üzerinden SHA-256 olarak hesaplanır. Böylece capacity değişimi eski evaluation snapshot’ının yanlışlıkla replay edilmesini engeller.

## 4. API ve yetkilendirme

| Method | Endpoint | Policy |
|---|---|---|
| `POST` | `/api/v1/shipments/{shipmentId}/vehicle-fit/evaluate` | `shipment.vehicle-fit` |
| `GET` | `/api/v1/shipments/{shipmentId}/vehicle-fit/candidates?loadPlanId={id}` | `shipment.vehicle-fit` |

Evaluate komutu `LoadPlan FOR UPDATE` kullanır, stale row version değerini reddeder ve aynı payload için idempotent replay sağlar. `shipment.vehicle-fit` permission stable seed ID `10000000-0000-0000-0000-000000000053` ile system-admin rolüne atanmıştır.

## 5. Test kapsamı

| Test grubu | Sonuç |
|---|---:|
| FFD/PlanningItem domain tests | 10/10 PASS |
| Full Domain unit suite | 96/96 PASS |
| Architecture tests | 5/5 PASS |
| VehicleFitEvaluation EF model tests | 2/2 PASS |
| VehicleFit PostgreSQL integration tests | 2/2 PASS |
| Real `/api/v1/auth/login` security test | 1/1 PASS |
| Full Infrastructure suite | 58/58 PASS |
| Full solution | 159/159 PASS |

Özel integration coverage aynı input snapshot için deterministic evaluation, request vehicle sırasının sonucu değiştirmemesi, idempotent replay, candidate ordering ve `PHYSICAL_PROFILE_MISSING` hard rejection senaryolarını doğrular.

## 6. Verification gate

| Gate | Sonuç |
|---|---|
| `dotnet restore FactoryErp.sln` | PASS |
| `dotnet build FactoryErp.sln --configuration Release` | PASS — 0 warning, 0 error |
| `dotnet test FactoryErp.sln --configuration Release` | PASS — 159/159 |
| Local PostgreSQL migration apply | PASS |
| Migration history | `20260817094755_AddVehicleFitEvaluations` mevcut |
| Table/index/constraint inspection | PASS |
| `git diff --check` | PASS |

## 7. Kalan riskler ve sonraki boundary

FFD engine optimal 3D bin-packing veya traffic optimization garantisi vermez. Door opening, axle load ve gerçek stop-access kontrolleri bu slice’ta `NotChecked` veya policy seviyesinde basit status olarak bırakılmıştır; bunlar L4-B4 validation aşamasında gerçek kurallarla genişletilmelidir.

`evaluate` mevcut shipment package snapshot’larını normalize eder ve aday araç kapasitesini değerlendirir. Final LoadUnit allocation’larının approval/lock öncesi yeniden doğrulanması L4-B4 transaction’ında yapılmalıdır. Manuel değişiklik, validation result, warning resolution, approval, lock ve replan henüz yoktur.

**Sonraki bounded slice:** L4-B4 Validate, manual change, approval ve `LockLoadPlan`.

## 8. Sonuç

L4-B3 implementation, determinism, hard-constraint rejection, effective capacity snapshot, VehicleFitEvaluation persistence, migration, API, idempotency, PostgreSQL integration ve real-login security gate’lerini geçmiştir. L4-B4’e otomatik geçilmemiştir.
