# Factory ERP-Lite — Kodlama, UI ve Production Build Yol Haritası

**Tarih:** 2026-08-16
**Durum:** Planlama ve implementation handoff
**Başlangıç noktası:** `d8ec3ad` — ilk Domain slice tamamlandı ve `main` branch’ine push edildi.

## 1. Hedef ve çalışma ilkesi

Bu plan, kabul edilmiş Domain slice’ından üretim ortamında çalışabilecek şirket içi ERP’ye geçişi tanımlar. Hedef; peçete üretimi, depo, satış, sevkiyat, fatura, cari, ödeme, personel, raporlama ve public teklif kataloğunu tek bir tutarlı ürün deneyimi altında birleştirmektir.

Kodlama **büyük bir seferde tüm modülleri yazmak** şeklinde ilerlemeyecek. Her modül; Domain kuralı, Application command/query, Infrastructure persistence, API contract, UI ekranı, test ve release evidence içeren küçük bir vertical slice olarak tamamlanacaktır. Domain katmanı yalnızca iş kurallarını taşıyacak; transaction, lock, EF mapping, HTTP ve deployment sorumlulukları dış katmanlarda kalacaktır.[1]

> UI hedefi yalnızca “güzel ekran” değildir. Kullanıcı, her ekranda hangi kaydı gördüğünü, hangi durumda olduğunu, bir sonraki işlemin ne olduğunu ve yetkisinin neye izin verdiğini tek bakışta anlamalıdır.

## 2. Sabit teknik hedef mimari

Backend tarafında kabul edilmiş temel stack korunacaktır: **ASP.NET Core Web API, .NET 8, EF Core ve PostgreSQL**. Uygulama modular monolith olarak başlayacak; bounded context sınırları proje ve klasör yapısında açık tutulacaktır. Web iç ERP ve public katalog için **Next.js**, saha operasyonları için **Flutter** kullanılacaktır. Şirket içi deployment Docker Compose üzerinde, reverse proxy arkasında ve PostgreSQL’in yalnızca internal data network’ünde çalışacaktır.[1] [2]

| Katman | Teknoloji / karar | İlk üretim sorumluluğu |
|---|---|---|
| Domain | C# / .NET 8, framework bağımsız | Quantity, state, allocation, reversal, domain event |
| Application | CQRS command/query, port/interface | Validation, authorization, idempotency, transaction orchestration |
| Infrastructure | EF Core, Npgsql, PostgreSQL | Mapping, migration, row version, lock/re-read, outbox |
| API | ASP.NET Core REST + OpenAPI | DTO, ProblemDetails, permission policy, ETag/If-Match |
| Internal web | Next.js + TypeScript | Desktop operasyon, dashboard, list/detail/workspace ekranları |
| Mobile | Flutter | Barkod, üretim kaydı, sayım, transfer, durak teslimatı |
| Public surface | Next.js public route | Ürün kataloğu, teklif sepeti ve talep gönderimi |
| Runtime | Docker Compose + reverse proxy | LAN HTTPS, internal routing, health, backup ve release |

Şu anki solution yalnızca Domain, Domain unit test ve Architecture test projelerini içeriyor. Bir sonraki kodlama adımında bu çekirdeğe `FactoryErp.Application`, `FactoryErp.Infrastructure`, `FactoryErp.Api`, `FactoryErp.Migrator` ve gerektiğinde `FactoryErp.Worker` eklenecek; web, mobile ve deployment klasörleri bundan sonra kendi release kapılarıyla ilerleyecektir.[3]

## 3. Kodlama sırası ve çıkış kriterleri

### Faz A — Platform ve persistence temeli

İlk uygulama kodlama fazı solution dependency yönünü kurar. Application, Domain’e; Infrastructure, Application ve Domain’e; API ise Application ve composition root için Infrastructure’a referans verecektir. Domain’in ASP.NET Core, EF Core, Npgsql veya PostgreSQL referansı alması architecture testini kıracaktır.[3]

Bu fazda PostgreSQL local development Compose profili, `FactoryErpDbContext`, private backing-field mapping, `row_version bigint` trigger’ı, migration runner, seed version ve health endpoint’lerinin iskeleti oluşturulacaktır. Migration API startup’ında otomatik çalıştırılmayacak; kontrollü migrator job’ı olarak işletilecektir.[2]

**Çıkış kriteri:** Temiz PostgreSQL container’ı ayağa kalkar, migration 0001’den ilk persistence setine sırayla uygulanır, seed ikinci çalıştırmada duplicate üretmez, `/health/live` ve `/health/ready` doğru ayrılır, Domain dependency testi yeşildir.

