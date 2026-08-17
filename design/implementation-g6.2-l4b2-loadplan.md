# G6.2 L4-B2 — LoadPlan Draft, LoadUnit ve Allocation Evidence

**Tarih:** 2026-08-17
**Durum:** Implementation tamamlandı; commit ve remote push öncesi son doğrulama
**Kapsam:** LoadPlan Draft, LoadUnit, LoadUnitItem, LoadUnitStopAllocation, PostgreSQL migration, API yetkilendirmesi ve test gate’leri

## 1. Kapsam ve bounded sınır

L4-B2, L4-B1 `ShipmentPackage` kayıtlarının fiziksel LoadUnit’lere ve route-stop alt allocation’larına dağıtılabilmesi için gerekli Draft persistence sınırını açar. Bu slice ticari miktar, stok hareketi, araç rezervasyonu veya araç durum değişikliği üretmez.

Aşağıdaki özellikler bilinçli olarak sonraki slice’larda bırakılmıştır:

| Sonraki slice | Kapsam dışı bırakılan davranış |
|---|---|
| L4-B3 | `VehicleFitEvaluation`, PlanningItem normalization, deterministik FFD, `suggest`, `validate` |
| L4-B4 | Manual change, validation result, warning resolution, approval, `lock`, `replan` |
| Sonraki operasyon slice’ı | Actual loading verification ve planlanan-gerçekleşen farkı |

Canonical tasarım ve migration specification’daki büyük lojistik şema, bounded migration sırasına ayrılmıştır. L4-B2 migration’ı yalnızca `load_plans`, `load_units`, `load_unit_items` ve `load_unit_stop_allocations` tablolarını açar.[1] [2]

## 2. Domain davranışları

`LoadPlan` Draft olarak oluşturulur ve shipment, route plan, route plan version ve monotonik plan version bilgilerini taşır. Aynı shipment için aynı version database unique index’i ile ikinci kez oluşturulamaz. Draft yalnızca doğrulanmış route plan ownership zincirinden sonra yaratılır; araç ve kapasite Draft aşamasında nullable kalabilir.

`LoadUnit`, fiziksel taşıma birimidir ve `Pallet`, `Cage`, `CartonGroup` veya `Loose` tiplerinden birini taşır. Ölçüler millimetre, ağırlık kilogram, hacim metreküp standardında saklanır. Brüt ağırlık dara ağırlığından küçük olamaz; ölçüler, hacim ve unloading priority pozitif olmalıdır.

`LoadUnitItem`, ShipmentPackage miktarının bir LoadUnit’e atanmış halidir. Command service shipment satırlarını transaction içinde kilitleyerek toplam quantity ceiling’i doğrular. `splitAllowed=false` package’ın bütünüyle tek allocation olarak atanması zorunludur. L4-B2 MVP atomic-package sınırında kalır; aktif package’ın ikinci LoadUnit’e atanması `ux_active_package_load_unit` unique partial index’i ve service guard ile engellenir.

`LoadUnitStopAllocation`, LoadUnitItem miktarının route stop’a dağılımını taşır. Stop allocation toplamı LoadUnitItem quantity’sini aşamaz; aynı LoadUnitItem ve route stop çifti tekrar edemez; quantity ve sequence pozitif olmalıdır.

LoadPlan state mutasyonları Draft, Proposed ve NeedsReview durumlarıyla sınırlıdır. Valid, Locked ve Superseded planlar immutable kabul edilir. Bu koruma L4-B4 lock/approval implementation’ı gelmeden önce de domain seviyesinde uygulanmaktadır.

## 3. Persistence ve migration

Uygulanan migration:

```text
20260817091855_AddLoadPlanAndUnits
```

Migration local PostgreSQL `factory_erp_g1` database’ine uygulanmış ve `__EFMigrationsHistory` içinde son migration olarak doğrulanmıştır. L4-B1 migration’ından sonra uygulanma sırası şöyledir:

```text
20260817081758_AddShipmentPackages
20260817091855_AddLoadPlanAndUnits
```

| Database koruması | Sonuç |
|---|---|
| `(shipment_id, version)` unique | PASS |
| LoadPlan state CHECK | PASS |
| Feasibility CHECK | PASS |
| Approval pair CHECK | PASS |
| Lock pair CHECK | PASS |
| Locked prerequisite CHECK | PASS |
| LoadUnit type/status CHECK | PASS |
| LoadUnit physical dimension/weight/volume CHECK | PASS |
| LoadUnit code unique per plan | PASS |
| LoadUnit deterministic priority index | PASS |
| LoadUnitItem positive quantity/physical CHECK | PASS |
| Active package unique partial index | PASS |
| Stop allocation positive quantity/sequence CHECK | PASS |
| Stop allocation `(load_unit_item_id, route_stop_id)` unique | PASS |
| Restricted foreign keys | PASS |
| Plan/unit/item row-version concurrency mapping | PASS |

Migration `Down` sırası stop allocation → item → unit → plan şeklindedir. Production belge ve lojistik kayıtları için destructive rollback çalıştırılmamalı; forward-fix veya backup restore kullanılmalıdır.

## 4. Application ve API

L4-B2 için nested Draft command ve read response sözleşmeleri eklendi. Client `LoadUnitItem` physical weight/volume veya feasibility sonucu göndermez; Draft command’ında package snapshot’tan server-side oranla gross weight ve volume hesaplanır. Route stop, package, shipment item ve shipment ownership zinciri service transaction’ı içinde kontrol edilir.

