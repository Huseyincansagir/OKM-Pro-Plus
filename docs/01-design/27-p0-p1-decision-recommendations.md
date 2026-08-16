# Factory ERP — P0/P1 Karar Tavsiyeleri

**Aşama:** DISCOVER → DESIGN / Design Gate hazırlığı
**Durum:** Tavsiye paketi; proje sahibi ve ilgili iş sahipleri onayı olmadan hiçbir madde `DECIDED` değildir.
**Amaç:** P0 ve P1 açık kararlarını toplantıda hızlı, ölçülebilir ve implementation sınırı belli olacak şekilde karara bağlamak.

> **Kapsam notu:** Bu belge teknik ve operasyonel tasarım tavsiyesidir. Vergi, e-belge, bordro, KVKK ve benzeri yasal/uyum konuları ilgili mali müşavir, hukuk/uyum sorumlusu ve iş sahibi tarafından ayrıca doğrulanmalıdır.

## 1. Genel tavsiye

Projenin mevcut kapsamı için en güvenli yol, ilk sürümde **modüler monolith, local-first, temel stok/satış/sevkiyat/cari akışları güçlü, ileri otomasyon ve mevzuat entegrasyonları kontrollü biçimde sınırlı** bir MVP tanımlamaktır. P0 kararları mimarinin omurgasını belirlediği için kodlamadan önce kapatılmalıdır. P1 kararları için ya net değer seçilmeli ya da yazılı `ASSUMED WITH RISK` kaydı açılmalıdır.

Önerilen sıralama şöyledir:

```text
O-011 altyapı sınırı
→ O-001 vergi/belge sınırı
→ O-012 fiyat snapshot'ı
→ O-002 sevkiyat miktar/state
→ O-003 fatura allocation/cari
→ O-004 üretim kapsamı
→ O-005 lot/parti kapsamı
→ O-013 marka/assets
→ O-009 public/KVKK
→ O-010 backup/RPO/RTO
→ O-006 müşteri kabulü
→ O-007 risk politikası
→ O-008 maaş/bordro
→ O-014 kargo otomasyonu
```

Bu sıra, kararların teknik bağımlılıklarını azaltır. Örneğin O-003 fatura allocation kararı, O-001 vergi ve O-002 teslim edilen miktar kararı netleşmeden son haline getirilmemelidir.

## 2. P0 karar tavsiyeleri

### O-001 — Vergi, KDV ve e-belge

**Tavsiye:** İlk sürümde vergi kodu, oranı, istisna/tevkifat için genişlemeye açık alanlar ve belge durumları tanımlansın; gerçek e-Fatura/e-Arşiv/e-İrsaliye entegratörü ilk migration’a zorunlu bağlanmasın. Entegrasyon için `IInvoiceIntegrationService` adapter ve test/stub sağlayıcı hazırlansın. Vergi oranları kod içine hard-code edilmesin.

| Karar alanı | Önerilen MVP değeri |
|---|---|
| Fiyat sunumu | İç sistemde net fiyat + vergi ayrı; kullanıcı ekranında KDV dahil/hariç görünümü açık etiketli |
| Vergi modeli | `TaxCode`, `TaxRate`, geçerlilik tarihi ve belge snapshot’ı |
| Yuvarlama | Satır ve belge toplamı kuralı karar sahibi/mali müşavir tarafından yazılı seçilecek; sistem ikisini de destekleyecek alanlara sahip olacak |
| E-belge | Adapter/stub ve durum takibi; gerçek sağlayıcı entegrasyonu ayrı release sınırı |
| İade/iptal | Fiziksel silme yok; credit/reversal belge akışı |

**Gerekçe:** Vergi ve e-belge yanlış varsayılırsa fatura, cari ve rapor şeması sonradan kırılır. Buna karşılık gerçek sağlayıcı entegrasyonunu ilk sürümden ayırmak şirket içi ERP’nin temel stok/satış akışını bekletmez.

