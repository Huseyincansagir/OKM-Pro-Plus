# L2 İrsaliye–Sevkiyat Entegrasyonu API Sözleşmesi

**Tarih:** 2026-08-16
**Durum:** Kodlama öncesi API contract/design
**Kapsam:** `Issued DeliveryNote` kaynağından operasyonel `Shipment` oluşturma ve izleme
**Kapsam dışı:** Araç/şoför ataması, rota/durak yönetimi, LoadPlan, mixed pallet, barkodlu yükleme ve teslim proof

## 1. Amaç ve bounded slice sınırı

L2’nin amacı, stok ve sipariş etkisi tamamlanmış bir irsaliyeyi operasyonel sevkiyat kaydına bağlamaktır. İrsaliye kesinleştirme stok hareketini ve rezervasyon tüketimini üretir; shipment oluşturma ise bu sonucu fiziksel sevkiyat bağlamına taşır. **Shipment oluşturmak ikinci kez stok çıkışı, rezervasyon tüketimi, invoice allocation veya cari hareket üretmez.** Bu ayrım, canonical belge zincirindeki `SalesOrder → DeliveryNote → Shipment → Invoice` sırasını korur [1] [2].

L2’de bir shipment’ın kaynağı **tek bir Issued delivery note** olarak sınırlandırılır. Delivery note’ın tüm issued kalemleri shipment item olarak kopyalanır; kullanıcı L2’de shipment oluştururken yeni quantity girmez. Bir irsaliyenin farklı araçlara veya fiziksel yük birimlerine bölünmesi, çoklu delivery note’ların tek shipment’ta birleştirilmesi ve shipment split işlemi daha sonraki planlama slice’larına bırakılır.

> `DeliveryNote.Issued` ticari/stok kesinleşmesidir; `Shipment` operasyonel taşıma kaydıdır. İki state aynı entity veya aynı transaction sonucu değildir.

## 2. Mevcut implementation ile target contract arasındaki fark

Repository’de mevcut delivery-note API’si şu anda create, get ve issue endpoint’lerini expose etmektedir. `Issue` controller çağrısı body almadan actor, idempotency key ve correlation ID ile service’e gider [3] [4]. Current shipping service issue transaction’ında order item, reservation ve stock satırlarını kilitler; stock düşer, reservation tüketir, allocation ve movement yazar, audit ve idempotency sonucu kaydeder [5].

L2 implementation’ı başlamadan önce aşağıdaki contract farkları kapatılmalıdır:

| Alan | Mevcut durum | L2 target |
|---|---|---|
| Delivery validation | Controller/service yüzeyinde yok | `POST /delivery-notes/{id}/validate` ile validation sonucu |
| Issue body | Body yok | `confirmation` ve `expectedRowVersion` içeren explicit command body |
| ETag/If-Match | Middleware/controller’da henüz yok | Kritik issue/create/ready command’lerinde zorunlu |
| Shipment API | Repository’de yok | Create/list/detail/ready endpoint’leri |
| Shipment persistence | Henüz yok | `shipments` + `shipment_items`, duplicate source koruması |
| ProblemDetails | DomainException çoğunlukla 422’a mapleniyor | State/version/idempotency conflict’leri typed 409/422 ayrımıyla |
| Shipment link | Issue ve shipment ayrı | Issue başarılı olduktan sonra ayrı idempotent create command |

Bu doküman target contract’tır; implementation sırasında mevcut endpoint’lerle geriye dönük uyumluluk için geçiş adapter’ı gerekebilir.

## 3. Resource ve state sözleşmesi

### 3.1 DeliveryNote

L2’nin kaynak delivery note state’i `Issued` olmalıdır. `Draft`, `Prepared`, `ReadyToIssue` veya `Reversed` bir delivery note shipment kaynağı olamaz. `Issued` state’ine geçiş `delivery-note.issue` command’inin transaction’ı ile yapılır.

```text
Draft → Prepared → ReadyToIssue → Issued
                                  └→ Reversed (reversal command)
```

