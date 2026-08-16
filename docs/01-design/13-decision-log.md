# Factory ERP — Design Decision Log

**Aşama:** ARCHITECTURE ACCEPTED → IMPLEMENTATION SCAFFOLD
**Kapsam:** Kodlama öncesi tutarlılık, source of truth ve açık karar yönetimi

## 1. Karar sınıfları

| Sınıf | Anlam |
|---|---|
| DECIDED | Mevcut tasarım ve kullanıcı gereksinimiyle yeterince netleşmiş karar |
| ASSUMED | Kodlamayı durdurmadan ilerlemek için alınan, ileride değiştirilebilir varsayım |
| OPEN DECISION | Domain, maliyet, entegrasyon veya operasyon kararını doğrudan etkilediği için proje sahibi tarafından karara bağlanması gereken konu |

## 2. DECIDED

Aşağıdaki O-001–O-014 kararları, proje sahibinin **“hepsini kabul edildi”** beyanı üzerine 2026-08-16 tarihinde kabul edilmiş karar olarak kayda alınmıştır. Karar sahibi kaydı `Proje sahibi`; ilgili iş sahipleri uygulama ayrıntılarının Architecture aşamasında doğrulanacaktır. Bu kayıt, önceki öneri belgelerinin seçilen MVP değerine dönüştüğünü gösterir.

| Kabul kaydı | Değer |
|---|---|
| Karar sahibi | Proje sahibi |
| Karar tarihi | 2026-08-16 |
| Karar kapsamı | O-001–O-014 P0/P1 tavsiye paketi |
| Gerekçe | Şirket içi ERP-Lite MVP’sini kontrollü kapsamla Architecture aşamasına geçirmek |
| Kanıt | Kullanıcının açık kabul beyanı; `p0-p1-decision-recommendations.md` öneri paketi |
| Sonraki adım | Etkilenen artefact’ların yayılım kontrolü ve `READY FOR ARCHITECTURE` gate değerlendirmesi |

| ID | Karar | Gerekçe |
|---|---|---|
| D-001 | Sistem şirket içi kullanılacak merkezi ERP-lite olacaktır. | Web, mobil ve public yüzeyler aynı merkezi veri modelini kullanmalıdır. |
| D-002 | İlk teknik mimari modüler monolith + REST API + PostgreSQL yaklaşımıdır. | Erken aşamada mikroservis karmaşıklığına ihtiyaç yoktur. |
| D-003 | UI Türkçe, entity/property/API isimleri İngilizce olacaktır. | Kullanıcı operasyonu Türkçe; kod sözleşmeleri tutarlı ve taşınabilir olmalıdır. |
| D-004 | Public katalog iç ERP’den ayrı bir deneyimdir. | Dış müşteriye iç maliyet, risk, stok ve operasyon bilgisi gösterilmemelidir. |
| D-005 | `Product` ürün ana kaynağıdır. | Ürün adı, kodu, birimi ve katalog özellikleri tek yerde tutulur. |
| D-006 | `ProductBarcode` barkodların ana kaynağıdır. | Bir ürünün birden fazla barkodu olabilir; barkod ürün kartına bağlıdır. |
| D-007 | `Stock` mevcut özet stok; `StockMovement` değişmez stok geçmişidir. | Stok miktarı sessiz UI güncellemesiyle değişmemelidir. |
| D-008 | `Customer` müşteri ana kaynağıdır. | Müşteri farklı modüllerde bağımsız kopyalanmayacaktır. |
| D-009 | `CurrentTransaction` cari hareketlerin; `Payment` ödeme işleminin kaynağıdır. | Bakiye transaction'ların sonucu olmalıdır. |
| D-010 | `SalesOrder → DeliveryNote → Shipment → Invoice` belge zinciri korunacaktır. | Her operasyon bir önceki belge ve bir sonraki işlemle izlenebilir olmalıdır. |
| D-011 | Kritik stok, finans ve yetki hareketleri audit log üretmelidir. | Kim, ne zaman, hangi kaydı, hangi eski/yeni değerle değiştirdiği bilinmelidir. |
| D-012 | İptal veya ters kayıt, kritik kayıtları fiziksel silmeye tercih eder. | Finansal ve fiziksel geçmiş kaybolmamalıdır. |
| D-013 | Mobil öncelikleri barkod, stok, sayım, transfer, sevkiyat ve üretim kaydıdır. | Mobil saha kullanıcısının görevleri masaüstü rapor ekranlarından farklıdır. |
| D-014 | Büyük listelerde server-side pagination, arama, filtreleme ve sıralama vardır. | ERP operasyonlarında veri hacmi arttığında tarayıcıya tüm tablo çekilmemelidir. |
| D-015 | Tasarım tamamlanmadan implementasyon başlatılmayacaktır. | Bootstrap promptu DISCOVER → DESIGN aşamasını açıkça sınırlar. |

