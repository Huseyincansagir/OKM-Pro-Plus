# Factory ERP-Lite — G3 Catalog, Packaging and Stock Foundation Evidence

**Durum:** G3 tamamlandı — G4 satış/teklif/sipariş slice’ına geçişe hazır
**Tarih:** 2026-08-16
**G2 başlangıç commit’i:** `95b22bb`

## 1. Tamamlanan kapsam

G3, kabul edilmiş Palet → Koli → Paket → Temel Birim modelini persistence, API ve controlled seed seviyesinde uyguladı. Eklenen kayıtlar; `units_of_measure`, `product_categories`, `products`, `product_packagings`, `product_barcodes`, `product_images`, `warehouses`, `warehouse_locations`, `stocks`, `stock_movements`, `production_orders` ve `production_records` tablolarıdır.

Ürün ve ambalaj persistence’ında `snake_case` kolonlar, UOM precision alanı, ürün/public/category index’leri, tarihsel `effective_from/effective_to`, ambalaj seviyeleri, `quantity_in_base_uom`, `allow_partial`, aktif barkod unique index’i ve ürün başına packaging version kuralları tanımlandı. Stok ledger’ı `numeric(18,6)` temel birimde tutulur; on-hand, reserved ve available projection ayrımı korunur. Stok ve üretim miktarlarında PostgreSQL check constraint’leri bulunur.

G3 Application contract’ı public catalog DTO’larını, pagination’ı, barcode resolution’ı ve packaging-aware quantity preview’ı tanımlar. Infrastructure service yalnızca active/public ürün projection’ı döndürür; public response stok, maliyet, ledger, risk ve internal allocation alanlarını içermez.

`QuantityPreview` frontend’in gönderdiği `quantityBase` değerine güvenmez. Product, base UOM ve effective packaging database’den tekrar okunur; `PackagingSnapshot` oluşturulur; `PositiveQuantity` ile precision/positive/partial packaging kuralları uygulanır; `quantityBase` server’da yeniden hesaplanır. Warehouse verilirse available base quantity ve `INSUFFICIENT_AVAILABLE_STOCK` uyarısı üretilir.

Mobile endpoint’leri:

| Method | Path | Davranış |
|---|---|---|
| POST | `/api/v1/mobile/barcodes/resolve` | Barkodu ürün ve ambalaj seviyesine çözer |
| POST | `/api/v1/mobile/quantity-previews` | Miktarı server-side temel birime çevirir |

Public endpoint’leri:

| Method | Path | Davranış |
|---|---|---|
| GET | `/api/v1/public/catalog/products` | Allowlist public product pagination |
| GET | `/api/v1/public/catalog/products/{slug}` | Public product detail |

## 2. Seed edilmiş referans akışı

Controlled Migrator, duplicate üretmeden aşağıdaki örnek veri setini oluşturur:

| Nesne | Değer |
|---|---|
| Ürün | `NAP-001 / Premium Peçete 33x33` |
| Base UOM | `Piece / Adet`, scale `0` |
| Paket | `1 Paket = 100 adet` |
| Koli | `1 Koli = 20 Paket = 2.000 adet` |
| Palet | `1 Palet = 40 Koli = 80.000 adet` |
| Koli barkodu | `869000000002` |
| Ana depo stoku | `18.000 adet` |

`5 Koli` girişi `5 × 2.000 = 10.000 adet` olarak hesaplanır. Bu hesap product/packaging snapshot’a dayanır ve public catalog display’inden bağımsızdır.

## 3. Kanıtlar

| Kontrol | Sonuç |
|---|---|
| Release solution build | 0 warning / 0 error |
| Domain unit tests | 28 passed |
| Architecture tests | 5 passed |
| Infrastructure model/security/catalog tests | 11 passed |
| G3 migration | Isolated PostgreSQL’de başarılı |
| Public catalog endpoint | 200 |
| Barcode resolve endpoint | 200 |
| Quantity preview endpoint | 200; `quantityBase=10000` |
| Stock available projection | `18000` base quantity |
| Packaging precision/constraints | Model testleri başarılı |
| Migrator seed idempotency | Existing records duplicate edilmedi |

FluentAssertions testlerinde Xceed lisans bilgilendirme mesajı görülmektedir; bu test hatası değildir ve ticari kullanım öncesinde paket lisansı değerlendirilmelidir.

## 4. Bilinçli sınırlar

G3’te ürün master data, packaging conversion, public projection, barcode ve stock foundation hazırlandı. Ürün CRUD ekranları, image/file upload, quote basket write model, order aggregate, reservation transaction, production completion command ve stock movement command henüz eklenmemiştir. `ProductionOrderRecord` ve `StockMovementRecord` persistence foundation olarak hazırdır; business command’ları G6/G4 transaction slice’larında tamamlanacaktır.

## 5. G4 handoff

G4; customer/customer candidate, price snapshot, quote request, quote basket-to-review ve SalesOrder creation/submit/approve akışını kuracaktır. Sipariş approval ile reservation aynı transaction’da çalışmalı; `SalesOrderItem` allocation invariant’ları, `Idempotency-Key`, `If-Match`, audit ve permission policy birlikte uygulanmalıdır. Public quote request doğrudan active customer oluşturmamalı; review command’ı duplicate candidate sonucu üretmelidir.