### 3.2 Shipment

L2 shipment state makinesi:

```text
Preparing → Ready
```

`Loaded`, `InTransit`, `PartiallyDelivered`, `Delivered`, `Exception` ve `Returned` state’leri L3–L5’te route/load/package command’leriyle kullanılacaktır. L2’de shipment oluşturulunca `Preparing` olur. `Ready`, kaynağın bütünlüğü doğrulanmış ve route/load planning’e devre hazır durumdur; L2’de route veya araç ataması zorunlu değildir.

| State | Türkçe gösterim | L2 anlamı |
|---|---|---|
| `Preparing` | Hazırlanıyor | Issued delivery note’tan shipment oluşturuldu; operasyonel hazırlık devam ediyor |
| `Ready` | Hazır | Shipment item/source bütünlüğü doğrulandı; sonraki rota/yük planı slice’ına devredilebilir |

State alanı PATCH ile değiştirilemez. Her geçiş command endpoint’i ile yapılır.

## 4. HTTP ortak sözleşmesi

### 4.1 Base URL ve headers

```text
/api/v1
```

Authenticated internal API command’leri aşağıdaki header’ları kullanır:

| Header | Create/Read | Issue/Validate/Ready |
|---|---|---|
| `Authorization: Bearer <JWT>` | Zorunlu | Zorunlu |
| `Idempotency-Key` | POST’larda zorunlu | Zorunlu |
| `X-Correlation-Id` | İstemci verebilir; yoksa server üretir | İstemci verebilir; yoksa server üretir |
| `If-Match` | Shipment create için kaynak delivery note version’ı önerilir/zorunlu | Issue ve Ready için zorunlu |
| `Content-Type` | `application/json` | `application/json` |

`If-Match` değeri public `row_version` için ETag formatındadır; Npgsql `xmin` public contract değildir. Örnek:

```http
If-Match: "17"
X-Correlation-Id: corr-01J...
Idempotency-Key: dn-shipment-create-01J...
```

`Idempotency-Key` aynı actor, endpoint scope ve aynı payload için tekrar gönderildiğinde ilk committed response aynen döndürülür. Aynı key farklı payload veya farklı kaynak version ile gönderilirse yeni transaction başlamadan `IDEMPOTENCY_PAYLOAD_MISMATCH` döner [6].

### 4.2 Başarılı response standardı

Mutation response’ları current resource version’ı ve ETag’i döndürür:

```http
ETag: "18"
X-Correlation-Id: corr-01J...
Content-Type: application/json
```

İlk başarılı create `201 Created` döndürür. Aynı idempotency key’in replay’i, ilk response status/body/header semantiğini korur; yeni shipment veya yeni side effect oluşturmaz.

## 5. DeliveryNote endpoint’leri

### 5.1 Delivery note draft oluşturma

```http
POST /api/v1/orders/{orderId}/delivery-notes
```

**Permission:** `delivery-note.create`
**Idempotency:** Zorunlu
**Stok etkisi:** Yok
**Başarı:** `201 Created`

Bu endpoint, onaylanmış siparişin kalan miktarından bir delivery note draft’ı oluşturur. Kullanıcı entered quantity, entered packaging ve view mode gönderir; backend packaging katsayısını hesaplar ve `quantityBase` ile packaging snapshot’ı draft’a kaydeder.

#### Request

```json
{
  "salesOrderId": "11111111-1111-1111-1111-111111111111",
  "items": [
    {
      "salesOrderItemId": "22222222-2222-2222-2222-222222222222",
      "enteredQuantity": 5,
      "enteredPackagingId": "33333333-3333-3333-3333-333333333333",
      "viewMode": "Packaging"
    }
  ]
}
```

#### Response — `201 Created`

