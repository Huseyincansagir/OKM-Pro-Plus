---
name: factory-erp-architecture
description: Fabrika ERP sisteminin domain modelini, PostgreSQL veritabanını, API sözleşmelerini, transaction sınırlarını, RBAC yapısını, event/notification modelini ve deployment mimarisini tasarlamak için kullan.
---

# Factory ERP Architecture

## Amaç

Tasarım çıktısını üretime hazır teknik mimariye dönüştür.

## Design kararlarını tüketme kuralı

`/design/decision-log.md` ve `/design/open-decisions-solution-matrix.md` birlikte okunmalıdır. Solution matrix içindeki öneri, owner/date/evidence bulunmadan `DECIDED` kabul edilemez. Mevcut baseline’da O-001–O-014 proje sahibi tarafından 2026-08-16 tarihinde kabul edilmiş ve `decision-log.md` içinde `DECIDED` olarak kayıtlıdır; Architecture skill’i bu seçilmiş değerleri zorunlu teknik girdiler olarak tüketebilir. Yeni veya değişen kararlar yine owner/date/evidence olmadan zorunlu şema, migration veya API kuralına dönüştürülmemelidir.

Architecture başlamadan önce kararın `/design/domain-model.md`, `/design/business-workflows.md`, `/design/database-technical-architecture.md`, `/design/master-screen-inventory.md` ve ilgili skill-impact review'a yayıldığı doğrulanmalıdır.

## Karar netleştirme ve Design Gate

- `/design/decision-clarification-backlog.md` dosyasını mevcut kararların alt soru/kapanış kanıtı ve artefact yayılım kontrol listesi olarak kullan.
- O-001–O-014 için karar blokajı kaldırılmıştır; Architecture çıktılarında `decision-log.md` içindeki kabul edilen değerleri aynen uygula.
- O-007, O-009, O-010 ve O-014 için seçilmiş override/failure davranışı, audit olayı ve kabul testini Architecture artefact’larına taşı.
- Bir kararın kapanış kanıtı; seçilen değer, owner, tarih, gerekçe, etkilenen artefact'lar ve migration/API/state kısıtlarını birlikte içermelidir; mevcut karar kayıtlarında bunları traceable tut.
- Yeni bir karar değişikliği veya kapsam genişlemesi görülürse ilgili O-ID’yi yeniden `OPEN DECISION` yap, Design Gate’i tekrar değerlendir ve seçilmemiş değeri zorunlu teknik kurala dönüştürme.

## Mimari yaklaşım

Varsayılan mimari:

`Modular Monolith + REST API + PostgreSQL + Web + Mobile`

Gereksiz microservice, Kafka veya Kubernetes ekleme. Gerçek ihtiyaç oluşana kadar modüler monolith kullan.

## Domain sınırları

En az şu bounded context'leri ayır:

- Identity & Access
- Products
- Customers
- Sales
- Warehouse
- Shipping
- Invoicing
- Current Accounts
- Payments
- Production
- Machines
- Employees
- Attendance/Leave
- Reporting
- Notifications
- Audit
- File Storage

## Database

PostgreSQL kullan.

Temel entity grupları:

- users, roles, permissions, role_permissions, user_roles, audit_logs
- products, product_categories, product_barcodes, product_images, product_prices
- customers, customer_addresses, customer_contacts
- warehouses, warehouse_locations, stocks, stock_movements, stock_reservations
- quote_requests, quote_request_items, quotes, quote_items
- sales_orders, sales_order_items, sales_order_approvals
- delivery_notes, delivery_note_items
- shipments, shipment_items, vehicle_types, vehicles, vehicle_capacities, drivers
- route_plans, route_stops, shipment_packages
- pallet_types, load_plans, load_units, load_unit_items
- invoices, invoice_items
- current_accounts, current_transactions, payments, payment_methods
- production_orders, production_order_items, production_records, production_personnel, machines, machine_downtimes
- employees, departments, attendance, overtime, leave_requests, leave_types, salary_records
- notifications, notification_recipients, files

