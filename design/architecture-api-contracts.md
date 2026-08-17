# Factory ERP — ASP.NET Core API Contract Architecture

**Aşama:** ARCHITECTURE

**Durum:** Architecture tasarımı; production controller/handler kodu değildir.

**Baseline:** Proje sahibinin 2026-08-16 tarihinde kabul ettiği O-001–O-014 kararları.

**Teknoloji:** ASP.NET Core Web API, C#, EF Core, PostgreSQL, JWT + refresh token.

## 1. Contract hedefi

API, Web, Flutter mobil uygulaması ve kontrollü public katalog tarafından tüketilecek tek iş kuralı sınırıdır. Controller katmanı yalnızca route, authentication/authorization, request binding ve application command/query çağrısını yönetir. Miktar, stok, allocation, cari, state transition ve belge kesinleştirme kuralları controller içine yazılmaz.

```text
HTTP Request
  → Authentication
  → Authorization policy
  → DTO binding + validation
  → Application command/query
  → Domain aggregate/use-case
  → EF Core transaction
  → PostgreSQL
  → DTO / ProblemDetails response
```

API entity’leri doğrudan dışarı açmaz. Response DTO’ları projection üzerinden hazırlanır; özellikle müşteri, personel, risk, stok maliyeti, finans ve audit verileri permission’a göre filtrelenir.

## 2. Ortak HTTP sözleşmesi

| Konu | Sözleşme |
|---|---|
| Base path | `/api/v1` |
| JSON | `application/json`; property adları camelCase |
| Hata | `application/problem+json` |
| Tarih/saat | Request/response ISO-8601 UTC; UI Türkiye yerel zamanı gösterir |
| Kimlik | UUID string |
| Miktar | `quantityBase` server hesaplı temel UOM; entered/packaging snapshot birlikte taşınır |
| Para | `amount`, `taxAmount`, `totalAmount`; para birimi `currencyCode` ile birlikte |
| Sayfalama | `page`, `pageSize`, `totalCount`, `items` |
| Sıralama | Allowlist alanlar; serbest SQL order kabul edilmez |
| Correlation | `X-Correlation-Id` opsiyonel; yoksa server üretir |
| Idempotency | Kesinleştiren POST işlemlerinde `Idempotency-Key` zorunlu |
| Concurrency | `If-Match` veya body `rowVersion`; kritik kaynak satırı commit öncesi tekrar okunur |
| Version | URL major version; geriye uyumlu alan ekleme tercih edilir |

### 2.1 Standart response envelope’ları

