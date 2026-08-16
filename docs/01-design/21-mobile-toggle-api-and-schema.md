# Mobil Palet/Koli/Paket Toggle — Database ve API Sözleşmesi

**Durum:** Kodlama öncesi canonical teknik tasarım
**Kapsam:** Mobil barkod okuma, miktar önizleme, stok sayımı, transfer, sevkiyat yükleme ve durak teslimatı
**Arayüz dili:** Türkçe; entity, property, endpoint ve enum isimleri İngilizce

## 1. Temel ayrım

Mobilde iki farklı seçim birbirinden ayrılmalıdır:

| Alan | Örnek | Veritabanı etkisi |
|---|---|---|
| `viewMode` | `BaseUnit`, `Packaging`, `Breakdown` | Başarılı işlem ledger'ını değiştirmez; yalnızca response görünümünü etkiler |
| `operationPackagingId` | `Case / Koli` | Miktarın hangi ambalaj seviyesinde girildiğini belirler |
| `enteredQuantity` | `5` | Kullanıcının işlem miktarı |
| `quantityBase` | `10.000 adet` | Backend tarafından hesaplanan stok doğruluk değeri |
| `packagingSnapshot` | `5 Koli (20 Paket/Koli, 100 adet/Paket)` | İşlem tarihindeki dönüşümün değişmez kopyası |

> **İstemci `quantityBase` gönderse bile backend bu değere güvenmez.** Backend yalnızca ürün, geçerli ambalaj seviyesi ve `enteredQuantity` üzerinden temel miktarı yeniden hesaplar.

Toggle değişimi aşağıdaki işlemlerden hiçbirini tek başına yapmaz:

- Stok hareketi oluşturmaz.
- Rezervasyon oluşturmaz veya çözmez.
- Sevkiyat miktarını değiştirmez.
- Teslimat durumunu değiştirmez.
- Fatura veya cari hareket üretmez.

## 2. Enum ve veri tipleri

```text
enum QuantityViewMode {
  BaseUnit,    // Temel Birim
  Packaging,   // Ambalaj
  Breakdown    // Kırılım
}

enum PackagingLevel {
  BaseUnit,
  Package,     // Paket
  Case,        // Koli
  Pallet       // Palet
}

enum QuantityOperationType {
  StockLookup,
  StockCount,
  WarehouseTransfer,
  ShipmentLoad,
  ShipmentDelivery,
  DeliveryNoteIssue,
  ProductionOutput
}
```

`QuantityViewMode` response ve kullanıcı tercihi içindir. `PackagingLevel` veya `operationPackagingId` transaction miktarı içindir. Bu iki enum aynı amaçla kullanılmamalıdır.

## 3. Database şeması

### 3.1 `product_packagings`

Ürünün palet/koli/paket dönüşüm hiyerarşisini tutar.

| Alan | Tip | Kural |
|---|---|---|
| `id` | UUID | Primary key |
| `product_id` | UUID | `products.id` foreign key |
| `level` | varchar/enum | `BaseUnit`, `Package`, `Case`, `Pallet` |
| `name` | varchar | Türkçe kullanıcı etiketi |
| `parent_packaging_id` | UUID nullable | Üst ambalaj ilişkisi |
| `display_order` | smallint | Toggle/selector sırası |
| `units_per_parent` | numeric | Üst seviyedeki alt ambalaj sayısı |
| `quantity_in_base_uom` | numeric | Bir ambalajın kesin temel karşılığı |
| `decimal_scale` | smallint | Giriş hassasiyeti |
| `is_sellable` | boolean | Satış/teklif/sevkiyat seçilebilirliği |
| `allow_partial` | boolean | Ambalaj açılabilir mi |
| `is_active` | boolean | Kullanılabilirlik |
| `effective_from` | timestamptz | Geçerlilik başlangıcı |
| `effective_to` | timestamptz nullable | Geçerlilik bitişi |
| `row_version` | bigint/xmin | Concurrency |

Kısıtlar:

```text
UNIQUE(product_id, level, effective_from)
CHECK(quantity_in_base_uom > 0)
CHECK(units_per_parent IS NULL OR units_per_parent > 0)
CHECK(effective_to IS NULL OR effective_to > effective_from)
```

Aynı ürün ve işlem tarihi için yalnızca bir geçerli `PackagingLevel` kaydı bulunmalıdır. Katsayı değiştiğinde mevcut kayıt sessizce güncellenmez; yeni effective version açılır.

