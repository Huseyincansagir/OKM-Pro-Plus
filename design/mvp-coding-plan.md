# Factory ERP-Lite — MVP’ye Kadar Kodlama Planı

**Tarih:** 2026-08-16
**Başlangıç commit’i:** `d487f1d` — UI reference set ve component sözleşmesi tamamlandı
**Mimari hedef:** ASP.NET Core + EF Core + PostgreSQL backend; Next.js internal/public web; Flutter mobile; Docker Compose ile şirket içi deployment

## 1. MVP tanımı

MVP, fabrikanın günlük operasyonunu uçtan uca çalıştıran ve kritik kayıtları audit edilebilir biçimde saklayan ilk production candidate’tır. MVP’nin ölçüsü ekran sayısı değil, aşağıdaki iş akışlarının baştan sona güvenli ve test edilebilir şekilde tamamlanmasıdır:

> Ürün/ambalaj tanımı → üretimden stoğa → public teklif talebi → satış incelemesi → sipariş onayı → stok rezervasyonu → kısmi irsaliye → sevkiyat/rota → kısmi veya tam fatura → cari borç → ödeme → güncel bakiye ve rapor.

Bu MVP, mevcut Domain slice’ındaki quantity, snapshot, state, allocation, reversal ve typed error kurallarını yeniden yazmayacak; onları Application, Infrastructure, API, web ve mobile katmanlarında kullanacaktır.[1]

## 2. MVP kapsam sınırı

| MVP’ye dahil | MVP’de kontrollü olarak sonraya bırakılan |
|---|---|
| Kullanıcı, rol, permission, audit ve oturum | Hukuken tam bordro/uygulama motoru |
| 100’e yakın ürün, kategori, barkod, temel UOM | BOM ve çok seviyeli hammadde planlama |
| Palet → koli → paket → temel birim packaging | Lot/seri izlenebilirliği; ihtiyaç halinde ayrı karar |
| Quantity toggle, packaging filter ve server-side base quantity | Client’ın ledger miktarı hesaplaması |
| Üretim iş emri, makine, üretim kaydı, fire, duruş, personel süresi | İleri seviye OEE ve otomatik plan optimizasyonu |
| Finished-good receipt ile stok girişi | Hammadde tüketim motoru |
| Depo, konum, stok, hareket, rezervasyon, transfer, sayım, barkod | Tam otomatik depo robotik entegrasyonu |
| Public katalog, teklif sepeti, talep ve anti-abuse | Public online ödeme ve doğrudan checkout |
| Teklif → sipariş → sorumlu onayı | Onaysız kesin sipariş |
| Kısmi sevkiyat, allocation, irsaliye, backorder | Allocation lock/re-read yerine yalnızca UI kontrolü |
| Sevkiyat, araç, şoför, durak, paket, karışık palet ve manuel kilitli yük planı | Optimal yükleme garantisi ve otonom rota optimizasyonu |
| Fatura allocation, vergi/fiyat snapshot, cari debit | Gerçek e-belge provider entegrasyonu; adapter/stub sınırı |
| Ödeme, cari ekstre, borç/alacak/bakiye, risk seviyesi | Banka otomatik mutabakatı |
| Personel, puantaj, izin, mesai, özet maaş kaydı | Yasal bordro motoru |
| Satış, üretim, stok, cari, fatura, sevkiyat ve personel raporları | Serbest BI platformu |
| Web, mobil barkod/üretim/sevkiyat ve public katalog | Tüm mobil işlemlerde tam offline conflict merge |
| Docker Compose, LAN HTTPS, backup/restore, health ve release evidence | Public cloud multi-region operasyon |

MVP dışında bırakılan her konu route, API, database table veya UI popup olarak yanlışlıkla eklenmeyecek; backlog’da açıkça “later” etiketiyle tutulacaktır. Bu sınır, ilk sürümün fabrika operasyonunu tamamlamasına ve aynı anda aşırı genişlememesine yarar.[2]