```json
{
  "id": "44444444-4444-4444-4444-444444444444",
  "documentNumber": "DN-2026-000031",
  "salesOrderId": "11111111-1111-1111-1111-111111111111",
  "customerId": "55555555-5555-5555-5555-555555555555",
  "status": "Draft",
  "items": [
    {
      "id": "66666666-6666-6666-6666-666666666666",
      "salesOrderItemId": "22222222-2222-2222-2222-222222222222",
      "productId": "77777777-7777-7777-7777-777777777777",
      "enteredQuantity": 5,
      "enteredPackagingId": "33333333-3333-3333-3333-333333333333",
      "quantityBase": 10000,
      "packagingSnapshot": {
        "level": "Case",
        "name": "Koli",
        "quantityInBaseUom": 2000,
        "allowPartial": false,
        "effectiveVersion": 1
      },
      "shippedQtyBase": 0,
      "remainingToInvoiceBase": 0
    }
  ],
  "rowVersion": 17
}
```

`quantityBase`, `packagingSnapshot` ve display text istemci doğruluk kaynağı değildir. Client `quantityBase` gönderse bile server sonucu yeniden hesaplar; kesinleştirme request’inde gönderilen base miktar server sonucu ile farklıysa `QUANTITY_BASE_MISMATCH` uygulanır [7].

### 5.2 Delivery note validate

```http
POST /api/v1/delivery-notes/{deliveryNoteId}/validate
```

**Permission:** `delivery-note.validate`
**Idempotency:** Zorunlu
**Stok etkisi:** Yok
**Başarı:** `200 OK`

Validation komutu draft/prepare state’ini kontrol eder; sipariş onayı, kalan miktar, reservation, quantity precision, packaging snapshot, product/customer bağlantısı ve stok kullanılabilirliği kontrol edilir. Bu endpoint stok movement veya allocation yazmaz. Validation sonucu `Prepared`, `ReadyToIssue` veya correction gerektiren hata özetini response’ta verir; kesin kaynak etkisi yalnızca issue endpoint’inde oluşur.

#### Response

```json
{
  "deliveryNoteId": "44444444-4444-4444-4444-444444444444",
  "validationStatus": "ReadyToIssue",
  "canIssue": true,
  "items": [
    {
      "deliveryNoteItemId": "66666666-6666-6666-6666-666666666666",
      "quantityBase": 10000,
      "reservedRemainingBase": 10000,
      "availableStockBase": 12000,
      "errors": [],
      "warnings": []
    }
  ],
  "rowVersion": 18
}
```

### 5.3 Delivery note issue

```http
POST /api/v1/delivery-notes/{deliveryNoteId}/issue
```

**Permission:** `delivery-note.issue`
**Idempotency:** Zorunlu
**If-Match:** Zorunlu
**Başarı:** `200 OK`

Issue request’i yeni quantity taşımaz. Draft üzerinde daha önce kaydedilmiş item miktarları kesinleştirilir; böylece issue sırasında client’ın ikinci bir miktar kaynağı oluşturması engellenir. Mevcut controller’ın body almayan davranışı için geçişte body opsiyonel kabul edilebilir; canonical target’ta explicit confirmation ve expected version kullanılması önerilir.

#### Request

```json
{
  "confirmation": true,
  "expectedRowVersion": 18
}
```

#### Server transaction sırası

```text
validation
→ authorization
→ idempotency replay/mismatch lookup
→ transaction begin
→ delivery note row lock + If-Match re-read
→ SalesOrderItem lock
→ StockReservation lock
→ Stock row lock
→ server-side quantity/remaining/available re-check
→ DeliveryNoteItemAllocation
→ StockMovement(SalesShipment)
→ reservation consume/release
→ SalesOrderItem shipped/remaining projection
→ DeliveryNote status = Issued
→ audit + outbox
→ SaveChanges
→ idempotency result
→ commit
```

İşlem başarısız olduğunda delivery note, allocation, stock movement, reservation ve order projection birlikte rollback olur. Bu transaction shipment oluşturmaz; shipment ayrı endpoint ile issue edilmiş delivery note’tan oluşturulur.

#### Response — `200 OK`

