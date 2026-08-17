# G6.2 L4-B1 — ShipmentPackage Implementation Evidence

**Tarih:** 2026-08-17

**Durum:** Implementation tamamlandı; commit ve remote push öncesi son doğrulama
**Kapsam:** ShipmentPackage normalization, route-stop ownership, physical snapshot, PostgreSQL migration, API authorization ve test gate’leri

## 1. Mevcut durum

G6.2 routing ve L4-A Physical Logistics Master implementation’ı `29111a8` commit’iyle, L4-B ShipmentPackage/LoadPlan/FFD teknik tasarımı ise `ed88775` commit’iyle remote `main` dalına gönderilmişti. L4-B1 implementation başlamadan önce migration taslağı, mevcut EF model snapshot’ı ve L4-A effective physical-profile yapısı karşılaştırıldı.

Migration gözden geçirmesinde önemli bir ayrım netleştirildi. PostgreSQL migration specification içindeki 0009 bölümü L4-B’nin tamamı için canonical bounded taslağı içeriyor; ancak implementation tek büyük migration olarak yapılmadı. L4-B1 yalnızca ShipmentPackage sınırını açıyor. `load_plans`, `load_units`, `load_unit_items`, `load_unit_stop_allocations` ve `vehicle_fit_evaluations` sonraki bounded slice’larda kalıyor.

## 2. Uygulanan L4-B1 davranışları

| Alan | Uygulama sonucu |
|---|---|
| Aggregate | `ShipmentPackage` domain aggregate’ı eklendi. |
| Miktar | `quantityBase`, `packageCount × quantityBasePerPackage` ile yalnızca server’da hesaplanıyor. |
| Ceiling | Transaction içinde `shipment_item` satırı `FOR UPDATE` ile kilitleniyor; aktif package toplamı shipment item miktarını aşamıyor. |
| Ownership | `shipment_item` aynı shipment’a ait olmalı; route stop aynı shipment’ın route plan zincirinden gelmeli. |
| Snapshot | Packaging snapshot shipment item/packaging kaynağından; physical snapshot L4-A effective profile as-of lookup’ından server’da oluşturuluyor. |
| Package type | `Case`, `Package`, `Pallet`, `Loose`. |
| Status | `Available`, `Allocated`, `Loaded`, `Cancelled`. |
| Split | MVP default `splitAllowed=false`; gerçek LoadUnit allocation kuralları L4-B2’de uygulanacak. |
| Code uniqueness | İptal edilmemiş ve boş olmayan `package_code` için unique partial index. |
| Idempotency | Create endpoint replay ve payload mismatch davranışı mevcut store ile uygulandı. |
| Audit | Package oluşturma işlemi `ShipmentPackageCreated` audit kaydı üretiyor. |
| Side effect sınırı | ShipmentPackage create stok hareketi, araç durumu veya LoadPlan mutasyonu üretmiyor. |

## 3. Persistence ve migration

Oluşturulan EF migration:

```text
20260817081758_AddShipmentPackages
```

Migration local PostgreSQL `factory_erp_g1` veritabanına uygulandı ve `__EFMigrationsHistory` içinde doğrulandı. `shipment_packages` tablosunda aşağıdaki database korumaları mevcut:

| Database koruması | Durum |
|---|---|
| `shipment_id` → `shipments.id` `ON DELETE RESTRICT` | PASS |
| `shipment_item_id` → `shipment_items.id` `ON DELETE RESTRICT` | PASS |
| `packaging_id` → `product_packagings.id` `ON DELETE RESTRICT` | PASS |
| `route_stop_id` → `route_stops.id` `ON DELETE RESTRICT` | PASS |
| Type CHECK | PASS |
| Status CHECK | PASS |
| Pozitif quantity CHECK | PASS |
| Quantity formula CHECK | PASS |
| Active package-code partial unique index | PASS |
| `row_version` concurrency token | PASS |

Migration `Down` yalnızca `shipment_packages` tablosunu kaldırır. Production belge/lojistik verileri için destructive rollback kullanılmamalı; forward-fix veya backup restore tercih edilmelidir.

## 4. API ve yetkilendirme

Aşağıdaki endpoint’ler eklendi:

| Method | Endpoint | Permission |
|---|---|---|
| `POST` | `/api/v1/shipments/{shipmentId}/packages` | `shipment.package-manage` |
| `GET` | `/api/v1/shipments/{shipmentId}/packages` | `shipment.package-read` |
| `GET` | `/api/v1/shipment-packages/{packageId}` | `shipment.package-read` |

