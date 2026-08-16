# G6.2 Stok ve Lojistik — İlk Bounded Slice

**Tarih:** 2026-08-16
**Durum:** PASS — warehouse transfer vertical slice

## 1. Slice seçimi ve kapsam

G6.1 production-to-stock akışının commit edilmesinden sonra G6.2 için en küçük faydalı stok/lojistik bounded slice olarak **depo transferi** seçildi. Bu seçim, mevcut `Stock`/`StockMovement` ledger kaynaklarını genişletir; ileride sayım, sevkiyat yükleme, araç kapasitesi ve rota planı akışlarının kullanacağı transaction/idempotency/quantity temelini hazırlar.

Bu slice tek ürünlü bir transfer aggregate’ını kapsar:

```text
Transfer draft oluştur
→ source/target warehouse-location doğrula
→ packaging üzerinden quantity_base server-side hesapla
→ Complete sırasında source/target stock row kilitle
→ Available stock kontrolü
→ WarehouseTransferOut + WarehouseTransferIn
→ stock projection güncelle
→ audit + idempotency replay
```

G6.2 bu aşamada tüm lojistik modülü değildir. Araç/araç tipi, şoför, sevkiyat, route stop, mixed pallet, load plan, vehicle-fit evaluation, delivery proof ve mobil yükleme/teslimat sonraki bounded slice’larda kalır. Makine/personel/duruş da bu slice’a eklenmemiştir.

## 2. Uygulanan domain kuralları

`StockTransfer` aggregate’ı `Draft → Completed` ve `Draft → Cancelled` geçişlerini yönetir. Tamamlanmış veya iptal edilmiş transfer yeniden tamamlanamaz ya da iptal edilemez. Kaynak ve hedef depo-konum aynı olamaz. Girilen miktar ve hesaplanan temel miktar pozitif olmalıdır. Ambalaj snapshot’ı boş bırakılamaz.

Client tarafından gönderilen bir `quantityBase` alanı kabul edilmez. Create command yalnızca ürün, girilen miktar, `EnteredPackagingId` ve `ViewMode` alır; backend `IProductCatalogService.PreviewQuantityAsync` ile dönüşümü yeniden hesaplar. Kayıt üzerinde girilen miktar, işlem ambalajı, görünüm modu, hesaplanan temel miktar ve snapshot saklanır.

Completion sırasında `AvailableBaseQuantity = OnHandQtyBase - ReservedQtyBase` kontrolü yapılır. Kaynak miktar yeterli değilse `INSUFFICIENT_AVAILABLE_STOCK` döner ve hiçbir stok movement/projection değişikliği commit edilmez. Başarılı completion aynı database transaction’ında iki pozitif ledger satırı üretir: kaynakta `WarehouseTransferOut`, hedefte `WarehouseTransferIn`.

## 3. API ve yetkilendirme

| Method | Endpoint | Permission | Davranış |
|---|---|---|---|
| `POST` | `/api/v1/warehouse-transfers` | `stock-transfer.create` | Draft transfer oluşturur; quantity snapshot server-side hesaplanır |
| `GET` | `/api/v1/warehouse-transfers/{id}` | `stock-transfer.read` | Transfer durumunu ve snapshot’ını döner |
| `POST` | `/api/v1/warehouse-transfers/{id}/complete` | `stock-transfer.complete` | Kaynak/hedef stoğu atomik biçimde hareket ettirir |
| `POST` | `/api/v1/warehouse-transfers/{id}/cancel` | `stock-transfer.cancel` | Movement üretmeden Draft transferi iptal eder |

Tüm transfer mutation POST’ları için `Idempotency-Key` middleware kapsamına `/api/v1/warehouse-transfers` segmenti eklendi. Aynı key ve aynı payload replay olduğunda ilk sonuç döner; farklı payload aynı key ile gönderilirse mevcut `IDEMPOTENCY_PAYLOAD_MISMATCH` davranışı korunur.

Yeni permission seed’leri idempotent biçimde `IdentitySeeder` içine eklendi:

```text
stock-transfer.create
stock-transfer.read
stock-transfer.complete
stock-transfer.cancel
```

## 4. Persistence ve migration

Yeni `stock_transfers` tablosu aşağıdaki bounded slice alanlarını taşır: ürün, kaynak/hedef depo ve konum, girilen miktar, işlem ambalajı, `view_mode`, server-calculated `quantity_base`, `packaging_snapshot`, status, lifecycle timestamps ve `row_version`.

