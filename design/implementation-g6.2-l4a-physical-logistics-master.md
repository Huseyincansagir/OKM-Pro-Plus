# Implementation G6.2 — L4-A Physical Logistics Master

**Durum:** PASS — bounded implementation slice

**Kapsam:** Bu slice, [L4-A/L4-B veritabanı şema tasarımında](g6.2-l4a-l4b-database-schema-design.md) tanımlanan L4-A Physical Logistics Master sınırının implementation karşılığıdır. Ürün ve ambalaj için effective-dated fiziksel profiller, palet tipi master verisi ve araç kapasitesi için palet uyumluluğu/yük bölgesi ilişkileri uygulanmıştır. LoadPlan, ShipmentPackage, LoadUnit, VehicleFitEvaluation ve First Fit Decreasing bu slice’a dahil değildir [1].

## 1. Uygulanan bounded davranış

Ürün fiziksel profili `ProductId + EffectiveFrom` anahtarıyla version’lanır. Ambalaj fiziksel profili aynı effective-date yaklaşımını kullanır. Yeni profil oluşturulurken aynı ürün veya ambalaj için kesişen geçerlilik aralığı reddedilir. Okuma işlemi `asOf` zamanı üzerinden geçerli profili seçer.

Fiziksel ölçüler milimetre, ağırlıklar kilogram ve hacim metreküp olarak saklanır. Boyutlar pozitif, ağırlıklar negatif olmayan, hacim ve istif kuralları tutarlı olmak zorundadır. Ambalaj brüt ağırlığı net ağırlık + dara toplamından küçük olamaz. İstiflenemeyen bir fiziksel birim için birden fazla kat tanımlanamaz.

Palet tipi fiziksel boyut, dara, opsiyonel brüt/payload/yükseklik ve istif sınırlarını taşır. `max_payload_kg` değeri `max_gross_weight_kg` değerini aşamaz. Palet kodu benzersizdir; hard delete yerine operasyonel tarihçeyi koruyan `IsActive` yaklaşımı kullanılır.

Araç kapasitesi ile palet tipi arasındaki ilişki composite key’li `vehicle_capacity_pallet_types` tablosunda tutulur. Araç kapasitesinin fiziksel yük bölgeleri `vehicle_capacity_zones` tablosunda sıralı ve benzersiz zone code ile tutulur. Bu iki ilişki daha sonra LoadPlan candidate evaluation için source of truth olacaktır.

Create command’leri transaction içinde audit ve idempotency replay kaydı üretir. Aynı `Idempotency-Key` aynı payload ile tekrarlandığında ilk DTO replay edilir; farklı payload ile tekrarlandığında `IDEMPOTENCY_PAYLOAD_MISMATCH` domain hatası üretilir.

## 2. Eklenen ve değiştirilen dosyalar

| Katman | Dosya | Sorumluluk |
|---|---|---|
| Domain | `src/FactoryErp.Domain/Shipping/PhysicalLogistics.cs` | ProductPhysicalProfile, PackagingPhysicalProfile ve PalletType invariant’ları |
| Application | `src/FactoryErp.Application/Shipping/PhysicalLogisticsContracts.cs` | L4-A request, DTO ve command service sözleşmeleri |
| Persistence entities | `src/FactoryErp.Infrastructure/Persistence/Entities/PhysicalLogisticsEntities.cs` | Physical profile, pallet, capacity-pallet ve capacity-zone records |
| Existing entity extension | `src/FactoryErp.Infrastructure/Persistence/Entities/LogisticsEntities.cs` | VehicleCapacity pallet/zone navigation collection’ları |
| EF mappings | `src/FactoryErp.Infrastructure/Persistence/Configurations/PhysicalLogisticsConfigurations.cs` | Table, FK, effective unique index, JSONB, precision ve CHECK constraint’leri |
| DbContext | `src/FactoryErp.Infrastructure/Persistence/FactoryErpDbContext.cs` | L4-A DbSet kayıtları |
| Infrastructure | `src/FactoryErp.Infrastructure/Shipping/PhysicalLogisticsCommandService.cs` | Transaction, effective overlap, audit ve idempotency |
| DI | `src/FactoryErp.Infrastructure/DependencyInjection.cs` | `IPhysicalLogisticsCommandService` registration’ı |
| API | `src/FactoryErp.Api/Controllers/PhysicalLogisticsController.cs` | Ürün/ambalaj profile ve pallet type endpoint’leri |
| Authorization | `PermissionPolicies.cs`, `Program.cs`, `IdentitySeeder.cs` | physical-profile ve pallet-type policy/seed kayıtları |
| Migration | `20260817073231_AddPhysicalLogisticsMaster` | L4-A PostgreSQL tabloları, FK, index ve check’ler |
| Domain tests | `tests/FactoryErp.Domain.UnitTests/Shipping/PhysicalLogisticsTests.cs` | 7 fiziksel profil/palet invariant testi |
| EF model tests | `tests/FactoryErp.Infrastructure.UnitTests/Shipping/PhysicalLogisticsModelTests.cs` | 4 mapping/index/check/concurrency testi |
| Integration | `tests/FactoryErp.Infrastructure.UnitTests/Shipping/PhysicalLogisticsIntegrationTests.cs` | PostgreSQL create, as-of lookup ve idempotent replay |
| Security | `tests/FactoryErp.Infrastructure.UnitTests/Shipping/LogisticsSecurityIntegrationTests.cs` | Gerçek login ile full/read-only L4-A permission sınırı |