### 3.2 `product_barcodes`

Ürün veya ambalaj/yük birimi barkodlarını çözer.

| Alan | Tip | Kural |
|---|---|---|
| `id` | UUID | Primary key |
| `product_id` | UUID | Ürün bağlantısı |
| `packaging_id` | UUID nullable | Barkod ambalaj seviyesine aitse dolu |
| `barcode` | varchar | Normalize edilmiş benzersiz kod |
| `barcode_type` | varchar | `Product`, `Packaging`, `LoadUnit`, `ShipmentPackage` |
| `is_primary` | boolean | Ürün ana barkodu |
| `is_active` | boolean | Kullanılabilirlik |
| `effective_from`, `effective_to` | timestamptz | Tarihsel geçerlilik |

`barcode` için aktif kayıtlar arasında unique index bulunmalıdır. Aynı barkodun iki aktif ürüne veya iki aktif ambalaj seviyesine bağlanması engellenir.

### 3.3 `quantity_operation_snapshots`

Başarılı miktar işlemlerinin ambalaj ve toggle bağlamını değişmez biçimde saklayan ortak teknik kayıttır. Bu tablo stok ledger'ının yerine geçmez; ledger veya belge satırını destekler.

| Alan | Tip | Açıklama |
|---|---|---|
| `id` | UUID | Snapshot kimliği |
| `operation_type` | varchar | `StockCount`, `ShipmentLoad` vb. |
| `operation_id` | UUID | İlgili transaction/belge kimliği |
| `product_id` | UUID | Ürün |
| `barcode_id` | UUID nullable | Okutulan barkod |
| `operation_packaging_id` | UUID | İşlem seviyesi |
| `view_mode_at_entry` | varchar | `BaseUnit`, `Packaging`, `Breakdown` |
| `entered_quantity` | numeric | Kullanıcı girişi |
| `quantity_base` | numeric | Backend hesaplaması |
| `base_uom_id` | UUID | Temel ölçü birimi |
| `packaging_snapshot` | jsonb | Ad, katsayı, hiyerarşi, geçerlilik |
| `packaging_breakdown` | jsonb nullable | `4 Koli + 6 Paket` gibi kırılım |
| `warehouse_id` | UUID nullable | Depo bağlamı |
| `warehouse_location_id` | UUID nullable | Konum bağlamı |
| `shipment_id` | UUID nullable | Sevkiyat bağlamı |
| `route_stop_id` | UUID nullable | Aktif durak bağlamı |
| `load_unit_id` | UUID nullable | Palet/kargo plan bağlamı |
| `client_request_id` | varchar | Idempotency anahtarı |
| `created_by`, `created_at` | UUID/timestamptz | Audit |

Kısıtlar:

```text
CHECK(entered_quantity > 0)
CHECK(quantity_base > 0)
UNIQUE(operation_type, operation_id, client_request_id)
```

Aynı `client_request_id` ile gelen tekrar istek yeni stok veya teslim hareketi üretmez; ilk başarılı snapshot ve sonuç tekrar döndürülebilir.

### 3.4 `user_mobile_preferences` (opsiyonel)

Toggle'ın son kullanılan görünümünü kullanıcı cihazları arasında taşımak istenirse kullanılabilir. Bu tablo operasyon doğruluğunun kaynağı değildir.

| Alan | Tip |
|---|---|
| `user_id` | UUID |
| `device_id` | varchar |
| `default_quantity_view_mode` | varchar |
| `updated_at` | timestamptz |

`default_quantity_view_mode` yalnızca başlangıç görünümünü seçer. İşlem seviyesi veya `quantity_base` üzerinde hiçbir yetkisi yoktur.

## 4. Hesaplama sözleşmesi

### 4.1 Tek ambalaj

```text
enteredQuantity = 5
operationPackaging = Case / Koli
quantityInBaseUom = 2.000 adet

quantityBase = 5 × 2.000 = 10.000 adet
```

### 4.2 Kırılım

```text
4 Koli + 6 Paket
quantityBase = (4 × 2.000) + (6 × 100)
quantityBase = 8.600 adet
```

### 4.3 Ağırlık bazlı ürün

```text
1 Koli = 60 kg
enteredQuantity = 5
quantityBase = 300 kg
```