### Faz B — Kimlik, yetki ve ortak API sözleşmesi

JWT + refresh token, kullanıcı, rol, permission policy, audit context, correlation ID, ProblemDetails ve idempotency altyapısı ortak katman olarak tamamlanacaktır. UI tarafında kullanıcı yalnızca yetkili olduğu menü ve aksiyonları görecek; backend permission kontrolü her zaman ikinci ve gerçek güvenlik sınırı olacaktır.

**Çıkış kriteri:** Yetkisiz finans/stok/maaş aksiyonu API’de reddedilir, ProblemDetails içinde typed `errorCode` korunur, aynı idempotency key aynı payload ile aynı sonucu verir, farklı payload mismatch üretir, access log secret veya kişisel maaş verisi içermez.[4]

### Faz C — İlk operasyonel vertical slice: ürün, ambalaj, katalog

Ürün, kategori, barkod, temel UOM, `Palet → Koli → Paket → Temel Birim` packaging hierarchy ve quantity snapshot modeli kodlanacaktır. Internal ürün ekranı ile public katalog aynı ürün kaynağını kullanacak; public DTO stok, cari, risk, personel ve iç maliyet alanlarını içermeyecektir.

Bu faz UI tasarım sisteminin ilk gerçek doğrulama noktasıdır. Ürün kartı, ürün detay drawer’ı, ambalaj dönüşüm tablosu, görsel placeholder, barkod badge’i ve `Temel Birim / Ambalaj / Kırılım` toggle’ı birlikte uygulanacaktır.

**Çıkış kriteri:** `5 Koli (10.000 adet)` görünümü doğru hesaplanır, toggle ledger değerini değiştirmez, master packaging güncellense dahi geçmiş quantity snapshot değişmez, public teklif sepeti yalnızca teklif talebi üretir.

### Faz D — Satıştan sipariş onayına, irsaliyeden faturaya vertical slice

Kritik MVP senaryosu şu sırayla kodlanacaktır: public ürün seçimi → teklif talebi → iç satış incelemesi → sipariş → sorumlu onayı → stok rezervasyonu → kısmi irsaliye → sevkiyat → fatura → cari borç → ödeme → güncel bakiye. Bu sıra, kullanıcı arayüzünün iş akışını backend transaction sınırıyla aynı hizaya getirir.[5]

`ApproveOrder`, `IssueDeliveryNote` ve `IssueInvoice` handler’ları validation, authorization, idempotency, source re-read/lock, quantity recheck, domain mutation, stock/current ledger, audit ve outbox sırasını koruyacaktır. Fatura oluşturmak stok hareketi üretmeyecek; irsaliye allocation kaynağı `Issued` state ve kalan miktar ile doğrulanacaktır.[4]

**Çıkış kriteri:** Kısmi sevkiyat ve kısmi fatura gerçek PostgreSQL üzerinde iki bağlantılı concurrency testinden geçer; over-allocation, quantity base mismatch, stale row version, idempotency mismatch ve rollback davranışları typed error ile kanıtlanır.

### Faz E — Üretim ve depo operasyonları

Üretim iş emri, makine, üretilen miktar, fire, duruş, çalışan personel ve çalışma süresi kodlanacaktır. Üretim tamamlandığında finished-good receipt stok ledger’a yazılır; kabul edilen MVP kapsamı dışındaki BOM/lot/seri davranışları bu faza zorla eklenmez.[6]

Depo tarafında stok liste/detay, hareket, transfer, sayım, barkod ve sevkiyat hazırlama ekranları oluşturulacaktır. Barkoddan sonra kullanıcıya `Stok Görüntüle`, `Transfer Başlat`, `Sayım Yap` ve yetkisi varsa `Düzeltme Talebi` aksiyonları sunulacaktır.

**Çıkış kriteri:** Stok projection’ları temel birimde tutarlı, ambalaj görünümü doğru, negatif stok ve yetkisiz düzeltme engelli, üretim receipt ile stok hareketi audit kaydıyla birlikte oluşur.

### Faz F — Sevkiyat, karışık palet, rota ve araç kapasitesi

Araç tipi, kapasite, doluluk, net/brüt ağırlık, hacim, palet sayısı, yük birimi, karışık palet, rota durağı, paket barkodu ve teslim kanıtı ayrı state’lerle kodlanacaktır. İlk sürüm otomatik planlama önerisi sunabilir ancak manuel depo onayı olmadan planı kilitlemeyecektir.

