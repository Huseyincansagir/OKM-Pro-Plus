# Factory ERP-Lite
## Açık Kararlar ve Design Gate Karar Atölyesi

> **Amaç:** O-001–O-014 arasındaki açık kararları; önerilen MVP çözümü, karar sahibi, mimari etkisi ve kapanış kriterleriyle birlikte proje yönetimi ekibinin onayına sunmak.

| Alan | Durum |
|---|---|
| Proje | Factory ERP-Lite — üretim, depo, satış, sevkiyat, fatura, cari, ödeme, personel ve raporlama |
| Aşama | `DISCOVER → DESIGN` |
| Design Gate | **BLOCKED** |
| Implementation | **NOT READY** |
| Sonraki skill | `factory-erp-architecture` — açık kararlar kapatıldıktan sonra |
| Karar kuralı | Agent önerisi tek başına `DECIDED` değildir; proje sahibi onayı gerekir |
| Kanonik kaynak | [`/design/`](./) |

---

## 1. Yönetici özeti

Factory ERP-Lite için ekran envanteri, UX mimarisi, mobil operasyon akışları, public katalog tasarımı, domain modeli, iş akışları ve PostgreSQL teknik ön taslağı hazırlanmıştır. Bu artefact'lar kodlamaya başlamadan önce gerekli tasarım omurgasını oluşturur.

Bununla birlikte O-001–O-014 arasındaki **14 açık karar**, veri modeli, belge durumları, yetki politikaları, entegrasyon sınırları, public erişim güvenliği ve deployment davranışını doğrudan etkiler. Bu nedenle Design Gate şu anda **BLOCKED** durumundadır. Karar sahipleri seçimleri, gerekçeleri ve karar tarihlerini yazılı biçimde onaylamadan production code yazılmamalıdır.

> **Toplantının temel çıktısı:** Her O maddesi için `seçim + karar sahibi + tarih + gerekçe + etkilenen artefact listesi` kaydedilmiş olmalıdır.

---

## 2. Korunan mimari temel

Açık kararlar henüz kapanmamış olsa da aşağıdaki temel yönler korunmaktadır:

| Konu | Korunan karar |
|---|---|
| Uygulama yüzeyleri | İç web ERP, mobil saha uygulaması ve ayrı public ürün kataloğu |
| Backend yönü | ASP.NET Core Web API, EF Core, PostgreSQL ve modüler monolith yaklaşımı |
| Kullanıcı deneyimi | Türkçe arayüz; entity, property ve API isimleri İngilizce |
| Veri kaynağı | `Product`, `Customer`, `Stock`, `StockMovement`, `CurrentTransaction` ve `Payment` için tek source of truth |
| Stok ve cari | Stok/cari hareketleri immutable ledger mantığıyla tutulur; kritik kayıtlar fiziksel olarak silinmez |
| Belge zinciri | `SalesOrder → DeliveryNote → Shipment → Invoice` ilişkisi izlenebilir kalır |
| Yetkilendirme | RBAC + permission tabanlı erişim ve kritik işlemlerde audit log |
| Deployment | Şirket içi, local-first, ücretsiz Docker Compose tabanlı kurulum yönü |

Ayrıntılı domain ve veri tabanı etkileri için [`domain-model.md`](./domain-model.md), [`business-workflows.md`](./business-workflows.md) ve [`database-technical-architecture.md`](./database-technical-architecture.md) dosyaları esas alınır.

### Ürün miktarı ve ambalaj hiyerarşisi

Ürünler tek bir `birim` alanıyla değil, ürün bazlı ambalaj hiyerarşisiyle tarif edilir:

```text
Palet → Koli → Paket → Temel Birim (adet, kg, metre, litre)
```

Her ürünün bir `base_uom` değeri bulunur. Palet, koli ve paket seviyeleri aynı ürün altında `ProductPackaging` kayıtları olarak tanımlanır. Bu seviyeler ayrı ürün kartları değildir; her biri temel birime dönüşüm katsayısı ve gerekirse ayrı barkod taşır.

Örneğin `1 Paket = 100 adet`, `1 Koli = 20 Paket = 2.000 adet` ise kullanıcı `5 Koli` girdiğinde sistem yeni bir ürün oluşturmaz. İşlem `entered_quantity = 5`, `entered_packaging = Koli`, `quantity_base = 10.000 adet` olarak kaydedilir ve ekranda **`5 Koli (10.000 adet)`** gösterilir. Ağırlıkla yönetilen bir ürün için aynı mantık geçerlidir: `1 Koli = 60 kg` ise `5 Koli = 300 kg` olur.