Backend response'u üç görünümü birlikte üretmelidir:

```json
{
  "viewMode": "Packaging",
  "operationPackagingId": "case-id",
  "enteredQuantity": 5,
  "quantityBase": 10000,
  "baseUom": { "code": "Piece", "displayName": "adet" },
  "display": {
    "baseUnit": "10.000 adet",
    "packaging": "5 Koli",
    "breakdown": "5 Koli",
    "helperText": "1 Koli = 2.000 adet"
  },
  "packagingSnapshot": {
    "name": "Koli",
    "quantityInBaseUom": 2000,
    "hierarchy": "20 Paket/Koli × 100 adet/Paket"
  }
}
```

## 5. Mobil API endpoint'leri

### 5.1 Barkod çözümleme

```http
GET /api/mobile/barcodes/resolve?code={barcode}&operationType={operationType}&warehouseId={warehouseId}&shipmentId={shipmentId}&routeStopId={routeStopId}
```

Amaç; barkodun ürün, ambalaj veya yük birimi bağlamını çözmek ve işlem yapılabilirliğini kontrol etmektir.

Başarılı response:

```json
{
  "barcodeId": "...",
  "barcodeType": "Packaging",
  "product": {
    "id": "...",
    "code": "NAP-3333-PREM",
    "name": "Premium Napkin 33x33"
  },
  "packaging": {
    "id": "...",
    "level": "Case",
    "name": "Koli",
    "quantityInBaseUom": 2000,
    "baseUom": "adet"
  },
  "availableQuantity": {
    "base": 36000,
    "packaging": "18 Koli"
  },
  "context": {
    "warehouseId": "...",
    "shipmentId": "...",
    "routeStopId": "...",
    "isAllowedForOperation": true
  },
  "allowedViewModes": ["BaseUnit", "Packaging", "Breakdown"],
  "allowedOperationPackagings": ["...", "...", "..."]
}
```

Sunucu, `routeStopId`, `warehouseId`, shipment ve kullanıcı permission bağlamını doğrular. Aktif durağa ait olmayan paket başarı response'u gibi gösterilmez; açık bir `PACKAGE_NOT_IN_ACTIVE_STOP` hatası döner.

### 5.2 Miktar seçeneklerini getirme

```http
GET /api/mobile/products/{productId}/quantity-options?operationType={operationType}&warehouseId={warehouseId}&shipmentId={shipmentId}
```

Response, toggle seçeneklerini ve işlem seviyesi seçeneklerini birbirinden ayırır:

```json
{
  "defaultViewMode": "Packaging",
  "viewModes": [
    { "code": "BaseUnit", "label": "Temel Birim", "enabled": true },
    { "code": "Packaging", "label": "Ambalaj", "enabled": true },
    { "code": "Breakdown", "label": "Kırılım", "enabled": true }
  ],
  "operationPackagings": [
    { "id": "base-id", "level": "BaseUnit", "label": "Adet", "allowPartial": true },
    { "id": "package-id", "level": "Package", "label": "Paket", "allowPartial": true },
    { "id": "case-id", "level": "Case", "label": "Koli", "allowPartial": false },
    { "id": "pallet-id", "level": "Pallet", "label": "Palet", "allowPartial": false }
  ]
}
```

### 5.3 Miktar önizlemesi

```http
POST /api/mobile/quantity-previews
Idempotency-Key: {clientRequestId}
```

Request:

```json
{
  "operationType": "ShipmentLoad",
  "productId": "...",
  "barcodeId": "...",
  "operationPackagingId": "case-id",
  "enteredQuantity": 5,
  "viewMode": "Packaging",
  "warehouseId": "...",
  "warehouseLocationId": "...",
  "shipmentId": "...",
  "routeStopId": "...",
  "loadUnitId": "...",
  "clientRequestId": "mobile-device-123:scan-00042"
}
```

Bu endpoint transaction üretmez. Yalnızca geçerli ambalajı, temel dönüşümü, kullanılabilir miktarı, kapasite/rota bağlamını ve uyarıları hesaplar.

Response:

```json
{
  "previewId": "...",
  "isValid": true,
  "quantityBase": 10000,
  "display": {
    "baseUnit": "10.000 adet",
    "packaging": "5 Koli",
    "breakdown": "5 Koli"
  },
  "availability": {
    "availableBase": 36000,
    "remainingAfterOperation": 26000
  },
  "warnings": [],
  "blockingErrors": [],
  "snapshot": {
    "operationPackagingId": "case-id",
    "packagingName": "Koli",
    "quantityInBaseUom": 2000,
    "calculatedAt": "2026-08-16T...Z"
  }
}
```

### 5.4 Sayım kaydı

```http
POST /api/mobile/stock-counts/{stockCountId}/items
Idempotency-Key: {clientRequestId}
```

Request, `quantity-previews` sözleşmesine ek olarak `countReason` veya `note` alır. Sunucu `quantityBase` değerini yeniden hesaplar; farkı temel birimde üretir ve ambalaj snapshot'ını kaydeder.

### 5.5 Depo transferi

```http
POST /api/mobile/warehouse-transfers/{transferId}/items
Idempotency-Key: {clientRequestId}
```

İstek kaynak depo/konum, hedef depo/konum, ürün, `operationPackagingId`, `enteredQuantity`, barkod ve client request id taşır. Kaynak `AvailableBaseQuantity` kontrolü geçmeden transfer hareketi oluşturulmaz.

### 5.6 Sevkiyat yükleme doğrulaması

```http
POST /api/mobile/shipments/{shipmentId}/load-scans
Idempotency-Key: {clientRequestId}
```

Request:

```json
{
  "barcode": "PALLET-001",
  "loadUnitId": "...",
  "routeStopId": "...",
  "operationPackagingId": "case-id",
  "enteredQuantity": 5,
  "viewMode": "Packaging",
  "warehouseId": "...",
  "clientRequestId": "device-123:load-00042"
}
```

Sunucu; planın kilitli olup olmadığını, barkodun doğru `LoadUnit` ve shipment'a ait olduğunu, alıcı durağını, planlanan/kalan miktarı ve araç kapasite etkisini kontrol eder. Tekrar istek aynı idempotency sonucunu döndürür.

### 5.7 Durak teslimatı

```http
POST /api/mobile/shipments/{shipmentId}/route-stops/{routeStopId}/deliveries
Idempotency-Key: {clientRequestId}
```

Request barkod listesi, kısmi teslim bilgisi, teslim alan kişi, imza/fotoğraf/not ve gerekirse istisna nedenini taşır. Paket aktif durağa ait değilse veya başka durakta kapanmışsa işlem engellenir.

### 5.8 Kullanıcı görünüm tercihi (opsiyonel)

```http
GET /api/mobile/preferences/quantity-view
PUT /api/mobile/preferences/quantity-view
```

`PUT` yalnızca `defaultViewMode` ve cihaz tercihini değiştirir; stok, sevkiyat veya teslim hareketi üretmez.

## 6. Yetki ve güvenlik

| Endpoint grubu | Örnek permission | Ek kontrol |
|---|---|---|
| Barkod/ürün sorgu | `stock.read`, `shipment.read` | Kullanıcı depo/route bağlamı |
| Miktar preview | `stock.read` veya işlem read | Preview transaction üretmez |
| Sayım | `stock.count` | Depo/konum yetkisi |
| Transfer | `stock.transfer` | Kaynak ve hedef depo yetkisi |
| Yükleme | `shipment.load-verify` | Locked plan, assigned vehicle/load unit |
| Teslim | `shipment.deliver` | Active route stop, package owner |
| Override | `shipment.plan-override`, `stock.adjust` | Gerekçe + audit |

Sunucu `viewMode`, `quantityBase`, `display` ve `packagingSnapshot` alanlarını istemciden gelen doğruluk kaynağı olarak kabul etmez. `viewMode` response'un hangi formatta üretileceğini etkileyebilir; `quantityBase` ve snapshot backend'de yeniden üretilir.

## 7. Hata sözleşmesi

Tüm mobil endpoint'leri aynı hata yapısını kullanmalıdır:

```json
{
  "code": "PACKAGING_NOT_ALLOWED",
  "message": "Bu işlem için Koli seviyesi kullanılamaz.",
  "field": "operationPackagingId",
  "details": {
    "allowedPackagingIds": ["..."],
    "operationType": "ShipmentDelivery"
  },
  "retryable": false,
  "correlationId": "..."
}
```