## 3. ASSUMED

| ID | Varsayım | Etkisi / revizyon koşulu |
|---|---|---|
| A-001 | İlk sürüm tek şirketlidir; multi-company tenant modeli tasarlanmaz. | İleride `company_id` eklenebilecek sınır korunur. |
| A-002 | Birden fazla depo ilk sürümden desteklenir. | Depo, konum, transfer ve stok sorgusu buna göre modellenir. |
| A-003 | Üretim tamamlanması, tanımlı bitmiş ürün miktarı için stok girişi üretir. | Ara üretim veya kalite karantinası kararı netleşirse akış genişletilir. |
| A-004 | Public katalog fiyat ve stok miktarı göstermeden teklif talebi toplar. | B2B fiyat listesi politikası kesinleşirse public deneyim güncellenir. |
| A-005 | Sipariş onayı en az bir sorumlu kullanıcının kararıdır. | Tutar/departman bazlı kademeli onay gelirse approval policy gerekir. |
| A-006 | İlk sürüm lot/seri takibi gerektirmez; ürün miktarı stok seviyesinde izlenir. | Gıda, kalite veya mevzuat gereği lot gerekiyorsa açık karar olarak işlenmelidir. |
| A-007 | İlk sürüm BOM/reçete kapsamı sınırlıdır; üretim gerçekleşmesi doğrudan bitmiş ürün stoğuna bağlanır. | Hammadde tüketimi ve reçete ihtiyaçları netleşirse ProductionMaterial ve BOM derinleştirilir. |
| A-008 | Mobil ağ kesintisinde stok ve finans işlemleri sessizce commit edilmez. | Offline güvenli kuyruk ancak idempotency ve conflict tasarımı sonrası ele alınır. |
| A-009 | Maaş modülü kayıt ve rapor kapsamındadır; tam yasal bordro motoru değildir. | Harici bordro entegrasyonu gerekirse adapter sözleşmesi eklenir. |
| A-010 | Belge numaraları yıllık prefix ve transaction-safe sequence ile üretilir. | Şirket belge politikası değişirse numaralandırma ayarı güncellenir. |

## 4. RESOLVED ACCEPTED DECISIONS — O-001–O-014

