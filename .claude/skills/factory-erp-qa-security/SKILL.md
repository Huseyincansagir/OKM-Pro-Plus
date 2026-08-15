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

`Customer → Product → Quote Request → Quote → Order → Approval → Reservation → Delivery Note → Shipment → Invoice → Payment → Current Balance`

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

## Performance

Ölç:

- dashboard response time
- large table queries
- report generation
- stock lookup
- barcode lookup
- customer search
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