```json
{
  "deliveryNoteId": "44444444-4444-4444-4444-444444444444",
  "status": "Issued",
  "salesOrderStatus": "PartiallyShipped",
  "issuedItems": [
    {
      "deliveryNoteItemId": "66666666-6666-6666-6666-666666666666",
      "issuedQuantityBase": 10000,
      "salesOrderRemainingQtyBase": 10000,
      "stockMovementId": "88888888-8888-8888-8888-888888888888",
      "allocationId": "99999999-9999-9999-9999-999999999999"
    }
  ],
  "auditId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "rowVersion": 19
}
```

Aynı issue key replay edildiğinde ikinci `StockMovement` veya allocation oluşmaz. Farklı key ile zaten `Issued` kaynak tekrar issue edilmeye çalışılırsa `DELIVERY_NOTE_ALREADY_ISSUED` veya typed `RESOURCE_STATE_CONFLICT` döner; response yeni stok hareketi üretmez.

## 6. Shipment endpoint’leri

### 6.1 Shipment oluşturma

```http
POST /api/v1/shipments
```

**Permission:** `shipment.create`
**Idempotency:** Zorunlu
**If-Match:** Kaynak delivery note version’ı için zorunlu
**Başarı:** `201 Created`

#### Request

L2’de request quantity içermez. `deliveryNoteId` tek ve zorunlu source’tur.

```json
{
  "deliveryNoteId": "44444444-4444-4444-4444-444444444444",
  "expectedDeliveryNoteRowVersion": 19,
  "confirmation": true
}
```

`confirmation=false` veya eksik source reddedilir. `shipmentItem` listesi client’tan kabul edilmez; server `DeliveryNoteItem` kayıtlarını source lock altında okur ve her issued item için shipment item üretir.

#### Guard’lar

1. Delivery note bulunmalıdır.
2. Delivery note status’u `Issued` olmalıdır.
3. Delivery note item’larının tümü aynı delivery note’a ait olmalıdır.
4. `quantityBase` shipment’a kopyalanırken `DeliveryNoteItem.ShippedQty` source alınmalıdır.
5. `ShippedQty > 0` olmayan item shipment’a eklenmemelidir.
6. Customer, product ve packaging FK bağlantıları geçerli olmalıdır.
7. Aynı delivery note daha önce shipment’a bağlanmamış olmalıdır.
8. L2’de shipment item quantity client’tan artırılamaz, azaltılamaz veya yeniden packaging’e çevrilemez.
9. Source row version `If-Match` ile eşleşmelidir.
10. Aynı idempotency key farklı delivery note veya confirmation payload’ı ile kullanılırsa mismatch dönmelidir.

#### Response — `201 Created`

```json
{
  "id": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  "status": "Preparing",
  "sourceDeliveryNote": {
    "id": "44444444-4444-4444-4444-444444444444",
    "documentNumber": "DN-2026-000031",
    "status": "Issued",
    "rowVersion": 19
  },
  "customerId": "55555555-5555-5555-5555-555555555555",
  "items": [
    {
      "id": "cccccccc-cccc-cccc-cccc-cccccccccccc",
      "deliveryNoteItemId": "66666666-6666-6666-6666-666666666666",
      "productId": "77777777-7777-7777-7777-777777777777",
      "quantityBase": 10000,
      "packagingSnapshot": {
        "level": "Case",
        "name": "Koli",
        "quantityInBaseUom": 2000,
        "enteredQuantity": 5
      }
    }
  ],
  "routePlanId": null,
  "loadPlanId": null,
  "createdAt": "2026-08-16T10:30:00Z",
  "rowVersion": 1
}
```

Bu command’ın database etkisi yalnızca shipment header, shipment items, audit/outbox ve idempotency result’tır. `stocks`, `stock_movements`, `stock_reservations`, `sales_order_items`, `delivery_note_items.shipped_qty` ve current account değişmez.

### 6.2 Shipment listeleme

```http
GET /api/v1/shipments?status=Preparing&deliveryNoteId={id}&page=1&pageSize=50
```

