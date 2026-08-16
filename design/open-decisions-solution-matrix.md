# Factory ERP — O-001–O-014 Açık Karar Çözüm Matrisi

**Durum:** Proje yönetimi için öneri; karar sahibi onayı olmadan `DECIDED` sayılmaz.  
**Kullanım:** Her satır karar toplantısında seçilmeli veya gerekçeli biçimde revize edilmelidir.

> **Uyarı:** Vergi, e-belge, bordro, KVKK ve finansal kayıt önerileri teknik tasarım içindir; uygulamaya alınmadan önce mali müşavir, hukuk/uyum ve ilgili iş sahipleri tarafından doğrulanmalıdır.

## Önerilen kararlar

| ID | Konu | Önerilen MVP çözümü | Mimari / workflow etkisi | Karar sahibi | Karar verilmezse |
|---|---|---|---|---|---|
| O-001 | Vergi/VAT ve e-belge | Fatura domaininde vergi kodu, oranı ve hesaplama alanlarını hazırla; `IInvoiceIntegrationService` adapter’ını tanımla; ilk sürümde gerçek entegratör yerine test/stub sağlayıcı kullan. KDV oranlarını hard-code etme. | Invoice/InvoiceItem vergi alanları, vergi yuvarlama, e-belge durumları ve entegrasyon audit’i gerekir. | Muhasebe + mali müşavir | Fatura şeması ve entegrasyon sözleşmesi yeniden tasarlanır. |
| O-002 | Kısmi sevkiyat | **İzin ver.** `ordered_qty`, `reserved_qty`, `shipped_qty`, `remaining_qty` alanlarını kalem seviyesinde yönet; bir siparişten birden fazla irsaliye üret. | `PartiallyReserved`, `PartiallyShipped`, `Completed` state’leri; rezervasyon serbest bırakma ve idempotent sevk gerekir. | Satış + depo yöneticisi | Depo operasyonu tamamı hazır olmayan siparişte kilitlenir. |
| O-003 | Kısmi fatura | **İzin ver.** Fatura kalemi irsaliye kalemine miktar bazında bağlansın; aynı irsaliyenin farklı miktarları yalnızca faturalandırılmamış kalan kadar faturalanabilsin. | `invoiced_qty`, remaining-to-invoice, çoklu belge ilişkisi ve double-invoice kontrolü gerekir. | Muhasebe | Fatura ve irsaliye ilişkisi sonradan kırıcı migration ister. |
| O-004 | BOM/reçete ve hammadde | **MVP’de kapalı;** ancak `ProductionMaterial` için genişleme sınırı belgede korunmalı. İlk sürüm yalnızca gerçekleşen bitmiş ürün stoğa girişini işler. | Production completion yalnızca finished-good `StockMovement IN` üretir; hammadde ledger’ı MVP dışıdır. | Üretim sorumlusu | Hammadde tüketimi bekleniyorsa stok ve maliyet raporları eksik kalır. |
| O-005 | Lot/seri/parti | **MVP’de kapalı;** ürün + depo + miktar seviyesiyle ilerle. Kalite/geri çağırma ihtiyacı varsa lot decision’ı MVP’den önce açılmalı. | `Lot`, `SerialNumber`, expiry ve traceability tabloları ilk migration’a alınmaz; karar değişirse tüm stok ekranları etkilenir. | Kalite + üretim | Sonradan lot eklemek stok geçmişi ve migration açısından pahalıdır. |
| O-006 | Public talepten müşteri kartı | Public teklif talebi doğrudan aktif müşteri oluşturmasın. Satış kullanıcısı talebi incelesin, mevcut müşteriye bağlasın veya manuel onayla yeni müşteri oluştursun. | `QuoteRequest → CustomerCandidate/Customer → Quote` ayrımı; duplicate müşteri kontrolü ve audit gerekir. | Satış yöneticisi | Public spam’i doğrudan cari ve müşteri ana verisini kirletir. |
| O-007 | Risk algoritması ve blokaj | **Soft block önerilir:** risk uyarısı görünür, sipariş onayında yetkili override gerekir; hard block yalnızca yönetimce belirlenen kritik durumlarda uygulanır. | Risk snapshot, scoring run, override reason, permission ve audit gerekir. | Yönetim + muhasebe | Ya gereksiz operasyon blokajı ya da kontrolsüz risk oluşur. |
| O-008 | Maaş/bordro | MVP’de kayıt, puantaj bağlantısı, dönem özeti ve kontrollü export; yasal bordro hesap motoru ve beyan entegrasyonu kapsam dışı. | Maaş verisi için ayrı permission, hassas alan masking’i, export audit’i gerekir. | İK + muhasebe | Bordro beklentisi yanlış kurulursa kapsam ve uyum riski oluşur. |
| O-009 | Public erişim, rate limit, bot ve KVKK | Katalog açık; form endpoint’lerinde rate limit, bot/CAPTCHA veya honeypot, e-posta/telefon doğrulaması, minimum veri ve açık aydınlatma/onay metni kullan. | Public API ayrımı, abuse logları, privacy consent kaydı, veri saklama/silme politikası gerekir. | Yönetim + hukuk/uyum | Spam, veri güvenliği ve KVKK uyum riski oluşur. |
| O-010 | Backup/RPO/RTO | Günlük full backup + ayrı disk/volume; en az 14 gün retention; aylık restore testi. Kritik operasyon için RPO/RTO hedefi ayrıca yazılı onaylanmalı. | Backup job, başarı/başarısızlık bildirimi, restore runbook ve monitoring gerekir. | Sistem yöneticisi | Backup var sanılır fakat geri dönülemeyen veri kaybı yaşanabilir. |
| O-011 | Şirket serverı, LAN ve HTTPS | **Local-first kesinleştirilebilir;** işletim sistemi, reverse proxy ve sertifika seçimi ayrı teknik karar olarak kalmalı. Öneri: Ubuntu LTS + Docker Compose + Nginx/Traefik + şirket LAN HTTPS. | Deployment topology, DNS/sertifika, mobil erişim, firewall ve health check gerekir. | Sistem yöneticisi | Mobil/public erişim, güvenlik ve kurulum planı belirsiz kalır. |
| O-012 | Fiyat listesi ve müşteri grubu | `PriceList`, `CustomerPriceGroup`, `ProductPrice` modeli kullan; teklif/sipariş oluşunca uygulanan fiyatı snapshot olarak kilitle. Public katalog fiyat göstermez. | Price validity, currency/tax, customer mapping, quote/order snapshot ve permission gerekir. | Satış + yönetim | Fiyat geçmişi ve müşteri bazlı ticari koşullar izlenemez. |
| O-013 | Marka, logo, token ve görsel lisans | Kodlamadan önce tek marka adı, logo, favicon, renk token’ları, font ve ürün görseli lisans/placeholder politikası onaylansın. Geçici olarak nötr `Factory ERP` kullan. | Web/mobile/public theme token’ları, asset manifest, favicon ve public header etkilenir. | Proje sahibi + pazarlama | Mockup markaları production’a taşınır, UX ve hukuki risk oluşur. |
| O-014 | Kargo planlama otomasyon seviyesi ve araç eşleştirme | **MVP’de açıklanabilir uygunluk ön kontrolü + First Fit Decreasing sezgisel öneri + depo sorumlusu manuel onayı önerilir.** Hard constraint ihlali kilidi engellesin; soft warning açıklama/override ile ilerlesin. Optimal 3D packing, aks optimizasyonu ve kesin trafik rotası MVP dışında kalsın. | `VehicleFit`, `LoadPlan`, `LoadUnit`, `LoadUnitStopAllocation`, validation severity, algorithm/version snapshot, manual replan/audit ve yeni workflow state’leri gerekir. | Depo + sevkiyat yöneticisi | Otomasyon seviyesi netleşmezse UI, transaction, permission ve operasyon sorumluluğu yeniden tasarlanır. |