Stok, rezervasyon, sevkiyat, fatura allocation ve üretim hareketlerinin doğruluk kaynağı temel birim miktarıdır. Kullanıcının girdiği ambalaj ve belge tarihindeki dönüşüm snapshot'ı da saklanır. Açılmış ambalajlarda `0,5 Koli` gibi belirsiz bir gösterim yerine `4 Koli + 6 Paket` gibi açık kırılım kullanılır.

### Fiziksel ölçüler ve karışık palet planlama

Ambalaj miktarı ile fiziksel lojistik bilgisi ayrı tutulur. Ürün veya ambalaj seviyesinde boyut, net/brüt/dara ağırlık, hacim, kırılabilirlik ve istiflenebilirlik bilgileri tanımlanır. Böylece `5 Koli` yalnızca miktar olarak değil, kargo için gereken kg ve m³ olarak da hesaplanabilir.

Karışık palet ayrı bir ürün değildir; sevkiyata bağlı `LoadPlan → LoadUnit → LoadUnitItem` yapısıdır. Bir `LoadUnit` aynı palet üzerinde farklı ürün veya ambalaj kalemlerini taşıyabilir. Sistem araç/kargo kapasitesini ağırlık, hacim, palet adedi, ölçü ve istifleme kurallarıyla kontrol eder. İlk sürümde otomatik öneri yalnızca uygunluk ön kontrolü ve manuel düzenleme desteği verir; depo sorumlusu planı kilitler ve gerçek yükleme barkodla doğrulanır.

Sevkiyata ayrıca araç tipi, gerçek araç, şoför ve çok duraklı `RoutePlan` atanır. `RouteStop` müşteri/adres, sıra, planlanan-gerçekleşen zaman ve teslim durumunu taşır. `ShipmentPackage` ise palet/koli/paket barkodunu ilgili müşteri ve adrese bağlar. Böylece sistem şu soruları tekil olarak cevaplayabilir: **Araç şu an hangi durumda? Hangi adrese gidiyor? Araçta hangi palet/koli/paket var? Hangi yük kime teslim edilecek? Hangi paket teslim edildi veya istisnaya düştü?**

```text
Sevkiyat: SHP-2026-000142       Kapasite: 1.200 kg | 8,0 m³ | 4 palet
PALLET-001  Karışık Palet
├─ Premium Napkin 33x33   3 Koli   36 kg   0,216 m³
└─ Kokteyl Napkin 24x24   6 Koli   78 kg   0,468 m³
```

---

## 3. Karar haritası

Açık kararlar beş çalışma grubunda ele alınmalıdır.

| Çalışma grubu | Kararlar | Birincil sahipler |
|---|---|---|
| Finans ve satış | O-001, O-002, O-003, O-012 | Muhasebe, mali müşavir, satış ve depo yöneticisi |
| Üretim ve kalite | O-004, O-005 | Üretim sorumlusu, kalite sorumlusu |
| Müşteri, risk ve public | O-006, O-007, O-009 | Satış yönetimi, yönetim, hukuk/uyum |
| Personel ve altyapı | O-008, O-010, O-011 | İK, muhasebe, sistem yöneticisi |
| Marka ve görsel varlık | O-013 | Proje sahibi, pazarlama |
| Lojistik otomasyon ve kapasite | O-014 | Depo ve sevkiyat yöneticisi |

### O-014 — Kargo planlama otomasyon seviyesi

| Alan | Öneri |
|---|---|
| MVP | Hard constraint doğrulaması + açıklanabilir `First Fit Decreasing` sezgisel öneri + depo sorumlusu manuel onayı |
| Blokaj | Ağırlık, hacim, ölçü, kapı, palet, uyumluluk, istifleme, miktar, paket sahibi veya rota çakışması ihlalinde kilitleme engellenir |
| Uyarı | Soft constraint ihlali warning/penalty olarak gösterilir; override için yetki ve gerekçe gerekir |
| Otopilot sınırı | Optimal 3D packing, aks ağırlığı ve kesin trafik rotası MVP dışında; sistem optimalite garantisi vermez |
| Karar sahibi | Depo ve sevkiyat yöneticisi |
| Etkilenen artefact'lar | Domain, database, workflow, UI, QA/security, architecture ve implementation skill'leri |