## Veri tasarım kuralları

- PK, FK, unique constraints ve gerekli index'leri açıkça tasarla.
- Audit gerektiren entity'lerde CreatedAt/UpdatedAt/CreatedBy/UpdatedBy kullan.
- Kritik finansal ve stok tablolarında fiziksel delete kullanma.
- Belge numaraları transaction-safe oluştur.
- Tarih/saat database'de UTC; UI Türkiye lokal zamanı.
- Büyük tablolarda server-side pagination/filtering planla.
- N+1 sorgularına karşı projection ve uygun eager loading kullan.
- Kısmi sevkiyat veya fatura kararı seçilmişse ordered/shipped/invoiced/remaining miktarlarını kalem seviyesinde modelle; allocation toplamının sevk edilen/faturalanmamış miktarı aşmasını engelle.
- `PriceList` / `CustomerPriceGroup` yalnızca karar logunda seçilmişse zorunlu schema kapsamına al; seçilmemişse karar olarak kaydet.
- Ambalaj dönüşümünü `ProductPackaging` altında tut; stok ve finans ledger'ını `quantity_base` ile koru; kullanıcı görünümünü toggle/filter ile değiştir ama ledger değerini değiştirme.
- Ürün/ambalaj fiziksel profilinde boyut, ağırlık, hacim, dara, istifleme ve kapasite kurallarını tanımla; `LoadPlan` ağırlık, hacim, palet, ölçü ve alıcı durakları açısından doğrulansın.
- `VehicleType` kapasite şablonudur; `Vehicle` gerçek plaka ve anlık durumdur; `RoutePlan`/`RouteStop` rota ve teslimat bağlamını, `ShipmentPackage` barkodlanabilir alıcı yükünü taşır.
- Araç, sevkiyat, rota durağı ve paket state'lerini bağımsız state machine olarak tasarla; kısmi teslim, eksik, iade ve teslim kanıtını kaybetme.
- Kargo planlamada hard constraint ile soft constraint ayrımını açıkça modelle; `Infeasible`, `FeasibleWithWarnings` ve `Feasible` sonuçlarını sakla.
- Vehicle-fit ve LoadPlan önerisinin `algorithm_name`, `algorithm_version`, input/capacity snapshot, score, validation summary ve manual change audit bilgilerini koru; optimalite iddiası yoksa UI/API bunu açıkça belirtmeli.
- Her değerlendirilen araç için `load_plan_vehicle_candidates` veya eşdeğer `VehicleFitEvaluation` kaydı tut; seçilmeyen adayın `rejection_code`, kapasite kullanım oranları, kapı/boyut/istif/aks/durak sonucu ve açıklamasını kaybetme.
- Araç eşleştirme ayrıntıları için `vehicle-capacity-matching.md` canonical referansını kullan; toplam kg/m³ kontrolünü palet zemini, kapı açıklığı, yükseklik, yön ve durak erişimi kontrolleriyle karıştırma.
- Plan değişikliği shipment miktarı, araç/rota, fiziksel profil, palet veya gerçek yük değiştiğinde versioned replan üretmeli; locked plan sessizce güncellenmemeli.
- Mobil `viewMode` (`BaseUnit`, `Packaging`, `Breakdown`) yalnızca görünüm sözleşmesidir; `operationPackagingId` işlem seviyesidir ve `quantityBase` backend'de yeniden hesaplanır.
- Mobil miktar işlemleri için barkod resolve, quantity preview, count, transfer, load scan ve delivery endpoint'leri aynı context/permission/idempotency/hata sözleşmesini kullanmalıdır.
- `Idempotency-Key` ve `quantity_operation_snapshots` olmadan ikinci stok, transfer, yükleme veya teslim hareketi kabul edilmemeli.
- BOM/hammadde ve lot/seri için kabul edilen MVP dışı sınırı koru; `ProductionMaterial`, `Lot` ve `SerialNumber` tablolarını implementation migration’ına ekleme.
- O-001 e-belge için vergi alanlarını ve `IInvoiceIntegrationService` adapter/stub sınırını uygula; gerçek sağlayıcı entegrasyonunu ayrı release olarak tut.
- O-011 için Ubuntu LTS + Docker Compose + PostgreSQL + reverse proxy + LAN HTTPS ve public route izolasyonunu architecture deployment baseline’ına al.
- O-012 için `PriceList`, `CustomerPriceGroup`, `ProductPrice` ve quote/order/invoice price snapshot’larını zorunlu tasarım girdisi olarak ele al.
- O-014 için hard constraint + First Fit Decreasing öneri + manuel depo onayı akışını uygula; MVP’de optimalite garantisi verme.

