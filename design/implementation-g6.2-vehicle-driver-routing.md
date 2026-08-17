# G6.2 Vehicle, Driver ve Manuel Rotalama Implementation

**Durum:** PASS — bounded implementation slice

**Kapsam:** Bu slice, kabul edilmiş [G6.2 araç-şoför-rotalama tasarımını](g6.2-vehicle-driver-routing-design.md) implementation seviyesine taşır. Araç tipi, effective-date kapasite profili, gerçek araç, şoför, issued delivery note kaynaklı shipment, manuel route plan, route stop, vehicle/driver assignment, planlama, kilitleme ve versioned replan akışları uygulanmıştır. LoadPlan, vehicle-fit evaluation, mixed pallet, GPS/traffic provider, barkodlu yükleme ve teslim proof kapsam dışıdır [1] [2].

## 1. Uygulanan bounded davranış

Shipment yalnızca `Issued` durumundaki bir delivery note kaydından oluşturulabilir. Shipment oluşturma ikinci bir stok hareketi üretmez; delivery note source link’i `shipments.delivery_note_id` üzerinde unique tutulur. Sevk edilmiş delivery-note kalemleri immutable quantity ve packaging snapshot olarak shipment item kaydına kopyalanır.

Route plan, shipment’a bağlı versioned aggregate olarak uygulanmıştır. Yeni plan `Draft` durumda başlar. Duraklar 1’den başlayan ve boşluksuz ilerleyen sequence ile değiştirilir. Araç ve şoför ataması aynı command transaction’ı içinde yapılır. Planlama için zaman penceresi, en az bir durak ve iki operasyonel kaynak zorunludur. State akışı `Draft → Planned → Locked` şeklindedir. Kilitli plan doğrudan düzenlenemez; yeni version `ReplannedFromId` ile oluşturulur.

Araç ve şoför atamasında ortak resource satırları deterministik biçimde `FOR UPDATE` ile kilitlenir. Kilit sonrasında zaman çakışması yeniden okunur. Zaman aralıkları `[planned_start_at, planned_end_at)` semantiğiyle değerlendirilir; `end == start` komşu planlar çakışmış sayılmaz. Aynı araç veya şoför için örtüşen aktif planlar typed domain conflict ile reddedilir.

## 2. Değiştirilen ve eklenen dosyalar

| Katman | Dosya | Sorumluluk |
|---|---|---|
| Domain | `src/FactoryErp.Domain/Shipping/Logistics.cs` | VehicleType, VehicleCapacity, Vehicle, Driver, RoutePlan ve RouteStop state/invariant’ları |
| Application | `src/FactoryErp.Application/Shipping/LogisticsContracts.cs` | Command request’leri, DTO’lar ve `ILogisticsCommandService` |
| Persistence entities | `src/FactoryErp.Infrastructure/Persistence/Entities/LogisticsEntities.cs` | Shipment, shipment item, vehicle, capacity, driver, route ve stop kayıtları |
| EF mappings | `src/FactoryErp.Infrastructure/Persistence/Configurations/LogisticsConfigurations.cs` | Table mapping, FK, unique index, status/time/quantity check ve concurrency token’lar |
| DbContext | `src/FactoryErp.Infrastructure/Persistence/FactoryErpDbContext.cs` | Logistics DbSet kayıtları |
| Infrastructure | `src/FactoryErp.Infrastructure/Shipping/LogisticsCommandService.cs` | Transaction, idempotency, audit, row lock, assignment conflict ve state command’leri |
| API | `src/FactoryErp.Api/Controllers/VehicleTypesController.cs` | Vehicle type ve capacity profile endpoint’leri |
| API | `src/FactoryErp.Api/Controllers/VehiclesController.cs` | Vehicle create/read/status endpoint’leri |
| API | `src/FactoryErp.Api/Controllers/DriversController.cs` | Driver create/read/status endpoint’leri |
| API | `src/FactoryErp.Api/Controllers/ShipmentsController.cs` | Shipment create/read endpoint’leri |
| API | `src/FactoryErp.Api/Controllers/RoutePlansController.cs` | Route plan, stop, assignment, plan, lock ve replan endpoint’leri |
| API | `src/FactoryErp.Api/Controllers/LogisticsControllerBase.cs` | Actor, correlation, idempotency ve If-Match helper’ları |
| Authorization | `PermissionPolicies.cs`, `Program.cs`, `IdentitySeeder.cs` | Vehicle, driver, shipment ve route permission policy/seed kayıtları |
| Middleware | `IdempotencyKeyMiddleware.cs` | Logistics mutation POST route’larında idempotency enforcement |
| Migration | `20260816195958_AddVehicleDriverRoutePlanning` | PostgreSQL logistics tabloları ve constraints |
| Tests | `tests/FactoryErp.Domain.UnitTests/Shipping/LogisticsTests.cs` | Domain invariants ve state transition testleri |
| Tests | `tests/FactoryErp.Infrastructure.UnitTests/Shipping/LogisticsModelTests.cs` | EF metadata, index, FK, check ve concurrency testleri |
| Tests | `tests/FactoryErp.Infrastructure.UnitTests/Shipping/LogisticsIntegrationTests.cs` | PostgreSQL create/assignment/concurrency/exact-boundary fixture’ı |
| Tests | `tests/FactoryErp.Infrastructure.UnitTests/Shipping/LogisticsSecurityIntegrationTests.cs` | Gerçek `/auth/login` üzerinden RBAC sınırı |