**MVP dışı:** Gerçek entegratör gönderimi, otomatik GİB durum senkronizasyonu ve tüm istisna senaryoları mali müşavir onayı olmadan production kapsamına alınmamalıdır.

**Kapanış sahibi:** Muhasebe sorumlusu + mali müşavir.
**Kapanış kanıtı:** Vergi kodu listesi, dahil/hariç kuralı, yuvarlama örneği, belge türleri ve entegrasyon kapsamı.

### O-002 — Kısmi sevkiyat

**Tavsiye:** Kısmi sevkiyat **açık** olsun. Sevk kararı kalem seviyesinde verilsin; bir siparişten birden fazla irsaliye ve sevkiyat üretilebilsin. Kalan miktar aynı sipariş kaleminde `remaining_qty` olarak izlenmeli, yeni sevk yeni allocation oluşturmalıdır.

| Konu | Önerilen değer |
|---|---|
| Rezervasyon | Sevk edilen miktar tüketilir; kalan miktar için rezervasyon policy’ye göre korunur veya açıkça serbest bırakılır |
| Backorder | Yeni müşteri siparişi açmak yerine aynı sipariş kaleminde açık remainder/backorder |
| State | `Approved → Preparing → PartiallyShipped → Fulfilled/Completed` |
| Miktar kaynağı | `quantity_base`; koli/paket/palet snapshot yalnızca giriş ve belge görünümü |
| Düzeltme | Kesinleşmiş sevk doğrudan edit edilmez; reversal/return |
| Müşteri politikası | Varsayılan izin; müşteri veya ürün bazında `partial_delivery_allowed=false` ile bloklanabilir |

**Gerekçe:** Üretim ve stok hazır olma durumu sevkiyat zamanında her siparişi tamamen karşılamayabilir. Kısmi sevkiyat kapalı olursa depo operasyonu gereksiz yere siparişi bekletir veya manuel dışı kayıtlar oluşur.

**Kapanış sahibi:** Satış yöneticisi + depo yöneticisi.
**Kapanış kanıtı:** Kalem state machine’i, rezervasyon release kuralı, backorder ekranı, müşteri/ürün kısıtının gerekli olup olmadığı.

### O-003 — Kısmi fatura

**Tavsiye:** Kısmi fatura **açık** olsun ve fatura miktarı `DeliveryNoteItem` allocation’ına bağlansın. Fatura yalnızca `DeliveryNote.Issued` olmuş, fiilen sevk edilmiş ve henüz faturalanmamış miktarı tüketebilsin. Bir irsaliye birden fazla faturaya bölünebilsin.

| Konu | Önerilen değer |
|---|---|
| Kaynak belge | Kesinleşmiş irsaliye kalemi |
| Üst sınır | `new_invoice_qty ≤ remaining_to_invoice` |
| Cari hareket | Yalnızca `Invoice.Issued` sırasında `CurrentTransaction(Debit)` |
| Stok etkisi | Yok; fatura stok hareketi oluşturmaz |
| Fiyat/vergi | Fatura snapshot’ı; sessiz güncelleme yok |
| Kalan miktar | `PartiallyInvoiced`; yetkili close/waiver olmadan kendiliğinden kapanmaz |
| Düzeltme | Reversal/credit; kesin fatura doğrudan edit edilmez |

**Gerekçe:** Sevk ile faturalama aynı anda veya aynı belgeye bağlı ilerlemeyebilir. Allocation modeli çift faturalama riskini önler ve cari hesabın yalnızca kesin fatura ile hareket etmesini sağlar.

**Kapanış sahibi:** Muhasebe sorumlusu + satış yöneticisi.
**Kapanış kanıtı:** Fatura kaynağı, kalan miktar hesabı, waiver/close politikası, kredi/reversal ve fiyat snapshot kuralı.

### O-004 — BOM, reçete ve hammadde tüketimi