---

## 4. Finans ve satış kararları

| ID | Açık karar | Önerilen MVP çözümü | Karar sahibi | Seçim / not |
|---|---|---|---|---|
| **O-001** | Vergi/VAT ve e-belge entegrasyonu | Fatura domaininde vergi kodu, oranı ve hesaplama alanları hazır tutulur. `IInvoiceIntegrationService` adapter sözleşmesi tanımlanır; ilk sürümde gerçek entegratör yerine test/stub sağlayıcı kullanılır. KDV oranları hard-code edilmez. | Muhasebe + mali müşavir | ☐ Onay ☐ Revizyon |
| **O-002** | Kısmi sevkiyat politikası | **İzin ver.** `ordered_qty`, `reserved_qty`, `shipped_qty`, `remaining_qty` kalem seviyesinde yönetilir. Tek siparişten birden fazla irsaliye üretilebilir. | Satış + depo yöneticisi | ☐ Onay ☐ Revizyon |
| **O-003** | Kısmi fatura politikası | **İzin ver.** Fatura kalemi irsaliye kalemine miktar bazında bağlanır. Aynı irsaliyenin yalnızca faturalanmamış kalan miktarı faturalandırılabilir. | Muhasebe | ☐ Onay ☐ Revizyon |
| **O-012** | Fiyat listesi ve müşteri bazlı fiyatlandırma | `PriceList`, `CustomerPriceGroup` ve `ProductPrice` modeli kullanılır. Teklif veya sipariş oluştuğunda uygulanan fiyat snapshot olarak kilitlenir. Public katalog fiyat göstermez. | Satış + yönetim | ☐ Onay ☐ Revizyon |

### Finans/satış kararlarının mimari etkisi

Bu karar grubu; `Invoice` ve `InvoiceItem` vergi alanlarını, sevkiyat ve fatura allocation ilişkilerini, sipariş durum makinesini, fiyat geçerlilik tarihlerini, müşteri grubu yetkilerini ve raporlamada kullanılacak snapshot alanlarını etkiler. Özellikle O-002 ve O-003 kapanmadan `SalesOrderItem`, `DeliveryNoteItem` ve `InvoiceItem` arasındaki miktar kuralları kesinleştirilemez.

---

## 5. Üretim ve kalite kararları

| ID | Açık karar | Önerilen MVP çözümü | Karar sahibi | Seçim / not |
|---|---|---|---|---|
| **O-004** | BOM/reçete ve hammadde tüketimi | **MVP’de kapalı.** İlk sürüm üretim gerçekleştiğinde yalnızca bitmiş ürün için `StockMovement IN` üretir. `ProductionMaterial` genişleme sınırı belgede korunur. | Üretim sorumlusu | ☐ Onay ☐ Revizyon |
| **O-005** | Lot/seri/parti izleme | **MVP’de kapalı.** İlk sürüm ürün + depo + miktar seviyesinde ilerler. Kalite, geri çağırma veya mevzuat ihtiyacı varsa karar MVP’den önce yeniden açılır. | Kalite + üretim | ☐ Onay ☐ Revizyon |

### Üretim/kalite kararlarının mimari etkisi

O-004 açık kalırsa üretim tamamlanmasının yalnızca bitmiş ürün stoğuna mı, yoksa hammadde tüketim ledger'ına da mı bağlanacağı belirsiz kalır. O-005 ise stok hareketlerinin lot/seri ana kayıtlarına bağlanıp bağlanmayacağını belirler. Bu kararlar maliyet, kalite, iade ve izlenebilirlik raporlarını doğrudan etkiler.

---

## 6. Müşteri, risk ve public kararları