| Code | Anlam | Mobil davranış |
|---|---|---|
| `BARCODE_UNKNOWN` | Barkod bulunamadı | Manuel arama; işlem formu açılmaz |
| `BARCODE_AMBIGUOUS` | Birden fazla aktif eşleşme | İşlem bloke; yönetici düzeltmesi |
| `PRODUCT_NOT_FOUND` | Ürün pasif/yok | Yeniden tara veya ara |
| `PACKAGING_NOT_FOUND` | Ambalaj seviyesi yok | Geçerli seçenekleri getir |
| `PACKAGING_NOT_ALLOWED` | İşlem seviyesi bu bağlamda yasak | Allowed list göster |
| `INVALID_QUANTITY` | Ondalık/precision/pozitiflik kuralı ihlali | Alanı düzelt |
| `INSUFFICIENT_STOCK` | Temel stok yetersiz | Kullanılabilir miktarı göster |
| `WRONG_WAREHOUSE` | Barkod başka depoda | Konum uyarısı; işlem yok |
| `PACKAGE_NOT_IN_ACTIVE_STOP` | Paket aktif durağa ait değil | Teslim butonunu açma |
| `PLAN_LOCKED` | Kilitli plan değiştirilemez | Replan/override iste |
| `DUPLICATE_SCAN` | Aynı barkod yakın zamanda işlendi | İlk sonucu göster |
| `IDEMPOTENCY_REPLAY` | Aynı client request tekrarlandı | İlk sonucu güvenle döndür |
| `CONCURRENCY_CONFLICT` | Stok/plan versiyonu değişti | Yenile ve yeniden doğrula |
| `OFFLINE_NOT_COMMITTED` | Sunucu onayı alınmadı | Kesinleşmiş gösterme; tekrar dene |

## 8. Transaction ve idempotency kuralları

Toggle değişimi transaction başlatmaz. Aşağıdaki endpoint'ler transaction başlatır ve tamamı idempotency ile korunur:

```text
Stock count item
Warehouse transfer item
Shipment load scan
Route-stop delivery
Delivery-note issue
```

Transaction içinde şu sıra izlenir:

```text
Idempotency key kontrolü
→ Kullanıcı/permission/context kontrolü
→ Barkod ve ambalaj çözümleme
→ Ambalaj katsayısını tarihsel snapshot ile hesaplama
→ quantity_base ve stok/plan miktarı kontrolü
→ Ledger/belge/paket state update
→ quantity_operation_snapshot yaz
→ Audit log
→ Idempotency sonucu yaz
→ Commit
```

## 9. Kabul kriterleri

- [ ] Toggle `viewMode` olarak response görünümünü değiştiriyor; işlem seviyesi `operationPackagingId` ile ayrı tutuluyor.
- [ ] Backend hiçbir zaman istemcinin gönderdiği `quantityBase` değerine güvenmiyor.
- [ ] `5 Koli` tüm işlem endpoint'lerinde geçerli ambalaj snapshot'ı ile `10.000 adet` hesaplanıyor.
- [ ] `Breakdown` görünümü açılmış ambalajları ve temel karşılığını doğru gösteriyor.
- [ ] Barkod ürün/ambalaj/yük birimi türüyle çözümleniyor ve işlem bağlamı doğrulanıyor.
- [ ] Aynı `Idempotency-Key` ikinci stok, transfer, yükleme veya teslim hareketi üretmiyor.
- [ ] Aktif rota durağı dışındaki paket teslim edilemiyor.
- [ ] Kilitli kargo planında miktar/ambalaj/durak değişikliği yapılamıyor.
- [ ] Offline cevap işlem kesinleşmiş gibi gösterilmiyor.
- [ ] Hata kodları mobilin düzeltme aksiyonunu belirleyebilecek ayrıntıyı taşıyor.

## References

Bu belge harici veri kullanmaz; repository içindeki canonical tasarım kararlarını API ve schema sözleşmesine dönüştürür:

- [`mobile-barcode-and-quantity-ux.md`](./mobile-barcode-and-quantity-ux.md)
- [`product-packaging-and-uom.md`](./product-packaging-and-uom.md)
- [`database-technical-architecture.md`](./database-technical-architecture.md)
- [`business-workflows.md`](./business-workflows.md)
- [`domain-model.md`](./domain-model.md)
- [`shipment-logistics-ui-design.md`](./shipment-logistics-ui-design.md)

**Hazırlayan:** Manus AI
**Tarih:** 16 Ağustos 2026
