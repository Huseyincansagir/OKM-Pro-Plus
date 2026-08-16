# Factory ERP — Project Discovery Report

## 1. Project Status

Repository inspection and merge review were performed against the remote consolidated commit `e8873e8`; the local uncommitted implementation scaffold was removed before this design review. The repository currently contains documentation, visual assets, skill instructions and the canonical `/design` artefacts plus the supporting numbered `docs/00`–`docs/06` archive; it does not contain a committed production application. The Grok session notes dated 16 August 2026 were reviewed and archived; useful technical proposals were merged as candidates while open decisions remained open.

| Area | Current status |
|---|---|
| Backend | Mevcut production backend yok. |
| Frontend | Mevcut production web uygulaması yok. HTML slide kaynakları dokümantasyon asset'idir. |
| Mobile | Mevcut mobil uygulama yok. Mobil UX tasarım dokümanı vardır. |
| Database | Çalışan migration veya production schema yok; teknik ön taslak dokümanı vardır. |
| Docker/infrastructure | Çalışan deployment dosyası yok; şirket içi server ve Docker Compose tasarım gereksinimi vardır. |
| Tests | Test projesi ve CI test akışı yok. |
| CI/CD | CI/CD pipeline mevcut değildir. |
| Documentation | Mevcut tasarım, teknik mimari, public katalog, mobil, sunum ve skill dokümanları vardır. |
| Skills | Design, architecture, implementation, QA/security ve operations skill'leri `.claude/skills/` altında bulunur. |

## 2. Existing Architecture

Mevcut repository bir **dokümantasyon ve tasarım başlangıç deposudur**, henüz uygulama runtime'ı değildir. Önerilen hedef mimari; şirket içi server üzerinde Docker Compose ile çalışan modüler monolith, ASP.NET Core REST API, PostgreSQL, Next.js web, Flutter mobile ve reverse proxy bileşenlerinden oluşur.

Mevcut tasarım dosyalarının canonical source of truth'u `/design` klasörüdür; numaralı `docs/00`–`docs/06` yapısı geriye dönük arşiv ve paylaşım kopyası olarak korunur. Presentation dosyaları mevcut olsa da bu bootstrap promptunda yeni sunum üretimi yapılmamıştır.

## 3. Technical Stack

### Belirlenmiş

| Katman | Seçim |
|---|---|
| Backend | ASP.NET Core Web API / C# |
| ORM | Entity Framework Core |
| Database | PostgreSQL |
| Validation | FluentValidation yaklaşımı |
| Auth | JWT + refresh token |
| Authorization | RBAC + permission policy |
| Web | Next.js + React + TypeScript |
| Mobile | Flutter / Dart |
| Logging | Serilog / structured logging |
| Deployment | Docker Compose, reverse proxy, LAN/server |
| API | Versionable REST + DTO + OpenAPI |

### Henüz seçilmemiş veya açık

- VAT/e-belge entegrasyon sağlayıcısı.
- Fiyat listesi ve müşteri bazlı fiyat politikası.
- BOM/reçete ve hammadde tüketimi kapsamı.
- Lot/seri takibi gereksinimi.
- Maaş/bordro entegrasyon sınırı.
- Şirket server işletim sistemi ve HTTPS sertifika modeli.
- Backup RPO/RTO ve retention süreleri.
- CI sağlayıcısı ve release otomasyonu.

## 4. Domain Map

```text
Identity & Access
  → tüm internal action'ların authorization kaynağı

Products / Customers
  → Public Catalog / Quote Requests / Sales

Sales
  → Quote → SalesOrder → Approval

SalesOrder + Warehouse
  → StockReservation → DeliveryNote → Shipment

DeliveryNote
  → Invoice → CurrentTransaction → Payment

Production
  → ProductionOrder → Machine + Employee → ProductionRecord
  → Scrap/Downtime → StockReceipt

Employees
  → Attendance → Overtime/Leave → PayrollRecord

Reporting / Notifications / Audit
  → domain kararlarını ve kritik etkileri görünür kılar
```

Canonical source-of-truth matrisi `/design/domain-model.md` içindedir. Özellikle `Product`, `Customer`, `Stock`, `StockMovement`, `SalesOrder`, `DeliveryNote`, `Invoice`, `CurrentTransaction`, `Payment`, `ProductionRecord` ve `Employee` için duplicate ana kayıt oluşturulmayacaktır.

## 5. Main Workflows

### Sales

`Public Quote Request → Quote → Sales Order → Approval → Stock Reservation → Delivery Note → Shipment → Invoice → Current Account → Payment`

### Production

`Production Order → Machine Assignment → Personnel Assignment → Production → Scrap/Downtime → Production Completion → Stock Receipt`

### Personnel

`Employee → Attendance → Overtime/Leave → Approval → Production Assignment → Payroll Record`

Her workflow için actor, input, state, transition, permission, database effect, stock effect, financial effect ve audit gereksinimi `/design/business-workflows.md` içinde tanımlanmıştır.

## 6. Screen Inventory Summary

Web, public ve mobil olmak üzere üç kullanıcı yüzeyi tasarlanmıştır.

| Yüzey | Kapsanan ana ekranlar |
|---|---|
| Internal Web | Dashboard, ürün, müşteri, teklif, sipariş, onay, depo, stok, üretim, makine, irsaliye, sevkiyat, fatura, cari, ödeme, risk, rapor, bildirim, kullanıcı, rol, permission, audit, settings |
| Public | Public ana sayfa, ürün listesi, ürün detayı, teklif sepeti, müşteri formu, talep özeti, başarı ekranı |
| Mobile | Giriş, bağlantı durumu, görev ana sayfası, barkod, stok sorgu, sayım, transfer, sevkiyat, üretim, bildirim ve profil |