| Method | Endpoint | Policy |
|---|---|---|
| `POST` | `/api/v1/shipments/{shipmentId}/load-plans` | `shipment.load-plan` |
| `GET` | `/api/v1/load-plans/{loadPlanId}` | `shipment.read` |

POST command’ı `Idempotency-Key` ve correlation bilgisiyle çalışır. Aynı key ve aynı payload replay edilir; farklı payload mismatch davranışı mevcut idempotency store sözleşmesine bırakılmıştır. Draft oluşturmak vehicle reservation, stock movement, vehicle status veya FFD side-effect’i üretmez.

## 5. Değiştirilen dosyalar

| Katman | Dosyalar |
|---|---|
| Domain | `src/FactoryErp.Domain/Shipping/LoadPlanning.cs`, `src/FactoryErp.Domain/Common/Entity.cs` |
| Application | `src/FactoryErp.Application/Shipping/LogisticsContracts.cs` |
| Persistence entities | `src/FactoryErp.Infrastructure/Persistence/Entities/LogisticsEntities.cs` |
| EF mappings | `src/FactoryErp.Infrastructure/Persistence/Configurations/LoadPlanningConfigurations.cs` |
| DbContext | `src/FactoryErp.Infrastructure/Persistence/FactoryErpDbContext.cs` |
| Command service | `src/FactoryErp.Infrastructure/Shipping/LoadPlanCommandService.cs` |
| API | `src/FactoryErp.Api/Controllers/LoadPlansController.cs`, `Program.cs`, `PermissionPolicies.cs` |
| DI/seed | `DependencyInjection.cs`, `IdentitySeeder.cs` |
| Migration | `20260817091855_AddLoadPlanAndUnits.cs`, designer ve model snapshot |
| Tests | `LoadPlanningTests.cs`, `LoadPlanningModelTests.cs`, `LoadPlanIntegrationTests.cs`, güncellenen `LogisticsSecurityIntegrationTests.cs` |
| Design | `postgresql-18-migration-sql-specification.md`, bu evidence dosyası |

## 6. Test ve verification sonuçları

| Gate | Sonuç |
|---|---|
| `dotnet restore FactoryErp.sln` | PASS |
| `dotnet build FactoryErp.sln --configuration Release` | PASS — 0 warning, 0 error |
| Domain unit tests | PASS — 86/86 |
| Architecture dependency tests | PASS — 5/5 |
| Infrastructure unit/model/integration/security tests | PASS — 54/54 |
| Full solution test toplamı | PASS — 145/145 |
| Real PostgreSQL migration apply | PASS |
| PostgreSQL migration history/schema inspection | PASS |
| Nested Draft persistence integration | PASS — 3/3 L4-B2 integration tests |
| Real `/api/v1/auth/login` LoadPlan permission boundary | PASS — 1/1 |
| `git diff --check` | PASS |

Infrastructure integration tests, sandbox reset sonrasında delivery-note seed fixture’ı bulunmadığı için testin kendi içinde minimal valid commercial chain oluşturarak çalışır: sales order, sales order item, issued delivery note, delivery-note item, shipment, route plan, package ve physical profile. Bu fixture product veya production koduna kalıcı seed davranışı eklemez.

## 7. Kalan riskler

L4-B2 migration’ı `load_unit_items` üzerinde atomic package MVP unique index’i kullanır. `splitAllowed=true` ile aynı package’ın birden fazla LoadUnit’e kontrollü bölünmesi henüz uygulanmış bir davranış değildir; bu özellik için L4-B3/B4 öncesinde index/serialized allocation stratejisinin ayrıca karara bağlanması gerekir.

`LoadPlanCommandService` Draft nested command’ında allocation quantity ceiling’i, package assignment guard’ı ve route-stop ownership kontrolü uygulanmaktadır. FFD normalization, vehicle capacity hard checks, deterministic stable sort, validation result persistence ve lock-time revalidation bu slice’ın kapsamı dışındadır.

Yeni clone edilmiş remote repository’de `AGENTS.md` ve `.claude/skills/` dosyaları bulunmadı; uygulama mevcut design belgeleri, remote L4-B1 baseline’ı ve repository’nin mevcut architecture/test konvansiyonları izlenerek yapıldı. Bu rehber dosyaları daha sonra remote’a eklenecekse sonraki slice başlamadan tekrar okunmalıdır.

## 8. Sonraki bounded slice

Bir sonraki slice **L4-B3 VehicleFitEvaluation ve deterministik FFD suggestion** olmalıdır. L4-B3 başlamadan önce şu kurallar korunmalıdır:

1. FFD optimal 3D packing olarak sunulmamalıdır.
2. Stable sort key UUID veya insertion order’a dayanmamalıdır.
3. Physical profile eksikliği `PHYSICAL_PROFILE_MISSING` hard sonucu üretmelidir.
4. `suggest` ve `evaluate` araç rezervasyonu veya stok hareketi üretmemelidir.
5. Lock/approval implementation’ı L4-B4 gate’ine kadar açılmamalıdır.

> **L4-B2 sonucu:** LoadPlan Draft, LoadUnit, LoadUnitItem, stop allocation persistence, state/quantity/ownership invariant’ları, PostgreSQL migration’ı, API policy boundary’si ve gerçek PostgreSQL testleri PASS. L4-B3’e otomatik geçilmedi.

## References

[1]: [L4-B ShipmentPackage, LoadPlan ve FFD teknik tasarımı](g6.2-l4b-shipmentpackage-loadplan-ffd-design.md)
[2]: [PostgreSQL migration SQL specification](postgresql-18-migration-sql-specification.md)
[3]: [L4-B1 ShipmentPackage implementation evidence](implementation-g6.2-l4b1-shipmentpackage.md)
