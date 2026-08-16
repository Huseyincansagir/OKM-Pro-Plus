# Factory ERP-Lite — G4 Sales, Quote and Order Approval Evidence

**Durum:** G4 tamamlandı — G5 irsaliye/kısmi sevkiyat/fatura/cari slice’ına geçişe hazır
**Tarih:** 2026-08-16
**G3 başlangıç commit’i:** `05ba6fe`

## 1. Tamamlanan kapsam

G4; public teklif talebi, müşteri adayı, aktif müşteri, fiyat listesi/price group temeli, satış siparişi, sipariş kalemi, approval kaydı, stock reservation ve order command API yüzeyini ekledi. Public teklif talebi doğrudan active customer oluşturmaz; `quote-request.review` command’ı mevcut active customer’a bağlanmayı seçebilir.

G4 persistence tabloları:

```text
customers
customer_addresses
customer_contacts
price_lists
customer_price_groups
customer_price_group_members
product_prices
quote_requests
quote_request_items
stock_reservations
sales_orders
sales_order_items
sales_order_approvals
```

Sipariş kalemlerinde kullanıcının girdiği ambalaj miktarı, server-side hesaplanan `ordered_qty`, packaging snapshot, `reserved_qty`, `shipped_qty`, `cancelled_qty`, `remaining_qty`, partial-delivery flag ve price snapshot birlikte tutulur. `remaining_qty` PostgreSQL check constraint ile `ordered_qty - shipped_qty - cancelled_qty` projection’ına bağlanmıştır.

## 2. API yüzeyi

| Method | Path | Permission / davranış |
|---|---|---|
| POST | `/api/v1/public/quote-requests` | Public teklif talebi; minimum firma/iletişim/consent + ürün miktarı |
| GET | `/api/v1/quote-requests` | `quote-request.read` |
| GET | `/api/v1/quote-requests/{id}` | `quote-request.read` |
| POST | `/api/v1/quote-requests/{id}/review` | `quote-request.review`, customer link ve audit |
| POST | `/api/v1/orders` | `order.create`, Draft oluşturur |
| GET | `/api/v1/orders/{id}` | `order.read` |
| POST | `/api/v1/orders/{id}/submit` | `order.submit`, PendingApproval |
| POST | `/api/v1/orders/{id}/approve` | `order.approve`, reservation + Approved |
| POST | `/api/v1/orders/{id}/reject` | `order.reject`, gerekçe + Cancelled |

Controller’lar state mutation yapmaz; tüm state/quantity/reservation davranışı Infrastructure service içindeki Application contract arkasından çalışır.

## 3. Approval transaction davranışı

Sipariş approval sırasında aktif depo seçilir ve her sipariş kalemi için stok satırı `SELECT ... FOR UPDATE` ile kilitlenir. Güncel `on_hand - reserved` tekrar hesaplanır; stok yetersizse transaction hard error ile rollback olur. Uygun kalemde `stock_reservations` kaydı açılır, `stocks.reserved_qty_base` artırılır, `sales_order_items.reserved_qty` güncellenir, approval kaydı ve audit aynı transaction içinde yazılır. Fatura veya cari hareket G4 approval aşamasında oluşturulmaz.

> **MVP invariant:** Onaylanmamış sipariş rezervasyon oluşturamaz; rezervasyon stok ledger’ının temel birimindeki kullanılabilir miktarı aşamaz.

Order command’leri aşağıdaki idempotency davranışını uygular:

```text
Idempotency-Key + actor + endpoint scope + payload hash
→ aynı key / aynı payload: ilk response replay
→ aynı key / farklı payload: IDEMPOTENCY_PAYLOAD_MISMATCH
→ ilk commit ile response kaydı aynı transaction
```

## 4. Seed ve smoke kanıtı

Controlled Migrator G4 ile bir demo aktif müşteri, default TRY price list, standard customer price group ve Premium Peçete için Paket/Koli price records oluşturur. Yeni order/quote numaraları `document_sequences` satırını transaction içinde kilitleyerek üretilir.

| Kontrol | Sonuç |
|---|---|
| Release solution build | 0 warning / 0 error |
| Domain unit tests | 28 passed |
| Architecture tests | 5 passed |
| Infrastructure model/security/catalog/sales tests | 16 passed |
| G4 migration | Isolated PostgreSQL’de başarılı |
| Public quote request | 201; `2 Koli = 4.000 adet` snapshot |
| Internal quote list | 200 |
| Quote review + customer link | 200; `InReview` |
| Sales order create | 201; `5 Koli = 10.000 adet` |
| Same create Idempotency-Key replay | Aynı order/item response tekrarlandı |
| Order submit | 200; `PendingApproval` |
| Order approve | 200; `Approved`, `reservedQty=10.000` |
| Same approve Idempotency-Key replay | İkinci reservation oluşturulmadan aynı response döndü |
| PostgreSQL stock reservation | `stocks.reserved_qty_base=10.000` |

FluentAssertions testlerinde Xceed lisans bilgilendirme mesajı görülmektedir; bu test hatası değildir ve ticari kullanım öncesinde paket lisansı değerlendirilmelidir.

## 5. Bilinçli sınırlar

G4’te fiyat master data ve order item price snapshot temeli hazırdır; kapsamlı quotation/price approval, customer CRUD ekranları, risk policy, multi-warehouse allocation, cancellation/release command ve gerçek frontend form akışları sonraki slice’larda tamamlanacaktır. Sipariş rejection domain baseline’ına uygun biçimde `Cancelled` projection’ı kullanır; approval kararı ayrı `sales_order_approvals` tablosunda korunur.

## 6. G5 handoff

G5; `DeliveryNote`, `DeliveryNoteItem`, `DeliveryNoteItemAllocation`, shipment preparation ve O-002 kısmi sevkiyat transaction’ını kuracaktır. İrsaliye issue sırasında kaynak `SalesOrderItem` ve `StockReservation` kilitlenmeli, kalan miktar yeniden okunmalı, `quantity_base` upper bound doğrulanmalı, stock movement append-only yazılmalı ve reservation consume/release aynı transaction’da yapılmalıdır. Ardından G5’in ikinci yarısında `Invoice`, `InvoiceItem`, `InvoiceItemAllocation`, current-account debit ve O-003 kısmi fatura sınırı uygulanacaktır.