**Tavsiye:** MVP’de **BOM ve hammadde tüketimi kapalı** tutulsun. İlk sürüm üretim emri, makine/personel gerçekleşmesi, fire/duruş kaydı ve bitmiş ürünün stoğa girişiyle sınırlansın. Domain modelde `ProductionMaterial` için genişleme sınırı belgeli kalsın.

| MVP’de olsun | MVP dışında kalsın |
|---|---|
| Üretim iş emri ve üretim miktarı | Reçete versiyonlama |
| Makine ve personel süreleri | Otomatik hammadde tüketimi |
| Fire/duruş kaydı | Maliyet muhasebesi ve yan ürün |
| Finished-good stock receipt | Yeniden işleme ve kalite karantina otomasyonu |

**Gerekçe:** Hammadde ledger’ı, reçete versiyonu, maliyet ve fire ilişkisi ayrı bir tasarım paketi gerektirir. Peçete fabrikasının ilk operasyonel değerini bitmiş ürün, depo ve satış akışları daha hızlı sağlar.

**Risk:** Hammadde stok takibi kritikse bu karar ertelenmemeli; `ASSUMED WITH RISK` ile geçiştirilmemelidir.

**Kapanış sahibi:** Üretim sorumlusu + muhasebe/maliyet sahibi.
**Kapanış kanıtı:** MVP üretim sınırı ve hammadde ihtiyacının gerçekten kapsam dışı olduğuna dair onay.

### O-005 — Lot, seri ve parti takibi

**Tavsiye:** Peçete ürünleri için ilk sürümde **lot/seri takibi kapalı** tutulabilir; ancak üretim partisi veya müşteri geri çağırma gereksinimi varsa karar mutlaka MVP’den önce açılmalıdır. Lot ekleme ihtimali varsa stok hareketi ve belge modeli genişletilebilir bırakılmalıdır.

| Konu | Önerilen MVP değeri |
|---|---|
| Stok doğruluğu | Ürün + depo + miktar |
| Lot zorunluluğu | Varsayılan kapalı |
| Parti bilgisi | Gerekirse üretim emri/üretim tarihi ile operasyon notu; lot ledger yerine geçmez |
| FIFO/FEFO | MVP dışı |
| Geri çağırma | Lot kararı verilmeden otomatik iddia edilmez |

**Gerekçe:** Lot, ürün güvenliği ve geri çağırma için değerlidir; fakat açıldıktan sonra satın alma, üretim, stok, sevk, iade ve rapor ekranlarının tamamını etkiler.

**Kapanış sahibi:** Kalite + üretim + yönetim.
**Kapanış kanıtı:** Lot/parti zorunluluğu veya gerekçeli MVP dışı bırakma kaydı.

### O-011 — Şirket serverı, LAN ve HTTPS

**Tavsiye:** **Local-first** deployment kesinleştirilsin: şirket içinde Ubuntu LTS server + Docker Compose + PostgreSQL + reverse proxy + şirket LAN HTTPS. Public katalog gerekiyorsa public route ayrı ağ/erişim politikasıyla yayınlansın; iç ERP endpoint’leri internete açılmasın.

| Katman | Önerilen değer |
|---|---|
| Server OS | Ubuntu LTS |
| Paketleme | Docker Compose |
| Proxy | Nginx veya Traefik; Architecture aşamasında biri seçilecek |
| Mobil erişim | Şirket Wi-Fi/VLAN üzerinden HTTPS API |
| Public katalog | Ayrı route/subdomain; yalnızca public allowlist endpoint’leri |
| Sertifika | Şirket DNS/sertifika yöntemiyle otomatik yenileme planı |
| Dış destek | VPN veya kontrollü yönetim erişimi; doğrudan port açma yok |
| Sağlık | `/health`, loglama ve backup başarısızlık bildirimi |

**Gerekçe:** Kullanıcının ücretsiz, şirket bilgisayarında çalışan ve local-first gereksinimini karşılar. Docker Compose kurulumu ve geri yükleme sürecini tekrarlanabilir tutar.