Her ekran için route, role, data source, action, state, permission, related document, database effect, empty/loading/error/offline, mobile behavior ve acceptance criteria kontrolü `/design/master-screen-inventory.md` ve ilgili UX dosyalarında yapılmıştır.

## 7. Technical Decisions

- İlk mimari modüler monolith olarak kalmalıdır.
- PostgreSQL ana ve ilişkisel database'dir.
- Kritik finansal/stok hareketleri immutable veya reversal tabanlıdır.
- `AvailableQuantity = Quantity - ReservedQuantity` olarak ayrıştırılır.
- Belge zinciri ID ilişkileriyle izlenir.
- API entity'leri doğrudan dışarı açmaz; DTO ve validation kullanır.
- Authorization backend'de permission seviyesinde uygulanır.
- Web yoğun operasyon tabloları için server-side pagination/filtering gerekir.
- Mobil ağ yokken finans/stok işlemi sessizce offline commit edilmez.
- Şirket içi server deployment'ında web, api, postgres ve reverse proxy bileşenleri planlanır.
- Backup oluşturma ve restore doğrulaması operasyon tasarımının parçasıdır.

## 8. Open Decisions

Design Gate'i etkileyen açık kararlar:

1. VAT/e-belge ve fatura entegrasyonu.
2. Partial shipment ve partial invoice.
3. BOM/reçete ve hammadde hareketleri.
4. Lot/seri/parti takibi.
5. Fiyat listesi ve müşteri bazlı fiyatlandırma.
6. Public katalog erişim, doğrulama ve KVKK metinleri.
7. Risk skorunun sipariş onayını bloke edip etmeyeceği.
8. Maaş/bordro kapsamı ve entegrasyonu.
9. Server işletim sistemi, HTTPS ve LAN erişim modeli.
10. Backup RPO/RTO, retention ve restore test takvimi.
11. Final marka adı, logo, favicon, renk token'ları ve ürün görseli lisans/placeholder politikası.

Ayrıntılı sahip/etki/öneri matrisi `/design/decision-log.md` içindedir.

## 9. Risks

| Risk | Etki | İlk azaltma yaklaşımı |
|---|---|---|
| Tasarım ve implementation arasında duplicate source of truth | Veri tutarsızlığı | Domain model ve decision log'u Design Gate'in zorunlu girdisi yapmak |
| Partial shipment/invoice kararının geç kalması | Sipariş, irsaliye ve fatura şeması değişir | Open decision olarak erken kapatmak |
| BOM/lot kapsamının belirsizliği | Üretim ve stok hareketleri yeniden tasarlanabilir | Üretim sorumlusu ile MVP sınırı belirlemek |
| Finansal kayıtların yanlış silinmesi | Denetim ve cari bakiye güvenilirliği bozulur | Immutable ledger + reversal |
| Yetkinin yalnızca frontend'de kalması | IDOR/BOLA ve finansal suistimal | Backend policy + role matrix testleri |
| Şirket server/backup operasyonunun net olmaması | Veri kaybı ve kesinti | Operations skill ile RPO/RTO, restore ve monitoring planı |
| Mobil ağ kesintisi | Çift/eksik stok hareketi | Offline stok/finans commit'ini yasaklamak |
| Büyük listelerde client-side data | Performans ve güvenlik riski | Server-side query, pagination ve index |
| Mockup markalarının ve ürün görsellerinin production'a doğrudan taşınması | Yanlış marka, lisans ve kullanıcı güveni riski | Marka/asset kararını kodlamadan önce kapat; nötr Factory ERP token'ları kullan |
| Aynı design dosyalarının birden fazla klasörde tutulması | Yanlış dosya source of truth seçimi | `/design` canonical; numaralı docs yalnızca senkronize arşiv/mirror olarak kullanılmalı |

## 10. Design Gate Result

Kontrol sonucu: Domain, workflow, source of truth, ekran inventory, public/mobile ayrımı ve ana teknik ön taslak hazırdır. Ancak VAT/e-belge, partial shipment/invoice, BOM/lot, fiyatlandırma, bordro, public erişim ve deployment/backup gibi açık kararlar implementation'ın veri modelini ve state transition'larını etkileyebilir.

```text
DESIGN STATUS:
BLOCKED
```

Blocker'lar `/design/decision-log.md` ve `/design/implementation-readiness.md` dosyalarında kayıtlıdır. Tasarım Gate başarılı duruma çekilmeden production code, migration veya API implementasyonuna başlanmamalıdır.

## 11. Recommended Next Skill

Bir sonraki aşama, açık kararlar kapatıldıktan sonra `factory-erp-architecture` skill'idir. Bu skill şu çıktıları üretmelidir:

- Kararları PostgreSQL schema ve migration planına dönüştürmek.
- Bounded context'lerin API ve application service sınırlarını netleştirmek.
- DTO, validation, permission policy ve transaction sözleşmelerini yazmak.
- Docker Compose, reverse proxy, backup, health check ve secret yönetimi planını kesinleştirmek.

Bundan sonra `factory-erp-implementation`, ardından `factory-erp-qa-security` ve `factory-erp-operations` uygulanmalıdır.