Mevcut `LogisticsTests.cs` içinde ehliyet expiry fixture’ı da düzeltilmiştir: şoför oluşturma sırasında geçerli, route-end kontrolünde süresi dolmuş bir tarih kullanılmıştır. Bu değişiklik L4-A test gate’inin mevcut G6.2 test suite’iyle deterministik çalışmasını sağlar.

## 3. API özeti

| Method | Endpoint | Permission | Not |
|---|---|---|---|
| `POST` | `/api/v1/physical-logistics/products/{productId}/profiles` | `physical-profile.manage` | Effective ürün fiziksel profili oluşturur |
| `GET` | `/api/v1/physical-logistics/products/{productId}/profile?asOf=...` | `physical-profile.read` | As-of geçerli profili döner |
| `POST` | `/api/v1/physical-logistics/packagings/{packagingId}/profiles` | `physical-profile.manage` | Effective ambalaj fiziksel profili oluşturur |
| `GET` | `/api/v1/physical-logistics/packagings/{packagingId}/profile?asOf=...` | `physical-profile.read` | As-of geçerli profili döner |
| `POST` | `/api/v1/physical-logistics/pallet-types` | `pallet-type.manage` | Palet tipi master verisi oluşturur |
| `GET` | `/api/v1/physical-logistics/pallet-types/{id}` | `pallet-type.read` | Palet tipi detayını döner |

Tüm mutation endpoint’lerinde ortak idempotency middleware ve actor/correlation bilgisi kullanılır. Profile route’larında path ID ile request body ID eşleşmesi controller seviyesinde zorunludur.

## 4. PostgreSQL ve migration

L4-A için yeni migration gereklidir; mevcut schema’da fiziksel profile, pallet type, capacity-pallet ve capacity-zone tabloları yoktu.

```text
20260817073231_AddPhysicalLogisticsMaster
```

Migration local PostgreSQL `factory_erp_g1` veritabanına uygulanmıştır. Oluşturulan ana tablolar:

```text
product_physical_profiles
packaging_physical_profiles
pallet_types
vehicle_capacity_pallet_types
vehicle_capacity_zones
```

Kritik persistence guard’ları şunlardır:

- `(product_id, effective_from)` ve `(packaging_id, effective_from)` unique index’leri,
- `pallet_types.code` unique index’i,
- vehicle-capacity/pallet composite primary key’i,
- vehicle-capacity zone code ve sequence unique index’leri,
- boyut, ağırlık, hacim, effective-range, gross/payload ve stack rule CHECK constraint’leri,
- ürün, ambalaj, vehicle capacity ve pallet type foreign key’lerinde `RESTRICT`,
- profile ve pallet kayıtlarında `row_version` concurrency token’ları.

Effective-range overlap tam olarak unique index ile çözülemez; service transaction içindeki overlap guard ve gelecekte önerilecek PostgreSQL `tstzrange` exclusion constraint birlikte defense-in-depth sağlamalıdır.

## 5. Verification gate

| Gate | Sonuç |
|---|---|
| L4-A domain implementation | PASS |
| L4-A domain invariants | PASS |
| Domain unit tests | PASS — 64/64 |
| L4-A EF model tests | PASS — 4/4 |
| L4-A PostgreSQL integration | PASS — 1/1 |
| Real `/api/v1/auth/login` L4-A security | PASS — 1/1 |
| Read-only physical profile read | PASS |
| Read-only pallet create denial | PASS — 403 |
| Architecture tests | PASS — 5/5 |
| Full solution tests | PASS — Domain 64/64, Architecture 5/5, Infrastructure 44/44; toplam 113/113 |
| Release build | PASS — 0 warning, 0 error |
| Migration apply | PASS |
| `git diff --check` | PASS |

Yeni permission seed’leri Migrator üzerinden local veritabanına idempotent uygulanmıştır:

```text
physical-profile.read
physical-profile.manage
pallet-type.read
pallet-type.manage
```

## 6. Bilinçli olarak sonraya bırakılanlar

Bu bounded slice’ta `ShipmentPackage`, `LoadPlan`, `LoadUnit`, `LoadUnitItem`, stop allocation, vehicle fit evaluation, hard/soft capacity scoring, First Fit Decreasing, mixed pallet, manual override, LoadPlan approval/lock, barcode loading, GPS/traffic ve web/mobile UI uygulanmamıştır.

L4-B başlamadan önce şu source-of-truth eksikleri kapatılmalıdır: shipment item’dan fiziksel package üretim kuralı, package quantity/weight snapshot’ı, route stop ownership doğrulaması, palletization policy, vehicle capacity physical extension alanlarının API’ye açılması ve algorithm parameter set versioning.

## 7. Sonraki bounded slice

Bir sonraki slice **L4-B ShipmentPackage + LoadPlan draft + VehicleFitEvaluation read-only suggestion** olmalıdır. İlk L4-B implementation’ında yalnızca fiziksel profili eksiksiz shipment’lar için deterministic candidate evaluation ve FFD suggestion üretilmeli; `LoadPlan.Locked` ve araç rezervasyonu ayrı bir approval gate’inde tutulmalıdır. O-014 gereği algoritma açıklanabilir, deterministic ve manuel onaylı kalmalıdır [1].

## References

[1]: `g6.2-loadplan-ffd-design.md` — LoadPlan ve First Fit Decreasing bounded tasarımı.
[2]: `g6.2-l4a-l4b-database-schema-design.md` — L4-A/L4-B PostgreSQL şema tasarımı.
[3]: `product-packaging-and-uom.md` — Ürün, ambalaj, palet ve fiziksel profil kararları.
[4]: `implementation-ready.md` — Repository implementation gate.