**Kapanış sahibi:** Sistem yöneticisi.
**Kapanış kanıtı:** Topoloji, server sorumlusu, ağ segmenti, sertifika, DNS, firewall ve uzaktan erişim kararı.

### O-012 — Fiyat listesi ve müşteri bazlı fiyatlandırma

**Tavsiye:** `PriceList + CustomerPriceGroup + ProductPrice` modeli kullanılsın. Sipariş/teklif kesinleştiğinde uygulanan fiyat, iskonto, para birimi, vergi görünümü ve geçerlilik bilgisi snapshot olarak kilitlensin. Public katalog varsayılan olarak fiyat göstermesin.

| Konu | Önerilen değer |
|---|---|
| Fiyat hiyerarşisi | Genel liste → müşteri grubu → yetkili manuel override |
| Geçerlilik | `effective_from`, `effective_to` |
| Snapshot | Quote ve SalesOrder üzerinde; Invoice kendi belge snapshot’ını taşır |
| Manuel fiyat değişimi | Yetkili permission + gerekçe + audit |
| Para birimi | MVP’de TRY; çoklu para birimi için model genişlemeye açık |
| Public fiyat | Varsayılan gizli; teklif talebi akışı kullanılır |

**Gerekçe:** Fabrikanın müşteriye göre farklı fiyat uygulama ihtiyacını destekler ve geçmiş sipariş fiyatını sonradan değişen ürün fiyatından korur.

**Kapanış sahibi:** Satış yöneticisi + yönetim + muhasebe.
**Kapanış kanıtı:** Fiyat listesi hiyerarşisi, snapshot alanları, para/vergi ilişkisi ve override yetkisi.

### O-013 — Marka, logo ve görsel lisans

**Tavsiye:** Kodlamadan önce tek production marka paketi onaylansın. Marka adı netleşene kadar nötr `Factory ERP` placeholder kullanılabilir; ancak placeholder logo, renk ve ürün görselleri production’a taşınmamalıdır.

| Asset | Önerilen kural |
|---|---|
| Marka adı | Tek onaylı isim |
| Logo/favicon | SVG/PNG dosyası ve kullanım hakkı kaydı |
| Renk/font | Web, mobil ve public katalog için ortak token seti |
| Ürün fotoğrafı | Şirketin sağladığı veya lisansı kayıtlı görsel |
| Placeholder | Yalnızca mockup/development; production’da yasak veya açıkça onaylı olmalı |
| Asset manifest | Dosya yolu, lisans/sahip, kullanım alanı ve onay tarihi |

**Gerekçe:** Görsel sistem sonradan değiştirilebilir; fakat public katalog ve tüm ekranların theme/token yapısını etkiler. Lisanssız görsel riski kodlamadan önce önlenmelidir.

**Kapanış sahibi:** Proje sahibi + pazarlama/marka sorumlusu.
**Kapanış kanıtı:** Onaylı asset manifest ve production kullanım izni.

## 3. P1 karar tavsiyeleri

### O-006 — Public talepten müşteri kartına geçiş

**Tavsiye:** Public teklif talebi doğrudan aktif müşteri oluşturmasın. Akış `QuoteRequest → CustomerCandidate → satış incelemesi → mevcut müşteriye bağlama veya yetkili yeni müşteri açma → Quote` şeklinde olsun. E-posta, telefon ve vergi numarası duplicate kontrolünde kullanılsın; otomatik birleştirme yapılmasın.

**Gerekçe:** Public veri güvenilmezdir ve cari/müşteri ana verisinin spam ile kirlenmesini önlemek gerekir. Yeni müşteri açılması permission ve audit gerektirsin.

**Kapanış sahibi:** Satış yöneticisi.
**Minimum karar:** Duplicate eşleşme alanları, yeni müşteri açma yetkisi ve talep sahibine gönderilecek bildirim.

### O-007 — Risk algoritması ve blokaj