| ID | Açık karar | Önerilen MVP çözümü | Karar sahibi | Seçim / not |
|---|---|---|---|---|
| **O-006** | Public teklif talebinin müşteri kartına dönüşmesi | Public talep doğrudan aktif müşteri oluşturmaz. Satış kullanıcısı talebi inceler, mevcut müşteriye bağlar veya manuel onayla yeni müşteri açar. | Satış yöneticisi | ☐ Onay ☐ Revizyon |
| **O-007** | Risk algoritması ve blokaj eşiği | **Soft block.** Risk uyarısı görünür; sipariş girişi otomatik olarak kesilmez. Onay aşamasında yetkili override ve açıklama gerekir. Hard block yalnızca yönetimce belirlenen kritik durumlarda kullanılır. | Yönetim + muhasebe | ☐ Onay ☐ Revizyon |
| **O-009** | Public erişim, rate limit, bot kontrolü ve KVKK | Katalog açık kalabilir; form endpoint'lerinde rate limit, honeypot veya CAPTCHA, e-posta/telefon doğrulaması, minimum veri ve versiyonlu aydınlatma/onay metni uygulanır. | Yönetim + hukuk/uyum | ☐ Onay ☐ Revizyon |

### Müşteri/risk/public kararlarının mimari etkisi

O-006, `QuoteRequest → CustomerCandidate/Customer → Quote` ayrımını, duplicate müşteri kontrolünü ve audit davranışını belirler. O-007 risk snapshot, scoring run, override reason, permission ve audit alanlarını etkiler. O-009 ise public API sınırını, abuse loglarını, consent kaydını ve veri saklama/silme politikasını zorunlu hale getirir.

> **Uyum notu:** KVKK, e-belge, bordro ve finansal kayıtlarla ilgili nihai politika; teknik öneri olarak değil, ilgili hukuk/uyum, mali müşavir ve iş sahiplerinin onayıyla uygulanmalıdır.

---

## 7. Personel ve altyapı kararları

| ID | Açık karar | Önerilen MVP çözümü | Karar sahibi | Seçim / not |
|---|---|---|---|---|
| **O-008** | Maaş/bordro entegrasyon kapsamı | **Kayıt + kontrollü export.** Puantaj bağlantısı, dönem özeti ve hassas alan yetkileri bulunur; yasal bordro hesap motoru ve beyan entegrasyonu MVP dışındadır. | İK + muhasebe | ☐ Onay ☐ Revizyon |
| **O-010** | Backup saklama ve RPO/RTO hedefleri | Günlük full backup, ayrı disk/volume, en az 14 gün retention ve aylık restore testi. Kritik operasyonun RPO/RTO değeri ayrıca yazılı onaylanır. | Sistem yöneticisi | ☐ Onay ☐ Revizyon |
| **O-011** | Şirket serverı, LAN ve HTTPS modeli | **Local-first.** Öneri: Ubuntu LTS + Docker Compose + Nginx/Traefik + şirket LAN HTTPS. İşletim sistemi, reverse proxy ve sertifika seçimi Architecture aşamasında kesinleştirilir. | Sistem yöneticisi | ☐ Onay ☐ Revizyon |

### Personel/altyapı kararlarının mimari etkisi

O-008 hassas maaş alanları için ayrı permission, masking ve export audit'i gerektirir. O-010 yalnızca bir backup komutu değil; başarısızlık bildirimi, retention, restore runbook ve düzenli geri dönüş testi gerektirir. O-011 ise mobil erişim, firewall, DNS, sertifika ve health-check yapılandırmasını belirler.

---

## 8. Marka ve görsel varlık kararı

| ID | Açık karar | Önerilen MVP çözümü | Karar sahibi | Seçim / not |
|---|---|---|---|---|
| **O-013** | Final marka adı, logo, design token ve ürün görseli lisansı | Kodlamadan önce tek marka adı, logo, favicon, renk token'ları, font ve ürün görseli lisans/placeholder politikası onaylanır. Geçici olarak nötr `Factory ERP` adı kullanılabilir. | Proje sahibi + pazarlama | ☐ Onay ☐ Revizyon |

Mockup aşamasında birden fazla marka adı kullanılmıştır. Bu durum tasarım keşfi için kabul edilebilir; ancak production'a geçmeden önce web, mobil ve public yüzeyler tek marka altında birleştirilmelidir.

---

## 9. Karar seçimi ile mimari yayılım zinciri

Her karar yalnızca `decision-log.md` içinde işaretlenmemelidir. Seçim, etkilenen artefact'lara yayılmadan Design Gate açılamaz.

