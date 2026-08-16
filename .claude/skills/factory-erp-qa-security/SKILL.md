---
name: factory-erp-qa-security
description: Fabrika ERP'sinin functional QA, integration/E2E, security, authorization, stock/cari integrity, performance ve release readiness kontrollerini yapmak için kullan.
---

# Factory ERP QA & Security

## Amaç

Sistemin yalnızca çalışmasını değil, yanlış kullanım altında da güvenli ve tutarlı kalmasını doğrula.

## Test katmanları

### Unit

- domain rules
- state transitions
- risk scoring
- calculations
- validation

### Integration

- API + database
- transactions
- authorization
- document numbering
- stock movements
- current account transactions

### E2E

Ana akış:

`Customer → Product → Quote Request → Quote → Order → Approval → Reservation → Delivery Note → Shipment → Vehicle/RoutePlan → LoadPlan/LoadUnit → ShipmentPackage → RouteStop Delivery → Invoice → Payment → Current Balance`

Üretim:

`Production Order → Production Record → Personnel → Machine → Completion → Stock Receipt`

## Critical integrity tests

- Aynı irsaliyenin iki kez faturalanması engellenmeli.
- Aynı ödeme iki kez cari hesaba yansıtılmamalı.
- Yetkisiz kullanıcı ödeme/fatura değiştirememeli.
- Onaysız sipariş sevk edilememeli.
- Stokta olmayan miktar sevk edilememeli.
- İptal edilmiş belge tekrar aktifleşmemeli.
- Audit log silinememeli.
- Belge numarası collision olmamalı.
- Concurrent stock operations veri kaybı oluşturmamalı.
- Aynı paket barkodu iki kez yüklenmemeli veya iki farklı müşteriye teslim edilmemeli.
- Paket müşteri/adres/route stop eşleşmesi olmadan rota veya yük planı kilitlenememeli.
- Araç kapasitesi, rota tarih çakışması ve palet/ölçü sınırları server-side doğrulanmalı.
- Kısmi teslimde teslim edilen paketler kapanmalı; kalan paketler yanlışlıkla teslim edilmiş sayılmamalı.
- Teslim kanıtı (imza/fotoğraf/not) yanlış müşteriye veya durağa bağlanmamalı.
- `quantity_base` ile ambalaj görünümü arasında hesap farkı oluşmamalı.

## Security review

Kontrol et:

- Authn/Authz
- IDOR/BOLA
- SQL injection
- XSS
- CSRF
- file upload abuse
- broken access control
- excessive data exposure
- package/customer/address IDOR veya BOLA
- başka müşterinin rota ve paket bilgilerinin görüntülenmesi
- teslim kanıtı dosyalarının yetkisiz erişime açık olması
- weak password/session handling
- secret leakage
- insecure default configuration
- missing rate limit

## Role matrix test

En az şu roller için pozitif ve negatif test yap:

- Admin
- Manager
- Sales
- Warehouse
- Production
- Accounting
- HR
- Viewer
- Dispatcher/Shipment Operator
- Driver

## Performance

Ölç:

- dashboard response time
- large table queries
- report generation
- stock lookup
- barcode lookup
- customer search
- route board and stop query
- package trace by barcode/customer/address
- capacity validation and load-plan calculation
- concurrent document creation

N+1, missing indexes ve full-table scan risklerini ara.

## Release gate

Release öncesi:

- build green
- migrations clean
- unit/integration/E2E green
- security review green
- backup test successful
- restore procedure verified
- health endpoint green
- critical logs monitored

`release-readiness.md` oluştur.