## Transaction sınırları

### Order approval

`approval + stock check + reservation + status + audit + notification`

### Delivery note

`delivery note + stock deduction + stock movement + audit`

### Payment

`payment + current transaction + balance update + audit`

### Production completion

`production completion + stock receipt + machine statistics + audit`

### Shipment planning and delivery

`vehicle assignment + route plan + load plan validation + package/stop mapping + audit`

### Package delivery

`package barcode scan + recipient/stop validation + delivery proof + package status + audit`

Kapasite planı ve teslimat durumları stok çıkışını ikinci kez üretmemelidir. Yük planı ve rota değişiklikleri versiyonlu/audit'li olmalıdır. Hepsi ilgili use-case sınırında atomik olmalıdır.

## API

REST API tasarla. Entity'leri doğrudan dışarı açma.

DTO + validation + authorization + consistent error model kullan.

Örnek namespace:

```text
/api/auth
/api/users
/api/roles
/api/products
/api/customers
/api/quotes
/api/orders
/api/delivery-notes
/api/shipments
/api/shipments/{shipmentId}/load-plan
/api/shipments/{shipmentId}/route
/api/shipments/{shipmentId}/packages
/api/shipments/{shipmentId}/vehicle-fit
/api/shipments/{shipmentId}/load-plan/validate
/api/shipments/{shipmentId}/load-plan/replan
/api/mobile/barcodes/resolve
/api/mobile/products/{productId}/quantity-options
/api/mobile/quantity-previews
/api/mobile/stock-counts/{stockCountId}/items
/api/mobile/warehouse-transfers/{transferId}/items
/api/mobile/shipments/{shipmentId}/load-scans
/api/mobile/shipments/{shipmentId}/route-stops/{routeStopId}/deliveries
/api/mobile/preferences/quantity-view
/api/vehicles
/api/vehicle-types
/api/invoices
/api/payments
/api/current-accounts
/api/warehouses
/api/stocks
/api/production
/api/machines
/api/employees
/api/attendance
/api/leaves
/api/reports
/api/notifications
```

## RBAC

Role yalnızca başlangıç seviyesi olmalı; gerçek erişim permission seviyesinde uygulanmalı.

Örnek:

`order.read`, `order.create`, `order.approve`, `invoice.create`, `payment.create`, `stock.read`, `stock.count`, `stock.transfer`, `stock.adjust`, `production.complete`, `shipment.read`, `shipment.create`, `shipment.load-plan`, `shipment.vehicle-fit`, `shipment.plan-suggest`, `shipment.route-manage`, `shipment.package-assign`, `shipment.plan-replan`, `shipment.plan-override`, `shipment.load-verify`, `shipment.deliver`, `vehicle.manage`, `vehicle.status-update`

## Security architecture

- Password hashing
- JWT + refresh token
- Authorization middleware/policies
- Rate limiting
- Secure headers
- File upload validation
- Audit logging
- Session invalidation
- HTTPS
- CORS policy

## Deployment

Docker Compose tabanlı local/company deployment tasarla.

En az:

- API
- Web
- PostgreSQL
- Reverse proxy

Backup, restore, health check ve log rotation planı oluştur. Local-first veya ücretsiz deployment önerisi, işletim sistemi, HTTPS, reverse proxy ve RPO/RTO sahibi tarafından onaylanmadan kesin deployment kararı sayılmaz.