## 3. Uygulama prensibi: vertical slice + acceptance gate

Her kodlama kapısı aynı sırayı izleyecektir:

1. Domain/Application contract ve negatif senaryo testleri yazılır.
2. EF mapping, migration ve PostgreSQL constraint’i eklenir.
3. API command/query, DTO, permission ve ProblemDetails davranışı uygulanır.
4. Web/mobile/public ekranı kilitlenmiş reference PNG ve component sözleşmesine göre kodlanır.
5. Unit, integration, contract, security ve kritik UI smoke testleri çalıştırılır.
6. Build evidence, migration version, screenshot comparison ve audit/outbox sonucu kaydedilir.
7. Gate yeşil olmadan sonraki feature’a geçilmez.

Kodlama büyük feature branch’inde biriktirilmeyecek. Her gate küçük ve geri alınabilir commit’ler halinde `main` branch’ine PR ile alınacaktır. Domain’in dependency yönü ve production secret sınırı her PR’da korunacaktır.[3]

## 4. MVP kodlama sırası

### G0 — Tamamlanmış başlangıç: Domain ve UI reference foundation

Mevcut durumda `FactoryErp.Domain`, Domain unit testleri, Architecture dependency testleri, UI reference PNG’leri, `tokens.json`, `reference.css`, component implementation contract ve coverage matrix tamamlanmıştır. Bu gate’in kanıtı `d8ec3ad` ve `d487f1d` commit’leridir.

**Çıkış kriteri:** Domain build/test yeşil; UI reference dosyaları repository’de; ortak component ve token sözleşmesi mevcut.

### G1 — Backend solution ve local persistence skeleton

Solution’a şu projeler eklenecektir: `FactoryErp.Application`, `FactoryErp.Infrastructure`, `FactoryErp.Api`, `FactoryErp.Migrator` ve Worker ihtiyacı kesinleştiğinde `FactoryErp.Worker`. Test tarafına Application unit, Persistence integration, API integration, Contract ve Security test project’leri eklenecektir.

Infrastructure’da `FactoryErpDbContext`, entity configuration sınıfları, private backing-field mapping, UTC timestamp policy, `row_version bigint` trigger, migration history, seed version ve Npgsql connection health oluşturulacaktır. Local Compose yalnızca development database için port bind edebilir; production Compose PostgreSQL’i host public interface’e açmayacaktır.

**Test kapısı:** Domain/Application build, architecture dependency matrix, clean PostgreSQL migration, seed idempotency, health live/ready/startup.

### G2 — Identity, permission, audit ve ortak API davranışı

Kullanıcı, rol, permission policy, JWT/refresh token, correlation ID, structured logging, audit context, idempotency key + payload hash, typed ProblemDetails ve ETag/If-Match ortak altyapı olarak tamamlanacaktır. `ValidationBehavior → AuthorizationBehavior → IdempotencyBehavior → TransactionBehavior → AuditBehavior` sırası bütün kritik command’lerde aynı kalacaktır.

**Test kapısı:** Yetkisiz kullanıcı işlemi reddedilir; IDOR/BOLA testleri geçer; aynı idempotency payload tekrarında ikinci business movement oluşmaz; farklı payload mismatch üretir; concurrency ve typed error mapping korunur.

### G3 — Ürün, packaging, barkod, üretimden stoğa ve public katalog

Ürün master, kategori, temel UOM, packaging hierarchy, fiziksel profil, barcode ve image metadata kodlanacaktır. Üretim iş emri, makine, personel time record, fire/duruş ve finished-good receipt ile stok girişi bu fazda tamamlanacaktır. Public katalog ürünleri yalnızca minimum public DTO ile gösterecek; teklif talebi iç ERP’ye kontrollü bir kayıt olarak düşecektir.