Permission policy sabitleri, ASP.NET Core authorization registration’ı, IdentitySeeder ve system-admin permission ataması güncellendi. Gerçek `/api/v1/auth/login` akışı üzerinden full kullanıcının manage/read, read-only kullanıcının yalnızca read sınırı doğrulandı.

## 5. Değiştirilen dosyalar

| Katman | Dosyalar |
|---|---|
| Domain | `src/FactoryErp.Domain/Shipping/ShipmentPackage.cs` |
| Application | `src/FactoryErp.Application/Shipping/LogisticsContracts.cs` |
| Infrastructure entity | `src/FactoryErp.Infrastructure/Persistence/Entities/LogisticsEntities.cs` |
| EF mapping | `src/FactoryErp.Infrastructure/Persistence/Configurations/ShipmentPackageConfiguration.cs` |
| DbContext | `src/FactoryErp.Infrastructure/Persistence/FactoryErpDbContext.cs` |
| Command service | `src/FactoryErp.Infrastructure/Shipping/ShipmentPackageCommandService.cs` |
| DI | `src/FactoryErp.Infrastructure/DependencyInjection.cs` |
| Migration | `20260817081758_AddShipmentPackages.cs` ve designer/snapshot dosyaları |
| API | `src/FactoryErp.Api/Controllers/ShipmentPackagesController.cs`, `Program.cs`, `PermissionPolicies.cs` |
| Permission seed | `src/FactoryErp.Infrastructure/Authentication/IdentitySeeder.cs` |
| Domain tests | `ShipmentPackageTests.cs` |
| EF model tests | `ShipmentPackageModelTests.cs` |
| PostgreSQL tests | `ShipmentPackageIntegrationTests.cs` |
| Security tests | `LogisticsSecurityIntegrationTests.cs` |
| Test fixture policy | `tests/FactoryErp.Infrastructure.UnitTests/AssemblyInfo.cs` |
| Design docs | `postgresql-18-migration-sql-specification.md`, bu evidence dosyası |

Daha önce bilerek dışarıda bırakılan whitespace-only tasarım dosyaları commit kapsamına alınmamıştır:

```text
design/database-technical-architecture.md
design/grok-session-review.md
design/ui-mockup-review.md
```

## 6. Verification sonuçları

| Gate | Sonuç |
|---|---|
| `dotnet build FactoryErp.sln --configuration Release` | PASS — 0 warning, 0 error |
| Domain unit tests | PASS — 76/76 |
| Architecture dependency tests | PASS — 5/5 |
| Infrastructure test suite | PASS — 48/48 |
| Full solution test toplamı | PASS — 129/129 |
| Real PostgreSQL migration apply | PASS |
| PostgreSQL constraint/index inspection | PASS |
| Real `/auth/login` security boundary | PASS — 1/1 |
| `git diff --check` | PASS |

Tam test suite, shared issued-delivery-note fixture’ı kullanan integration testlerinin birbirleriyle yarışmaması için infrastructure test assembly’sinde parallel execution kapatılarak çalıştırıldı. Bu değişiklik product code davranışını değiştirmez; test verisi izolasyon sınırını açık hale getirir.

## 7. Kalan riskler ve sonraki slice

L4-B1 tamamlanmış olsa da fiziksel package kayıtları henüz LoadPlan allocation’ı üretmiyor. `splitAllowed` alanı saklanıyor; fakat aynı package’ın LoadUnit’lere dağıtılmasını ve `LoadUnitStopAllocation` quantity ceiling’ini uygulayan persistence ve domain kuralları L4-B2 kapsamındadır.

Physical profile bulunmadığında create işlemi `PHYSICAL_PROFILE_MISSING` ile reddedilir. Bu, L4-B3 vehicle-fit ve lock gate’lerinin fiziksel veri eksikliğinde güvenli kalması için bilinçli bir hard constraint’tir.

Bir sonraki bounded slice **L4-B2: LoadPlan Draft, LoadUnit, LoadUnitItem ve LoadUnitStopAllocation persistence** olmalıdır. L4-B2 gate’i tamamlanmadan FFD engine, `suggest`, vehicle-fit evaluation veya LockLoadPlan implementation’ına geçilmemelidir.

> **L4-B1 sonucu:** ShipmentPackage quantity, ownership, physical snapshot, database constraints, API permission boundary, idempotency ve migration doğrulamaları PASS. LoadPlan/FFD implementation’ına otomatik geçilmedi.