**Permission:** `shipment.read`
**Transaction:** Query-only, `AsNoTracking`, server-side pagination
**Response:** `200 OK`

```json
{
  "items": [
    {
      "id": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      "status": "Preparing",
      "deliveryNoteNumber": "DN-2026-000031",
      "customerId": "55555555-5555-5555-5555-555555555555",
      "itemCount": 1,
      "totalQuantityBase": 10000,
      "routePlanId": null,
      "loadPlanId": null,
      "rowVersion": 1,
      "createdAt": "2026-08-16T10:30:00Z"
    }
  ],
  "page": 1,
  "pageSize": 50,
  "totalCount": 1
}
```

L2 list response’unda iç stock ledger, reservation, allocation veya başka müşteriye ait customer address alanları expose edilmez.

### 6.3 Shipment detay

```http
GET /api/v1/shipments/{shipmentId}
```

**Permission:** `shipment.read`
**Response:** `200 OK`

Detail response source delivery note, shipment items, temel miktar ve snapshot alanlarını döner. L2’de `route`, `vehicle`, `driver`, `loadPlan`, `packages` alanları `null` veya boş collection olarak contract’ta bulunabilir; bu alanlar L3–L5’te genişletilir. Resource bulunamazsa `404 SHIPMENT_NOT_FOUND` döner.

### 6.4 Shipment ready

```http
POST /api/v1/shipments/{shipmentId}/ready
```

**Permission:** `shipment.plan-manage`
**Idempotency:** Zorunlu
**If-Match:** Zorunlu
**Başarı:** `200 OK`

Bu command shipment’ın bütün item/source bağlantılarını yeniden doğrular ve `Preparing → Ready` geçişini yapar. L2’de araç, şoför veya route zorunlu değildir; `Ready` shipment’ın L3 route/load planning’e devredilebileceğini gösterir. Source delivery note `Reversed` olmuşsa veya shipment item kaynağı silinmiş/geçersiz hale gelmişse geçiş reddedilir.

```json
{
  "confirmation": true,
  "expectedRowVersion": 1
}
```

Response, `status = Ready`, yeni `rowVersion` ve `ETag` döndürür.

## 7. Shipment–delivery source bağlantısı ve persistence gereksinimi

Canonical SQL taslağında `shipment_items(shipment_id, delivery_note_item_id, quantity_base)` bulunur ve `(shipment_id, delivery_note_item_id)` unique’tir [8]. Bu unique kural aynı shipment içindeki duplicate item’ı engeller; ancak aynı delivery note item’ın iki farklı shipment’a eklenmesini tek başına engellemez.

L2’nin “bir Issued delivery note → en fazla bir shipment” kararı için persistence seviyesinde ek bir source uniqueness gerekir. İki seçenek vardır:

| Seçenek | Değerlendirme |
|---|---|
| `shipments.source_delivery_note_id` + unique | L2 için basit; ileride multi-delivery shipment’a migration gerekir |
| `shipment_delivery_notes(shipment_id, delivery_note_id)` bridge + `UNIQUE(delivery_note_id)` | Kaynak bağlantısını açıkça saklar ve ileride multi-source shipment’a genişlemeye izin verir |

**Öneri:** L2’de bridge tabloyu kullanmak; `shipment_delivery_notes.delivery_note_id` üzerinde unique constraint uygulamak ve shipment detail’de source delivery note’ı doğrudan göstermek. Bu, `shipment_items` üzerinden implicit source keşfine bağımlılığı ortadan kaldırır. Bridge migration’ı uygulanmadan duplicate shipment test gate’i tamamlanmış sayılmamalıdır.

## 8. Idempotency sözleşmesi