Liste response’u:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 25,
  "totalCount": 0,
  "hasNextPage": false
}
```

Detail response’ları doğrudan resource DTO olarak dönebilir; tüm response’larda `requestId` header’dan izlenebilir. Büyük export işlemleri synchronous response yerine export job ve notification ile sonuçlandırılır.

## 3. Standart hata sözleşmesi

Tüm application, domain, validation, database concurrency ve authorization hataları güvenli ProblemDetails response’una map edilir.

```json
{
  "type": "https://erp.local/problems/quantity-concurrency-conflict",
  "title": "Miktar başka bir işlem tarafından değiştirildi",
  "status": 409,
  "code": "QUANTITY_CONCURRENCY_CONFLICT",
  "detail": "Güncel kalan miktar talebi karşılamıyor.",
  "instance": "/api/v1/delivery-notes/{id}/issue",
  "requestId": "req-...",
  "correlationId": "corr-...",
  "retryable": true,
  "errors": [],
  "actions": []
}
```

| HTTP | Genel kodlar | Kullanım |
|---:|---|---|
| 400 | `INVALID_REQUEST`, `INVALID_JSON`, `MISSING_IDEMPOTENCY_KEY` | Request biçimi veya zorunlu header hatası |
| 401 | `UNAUTHENTICATED`, `TOKEN_EXPIRED` | JWT yok/geçersiz |
| 403 | `FORBIDDEN`, `OVERRIDE_PERMISSION_REQUIRED` | Permission veya role yetersiz |
| 404 | `RESOURCE_NOT_FOUND` | Kaynak bulunamadı |
| 409 | `STATE_TRANSITION_CONFLICT`, `QUANTITY_CONCURRENCY_CONFLICT`, `IDEMPOTENCY_PAYLOAD_MISMATCH`, `DUPLICATE_DOCUMENT` | Güncel state, concurrency veya duplicate çakışması |
| 422 | `VALIDATION_ERROR`, `QUANTITY_BASE_MISMATCH`, `QUANTITY_PRECISION_EXCEEDED`, `OVER_ALLOCATION`, `BUSINESS_RULE_VIOLATION` | Request parse edilebilir fakat iş kuralını geçmiyor |
| 429 | `RATE_LIMITED` | Public veya authentication abuse kontrolü |
| 500 | `UNEXPECTED_ERROR` | Kullanıcıya iç detay sızdırmadan genel hata |
| 503 | `DEPENDENCY_UNAVAILABLE`, `DATABASE_UNAVAILABLE` | Servis veya database hazır değil |

`QUANTITY_BASE_MISMATCH` için HTTP `422`, `retryable=false`; `QUANTITY_CONCURRENCY_CONFLICT` için HTTP `409`, `retryable=true` fakat aynı payload’ın körlemesine tekrar gönderilmemesi zorunludur. Bu iki contract’ın ayrıntılı JSON örnekleri [`quantity-error-handling-and-allocation-sql.md`](./quantity-error-handling-and-allocation-sql.md) içinde tutulur.

## 4. Authentication ve session endpoint’leri

| Method | Path | Permission | Açıklama |
|---|---|---|---|
| POST | `/auth/login` | Public/internal | Access + refresh token üretir |
| POST | `/auth/refresh` | Refresh token | Refresh rotation uygular |
| POST | `/auth/logout` | Authenticated | Session/refresh token iptali |
| GET | `/auth/me` | Authenticated | Kullanıcı, şirket ve permission summary |
| GET | `/users` | `user.read` | Kullanıcı listesi |
| POST | `/users` | `user.create` | Kullanıcı oluşturma |
| PATCH | `/users/{id}` | `user.update` | Kullanıcı aktiflik/temel bilgi güncelleme |
| GET | `/roles` | `role.read` | Rol ve permission katalogu |
| PUT | `/users/{id}/roles` | `user.role-assign` | Rol atama |

Login response’u access token, refresh token yerine refresh token metadata, expiry, user summary ve permission version içerir. Refresh token’ın ham değeri server log’una yazılmaz.

## 5. Ürün, ambalaj ve quantity endpoint’leri

| Method | Path | Permission | Transaction |
|---|---|---|---|
| GET | `/products` | `product.read` veya public allowlist | Yok |
| POST | `/products` | `product.create` | Master data |
| GET | `/products/{id}` | `product.read` | Yok |
| PATCH | `/products/{id}` | `product.update` | Master data |
| GET | `/products/{id}/packagings` | `product.read` | Yok |
| POST | `/products/{id}/packagings` | `product.packaging-manage` | Master data |
| POST | `/mobile/barcodes/resolve` | `barcode.resolve` | Yok |
| GET | `/mobile/products/{id}/quantity-options` | `quantity.read` | Yok |
| POST | `/mobile/quantity-previews` | `quantity.preview` | Preview only; ledger yok |

### 5.1 Quantity preview request/response

```json
{
  "productId": "uuid",
  "enteredQuantity": 5,
  "enteredPackagingId": "uuid",
  "viewMode": "Packaging",
  "operationType": "Delivery",
  "warehouseId": "uuid"
}
```

```json
{
  "productId": "uuid",
  "baseUom": { "id": "uuid", "code": "Piece", "scale": 0 },
  "enteredQuantity": 5,
  "enteredPackaging": { "id": "uuid", "name": "Koli", "quantityInBaseUom": 2000 },
  "quantityBase": 10000,
  "displayText": "5 Koli (10.000 Adet)",
  "breakdown": [],
  "availableBaseQuantity": 18000,
  "warnings": [],
  "packagingSnapshot": {}
}
```

Preview response’u kesinleştirme garantisi değildir. Commit endpoint’i packaging version, precision, stock/reservation, allocation ve row version kontrollerini yeniden çalıştırır.

## 6. Customer, public catalog ve quote request endpoint’leri

| Method | Path | Permission | Açıklama |
|---|---|---|---|
| GET | `/public/catalog/products` | Public allowlist | Aktif ve public ürünler; stok/fiyat/risk yok |
| GET | `/public/catalog/products/{slug}` | Public allowlist | Public ürün detayı |
| POST | `/public/quote-requests` | Public rate-limited | Minimum veriyle teklif talebi |
| GET | `/quote-requests` | `quote-request.read` | İç kullanıcı talep listesi |
| GET | `/quote-requests/{id}` | `quote-request.read` | Talep ve duplicate adayları |
| POST | `/quote-requests/{id}/review` | `quote-request.review` | CustomerCandidate oluşturma/bağlama |
| POST | `/customers` | `customer.create` | Yetkili müşteri kartı açma |
| GET | `/customers` | `customer.read` | Müşteri listesi |
| GET | `/customers/{id}` | `customer.read` | Müşteri, adres ve cari summary |
| PATCH | `/customers/{id}` | `customer.update` | Master data güncelleme |

Public talep doğrudan active `Customer` oluşturmaz. `review` command’ı duplicate adayları döndürür; yeni müşteri kartı açma ayrı permission ve audit gerektirir.

## 7. PriceList ve teklif/sipariş endpoint’leri

| Method | Path | Permission | State/transaction |
|---|---|---|---|
| GET | `/price-lists` | `price.read` | Yok |
| POST | `/price-lists` | `price.manage` | Master data |
| POST | `/products/{id}/prices` | `price.manage` | Fiyat version’ı |
| GET | `/customers/{id}/price-context` | `price.resolve` | Müşteri grubu + geçerli fiyat |
| POST | `/quotes` | `quote.create` | Draft |
| POST | `/quotes/{id}/issue` | `quote.issue` | Quote issue + audit |
| POST | `/orders` | `order.create` | Draft |
| POST | `/orders/{id}/submit` | `order.submit` | PendingApproval |
| POST | `/orders/{id}/approve` | `order.approve` | Reservation + Approved |
| POST | `/orders/{id}/reject` | `order.reject` | Rejected |
| GET | `/orders` | `order.read` | Filtreli liste |
| GET | `/orders/{id}` | `order.read` | Detail + item allocations summary |
| POST | `/orders/{id}/cancel` | `order.cancel` | Reservation release/reversal policy |

### 7.1 SalesOrder DTO

```json
{
  "id": "uuid",
  "orderNumber": "SO-2026-000001",
  "customerId": "uuid",
  "status": "Approved",
  "currencyCode": "TRY",
  "priceSnapshotVersion": "2026-08-16T09:00:00Z",
  "items": [
    {
      "id": "uuid",
      "productId": "uuid",
      "orderedQuantity": {
        "enteredQuantity": 10,
        "enteredPackagingId": "uuid",
        "quantityBase": 20000,
        "baseUomCode": "Piece",
        "packagingSnapshot": {}
      },
      "reservedQtyBase": 20000,
      "shippedQtyBase": 0,
      "cancelledQtyBase": 0,
      "remainingQtyBase": 20000,
      "partialDeliveryAllowed": true,
      "rowVersion": 4
    }
  ],
  "createdAt": "2026-08-16T09:00:00Z"
}
```

## 8. DeliveryNote ve kısmi sevkiyat endpoint’leri

| Method | Path | Permission | Transaction |
|---|---|---|---|
| POST | `/orders/{orderId}/delivery-notes` | `delivery-note.create` | Draft + quantity preview |
| GET | `/delivery-notes` | `delivery-note.read` | Liste |
| GET | `/delivery-notes/{id}` | `delivery-note.read` | Detail + allocation |
| POST | `/delivery-notes/{id}/validate` | `delivery-note.validate` | Validation only |
| POST | `/delivery-notes/{id}/issue` | `delivery-note.issue` | Stock + reservation + allocation + audit |
| POST | `/delivery-notes/{id}/reverse` | `delivery-note.reverse` | Reversal + reverse stock movement |
| POST | `/delivery-notes/{id}/close-remainder` | `order.close-remainder` | Remaining closure + audit |

### 8.1 Issue delivery request

```http
POST /api/v1/delivery-notes/{id}/issue
Authorization: Bearer <token>
Idempotency-Key: dn-issue-01J...
If-Match: "17"
X-Correlation-Id: corr-01J...
```

```json
{
  "items": [
    {
      "deliveryNoteItemId": "uuid",
      "enteredQuantity": 5,
      "enteredPackagingId": "uuid",
      "quantityBase": 10000,
      "warehouseId": "uuid",
      "locationId": "uuid",
      "packagingBreakdown": []
    }
  ],
  "shipmentId": "uuid",
  "confirmation": true
}
```

Server transaction sırası `SalesOrderItem`, `StockReservation` ve `Stock` satırlarının kilitlenmesi; server-side `quantityBase` yeniden hesaplanması; remaining/available kontrolü; `DeliveryNoteItemAllocation`, `StockMovement(SalesShipment)`, reservation consume/release, projection, audit ve idempotency result yazılmasıdır. Herhangi bir adım başarısızsa tamamı rollback olur.

Başarılı response:

```json
{
  "deliveryNoteId": "uuid",
  "status": "Issued",
  "salesOrderStatus": "PartiallyShipped",
  "issuedItems": [
    {
      "deliveryNoteItemId": "uuid",
      "issuedQuantityBase": 10000,
      "salesOrderRemainingQtyBase": 10000
    }
  ],
  "stockMovementIds": ["uuid"],
  "allocationIds": ["uuid"],
  "auditId": "uuid",
  "rowVersion": 18
}
```

## 9. Shipment, vehicle-fit ve LoadPlan endpoint’leri

| Method | Path | Permission | Açıklama |
|---|---|---|---|
| POST | `/shipments` | `shipment.create` | Issued delivery note’lardan shipment |
| GET | `/shipments` | `shipment.read` | Shipment board |
| GET | `/shipments/{id}` | `shipment.read` | Paket, route, load summary |
| POST | `/shipments/{id}/vehicle-fit/evaluate` | `shipment.vehicle-fit` | Tüm adayları değerlendirir |
| GET | `/shipments/{id}/vehicle-fit/candidates` | `shipment.vehicle-fit` | Uygun/elenen adaylar |
| POST | `/shipments/{id}/load-plan/suggest` | `shipment.plan-suggest` | FFD önerisi |
| POST | `/load-plans/{id}/validate` | `shipment.load-plan` | Hard/soft validation; server-side re-evaluation |
| GET | `/load-plans/{id}/validation-results` | `shipment.read` | Validation projection |
| POST | `/load-plans/{id}/manual-changes` | `shipment.load-plan` | Before/after snapshot + audit; plan `NeedsReview` |
| POST | `/load-plans/{id}/warning-resolutions` | `shipment.load-plan` | Warning resolution; `Override` için `shipment.plan-override` |
| POST | `/load-plans/{id}/lock` | `shipment.plan-lock` | Approval, warning guard ve domain lock |
| POST | `/shipments/{id}/load-plan/replan` | `shipment.plan-replan` | Yeni version + reason |
| POST | `/shipments/{id}/route` | `shipment.route-manage` | RoutePlan/RouteStop |
| POST | `/shipments/{id}/packages/assign` | `shipment.package-assign` | Package ve stop allocation |
| POST | `/shipments/{id}/route-stops/{stopId}/deliver` | `shipment.deliver` | Delivery proof |

O-014 gereği `suggest` veya `evaluate` otomatik final atama yapmaz. Hard constraint ihlali `Infeasible`, soft warning `FeasibleWithWarnings` sonucu üretir. `lock` endpoint’i depo sorumlusu onayı ve çözülmüş/override edilmiş warning kanıtı ister.

## 10. Invoice, current account ve payment endpoint’leri

| Method | Path | Permission | Transaction |
|---|---|---|---|
| GET | `/delivery-notes/{id}/invoiceable-quantities` | `invoice.read` | Query; allocation yok |
| POST | `/invoices` | `invoice.create` | Draft + allocation preview |
| GET | `/invoices` | `invoice.read` | Liste |
| GET | `/invoices/{id}` | `invoice.read` | Detail + allocation |
| POST | `/invoices/{id}/validate` | `invoice.validate` | Source/allocation/tax validation |
| POST | `/invoices/{id}/issue` | `invoice.issue` | Invoice + allocation + current debit |
| POST | `/invoices/{id}/reverse` | `invoice.reverse` | Credit/reversal + current reversal |
| GET | `/current-accounts/{customerId}/statement` | `current-account.read` | Query |
| GET | `/current-accounts/{customerId}/risk-summary` | `risk.read` | Risk snapshot |
| POST | `/payments` | `payment.create` | Payment + credit + allocation |
| POST | `/payments/{id}/reverse` | `payment.reverse` | Reverse credit |

`invoice.issue` source delivery note’un `Issued` olmasını, yeni invoice allocation’ın `remainingToInvoice` değerini aşmamasını ve idempotency/concurrency kontrollerini zorunlu kılar. Fatura issue transaction’ı stok hareketi üretmez; yalnızca Invoice, InvoiceItem, InvoiceItemAllocation ve `CurrentTransaction(Debit)` birlikte commit edilir.

## 11. Warehouse, production ve personnel endpoint’leri

| Modül | Endpoint örnekleri | Kritik permission |
|---|---|---|
| Warehouse | `/warehouses`, `/stocks`, `/stock-counts`, `/warehouse-transfers`, `/stock-movements` | `stock.read`, `stock.count`, `stock.transfer`, `stock.adjust` |
| Production | `/production/orders`, `/production/orders/{id}/release`, `/production/orders/{id}/records`, `/production/records/{id}/complete` | `production.read`, `production.start`, `production.complete` |
| Machines | `/machines`, `/machines/{id}/downtimes` | `machine.read`, `machine.manage` |
| Personnel | `/employees`, `/attendance`, `/overtime`, `/leave-requests`, `/salary-records` | `employee.read`, `attendance.manage`, `salary.read`, `salary.export` |
| Reports | `/reports/sales`, `/reports/stock`, `/reports/production`, `/reports/current-account`, `/reports/invoices` | Report-specific read/export permission |
| System | `/system/health`, `/system/document-sequences`, `/system/settings` | `system.read`, `system.manage` |

O-004 nedeniyle production completion MVP’de finished-good receipt üretir; hammadde `StockMovement(OUT)` veya `ProductionMaterial` zorunlu değildir. O-008 nedeniyle salary response’ları role/field masking ve export audit’i ile korunur.

## 12. Authorization policy matrisi

| Policy | Örnek izin | Kapsam |
|---|---|---|
| Read | `order.read`, `stock.read`, `invoice.read` | DTO alan filtreleme + şirket/depo scope |
| Create | `order.create`, `delivery-note.create`, `invoice.create` | Draft oluşturma |
| Validate | `delivery-note.validate`, `invoice.validate` | İş kuralları doğrulama |
| Approve/Issue | `order.approve`, `delivery-note.issue`, `invoice.issue` | State ve ledger etkisi |
| Override | `risk.override`, `shipment.plan-override`, `price.override` | Gerekçe + audit zorunlu |
| Reverse | `delivery-note.reverse`, `invoice.reverse`, `payment.reverse` | Kaynak referansı + ters kayıt |
| Export | `salary.export`, `report.export` | Alan masking + audit + download record |
| Public | `public.catalog.read`, `public.quote-request.create` | Allowlist; internal DTO yok |

Authorization yalnızca UI button görünürlüğü değildir. Her command handler backend policy ve aggregate state guard uygulamalıdır.

## 13. State transition command modeli

State alanı PATCH ile serbestçe değiştirilemez. Her kritik geçiş kendine ait command endpoint’i kullanır.

| Command | Kaynak state | Başarı state’i | Ana guard |
|---|---|---|---|
| `SubmitOrder` | Draft | PendingApproval | Customer/items/price valid |
| `ApproveOrder` | PendingApproval | Approved | Permission + risk policy |
| `IssueDeliveryNote` | Prepared/ReadyToIssue | Issued | Quantity, stock, reservation, idempotency |
| `IssuePartialShipment` | Approved/Preparing | PartiallyShipped | `0 < quantity ≤ remaining` |
| `IssueFinalShipment` | Preparing | Fulfilled | Remaining quantity zero |
| `IssueInvoice` | ReadyToIssue | Issued | Issued delivery + invoiceable quantity |
| `ApplyPayment` | Invoice Issued | PartiallyPaid/Paid | Open balance + payment idempotency |
| `LockLoadPlan` | Valid/NeedsReview | Locked | Hard errors zero, warning resolution/override |
| `CompleteProduction` | InProgress | Completed | Finished-good quantity + audit |

Command handler, aggregate state guard, ledger effect ve audit aynı application transaction sınırında değerlendirilir.

## 14. OpenAPI ve acceptance çıktıları

Architecture aşamasında bu contract’tan aşağıdaki çıktılar üretilecektir:

1. OpenAPI 3.1 document.
2. DTO schema ve ProblemDetails error catalogue.
3. Permission-to-endpoint matrix.
4. Idempotency/concurrency test matrix.
5. State transition contract testleri.
6. Web/mobile/public client kullanım örnekleri.
7. EF Core aggregate mapping ve migration referansları.

Bu belge endpoint yönünü ve uygulama sözleşmesini belirler; controller, handler, EF Core implementation veya production deployment kodu değildir.


## 15. Accepted Architecture ADR overlay

The API contract now consumes ADR-001–ADR-011. Quantity input endpoints accept entered quantity and operation packaging, but the server recalculates `quantityBase`; mismatches return `422 QUANTITY_BASE_MISMATCH`. State-changing commands require `Idempotency-Key`, correlation ID and, where a resource version is exposed, `If-Match`/ETag.

`IssueDeliveryNote`, `IssueInvoice`, `ApplyPayment`, `CompleteProduction` and `LockLoadPlan` execute validation, authorization, idempotency lookup, source-row re-read/lock and business effects inside one application transaction. The API never calls SMTP, e-document, external HTTP or push providers inside that transaction. It writes an outbox record and returns the committed command result.

`DbUpdateConcurrencyException`, PostgreSQL deadlock/serialization failures and allocation constraint violations are mapped to the typed ProblemDetails codes in `architecture-decision-baseline.md`. A retryable conflict requires a fresh read; the API does not blindly replay a command with a stale version or changed payload.

Successful mutation responses include the current resource version/ETag where applicable. Public endpoints never expose internal row versions, stock ledger details, current-account data, salary data or allocation internals.