Web’de önce `web-dashboard.png` shell’i, sonra ürün kart/table, ürün detay drawer’ı, packaging hierarchy ve public catalog/cart ekranı kodlanacaktır. Mobile’da önce giriş bağlantısı, görev özeti ve `mobile-barcode-quantity.png` akışı yapılacaktır.

**Test kapısı:** QTY-001–007, snapshot immutability, barcode resolution, production receipt, stock ledger, public DTO isolation, quote request rate/bot/consent rules.

### G4 — Depo, stok, transfer, sayım ve barkod operasyonu

Stock balance projection, stock movement ledger, warehouse/location, reservation projection, transfer, count session, count difference, adjustment request ve barcode operation command/query’leri eklenecektir. `Temel Birim / Ambalaj / Kırılım` toggle’ı UI davranışı olarak kalacak; server-side `quantity_base` tek gerçek ledger değeri olacaktır.

Web’de `web-warehouse-logistics.png` içindeki stok table, filter, capacity/quick actions; mobile’da scanner → result → action sheet flow aynı component sözleşmesiyle uygulanacaktır.

**Test kapısı:** Negative stock policy, count difference approval, transfer source/target, reserved/available projection, barcode packaging conversion, permission ve concurrent adjustment tests.

### G5 — Public teklif → iç satış → sipariş ve sorumlu onayı

Public talep, satış inceleme, müşteri oluşturma, teklif hazırlama, tekliften sipariş taslağı, onaya gönderme, risk özeti ve sorumlu onayı kodlanacaktır. Reddetmede açıklama zorunlu, onayda stok/ödeme/tarih/risk özeti görünür olacak; onay sonrası reservation başlangıcı transaction ile bağlanacaktır.

Web’de `web-order-detail.png` pixel referansı kullanılarak stepper, tabs, ürün kalemleri, temel miktar görünümü, approval summary ve critical modal uygulanacaktır. Public’te `public-catalog-quote.png` cart drawer ve quote-only metni korunacaktır.

**Test kapısı:** State transition, approval permission, risk soft/hard block, reservation, approval audit, public/internal isolation, idempotent submit ve rollback.

### G6 — İrsaliye, kısmi sevkiyat, sevkiyat ve kargo planı

Approved order’dan delivery note taslağı, quantity recheck, allocation, stock consume, issue, reversal ve backorder kodlanacaktır. Sevkiyat entity’si, araç/şoför atama, mixed pallet, load unit, vehicle capacity evaluation, route stop, package trace, delivery proof ve manual plan lock eklenecektir.

İlk MVP lojistik algoritması; ağırlık, hacim, palet adedi, ölçü ve istifleme için hard validation + manuel düzenlenebilir öneri verecektir. Otomatik öneri optimal çözüm olarak sunulmayacak. `web-warehouse-logistics.png` drawer ve route board, `mobile-production-delivery.png` stop/proof akışının pixel referansı olacaktır.[4]

**Test kapısı:** `ALLOC-001`–`ALLOC-003`, over-allocation, over-shipment, two-connection PostgreSQL concurrency, row-version conflict, capacity mismatch, lock-plan versioning, partial delivery/reversal, proof and exception audit.

### G7 — Fatura, cari, ödeme ve risk

Issued delivery allocation’dan invoice draft/issue, price/tax snapshot, invoice item allocation, current account debit, payment apply, payment type, balance projection, statement, aging ve risk calculation kodlanacaktır. Invoice issue stock movement üretmeyecek; credit/reversal yeni kayıt ve referans ile ilerleyecektir.

Web’de invoice allocation, current statement, payment modal, risk listesi ve report filter ortak component atlasıyla uygulanacaktır. Maaş veya finansal alanlarda API projection ve permission masking birlikte kullanılacaktır.

**Test kapısı:** Partial invoice, over-invoicing, invoice idempotency, no-stock-movement, debit/credit balance, payment duplicate, payment reversal, aging/risk, financial permission and audit/outbox transaction tests.