## Varsayılan karar paketi

Proje sahibi hızlı bir MVP kararı vermek isterse aşağıdaki paket önerilir:

```text
O-001  Adapter + stub; vergi alanları hazır, gerçek entegratör sonra
O-002  Kısmi sevkiyat açık
O-003  Kısmi fatura açık
O-004  BOM/hammadde MVP dışında
O-005  Lot/seri MVP dışında
O-006  Public talep → satış manuel müşteri onayı
O-007  Soft block + yetkili override
O-008  Kayıt + kontrollü export; yasal bordro yok
O-009  Açık katalog + rate limit + bot kontrolü + KVKK metni
O-010  Günlük full + 14 gün + aylık restore testi; RPO/RTO onayı ayrı
O-011  Local-first; Ubuntu/Docker/Reverse Proxy teknik seçimi Architecture’da
O-012  PriceList + CustomerPriceGroup; order snapshot
O-013  Tek marka ve asset politikası kodlamadan önce zorunlu
O-014  Heuristik araç/palet önerisi + hard constraint validation + manuel depo onayı; optimalite garantisi yok
```

Bu paket **öneridir**, karar sahibi onayı olmadan `decision-log.md` içinde `DECIDED` olarak işlenmemelidir.

## Karar kapatma prosedürü

Her karar için `decision-log.md` içinde şu bilgiler bulunmalıdır: seçilen değer, karar sahibi, karar tarihi, gerekçe, etkilenen tasarım dosyaları ve Architecture aşamasına aktarılacak teknik not. Bir kararın yalnızca Grok, ChatGPT veya başka bir agent tarafından önerilmiş olması, proje sahibi onayı yerine geçmez.

Bir karar kapatıldıktan sonra aşağıdaki tutarlılık kontrolü zorunludur:

```text
Decision Log
  ↓
Domain Model
  ↓
Business Workflow / State Machine
  ↓
Database Architecture
  ↓
Screen Inventory + Web/Mobile/Public UX
  ↓
Skill Impact Review
  ↓
Design Gate
```

Zincirin herhangi bir halkası güncellenmediyse `READY FOR ARCHITECTURE` verilmez.