| ID | Kabul edilen MVP değeri | Karar sahibi | Tarih | Ana etki |
|---|---|---|---|---|
| O-001 | Vergi kodu/oran/geçerlilik ve belge snapshot’ı; `IInvoiceIntegrationService` adapter/stub; gerçek e-belge entegrasyonu sonraki sınır; fiziksel silme yok, reversal/credit | Proje sahibi; muhasebe/mali müşavir doğrulaması Architecture’da | 2026-08-16 | Fatura, cari, vergi ve e-belge API sınırı |
| O-002 | Kısmi sevkiyat açık; kalem seviyesinde allocation; bir siparişten çoklu irsaliye; remainder/backorder aynı kalemde; reversal/return ile düzeltme | Proje sahibi; satış/depo doğrulaması Architecture’da | 2026-08-16 | SalesOrder, reservation, DeliveryNote, shipment state |
| O-003 | Kısmi fatura açık; yalnızca `DeliveryNote.Issued` ve sevk edilmiş miktar allocation’ı; çoklu fatura; cari debit yalnızca `Invoice.Issued`; fatura stok hareketi üretmez | Proje sahibi; muhasebe doğrulaması Architecture’da | 2026-08-16 | Invoice allocation, current account, reversal/credit |
| O-004 | BOM/hammadde MVP dışında; üretim emri, makine/personel gerçekleşmesi, fire/duruş ve finished-good receipt MVP’de | Proje sahibi; üretim/maliyet doğrulaması Architecture’da | 2026-08-16 | Production scope, stock receipt, future ProductionMaterial boundary |
| O-005 | Lot/seri MVP dışında; kalite/geri çağırma gereksinimi çıkarsa karar yeniden açılacak; stok ürün+depo+miktar düzeyinde | Proje sahibi; kalite/üretim doğrulaması Architecture’da | 2026-08-16 | Stock, traceability ve future lot migration boundary |
| O-006 | Public talep aktif müşteri oluşturmaz; `QuoteRequest → CustomerCandidate → sales review → Customer/Quote`; duplicate otomatik birleştirilmez | Proje sahibi; satış doğrulaması Architecture’da | 2026-08-16 | Public lead, customer master, approval |
| O-007 | Soft block + yetkili override; kritik eşiklerde hard block; override gerekçesi/audit; risk snapshot sipariş onayında | Proje sahibi; yönetim/muhasebe doğrulaması Architecture’da | 2026-08-16 | Risk score, order approval, permission/audit |
| O-008 | Puantaj/izin/mesai/maaş dönemi özeti + kontrollü export; yasal bordro motoru ve resmi beyan entegrasyonu MVP dışında | Proje sahibi; İK/muhasebe doğrulaması Architecture’da | 2026-08-16 | Salary visibility, masking, export audit |
| O-009 | Public katalog açık; minimum veri; rate limit, honeypot/CAPTCHA, doğrulama, consent ve saklama/silme; public API iç ERP detaylarını açmaz | Proje sahibi; hukuk/uyum doğrulaması Architecture’da | 2026-08-16 | Public API, KVKK, abuse controls |
| O-010 | Günlük full backup, ayrı disk/NAS, 14 gün retention, aylık restore; başlangıç hedefi RPO ≤ 24 saat, RTO ≤ 8 saat | Proje sahibi; sistem yöneticisi doğrulaması Operations’da | 2026-08-16 | Backup, restore, monitoring, operations |
| O-011 | Ubuntu LTS + Docker Compose + PostgreSQL + reverse proxy + şirket LAN HTTPS; public route ayrıştırılmış; iç endpoint’ler internete açılmaz | Proje sahibi; sistem yöneticisi doğrulaması Architecture/Operations’da | 2026-08-16 | Deployment, network, certificate, mobile access |
| O-012 | `PriceList + CustomerPriceGroup + ProductPrice`; quote/order/invoice price snapshot; MVP TRY; public fiyat varsayılan gizli | Proje sahibi; satış/yönetim/muhasebe doğrulaması Architecture’da | 2026-08-16 | Pricing, tax snapshot, permissions |
| O-013 | Tek production marka/asset manifest; logo, favicon, token, font ve lisanslı ürün görselleri; placeholder production’a taşınmaz | Proje sahibi | 2026-08-16 | Web, mobile, public catalog branding |
| O-014 | Hard constraint + First Fit Decreasing öneri + depo sorumlusu manuel onayı; soft warning override; optimal 3D/traffic/axle guarantee yok | Proje sahibi; depo/sevkiyat doğrulaması Architecture’da | 2026-08-16 | Vehicle fit, LoadPlan, manual replan, audit |