### G8 — Sevkiyat mobile, personel, bildirim ve MVP raporları

Mobile’da shipment task list, package barcode verification, loading completion, stop delivery, partial delivery, delivery proof ve exception flow tamamlanacaktır. Personel tarafında employee, attendance, leave, overtime, work duration ve controlled payroll summary; bildirim tarafında task/deep-link; raporlarda MVP için sabit filtreli liste/grafik/export yolları eklenecektir.

**Test kapısı:** Mobile API contract, permission-aware actions, retry/offline banner, leave approval, work-duration totals, report filter equality between chart/table, export authorization and notification deep-link.

### G9 — Web/public/mobile UI pixel implementation freeze

Backend vertical slice’ları kullanılabilir hale geldikçe UI reference’lar feature feature kodlanacaktır. Önce `AppShell`, `PublicShell`, `MobileShell`, tokens, typography, status, table, drawer, modal, toast, empty/error/permission states; sonra dashboard → products → order → warehouse → logistics → finance → production → public → mobile kritik yolları uygulanacaktır.

Her ekranın 1440×900 veya mobile artboard screenshot’ı alınarak baseline PNG ile karşılaştırılacaktır. Bir component referanstan farklı görünüyorsa sayfa özelinde düzeltme yapılmayacak; ortak component veya token düzeltilecektir.

**Test kapısı:** Responsive smoke, component state coverage, keyboard/focus/contrast, mobile touch target, Turkish copy, permission visibility, screenshot regression ve public/internal route separation.

### G10 — CI/CD, Docker Compose ve production candidate

PR pipeline’da `git diff --check`, format/static analysis, backend build, unit, PostgreSQL integration, migration, API contract, security, Next.js check/test ve Flutter analyze/test çalışacaktır. Main build’i testlerden sonra API/web/worker image’larını immutable SHA tag’iyle üretecek, SBOM/vulnerability gate’i uygulayacaktır.

Production Compose reverse proxy, web, API, PostgreSQL, worker ve backup profillerinden oluşacaktır. Release sırası backup freshness → image digest → controlled migrator → schema/seed → API/worker/web → health/ready → login/public/mobile smoke şeklinde olacaktır. Migration API startup’ında otomatik çalıştırılmayacaktır.[5]

**Test kapısı:** Clean host Compose smoke, private PostgreSQL network, LAN HTTPS, backup checksum/retention, isolated restore, migration 0001–0018, health endpoints, login/public/mobile smoke, rollback/forward-fix evidence.

## 5. UI uygulama önceliği

UI tüm ekranları aynı anda yapılmayacak; kullanıcıya en erken değer veren ve geri kalan component’leri doğrulayan sırayla ilerleyecektir.

| UI sıra | Kodlanacak set | Başarı ölçütü |
|---:|---|---|
| 1 | Internal shell + dashboard | Sidebar/topbar/KPI/task/risk reference ile eşleşir |
| 2 | Product card/table/detail + packaging | Product master ve quantity semantics doğru |
| 3 | Public catalog + quote cart | Public/internal ayrımı ve quote-only akış doğru |
| 4 | Order list/detail/approval | Tabs, stepper, modal, status ve base quantity doğru |
| 5 | Warehouse stock/barcode/count | Toggle/filter, scan result ve action sheet doğru |
| 6 | Delivery/load-plan/route/package | Capacity, drawer, route stop ve proof doğru |
| 7 | Invoice/current/payment/report | Ledger summary, allocation, risk ve export doğru |
| 8 | Production/mobile/personnel | Kanban, production form, mobile field density doğru |

Bu sıra, tasarım referanslarının “screenshot üretip bırakılması” yerine her referansı çalışan business contract ile bağlar. UI yalnızca backend’den gelen yanlış veriyi güzelleştirmeyecek; invalid state ve permission durumlarını görünür şekilde ele alacaktır.