| Command | Idempotency scope | Payload hash girdileri | Replay sonucu |
|---|---|---|---|
| Create delivery note | `delivery-note:create:{actorId}` | order, item inputs, packaging, view mode | İlk `201` body/status |
| Validate delivery note | `delivery-note:validate:{deliveryNoteId}:{actorId}` | source id, expected version | İlk validation response |
| Issue delivery note | `delivery-note:issue:{deliveryNoteId}:{actorId}` | source id, confirmation, expected version | İlk `200`; ikinci stock effect yok |
| Create shipment | `shipment:create:{actorId}` | delivery note id, expected DN version, confirmation | İlk `201`; duplicate shipment yok |
| Ready shipment | `shipment:ready:{shipmentId}:{actorId}` | shipment id, expected version, confirmation | İlk `200`; duplicate audit/state effect yok |

Idempotency lookup transaction başlamadan yapılır. Scope, actor, key ve payload hash birlikte değerlendirilir. Aynı key ile farklı actor’dan gelen request farklı scope olarak değerlendirilse bile kaynak authorization ve ownership tekrar çalışır; key gizli bir authorization bypass değildir.

## 9. Concurrency ve ETag davranışı

### 9.1 Issue yarışması

İki kullanıcı aynı delivery note’ı issue etmeye çalıştığında delivery note, source order item, reservation ve stock rows deterministic sırada kilitlenir. İlk transaction commit eder; ikinci transaction source state/version’ı yeniden okur ve stale `If-Match`, `DELIVERY_NOTE_ALREADY_ISSUED` veya quantity conflict döndürür. İkinci işlem ikinci stock movement yazamaz.

### 9.2 Shipment create yarışması

İki kullanıcı aynı Issued delivery note’tan shipment oluşturmayı denediğinde:

```text
idempotency lookup
→ delivery note FOR UPDATE
→ source status/version re-read
→ source uniqueness check
→ shipment + source bridge + shipment items insert
→ audit/idempotency
→ commit
```

Database unique conflict, `SHIPMENT_SOURCE_ALREADY_LINKED` veya `RESOURCE_VERSION_CONFLICT` olarak typed ProblemDetails’a çevrilir. İkinci transaction rollback olur.

### 9.3 Multi-item kaynak sırası

L2 create bütün delivery note item’larını aynı kaynakta okuduğu için item lock sırası `delivery_note_item.id ASC` olmalıdır. Gelecekte çoklu delivery note shipment desteklenirse delivery note ID ve item ID artan sırada deterministic lock alınır.

## 10. Authorization matrisi

| Endpoint | Permission | Tipik rol | Server guard |
|---|---|---|---|
| Create delivery note | `delivery-note.create` | Depo/satış | Order customer/scope ve remaining |
| Get delivery note | `delivery-note.read` | Depo/satış/muhasebe | Internal scope |
| Validate | `delivery-note.validate` | Depo sorumlusu | Draft/prepared state |
| Issue | `delivery-note.issue` | Depo sorumlusu/yönetici | Permission + stock/reservation/quantity |
| Create shipment | `shipment.create` | Sevkiyat/depo | Issued source + source uniqueness |
| List/detail shipment | `shipment.read` | Sevkiyat/depo/yönetici | Shipment/customer scope |
| Ready | `shipment.plan-manage` | Sevkiyat/depo sorumlusu | Source and item integrity |

Read-only kullanıcı create/issue/shipment-create/ready çağrılarında `403 Forbidden` alır. Anonymous kullanıcı `401 Unauthorized` alır. Başka customer veya başka shipment ID’si gönderildiğinde response farkı üzerinden IDOR/BOLA açığı oluşmaması için ownership/scope kontrolü yapılır; local single-company kurulumda bile permission testi korunur.

## 11. ProblemDetails hata sözleşmesi

Tüm hata response’ları `application/problem+json` döner. Mevcut middleware yalnızca temel `type`, `title`, `status`, `code`, `detail`, `instance`, `requestId`, `correlationId`, `retryable`, `errors` ve `actions` alanlarını üretmektedir [9]. L2 target contract’ında miktar/version conflict için `currentResource` ve `currentRemaining` alanları eklenmesi önerilir; secret, SQL, stack trace ve connection string asla dönmez.