```text
Proje sahibi seçimi
        ↓
Decision Log: sınıf + tarih + sahip + gerekçe
        ↓
Domain Model: entity, value object ve source-of-truth
        ↓
Business Workflow: state machine ve geçiş kuralları
        ↓
Database Architecture: tablo, ilişki, constraint ve migration
        ↓
API Contract: request/response, validation ve permission
        ↓
UX/UI: web, mobil ve public ekran davranışları
        ↓
Skill Impact Review: design / architecture / implementation / QA / operations
        ↓
Design Gate: READY FOR ARCHITECTURE
```

### Karar kapatma kontrolü

- [ ] Seçilen değer açıkça yazıldı.
- [ ] Karar sahibi belirlendi.
- [ ] Karar tarihi kaydedildi.
- [ ] Gerekçe ve kapsam sınırı yazıldı.
- [ ] Etkilenen domain artefact'ları güncellendi.
- [ ] Workflow ve state geçişleri güncellendi.
- [ ] Database/API/permission etkileri güncellendi.
- [ ] Web, mobil ve public ekran etkileri incelendi.
- [ ] Skill-impact review tamamlandı.
- [ ] `implementation-readiness.md` yeniden değerlendirildi.

---

## 10. Önerilen karar toplantısı formatı

Toplantı, kararları tek oturumda aceleyle kapatmak yerine sahiplik ve etki zincirini görünür kılan kısa çalışma bloklarıyla yürütülmelidir.

| Sıra | Çalışma bloğu | Kararlar | Çıktı |
|---:|---|---|---|
| 1 | Finans ve belge zinciri | O-001, O-002, O-003 | Vergi, partial shipment ve partial invoice seçimi |
| 2 | Ticari fiyat ve müşteri | O-006, O-012 | Müşteri açma ve fiyat snapshot politikası |
| 3 | Risk ve public güvenlik | O-007, O-009 | Soft/hard block, rate limit, consent ve abuse kapsamı |
| 4 | Üretim ve kalite | O-004, O-005 | BOM ve lot/seri MVP sınırı |
| 5 | Personel ve operasyon | O-008, O-010, O-011 | Bordro, backup ve local server hedefleri |
| 6 | Marka | O-013 | Tek marka ve asset politikası |
| 7 | Lojistik otomasyon ve kapasite | O-014 | Hard/soft constraint, heuristik öneri ve manuel onay sınırı |

Her çalışma bloğunun sonunda karar sahibi, seçilen seçenek, gerekçe ve teknik yayılım notu toplantı tutanağına işlenmelidir.

---

## 11. Design Gate açılma kriteri

Design Gate ancak aşağıdaki koşulların tamamı sağlandığında `READY FOR ARCHITECTURE` olarak değerlendirilebilir:

1. O-001–O-014 maddelerinin her biri için proje sahibi veya yetkili karar sahibi tarafından onay verilmiş olmalıdır.
2. Her kararda karar tarihi, gerekçe ve etkilenen artefact listesi bulunmalıdır.
3. `decision-log.md`, domain model, business workflows, database technical architecture ve screen inventory birbiriyle tutarlı olmalıdır.
4. Karar bağımlı permission, audit, integration ve deployment etkileri açıkça tanımlanmalıdır.
5. Design, architecture, implementation, QA/security ve operations skill paketleri kararları tüketmeye hazır olmalıdır.
6. `implementation-readiness.md` içinde blocker kalmamalı; `implementation-ready.md` geçiş kriterleri karşılanmalıdır.

> **Önemli:** Bir agent'ın, Grok'un veya ChatGPT'nin öneriyi yazmış olması karar sahibi onayı yerine geçmez. Öneri ile karar arasındaki ayrım korunmalıdır.

---

## 12. Karar kaydı şablonu

Aşağıdaki şablon her O maddesi için doldurulup [`decision-log.md`](./decision-log.md) dosyasına işlenmelidir.

| Alan | Değer |
|---|---|
| Karar ID | `O-___` |
| Konu |  |
| Seçilen seçenek |  |
| Karar sahibi |  |
| Karar tarihi | `YYYY-MM-DD` |
| Gerekçe |  |
| MVP kapsamı |  |
| Etkilenen domain artefact'ları |  |
| Etkilenen workflow/state'ler |  |
| Etkilenen database/API/permission alanları |  |
| Etkilenen web/mobile/public ekranları |  |
| Skill-impact sonucu |  |
| Architecture aşamasına teknik not |  |

---

## 13. Sonraki adımlar