Kararlar kabul edilmiş olsa da ilgili iş sahiplerinin Architecture/Operations çıktılarında uygulama ayrıntılarını doğrulaması gerekir. Bu doğrulama kararın açık olduğu anlamına gelmez; kabul edilen kapsamın teknik sözleşmeye doğru aktarılmasıdır.

## 5. Karar netleştirme gündemi ve kapanış durumu

`decision-clarification-backlog.md` artık açık karar üretmek için değil, kabul edilen O-001–O-014 kararlarının alt sorularının kapanış kanıtını ve artefact yayılımını izlemek için kullanılır. Her madde için seçilen değer, karar sahibi, 2026-08-16 tarihi, gerekçe, etkilenen artefact’lar ve kabul kapsamı bu log’a işlendi. Açık karar sayısı: **0**.

## 6. ACCEPTED ARCHITECTURE DECISIONS — ADR-001–ADR-011

Proje sahibi, Architecture aşamasında kalan teknik kararlar için araştırma sonrası sunulan önerilerin tamamının kabul edilmesini istemiştir. Bu nedenle aşağıdaki ADR baseline’ı 2026-08-16 tarihinde kabul edilmiştir. Araştırma kanıtları ve ayrıntılı etkiler `architecture-decision-baseline.md` dosyasındadır.

| ADR | Kabul edilen değer | Kanıt/etki |
|---|---|---|
| ADR-001 | Positive işlem miktarı için immutable `Quantity`; zero-capable projection için `NonNegativeQuantity` veya decimal projection | Domain invariant ve quantity testleri |
| ADR-002 | Immutable `PackagingSnapshot`/`QuantitySnapshot`; server-side `quantity_base` hesaplama | Mobil/API/ledger/audit |
| ADR-003 | Private EF Core backing field, read-only collection, explicit Fluent API access mode | Aggregate encapsulation |
| ADR-004 | Public `row_version`/ETag; trigger ile monotonic bigint; Npgsql `xmin` public contract değil | API/EF/PostgreSQL concurrency |
| ADR-005 | Read Committed + deterministic `SELECT FOR UPDATE` + re-read + deferred allocation guard | Sevk/fatura yarış koşulları |
| ADR-006 | Validation/authorization/idempotency sonrası tek command transaction; business effects atomic | CQRS/stock/current ledger |
| ADR-007 | Same-domain side effect için in-process domain event; external çağrı transaction içinde yok | Domain/Application |
| ADR-008 | Aynı DB transaction’ında `outbox_messages`; commit sonrası worker ve idempotent consumer | Notification/external adapter |
| ADR-009 | Concurrency/deadlock/serialization/unique hatalarını typed ProblemDetails’a map et; retry fresh read ile sınırlı | API/UX/operations |
| ADR-010 | Production self-hosted runner private, protected environment, restricted runner group ve release-only | GitHub Actions security |
| ADR-011 | Architecture artefact’ları kabul edildi; implementation gate Domain + tests scaffold’u için açıldı | Implementation handoff |

Açık teknik karar sayısı: **0**. ADR veya O-ID değişirse ilgili karar yeniden açılır, etkilenmiş artefact’lar güncellenir ve gate yeniden değerlendirilir.

## 7. Karar yönetimi kuralları

Yeni bir kapsam veya karar değişikliği gelirse ilgili O-ID yeniden `OPEN DECISION` durumuna alınır, etkilenen domain/workflow/database/UI/skill/QA/operations artefact’ları belirlenir ve Design Gate yeniden değerlendirilir. Mevcut O-001–O-014 kararları `DECIDED` durumundadır; Architecture skill’i bu seçilmiş değerleri zorunlu teknik girdiler olarak tüketebilir. Karar sahibi onayı ve yayılım kanıtı olmadan yeni bir varsayım `DECIDED` yapılamaz.