UI; kapasite özeti, sevkiyat kalemleri, yük birimleri, rota/durak panosu, paket izleme drawer’ı ve teslim proof panelini aynı çalışma alanında gösterecektir. Ağırlık, hacim, palet, ölçü ve istifleme kontrolleri ayrı uyarılar olarak sunulacaktır.[7]

**Çıkış kriteri:** Araç uygunluğu hard validation’dan geçer, plan kilitlenince yeni versiyon/audit oluşur, paket barkodu müşteri/adres/durak durumuna ulaşır, kısmi teslim ve teslim edilememe ayrı event/state olarak saklanır.

### Faz G — Cari, ödeme, personel ve raporlar

Fatura issue ile cari borç, ödeme apply ile cari alacak ve bakiye projection’ı oluşturulacaktır. Cari ekstrede belge, borç, alacak ve bakiye satırları immutable ledger mantığıyla gösterilecektir. Risk analizi gecikmiş ödeme ve açık bakiye üzerinden soft/hard block kurallarına bağlanacaktır.

Personel modülünde puantaj, izin, mesai, iş devam ve kontrollü maaş özeti yer alacaktır. Maaş detayının rol bazlı maskelenmesi UI görünürlüğüne bırakılmayacak; API policy ve response projection ile de korunacaktır.

**Çıkış kriteri:** Finansal işlem duplicate apply edilemez, reversal yeni kayıt olarak oluşur, cari ekstre hesaplanabilir ve audit/event/outbox kayıtları transaction ile birlikte commit edilir.

## 4. UI kalite sözleşmesi — “resimdeki gibi” hedefi

Mevcut kabul edilmiş görsel yön **açık yüzey + derin lacivert navigasyon + teal ana aksiyon** sistemidir. Arayüz bilgi yoğun ancak sakin olacaktır; amber bekleyen/dikkat, kırmızı kritik, yeşil tamamlandı ve gri pasif durumları temsil edecektir. Renk tek başına anlam taşımayacak, her badge’de Türkçe metin ve gerektiğinde ikon bulunacaktır.[8]

### 4.1 Ortak görsel omurga

Masaüstü iç ERP’de daraltılabilir sol menü, üstte global arama/bildirim/kullanıcı alanı, breadcrumb, sayfa başlığı, KPI satırı, filtre çubuğu, yoğun veri tablosu ve sayfalama aynı zihinsel modeli koruyacaktır. Detay ekranlarında belge numarası + durum, özet kartları, sekmeler, aktivite timeline’ı ve sağ işlem paneli ortak şablon olacaktır.[9]

| UI alanı | Uygulanacak kalite standardı |
|---|---|
| Renk | Açık zemin, derin lacivert shell, teal primary; kritik durumlarda metin + ikon |
| Tipografi | Sayfa başlığı 28–32 px; tablo 13–14 px; yardımcı metin 12–13 px; sayısal KPI baskın |
| Navigation | Yetkiye duyarlı sidebar; aktif route belirgin; modül rozetleri bekleyen işleri gösterir |
| Data table | Sabit başlık, hızlı filtre, yoğun ama taranabilir satır, satır aksiyonu ve pagination |
| Form | Girilen miktar + giriş birimi + temel karşılık üçlüsü; kritik işlemlerde açık teyit |
| Quantity | `Temel Birim / Ambalaj / Kırılım` görünüm toggle’ı; `Tümü / Palet / Koli / Paket / Temel Birim` filtresi |
| Status | Türkçe metin, renk, ikon, mümkünse sonraki işlem etiketi |
| Feedback | Loading skeleton, empty state, filter-empty, error-retry, success-next-step ve permission state |
| Responsive | Mobilde barkod/üretim aksiyonu başparmak erişiminde; desktop’ta kritik bilgiler tek ekranda |
| Accessibility | Kontrast, keyboard focus, görünür hata, label, touch target ve reduced-motion desteği |

### 4.2 Uygulama yöntemi

UI kodlaması ekran ekran rastgele başlamayacaktır. Önce design token’lar, layout shell, typography, button/input/select, table, status badge, KPI, drawer, modal, timeline, stepper, quantity toggle, packaging filter, capacity summary ve load-unit card bileşenleri oluşturulacaktır. Daha sonra dashboard, order detail, product catalog ve mobile barcode ekranları referans doğrulama seti olarak tamamlanacaktır.