**Tavsiye:** Varsayılan **soft block + yetkili override** modeli olsun. Gecikmiş bakiye, vade aşımı, açık bakiye/limit ve tahsilat geçmişi risk göstergeleri olarak kullanılabilir. Kritik yönetim eşikleri için hard block tanımlanabilir; her override gerekçeli ve audit’li olmalıdır.

**Gerekçe:** Küçük fabrikada her gecikme siparişi tamamen kilitlememeli; ancak riskli müşteride sorumluluk görünür olmalıdır. Algoritmanın sonucu sipariş onayında snapshot olarak saklanmalıdır.

**Kapanış sahibi:** Yönetim + muhasebe.
**Minimum karar:** Veri kaynakları, eşikler, override rolü ve risk snapshot zamanı.

### O-008 — Maaş ve bordro

**Tavsiye:** MVP’de puantaj, izin, mesai, maaş dönemi özeti ve kontrollü export olsun; yasal bordro hesap motoru ve resmi beyan entegrasyonu kapsam dışı bırakılsın. Maaş alanları ayrı permission, masking ve export audit’i ile korunmalıdır.

**Gerekçe:** Personel operasyonu için gerekli görünürlük sağlanır; yasal bordro sorumluluğu ERP’nin ilk sürümünde yanlış kurulmaz.

**Kapanış sahibi:** İK + muhasebe.
**Minimum karar:** Hangi maaş alanlarının tutulacağı, kimlerin göreceği ve export formatı.

### O-009 — Public erişim, rate limit, bot ve KVKK

**Tavsiye:** Ürün kataloğu public olabilir; teklif formu veri minimizasyonu ile çalışsın. Rate limit, honeypot ve mümkünse CAPTCHA, e-posta/telefon doğrulaması, aydınlatma/onay metni, consent kaydı ve saklama/silme politikası birlikte uygulansın. Public API yalnızca katalog ve teklif talebi uçlarıyla sınırlansın.

**Gerekçe:** Public katalog müşteri erişimini kolaylaştırır; ancak public formu iç müşteri/cari sistemine doğrudan bağlamak spam ve kişisel veri riskini artırır.

**Kapanış sahibi:** Yönetim + hukuk/uyum.
**Minimum karar:** Toplanacak alanlar, consent/saklama süresi, doğrulama ve abuse aksiyonu.

### O-010 — Backup, RPO ve RTO

**Tavsiye:** Başlangıç politikası **günlük full backup + ayrı disk/NAS + en az 14 gün retention + aylık restore testi** olsun. Hedef olarak operasyonel MVP için `RPO ≤ 24 saat`, `RTO ≤ 8 saat` önerilebilir; bu değerler sistem sahibi tarafından onaylanmalıdır. Kritik muhasebe veya sevkiyat dönemi için daha sık backup ayrıca seçilebilir.

**Gerekçe:** Local server’da tek diske güvenmek yeterli değildir. Restore testi yapılmayan backup yalnızca varsayımdır.

**Kapanış sahibi:** Sistem yöneticisi + yönetim.
**Minimum karar:** RPO/RTO, yedek hedefi, retention, restore sorumlusu ve test kanıtı.

### O-014 — Kargo planlama otomasyonu

**Tavsiye:** MVP’de **hard constraint kontrolü + First Fit Decreasing sezgisel öneri + depo sorumlusu manuel onayı** kullanılsın. Sistem aracı otomatik olarak son karar diye kilitlemesin. Hard constraint ihlali planı engellesin; soft warning açıklanarak yetkili override ile devam edilebilsin. Optimal 3D packing, kesin trafik optimizasyonu ve aks optimizasyonu MVP dışında kalsın.

**Gerekçe:** Depo operasyonunda açıklanabilirlik ve manuel kontrol, erken aşamada optimalite garantisinden daha değerlidir. Araç uygunluk skorları ve elenme nedenleri kullanıcıya gösterilmelidir.

**Kapanış sahibi:** Depo + sevkiyat yöneticisi.
**Minimum karar:** Otomatik atama düzeyi, override rolü, hard/soft kural listesi ve optimalite beklentisi.

