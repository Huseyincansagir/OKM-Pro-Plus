---
name: factory-erp-qa-security
description: Fabrika ERP'sinin functional QA, integration/E2E, security, authorization, stock/cari integrity, performance ve release readiness kontrollerini yapmak için kullan.
---

# Factory ERP QA & Security

## Amaç

Sistemin yalnızca çalışmasını değil, yanlış kullanım altında da güvenli ve tutarlı kalmasını doğrula.

## Karar ve Design Gate kanıtı

- Test planını `/design/decision-log.md` ve `/design/decision-clarification-backlog.md` ile eşleştir; mevcut O-001–O-014 değerlerini kabul edilmiş baseline olarak test fixture ve assertion’lara taşı.
- O-001–O-005, O-011 ve O-012 `DECIDED` olsa da ilgili fatura, sevkiyat, üretim, stok, deployment ve fiyat acceptance testleri tamamlanmadan release-ready sayma.
- O-007, O-009, O-010 ve O-014 için owner, override, failure, audit, privacy/recovery veya algorithm acceptance testleri eksikse release gate'i kırmızı tut.
- Karar yayılımını domain rule, workflow state, database constraint, API authorization, UI behavior, operations runbook ve test evidence arasında doğrula.
- Yeni veya değişen bir karar `OPEN DECISION` durumuna dönerse ilgili feature’ın release gate’ini durdur ve Design Gate’i yeniden değerlendir.
- Agent önerisi veya historical document testte güncel karar gibi kullanılmamalı; güncel `decision-log.md` authoritative kaynaktır.


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
- Aynı barkodun art arda mobil taranması ikinci stok, transfer, sayım veya teslim hareketi üretmemeli.
- Bilinmeyen barkod ürün/ambalaj/yük birimi eşleşmesi olmadan işlem başlatmamalı.
- `Temel Birim / Ambalaj / Kırılım` toggle değişimi `quantity_base` veya işlem seviyesini sessizce değiştirmemeli.
- Paket müşteri/adres/route stop eşleşmesi olmadan rota veya yük planı kilitlenememeli.
- Araç kapasitesi, rota tarih çakışması ve palet/ölçü sınırları server-side doğrulanmalı.
- Kısmi teslimde teslim edilen paketler kapanmalı; kalan paketler yanlışlıkla teslim edilmiş sayılmamalı.
- Teslim kanıtı (imza/fotoğraf/not) yanlış müşteriye veya durağa bağlanmamalı.
- `quantity_base` ile ambalaj görünümü arasında hesap farkı oluşmamalı.
- Hard constraint ihlalleri ile soft warning'ler farklı sonuç ve yetki davranışı üretmeli.
- Araç adayları bakım, zaman çakışması, kg, m³, palet, ölçü ve kapı açıklığı kurallarına göre doğru elenmeli.
- Her adayın `candidate_status`, `rejection_code`, kullanım oranları ve açıklaması saklanmalı; alternatif adayların neden elendiği doğrulanmalı.
- Toplam hacim yeterli olsa bile zemin ayak izi, kapı açıklığı, iç ölçü, yükseklik ve yön uyumsuzluğu doğru hard error üretmeli.
- İstiflenemeyen/ezilebilir ürün, uyumsuzluk grubu ve durak erişimi kontrolleri ayrı test edilmeli.
- Aks/yük bölgesi verisi yoksa sonuç `NotEvaluated` olmalı; test sistemi aks uygunluğu iddia etmemeli.
- Güvenlik payı ve skor parametre seti version değişiminde input/algorithm snapshot ile birlikte saklanmalı.
- First Fit Decreasing önerisi aynı input snapshot ve algorithm version ile tekrarlanabilir olmalı.
- `viewMode` değişimi `operationPackagingId` veya `quantityBase` değerini değiştirmemeli.
- İstemciden gelen `quantityBase` değiştirildiğinde backend bunu reddetmeli veya kendi hesabıyla değiştirmeli.
- Aynı `Idempotency-Key` ile tekrar gönderilen sayım, transfer, load scan veya delivery isteği ikinci hareket üretmemeli.
- Commit edilmiş miktar hareketinde `packaging_snapshot`, `operation_packaging_id` ve `view_mode_at_entry` bulunmalı.
- Bilinmeyen/ambiguous barkod ve aktif durak dışı paket için endpoint güvenli hata sözleşmesi döndürmeli.
- Karışık palet uyumluluk, istifleme ve durak erişim kurallarını ihlal ederse bloke veya açıklanabilir warning üretmeli.
- Plan kilitlendikten sonra yapılan manuel değişiklik yeni version, audit ve validation sonucu üretmeli.

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
- mobil kullanıcıya aktif durağı dışındaki paketlerin gösterilmesi veya teslim ettirilmesi
- barkod/ambalaj detayında excessive data exposure
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
- mobile camera scan response and duplicate-scan lock
- quantity toggle and base-unit conversion response
- customer search
- route board and stop query
- package trace by barcode/customer/address
- capacity validation and load-plan calculation
- vehicle candidate scoring and rejection reasons
- candidate rejection code and capacity-usage snapshot integrity
- floor footprint, door opening, orientation, height and axle-check behavior
- safety-margin and parameter-set versioning
- mixed-pallet constraint validation
- route stop access validation
- load-plan replan and validation result query
- mobile barcode resolve and context filtering
- quantity preview response and packaging conversion
- mobile count/transfer/load/delivery endpoint idempotency
- concurrent document creation

N+1, missing indexes ve full-table scan risklerini ara.

## Release gate

Design Gate ile Release Gate'i karıştırma: Design Gate mimariye geçiş uygunluğunu, Release Gate çalışan ürünün doğrulanmasını ölçer. Açık P0 karar veya eksik karar yayılımı varsa build başarılı olsa bile release readiness `BLOCKED` kalır.


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