| Code | HTTP | Retry | Anlam |
|---|---:|---|---|
| `MISSING_IDEMPOTENCY_KEY` | 400 | Hayır | Critical POST key olmadan geldi |
| `INVALID_REQUEST` | 400 | Hayır | JSON/validation/confirmation hatası |
| `DELIVERY_NOTE_NOT_FOUND` | 404 | Hayır | Kaynak irsaliye yok |
| `SHIPMENT_NOT_FOUND` | 404 | Hayır | Shipment yok |
| `DELIVERY_NOTE_NOT_ISSUED` | 409 | Hayır | Shipment kaynağı Issued değil |
| `DELIVERY_NOTE_ALREADY_ISSUED` | 409 | Hayır | Issue state transition tekrarlandı |
| `SHIPMENT_SOURCE_ALREADY_LINKED` | 409 | Hayır | Kaynak irsaliye başka shipment’a bağlı |
| `RESOURCE_VERSION_CONFLICT` | 409 | Fresh read | `If-Match` stale veya row_version değişmiş |
| `QUANTITY_CONCURRENCY_CONFLICT` | 409 | Fresh read | Kaynak miktar yarışta değişmiş |
| `OVER_ALLOCATION` | 422 | Hayır | Shipment/issue miktarı kalan üst sınırı aşar |
| `QUANTITY_BASE_MISMATCH` | 422 | Hayır | Client base miktarı server hesabıyla uyuşmuyor |
| `QUANTITY_PRECISION_EXCEEDED` | 422 | Hayır | UOM precision ihlali |
| `RESERVATION_SHIPMENT_CONFLICT` | 422 | Hayır | Açık reservation kalanından fazla issue |
| `STOCK_ISSUE_CONFLICT` | 422 | Hayır | Kullanılabilir stock yetersiz |
| `IDEMPOTENCY_PAYLOAD_MISMATCH` | 409 | Hayır | Aynı key farklı payload ile kullanıldı |
| `TRANSACTION_DEADLOCK` | 409 | Sınırlı retry | DB deadlock; command idempotent ise sınırlı jitter retry |
| `UNAUTHORIZED` | 401 | Token yenile | JWT yok/geçersiz |
| `FORBIDDEN` | 403 | Hayır | Permission yok |

#### Örnek stale version response

```json
{
  "type": "https://erp.local/problems/resource-version-conflict",
  "title": "Kayıt güncel değil",
  "status": 409,
  "code": "RESOURCE_VERSION_CONFLICT",
  "detail": "İrsaliye başka bir işlem tarafından değiştirildi. Güncel kaydı okuyup işlemi yeniden başlatın.",
  "instance": "/api/v1/shipments",
  "requestId": "req-01J...",
  "correlationId": "corr-01J...",
  "retryable": true,
  "currentResource": {
    "resourceType": "DeliveryNote",
    "resourceId": "44444444-4444-4444-4444-444444444444",
    "rowVersion": 20,
    "status": "Issued"
  },
  "errors": [],
  "actions": ["GET /api/v1/delivery-notes/44444444-4444-4444-4444-444444444444", "Yenile", "İşlemi yeniden onayla"]
}
```

## 12. Contract ve integration test matrisi

### 12.1 Delivery note API

| Test | Beklenen sonuç |
|---|---|
| Approved order’dan valid draft create | `201`, `Draft`, quantity snapshot doğru |
| Zero/negative quantity | `422`, no draft side effect |
| Client/server `quantityBase` mismatch | `422 QUANTITY_BASE_MISMATCH` |
| Issued olmayan source’u validate/issue | State’e göre typed conflict |
| Issue available/reserved stock ile | `200 Issued`, movement + allocation + reservation consume |
| Issue insufficient stock | `422 STOCK_ISSUE_CONFLICT`, full rollback |
| Same issue key replay | Same response, one movement/allocation |
| Same key different payload | `409 IDEMPOTENCY_PAYLOAD_MISMATCH` |
| Stale `If-Match` | `409 RESOURCE_VERSION_CONFLICT` |
| Two concurrent issue commands | Only one stock effect commits |