## 6. Test piramidi ve MVP DoD

MVP’de Domain unit testleri en geniş, Application unit testleri handler sırasını, PostgreSQL integration testleri persistence/concurrency davranışını, API integration testleri HTTP contract’ını, security testleri permission/isolation’ı, E2E smoke ise kritik kullanıcı yolunu kanıtlayacaktır.[6]

| DoD alanı | MVP kabul koşulu |
|---|---|
| Business flow | Public tekliften cari ödemeye kadar happy path çalışıyor |
| Production flow | İş emri → üretim kaydı → finished-good receipt → stok çalışıyor |
| Quantity | Positive/non-negative, packaging, base quantity ve precision testleri yeşil |
| Allocation | Partial shipment/invoice, over-allocation, reversal ve concurrency yeşil |
| Security | RBAC, IDOR/BOLA, public DTO isolation, secret masking yeşil |
| UI | Reference PNG’lerle pixel comparison; ortak component contract’a uyum |
| Mobile | Barkod, üretim, sevkiyat/durak ve proof smoke çalışıyor |
| Database | Migration temiz DB’de, seed idempotent, trigger/constraint çalışıyor |
| Operations | Compose, health, backup/restore, LAN HTTPS ve network isolation kanıtlı |
| Release | Immutable image digest, release approval ve evidence kaydı mevcut |

## 7. Commit ve teslim kapıları

| Gate | Önerilen commit konusu | Teslim kararı |
|---|---|---|
| G1 | `implementation: add application infrastructure api and migrator skeleton` | Persistence slice başlatılabilir |
| G2 | `implementation: add identity permissions and shared api behaviors` | Güvenli internal route başlatılabilir |
| G3 | `implementation: add product packaging stock and catalog vertical slice` | Ürün/katalog kabulü |
| G4 | `implementation: add quote order approval and reservation flow` | Satış acceptance |
| G5 | `implementation: add delivery shipment invoice and current account flow` | Financial/fulfillment acceptance |
| G6 | `implementation: add production warehouse logistics and mobile operations` | Shop-floor/dispatch acceptance |
| G7 | `implementation: add personnel reporting and notifications` | Operational completeness |
| G8 | `release: add compose ci cd backup and mvp acceptance evidence` | Production candidate |

Her gate sonrası `dotnet build`, ilgili `dotnet test`, PostgreSQL integration, frontend/mobile checks ve `git diff --check` çalıştırılacaktır. Yeşil olmayan gate commit’i MVP’ye ilerleme kanıtı sayılmayacaktır.

## 8. Hemen sonraki kodlama adımı

İlk gerçek kodlama adımı **G1**’dir: Application/Infrastructure/API/Migrator project’lerinin eklenmesi, EF Core/Npgsql persistence skeleton’ı, local PostgreSQL Compose, ilk migration/seed ve health endpoint’leri. UI tarafında aynı sprintte yalnızca mevcut reference’lara bağlı `AppShell`, token import ve dashboard/product catalog shell’leri hazırlanacaktır.

Böylece backend transaction ve veri modeli oturmadan tüm ekranlar sahte mock data ile çoğaltılmayacak; buna karşılık UI referansları da kaybolmadan gerçek API contract’larıyla birlikte gelişecektir.

## References

[1]: ./implementation-domain-slice.md "İlk Domain implementation slice"
[2]: ./implementation-and-production-build-plan.md "Implementation, UI ve production build roadmap"
[3]: ./aspnet-clean-architecture-and-cqrs.md "ASP.NET Clean Architecture ve CQRS blueprint’i"
[4]: ./shipment-logistics-ui-design.md "Sevkiyat ve lojistik UI tasarımı"
[5]: ./docker-compose-deployment-plan.md "Docker Compose ve on-prem deployment baseline"
[6]: ./mvp-test-strategy.md "MVP test stratejisi ve acceptance gate’leri"