| Adım | Durum |
|---|---|
| Karar atölyesinde O-001–O-014 seçimlerini yapmak | Bekliyor |
| Karar sahibi, tarih, gerekçe ve artefact listesini kaydetmek | Bekliyor |
| Canonical `/design/` belgelerine kararları yaymak | Bekliyor |
| Numbered `/docs/` mirror kopyalarını senkronize etmek | Bekliyor |
| Design Gate'i yeniden değerlendirmek | Bekliyor |
| `factory-erp-architecture` aşamasına geçmek | Design Gate açıldıktan sonra |
| Production code yazmaya başlamak | Architecture çıktıları onaylandıktan sonra |

---

## Canonical referanslar

- [`decision-log.md`](./decision-log.md) — DECIDED, ASSUMED ve OPEN DECISION kayıtları.
- [`open-decisions-solution-matrix.md`](./open-decisions-solution-matrix.md) — O-001–O-014 için önerilen MVP çözümleri ve etki matrisi.
- [`implementation-readiness.md`](./implementation-readiness.md) — Design Gate değerlendirmesi.
- [`implementation-ready.md`](./implementation-ready.md) — Kodlama öncesi geçiş kontrolü.
- [`domain-model.md`](./domain-model.md) — Bounded context ve source-of-truth modeli.
- [`business-workflows.md`](./business-workflows.md) — Satış, üretim ve personel iş akışları.
- [`database-technical-architecture.md`](./database-technical-architecture.md) — PostgreSQL, transaction, API ve deployment ön taslağı.
- [`master-screen-inventory.md`](./master-screen-inventory.md) — Web, mobil ve public ekran envanteri.
- [`logistics-planning-rules-and-algorithms.md`](./logistics-planning-rules-and-algorithms.md) — Hard/soft lojistik kuralları, araç kapasite eşleştirme ve karışık palet algoritması.

**Belge durumu:** Karar toplantısı taslağı — karar sahibi onayı alınana kadar öneri niteliğindedir.

**Hazırlayan:** Manus AI
**Tarih:** 16 Ağustos 2026

---

## Kaynak dosyalar

Bu belge, aynı repository içindeki canonical tasarım artefact'larından türetilmiştir. Harici kaynak veya doğrulanmamış dış veri kullanılmamıştır.

[1]: ./decision-log.md "Decision Log"
[2]: ./open-decisions-solution-matrix.md "Open Decisions Solution Matrix"
[3]: ./implementation-readiness.md "Implementation Readiness"
[4]: ./domain-model.md "Domain Model"
[5]: ./business-workflows.md "Business Workflows"
[6]: ./database-technical-architecture.md "Database Technical Architecture"
[7]: ./master-screen-inventory.md "Master Screen Inventory"

---

## Ek: Default MVP karar paketi

Hızlı bir yönetim kararı gerekiyorsa aşağıdaki paket başlangıç önerisi olarak kullanılabilir. Bu paket **karar verilmiş sayılmaz**; her satır karar sahibinin onayından sonra `decision-log.md` içine taşınmalıdır.

| ID | Varsayılan öneri |
|---|---|
| O-001 | Adapter + stub; vergi alanları hazır, gerçek entegratör sonraki fazda |
| O-002 | Kısmi sevkiyat açık |
| O-003 | Kısmi fatura açık |
| O-004 | BOM/hammadde MVP dışında |
| O-005 | Lot/seri MVP dışında |
| O-006 | Public talep → satış tarafından manuel müşteri onayı |
| O-007 | Soft block + yetkili override |
| O-008 | Kayıt + kontrollü export; yasal bordro motoru yok |
| O-009 | Açık katalog + rate limit + bot kontrolü + privacy/KVKK metni |
| O-010 | Günlük full backup + 14 gün retention + aylık restore testi |
| O-011 | Local-first; Ubuntu/Docker/reverse proxy seçimi Architecture aşamasında |
| O-012 | `PriceList` + `CustomerPriceGroup` + order price snapshot |
| O-013 | Tek marka ve asset politikası kodlamadan önce zorunlu |
| O-014 | Heuristik araç/palet önerisi + hard constraint validation + manuel depo onayı; optimalite garantisi yok |

> **Son karar:** Bu paketin tamamı veya herhangi bir satırı proje sahibi tarafından onaylanmadan `DECIDED` olarak işaretlenmeyecektir.