Kullanıcının bahsettiği belirli görsel mevcut çalışma alanında bulunamadı; repository’de görsel yön ve mockup referans yolları mevcut ancak PNG/JPG dosyaları bu checkout’ta yoktur. Bu nedenle ilk UI implementation kabul edilmiş görsel sistem ile başlayacaktır. Pixel-level eşleştirme isteniyorsa, ilgili resmi kodlama öncesinde tekrar yüklemek gerekir; referans geldiğinde yalnızca palette değil, spacing, hierarchy, card density, shell proportions ve component states de onun üzerinden sabitlenecektir.

### 4.3 UI ekran uygulama sırası

| Sıra | Ekran seti | Görsel doğrulama amacı |
|---:|---|---|
| 1 | Internal dashboard + sidebar shell | Marka, yoğunluk, KPI ve navigation dili |
| 2 | Ürün kataloğu + ürün detay | Kart/table geçişi, fotoğraf, packaging hierarchy |
| 3 | Sipariş detay + onay paneli | Stepper, state, quantity ve approval summary |
| 4 | Depo stok + barkod sonucu | Toggle, filtre, hızlı aksiyon ve kritik stok |
| 5 | İrsaliye + fatura + cari detay | Belge ilişkisi, finansal özet ve audit timeline |
| 6 | Üretim iş emri + üretim kaydı | Progress, makine, personel ve süre girişi |
| 7 | Kargo planı + rota panosu + paket izleme | Capacity summary, load unit ve route stop list |
| 8 | Mobil barkod + miktar + durak teslimatı | Kamera akışı, thumb reach, proof ve exception |
| 9 | Public katalog + teklif sepeti | Internal ERP’den farklı ama aynı marka ailesinde public deneyim |

Her ekran için önce loading, empty, error, permission denied, success ve critical confirmation durumları tasarlanacak; yalnızca happy path ekran görüntüsü kabul edilmeyecektir.

## 5. Build, CI/CD ve production release planı

### 5.1 Pull Request kapısı

Her PR’da repository guard, secret/generated-artifact taraması, `git diff --check`, .NET format/static analysis, backend build, Domain/Application unit, PostgreSQL migration/integration, API contract/security, Next.js type/lint/test ve Flutter analyze/test çalışacaktır. Build artifact’ları ve test evidence saklanacak; kırmızı gate ile merge yapılmayacaktır.[10]

### 5.2 Main branch build’i

`main` yalnızca yeşil PR kontrolleri sonrasında Docker image üretir. API, web ve worker image’ları multi-stage build ile üretilir; production image `latest` yerine Git SHA veya semver release tag’i ile immutable olarak etiketlenir. Image digest, test kanıtları, migration manifest ve commit SHA release metadata içinde birlikte tutulur.[10]

### 5.3 Production release

Production deployment şirket içi private self-hosted runner veya kontrollü internal pull/release mekanizması ile yapılacaktır. GitHub-hosted runner doğrudan LAN’a erişmeyecek; production runner protected environment ve required reviewer ile sınırlandırılacaktır. Release sırası backup freshness → image digest doğrulama → kontrollü migrator → schema/seed check → API/worker → web/reverse proxy → health/login/public/mobile smoke şeklinde ilerleyecektir.[10] [2]

| Release adımı | Başarısızlıkta davranış |
|---|---|
| Backup freshness | Release durur; eski backup ile devam edilmez |
| Migration | API deploy edilmez; destructive rollback yerine restore/forward-fix |
| Image pull/digest | Uyumsuz veya izlenemeyen image çalıştırılmaz |
| Health/ready | Traffic açılmaz; önceki uyumlu image korunur |
| Smoke test | Release başarısız işaretlenir; evidence kaydedilir |
| Deployment sonrası | Worker backlog, DB schema ve reverse-proxy route yeniden doğrulanır |

### 5.4 Docker Compose production topology

Production Compose; reverse proxy, Next.js web, ASP.NET Core API, PostgreSQL, worker ve backup profillerinden oluşacaktır. Dışarıya yalnızca reverse proxy’nin 80/443 portları açılacak; PostgreSQL 5432, web 3000 ve API 8080 host public interface’ine bind edilmeyecektir. Database, internal data network’ünde; backup hedefi PostgreSQL volume’undan ayrı tutulacaktır.[2]

API startup’ında migration çalıştırılmayacaktır. `postgres healthy → migration applied → seed verified → api ready → web ready → reverse-proxy smoke → mobile LAN smoke` zinciri release acceptance’ın parçası olacaktır. `/health/live`, `/health/ready` ve `/health/startup` endpoint’leri password, connection string veya stack trace döndürmeyecektir.[2]

## 6. Test piramidi ve Definition of Done