### 12.2 Shipment API

| Test | Beklenen sonuç |
|---|---|
| Issued delivery note’tan shipment create | `201 Preparing`, all issued items copied |
| Draft/prepared source’tan shipment create | `409 DELIVERY_NOTE_NOT_ISSUED` |
| Shipment request içinde quantity gönderme | Contract validation veya ignored-field rejection; server source quantity wins |
| Shipment create stock delta check | `stocks`, `stock_movements`, reservations unchanged |
| Duplicate same delivery note different keys | `409 SHIPMENT_SOURCE_ALREADY_LINKED` |
| Same shipment create key replay | Same `201`, one shipment |
| Same key different delivery note | `409 IDEMPOTENCY_PAYLOAD_MISMATCH` |
| Source delivery note changed after read | `409 RESOURCE_VERSION_CONFLICT` |
| Create then ready | `Preparing → Ready`, one audit/state effect |
| Ready invalid/reversed source | Typed state conflict, no state change |
| Read-only create | `403` |
| Anonymous create/read | `401` |
| IDOR shipment detail | Scoped `404`/`403`, no foreign data leakage |
| Concurrent shipment create | One source link, one shipment, unique conflict mapped |

### 12.3 Architecture and persistence gate

L2 tamamlanmış sayılmadan önce şu kanıtlar bulunmalıdır:

| Katman | Gate |
|---|---|
| Domain | DeliveryNote source guard, Shipment state transition, positive quantity and no duplicate source tests |
| Application | Validation → authorization → idempotency → lock/re-read → command order test |
| Persistence | `shipments`, `shipment_items`, source uniqueness, FK restrict, row_version, migration apply |
| API | OpenAPI DTO, headers, ETag, ProblemDetails, 201/200/409/422 contract tests |
| Integration | Issued DN → Shipment full flow; no duplicate stock effect; PostgreSQL concurrency |
| Security | Real `/auth/login`, full/read-only/anonymous, IDOR/BOLA |
| Documentation | State, quantity, permission, migration and test evidence |

## 13. L2 implementasyonunda özellikle yapılmaması gerekenler

L2 shipment create command’i araç seçmemeli, rota üretmemeli, load plan önermemeli, mixed pallet oluşturmamalı, package barcode üretmemeli ve teslimat kanıtı almamalıdır. Bu alanlar L3–L5 bounded slice’larına aittir. Shipment oluşturmayı `DeliveryNote.Issue` içine sessizce gömmek de önerilmez; iki aggregate’ın transaction sınırlarını büyütür ve retry/rollback davranışını belirsizleştirir.

L2’de delivery note’ın shipment’a bağlanması fiziksel yükleme anlamına gelmez. `Shipment.status = Preparing` iken stok ledger’ına yeni bir movement yazılmaz. Gerçek yükleme ancak locked load plan ve barcode verification slice’larında tanımlanmalıdır.

## References

[1]: ./domain-model.md "Domain model ve source-of-truth matrisi"
[2]: ./partial-shipment-invoicing-workflow.md "O-002/O-003 partial shipment and invoicing workflow"
[3]: ../src/FactoryErp.Api/Controllers/DeliveryNotesController.cs "Current delivery-note controller"
[4]: ../src/FactoryErp.Application/Shipping/ShippingFinanceContracts.cs "Current shipping finance contracts"
[5]: ../src/FactoryErp.Infrastructure/Shipping/DeliveryInvoiceFinanceService.cs "Current delivery-note issue transaction"
[6]: ./architecture-decision-baseline.md "ADR-004/ADR-005/ADR-006/ADR-009 API behavior"
[7]: ./architecture-api-contracts.md "Canonical API endpoint contract"
[8]: ./postgresql-18-migration-sql-specification.md "Shipment and shipment-item SQL specification"
[9]: ../src/FactoryErp.Api/Errors/ExceptionProblemDetailsMiddleware.cs "Current ProblemDetails mapping"