## 4. Ortak UX ve yönetişim tavsiyeleri

P0/P1 kararları ne olursa olsun aşağıdaki ortak kuralların tek standarda bağlanması önerilir:

| Konu | Tavsiye |
|---|---|
| Tarih/saat | Database UTC, Türkiye yerel saat gösterimi; raporların aralık başlangıç/bitişi açık |
| Miktar | `quantity_base` server doğruluğu; ambalaj toggle yalnızca giriş/görünüm; precision UOM bazlı |
| İptal | Kesinleşmiş belge silinmez; reversal/return/credit |
| Yetki | `view`, `create`, `validate`, `approve`, `issue`, `override`, `export`, `reverse` ayrı permission |
| Audit | State, miktar, fiyat/vergi, override, reversal, public consent ve backup olayları audit’li |
| Bildirim | Sipariş onayı, risk override, kısmi sevk, faturalanmamış irsaliye, backup failure hedefli bildirim |
| Public sınır | Public endpoint’ler iç müşteri, cari, stok ve personel detaylarını okuyamaz |
| Hata | `application/problem+json`, stabil error code, request/correlation id ve güvenli action alanı |

## 5. Önerilen karar paketi

Proje sahibi hızlı bir MVP kararı vermek isterse aşağıdaki paket en düşük belirsizlikli başlangıçtır:

```text
O-001  Vergi alanları + adapter/stub; gerçek e-belge entegrasyonu sonraki sınır
O-002  Kısmi sevkiyat açık; kalem seviyesinde allocation ve backorder/remainder
O-003  Kısmi fatura açık; irsaliye allocation'ı ve cari debit yalnızca issued invoice'ta
O-004  BOM/hammadde MVP dışında; finished-good receipt ve üretim gerçekleşmesi dahil
O-005  Lot/seri MVP dışında; kalite ihtiyacı varsa karar yeniden açılacak
O-006  Public talep satış onayı olmadan aktif müşteri oluşturmaz
O-007  Soft block + kritik eşiklerde hard block + gerekçeli override
O-008  Puantaj/izin/mesai/maaş özeti + kontrollü export; yasal bordro yok
O-009  Public katalog + minimize form + rate limit/honeypot/CAPTCHA + consent
O-010  Günlük full + ayrı hedef + 14 gün + aylık restore; RPO≤24s/RTO≤8s öneri
O-011  Ubuntu LTS + Docker Compose + LAN HTTPS + iç endpoint izolasyonu
O-012  PriceList + CustomerPriceGroup + quote/order/invoice snapshot
O-013  Tek marka/asset manifest production öncesi zorunlu
O-014  Hard validation + FFD öneri + manuel depo onayı; optimalite yok
```

Bu paket öneridir. Proje sahibi onayı olmadan `decision-log.md` içinde `DECIDED` olarak işlenmemeli; kabul edilmeyen veya ertelenen maddeler açıkça `ASSUMED WITH RISK` ya da `OPEN` kalmalıdır.

## 6. Karar toplantısı için kısa onay listesi

Toplantıda önce şu yedi P0 kararı kapatılmalıdır: O-011, O-001, O-012, O-002, O-003, O-004 ve O-005. O-013 public/web/mobil görsel kodlamadan önce kapatılmalıdır. Ardından O-009 ve O-010 operasyon/public güvenlik için, O-006 ve O-007 satış kabulü için, O-008 personel gizliliği için ve O-014 sevkiyat operasyonu için karara bağlanmalıdır.

Her karar için kayıt şu minimum alanları içermelidir:

```text
ID:
Seçilen değer:
Karar sahibi:
Karar tarihi:
Gerekçe:
Etkilenen canonical belgeler:
MVP dışında bırakılanlar:
Kabul testi:
Risk ve takip tarihi:
```

Bu kayıt tamamlanmadan karar kapatılmış sayılmaz ve Design Gate `READY FOR ARCHITECTURE` durumuna alınmaz.