Migration:

```text
20260816182002_AddStockTransfers
```

Migration mevcut `stocks`, `stock_movements`, `products`, `warehouses`, `warehouse_locations` ve `product_packagings` tablolarına foreign key bağlantıları kurar. `entered_quantity > 0` ve `quantity_base > 0` database check constraint’leri; status/created-at ve product/source/target sorgu index’leri eklenmiştir. `stocks` mevcut unique `(product_id, warehouse_id, location_id)` anahtarı ve non-negative projection kuralları korunmuştur.

Completion sırasında hedef stock row yoksa `INSERT ... ON CONFLICT DO NOTHING` ile oluşturulur; ardından kaynak ve hedef row’lar deterministik sırada `SELECT ... FOR UPDATE` ile kilitlenir. Bu, aynı hedef stoğa gelen transferlerde row-create yarışını azaltır ve ledger/projection güncellemesini transaction içinde tutar.

## 5. Değiştirilen dosyalar

| Katman | Dosyalar |
|---|---|
| Domain | `src/FactoryErp.Domain/Warehouse/StockTransfer.cs` |
| Application | `src/FactoryErp.Application/Warehouse/StockTransferContracts.cs` |
| Infrastructure entity/config | `CatalogAndStockEntities.cs`, `CatalogAndStockConfigurations.cs`, `FactoryErpDbContext.cs` |
| Infrastructure service | `src/FactoryErp.Infrastructure/Warehouse/StockTransferCommandService.cs` |
| API | `StockTransfersController.cs`, `PermissionPolicies.cs`, `Program.cs` |
| Cross-cutting | `IdempotencyKeyMiddleware.cs`, `IdentitySeeder.cs`, `DependencyInjection.cs` |
| Migration | `20260816182002_AddStockTransfers.cs`, designer ve model snapshot |
| Tests | `StockTransferTests.cs`, `StockTransferModelTests.cs`, `StockTransferIntegrationTests.cs`, gerçek login security test genişletmesi |

## 6. Verification gate

| Kontrol | Sonuç |
|---|---|
| `dotnet restore FactoryErp.sln` | PASS |
| Release build | PASS — 0 warning, 0 error |
| G6.2 Domain transfer tests | PASS — 7/7 |
| G6.2 EF model + PostgreSQL integration tests | PASS — 3/3 |
| Real `/api/v1/auth/login` production + stock-transfer security tests | PASS — 2/2 |
| Architecture dependency tests | PASS — 5/5 |
| Full solution tests | PASS — 85/85 |
| `git diff --check` | PASS |
| `20260816182002_AddStockTransfers` local PostgreSQL apply | PASS |
| Idempotent production permission seed | PASS |

Full solution test dağılımı:

| Test assembly | Passed |
|---|---:|
| `FactoryErp.Domain.UnitTests` | 48 |
| `FactoryErp.Infrastructure.UnitTests` | 32 |
| `FactoryErp.ArchitectureTests` | 5 |
| **Toplam** | **85** |

## 7. Kalan riskler ve sonraki bounded slices

İlk transfer slice’ında çok satırlı transfer belgesi, transferin `Preparing/InTransit` ara durumları, sayım oturumu ve fark onayı henüz yoktur. Bu nedenle UI’de tam depo operasyon ekranı varmış gibi gösterilmemelidir.

Gerçek two-connection concurrent transfer testi sonraki hardening adımıdır. Mevcut completion lock sırası deterministiktir; ancak PostgreSQL yarış testi aynı source stock, aynı target stock, insufficient available ve duplicate idempotency senaryolarını ayrı kanıtlamalıdır.

Tam lojistik sınırı şu sırayla ilerlemelidir:

```text
G6.2.1 stock count + adjustment approval
→ G6.2.2 multi-line warehouse transfer + in-transit state
→ G6.3 vehicle/driver master and shipment assignment
→ G6.4 load unit/mixed pallet/capacity evaluation
→ G6.5 route stop/package trace/delivery proof
```

Araç kapasitesi ve mixed-pallet algoritması O-014’teki hard constraint + First Fit Decreasing öneri + depo sorumlusu manuel onayı sınırını korumalıdır. Otomatik plan optimal çözüm olarak sunulmamalı; input snapshot, algorithm version, candidate rejection reason ve manual replan audit’i olmadan sonraki lojistik slice tamamlanmış sayılmamalıdır.