## 3. API özeti

| Method | Endpoint | Permission | Not |
|---|---|---|---|
| `POST` | `/api/v1/vehicle-types` | `vehicle-type.manage` | Araç tipi oluşturur |
| `POST` | `/api/v1/vehicle-types/{id}/capacities` | `vehicle-type.manage` | Effective kapasite profili ekler |
| `POST` | `/api/v1/vehicles` | `vehicle.manage` | Araç oluşturur |
| `GET` | `/api/v1/vehicles/{id}` | `vehicle.read` | Araç ve row version döner |
| `POST` | `/api/v1/vehicles/{id}/status` | `vehicle.status-update` | `If-Match` zorunludur |
| `POST` | `/api/v1/drivers` | `driver.manage` | Şoför oluşturur |
| `GET` | `/api/v1/drivers/{id}` | `driver.read` | Şoför ve lisans bilgisi döner |
| `POST` | `/api/v1/shipments` | `shipment.create` | Issued delivery note’tan shipment üretir |
| `GET` | `/api/v1/shipments/{id}` | `shipment.read` | Shipment ve item snapshot döner |
| `POST` | `/api/v1/shipments/{id}/route-plans` | `shipment.route-manage` | Draft route oluşturur |
| `GET` | `/api/v1/route-plans/{id}` | `shipment.route-manage` | Route ve stop’ları döner |
| `POST` | `/api/v1/route-plans/{id}/stops/replace` | `shipment.route-manage` | Draft stop listesini atomik değiştirir |
| `POST` | `/api/v1/route-plans/{id}/assign-resources` | `shipment.route-manage` | Araç ve şoförü aynı command’de atar |
| `POST` | `/api/v1/route-plans/{id}/plan` | `shipment.route-manage` | Draft’ı Planned yapar |
| `POST` | `/api/v1/route-plans/{id}/lock` | `shipment.route-lock` | `confirmation=true` ile kilitler |
| `POST` | `/api/v1/route-plans/{id}/replan` | `shipment.plan-replan` | Locked plan’dan yeni version üretir |

Tüm mutation endpoint’lerinde `Idempotency-Key` middleware tarafından zorunlu tutulur. Resource mutation’larında `If-Match: "<rowVersion>"` kullanılmalıdır. Stale version `RESOURCE_VERSION_CONFLICT` ile reddedilir.

## 4. PostgreSQL ve migration kararı

Yeni migration gereklidir; mevcut schema’da shipment, vehicle, driver ve route tabloları yoktu. Oluşturulan migration:

```text
20260816195958_AddVehicleDriverRoutePlanning
```

Migration local PostgreSQL `factory_erp_g1` veritabanına uygulanmıştır. Oluşturulan ana tablolar şunlardır:

```text
shipments
shipment_items
vehicle_types
vehicle_capacities
vehicles
drivers
route_plans
route_stops
```

Kritik persistence guard’ları şunlardır: delivery note source unique index’i, shipment item source unique index’i, route `(shipment_id, version)` unique index’i, route stop `(route_plan_id, sequence_no)` unique index’i, status/time/positive sequence check’leri ve vehicle/driver/route/shipment `row_version` concurrency token’larıdır.

## 5. Concurrency ve transaction kanıtı

Assignment command’inde transaction içindeki lock sırası şöyledir:

```text
RoutePlan → Shipment → Vehicle IDs ascending → Driver IDs ascending → overlap re-read → update → audit/idempotency → commit
```

Araç veya şoför satırını kilitlemeden yalnızca route plan satırını kilitlemek yeterli değildir; farklı route satırlarından gelen iki transaction aynı resource’a yarışabilir. Bu slice’ta shared resource row lock ve lock sonrası overlap re-read birlikte uygulanmıştır. Aynı zaman aralığı için iki PostgreSQL connection ile yapılan testte yalnızca bir assignment başarılı olmuş, diğer transaction `VEHICLE_SCHEDULE_CONFLICT` veya `DRIVER_SCHEDULE_CONFLICT` ile reddedilmiştir. Exact boundary testinde `[08:00, 10:00)` ile `[10:00, 12:00)` kabul edilmiştir.

## 6. Verification gate

| Gate | Sonuç |
|---|---|
| Domain implementation | PASS |
| Domain invariants | PASS |
| Domain unit tests | PASS — 57/57 |
| EF model tests | PASS — logistics model assertions dahil |
| PostgreSQL logistics integration | PASS |
| PostgreSQL concurrency race | PASS |
| Real `/api/v1/auth/login` security test | PASS — 1/1 |
| Architecture tests | PASS — 5/5 |
| Full solution tests | PASS — Domain 57/57, Architecture 5/5, Infrastructure 39/39; toplam 101/101 |
| Release build via solution test gate | PASS |
| Migration apply | PASS |
| `git diff --check` | Pending final pre-commit check |

## 7. Kalan riskler ve sonraki bounded slice

Bu slice’ın bilinçli sınırı nedeniyle vehicle-fit evaluation, hard capacity fit, First Fit Decreasing, mixed pallet, load plan, physical package/load-unit allocation, vehicle loading barcode, stop-level delivery quantity, proof of delivery, GPS, traffic provider, driver working-hour policy ve web/mobile UI henüz uygulanmamıştır.

Ayrıca `Shipment` source bridge bu slice içinde minimum create/read seviyesinde uygulanmıştır. Delivery-note issue akışının tüm L2 endpoint sözleşmesi, kısmi shipment ve shipment-ready workflow’ının genişletilmesi ayrı bir L2 hardening slice’ı olarak ele alınmalıdır. Bu slice, route plan’ın issued delivery note’tan türeyen shipment kaydı üzerinde çalışması için gereken persistence ve API temelini sağlar.

Önerilen sonraki bounded slice **L4 LoadPlan + VehicleFitEvaluation**’dır. Bu slice başlamadan önce kapasite profili alanlarının kg/m³/palet/istif yüksekliği semantiği ve hard/soft constraint kararları implementation gate olarak tekrar doğrulanmalıdır.

## References

[1]: `g6.2-vehicle-driver-routing-design.md` — Araç, şoför ve manuel rotalama bounded tasarımı.
[2]: `g6.2-vehicle-driver-assignment-concurrency-sql.md` — PostgreSQL row-lock ve assignment concurrency tasarımı.
[3]: `l2-delivery-note-shipment-api-contract.md` — Delivery note–shipment API sözleşmesi.
[4]: `implementation-ready.md` — Repository implementation gate.