İlk Domain slice hızlı ve deterministik bir temel sağlıyor. Sonraki slice’larda testler aşağıdaki dağılım ile ilerleyecektir: Domain unit en geniş katman; Application unit handler sırası ve permission; PostgreSQL integration mapping/constraint/transaction/concurrency; API integration route/DTO/auth/ProblemDetails; contract test web-mobile-public uyumu; E2E smoke kritik kullanıcı yolu; deployment acceptance ise Compose, health, backup/restore, LAN HTTPS ve network isolation.[11]

Bir vertical slice ancak aşağıdaki koşullarda tamamlanmış sayılacaktır:

| Kontrol | Kabul koşulu |
|---|---|
| Domain | İş kuralı ve negatif senaryo unit test ile kanıtlı |
| Application | Validation → authorization → idempotency → transaction sırası kanıtlı |
| Persistence | Migration, mapping, FK/check/index/trigger ve rollback kanıtlı |
| API | DTO, ProblemDetails, permission, ETag ve idempotency contract testi yeşil |
| UI | Loading/empty/error/permission/success/critical states hazır; Türkçe metinler tamam |
| Mobile | Barkod/üretim/durak akışı gerçek cihaz veya emülatörde smoke edilmiş |
| Security | IDOR/BOLA, secret leakage, public isolation ve rate limit testleri yeşil |
| Release | Docker build, image digest, health, smoke, backup freshness ve evidence mevcut |

## 7. Uygulama takvimi ve yönetim kontrol noktaları

Takvim takvim günü yerine **kanıt üreten sprint kapıları** ile yönetilecektir. Her kapıda önce test/acceptance kriteri yazılacak, sonra kodlanacak, ardından bir sonraki kapıya geçilecektir.

| Kapı | Teslim | Yönetim onayı |
|---|---|---|
| G1 | Backend project scaffold + local PostgreSQL + architecture gate | Persistence başlangıcı |
| G2 | Product/packaging/catalog API + ilk UI reference screen | UI system freeze |
| G3 | Order approval/reservation + public quote flow | Sales flow acceptance |
| G4 | Delivery/invoice/current/payment vertical slice | Financial control acceptance |
| G5 | Production/warehouse/barkod | Shop-floor acceptance |
| G6 | Logistics/vehicle/load-plan/route/mobile delivery | Dispatch acceptance |
| G7 | Personnel/reporting/notification | Operational completeness |
| G8 | Docker Compose, CI/CD, backup/restore, LAN HTTPS | Production candidate |

## 8. Hemen sonraki kodlama adımı

Bir sonraki uygulama adımı **G1** olacaktır. Önce `FactoryErp.Application`, `FactoryErp.Infrastructure`, `FactoryErp.Api` ve `FactoryErp.Migrator` project’leri solution’a eklenecek; ardından EF Core/Npgsql persistence, first migration, local Compose PostgreSQL, health endpoints ve architecture dependency matrix uygulanacaktır. UI tarafında aynı anda yalnızca dashboard shell, design tokens ve ilk ürün katalog referans ekranı hazırlanacaktır; diğer ekranlar ortak bileşenler doğrulanmadan çoğaltılmayacaktır.

Bu yol, hem backend riskini hem de UI tutarsızlığını erken görünür kılar. “Mükemmel UI” hedefi; son haftada renk değiştirerek değil, ilk shell’den itibaren aynı token, spacing, state, typography, responsive ve permission sözleşmesinin uygulanmasıyla sağlanacaktır.

## References

[1]: ./aspnet-clean-architecture-and-cqrs.md "ASP.NET Clean Architecture ve CQRS blueprint’i"
[2]: ./docker-compose-deployment-plan.md "Docker Compose ve PostgreSQL on-prem deployment planı"
[3]: ./implementation-domain-slice.md "İlk Domain implementation slice kanıtı"
[4]: ./architecture-api-contracts.md "ASP.NET Core API contract, error ve idempotency sözleşmesi"
[5]: ./web-ux-architecture.md "Web UX bilgi mimarisi ve uçtan uca ekran akışları"
[6]: ./mvp-test-strategy.md "MVP test stratejisi"
[7]: ./shipment-logistics-ui-design.md "Sevkiyat ve lojistik UI tasarımı"
[8]: ./visual-design-system.md "Görsel tasarım sistemi"
[9]: ./web-ux-architecture.md "Ortak liste, detay ve dashboard şablonları"
[10]: ./github-actions-cicd-plan.md "GitHub Actions CI/CD ve production release planı"
[11]: ./mvp-test-strategy.md "Test piramidi ve deployment acceptance"
