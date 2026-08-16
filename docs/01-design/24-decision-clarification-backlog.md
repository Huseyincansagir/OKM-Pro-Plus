# Factory ERP — Karar Netleştirme Gündemi

**Aşama:** DESIGN → DESIGN GATE / READY FOR ARCHITECTURE hazırlığı
**Durum:** O-001–O-014 proje sahibi tarafından 2026-08-16 tarihinde kabul edildi; backlog artık kapanış kanıtı ve artefact yayılımı izleme listesidir.
**Kural:** O-001–O-014 için karar sahibi, tarih, seçilen değer ve kapsam kaydedildi. Yeni kapsam değişikliği gelirse ilgili O-ID yeniden OPEN DECISION yapılır; mevcut kararlar `DECIDED` durumundadır.

## 1. Kullanım amacı

Açık kararlar yalnızca konu başlığı olarak bırakıldığında karar toplantısında farklı kişiler aynı kavramı farklı yorumlayabilir. Bu belge, O-001–O-014 maddelerini uygulanabilir alt sorulara dönüştürmüştür. 2026-08-16 kabulü sonrasında alt sorular, seçilen değerlerin domain/workflow/database/UI/QA/operations belgelerine yayıldığını doğrulamak için kullanılacaktır.

Bir kararın kapatılabilmesi için **seçilen değer**, **karar sahibi**, **karar tarihi**, **gerekçe**, **etkilenen belgeler** ve **uygulamaya aktarılacak kısıtlar** birlikte kaydedilmelidir.

## 2. Önceliklendirme

| Öncelik | Karar grubu | Neden |
|---|---|---|
| P0 | O-001, O-002, O-003, O-004, O-005, O-011, O-012 | Fatura, stok, üretim, sevkiyat, deployment ve fiyat modelinin temelini değiştirir. |
| P1 | O-006, O-007, O-008, O-009, O-010, O-014 | Yetki, müşteri kabulü, risk, public erişim, operasyon sürekliliği ve otomasyon davranışını belirler. |
| P0 — ürün | O-013 | Marka ve asset kararı public/web/mobil arayüz kodlamasından önce kesinleşmelidir. |

## 3. Karar bazında netleştirme soruları

### O-001 — Vergi, VAT ve e-belge

Muhasebe ve mali müşavir şu sorulara cevap vermelidir: Hangi vergi türleri ve oranları kullanılacak? Fiyatlar KDV dahil mi hariç mi girilecek? Satır bazında mı belge toplamında mı yuvarlama yapılacak? İade, iskonto, tevkifat, istisna ve stopaj ilk sürümde var mı? E-belge kapsamı e-Fatura, e-Arşiv, e-İrsaliye veya yalnızca PDF/numaralı iç belge mi? Entegratör, test ortamı ve belge gönderim durumları nasıl izlenecek?

**Kapanış çıktısı:** Vergi kodu kataloğu, fiyatın vergi dahil/hariç kuralı, yuvarlama kuralı, belge türleri ve entegrasyon kapsamı.

### O-002 — Kısmi sevkiyat

Sipariş kalemi kısmen sevk edilebilir mi? Kısmi sevkiyatta stok rezervasyonu kalan miktar için korunacak mı? Bir siparişten birden fazla irsaliye ve sevkiyat açılabilir mi? Eksik kalan miktar için backorder mı, iptal mi, yeni sipariş mi kullanılacak? Müşteri kısmi teslimi kabul etmiyorsa sipariş seviyesinde blokaj olacak mı?

**Kapanış çıktısı:** Kalem ve sipariş state machine’i, rezervasyon serbest bırakma, backorder ve teslimat onay politikası.

### O-003 — Kısmi fatura

Fatura irsaliyeye mi, siparişe mi, teslim edilen miktara mı bağlanacak? Aynı irsaliye birden fazla faturaya bölünebilir mi? Fatura kesilmeden önce sevkiyat tamamlanmalı mı? Kısmi fatura ile kalan miktarın cari ve rapor durumları nasıl gösterilecek? Fiyat veya vergi sonradan değişirse snapshot mı, güncel fiyat mı kullanılacak?

**Kapanış çıktısı:** Fatura kaynak belgesi, miktar bağlama kuralı, kalan faturalanabilir miktar hesabı ve iptal/iade davranışı.

### O-004 — BOM, reçete ve hammadde tüketimi

İlk sürümde yalnızca bitmiş ürün üretim gerçekleşmesi mi kaydedilecek? Hammadde stokları tutulacak mı? Ürün reçetesi versiyonlanacak mı? Fire, yan ürün, yeniden işleme ve kalite karantinası var mı? Makine ve personel gerçekleşmesi üretim miktarıyla nasıl ilişkilendirilecek?

**Kapanış çıktısı:** MVP üretim sınırı, hammadde ledger’ı, reçete versiyonu ve fire/kalite kapsamı.

### O-005 — Lot, seri ve parti izleme

Peçete ürünlerinde lot veya üretim partisi müşteri/kalite açısından zorunlu mu? Son kullanma tarihi veya geri çağırma ihtiyacı var mı? Lot üretimde mi, depoya girişte mi oluşturulacak? Sevkiyat sırasında FIFO/FEFO uygulanacak mı? İlk sürümde lot kapalı tutulacaksa ileride eklenecek migration sınırı nedir?

**Kapanış çıktısı:** Lot/seri kapsamı, zorunlu alanlar, stok çıkış stratejisi ve kalite geri çağırma gereksinimi.

### O-006 — Public talepten müşteri kartına geçiş

Teklif isteyen kişi mevcut müşteriye nasıl eşleştirilecek? Vergi numarası zorunlu mu? Yeni müşteri kartını kim açacak? Aynı e-posta, telefon veya vergi numarasıyla gelen talepler nasıl birleştirilecek? Müşteri onaylanmadan teklif hazırlanabilir mi? Talep sahibine otomatik e-posta gönderilecek mi?

**Kapanış çıktısı:** `QuoteRequest → CustomerCandidate → Customer → Quote` geçiş kuralları, duplicate politikası ve sorumlu rol.

### O-007 — Risk algoritması ve blokaj

Risk hangi verilerden hesaplanacak: gecikmiş bakiye, vade aşımı, açık çek/senet, limit, iade veya tahsilat geçmişi? Risk skoru hangi zaman aralığına bakacak? Hangi eşik yalnızca uyarı, hangi eşik onay blokajı oluşturacak? Kim override yapabilecek ve gerekçe zorunlu mu? Risk skoru sipariş anında snapshot olarak mı tutulacak?

**Kapanış çıktısı:** Veri kaynakları, ağırlıklar, eşikler, override yetkisi, audit ve snapshot kuralı.

### O-008 — Maaş ve bordro

Sistem yalnızca puantaj/maaş özeti mi tutacak? Net/brüt maaş, kesinti, avans ve prim alanları gösterilecek mi? Yasal bordro dışarıda tutulacaksa hangi formatta export alınacak? Maaş verisini hangi roller görebilecek? Personel kendi verisini görebilecek mi? Export, indirme ve değişiklikler audit edilecek mi?

**Kapanış çıktısı:** Hassas alan listesi, rol matrisi, export formatı ve bordro entegrasyonu sınırı.

### O-009 — Public katalog erişimi ve KVKK

Formda hangi kişisel veriler toplanacak? Açık rıza ve aydınlatma metni nasıl gösterilecek? E-posta/telefon doğrulaması zorunlu mu? Saklama süresi ve silme talebi nasıl yürütülecek? CAPTCHA, honeypot, rate limit ve IP abuse kaydı hangi seviyede uygulanacak? Public katalog yalnızca teklif mi toplar, dosya yükleme veya canlı iletişim de var mı?

**Kapanış çıktısı:** Veri minimizasyonu, consent kaydı, saklama/silme politikası, abuse önlemleri ve public endpoint sınırı.

### O-010 — Backup, RPO ve RTO

Kabul edilebilir veri kaybı süresi nedir? Sistem en fazla kaç saat içinde ayağa kalkmalıdır? Yedek yalnızca aynı server diskinde mi, ayrı fiziksel disk veya NAS üzerinde mi tutulacak? Harici/off-site kopya gerekli mi? Kaç günlük/haftalık/aylık retention tutulacak? Restore testini kim, hangi sıklıkta ve hangi kayıtla yapacak?

**Kapanış çıktısı:** RPO, RTO, retention, yedek hedefleri, restore sorumlusu ve kabul testi.

### O-011 — Şirket serverı, LAN ve HTTPS

Server işletim sistemi ve donanım sorumlusu kim? Mobil cihazlar hangi Wi-Fi/VLAN üzerinden erişecek? Public katalog şirket ağı içinden mi, ayrı reverse proxy/DMZ üzerinden mi yayınlanacak? HTTPS sertifikası nasıl alınacak ve yenilenecek? DNS, firewall, port, dış erişim ve uzaktan destek politikası nedir? İnternet kesildiğinde şirket içi operasyon devam etmeli mi?

**Kapanış çıktısı:** Topoloji, ağ bölgeleri, sertifika, erişim politikası, Docker yedekleme ve sağlık kontrolü.

### O-012 — Fiyat listesi ve müşteri bazlı fiyatlandırma

Kaç fiyat listesi olacak? Müşteri hangi listeye nasıl bağlanacak? Fiyat geçerlilik başlangıç/bitiş tarihi var mı? Para birimi, KDV dahil/hariç ve iskonto nasıl yönetilecek? Manuel fiyat değişikliği kim tarafından yapılabilir? Teklif ve sipariş oluşturulduğunda fiyat snapshot’ı hangi alanları kilitleyecek?

**Kapanış çıktısı:** Fiyat listesi hiyerarşisi, müşteri grubu, geçerlilik, para birimi, vergi ve snapshot kuralları.

### O-013 — Marka ve görsel lisans

Production’da kullanılacak tek marka adı nedir? Logo, favicon, renk token’ları ve font dosyalarının kullanım hakkı var mı? Ürün görselleri şirket tarafından mı sağlanacak? Placeholder görseller production’da kullanılabilir mi? Public katalogdaki ürün fotoğrafı, açıklama ve marka dili kim tarafından onaylanacak?

**Kapanış çıktısı:** Onaylı marka paketi, asset manifest, lisans kaydı ve placeholder’ın yasak/izin durumları.

### O-014 — Kargo planlama otomasyonu

Sistem yalnızca uygun araçları mı listeleyecek, yoksa önerilen aracı otomatik atayacak mı? First Fit Decreasing sonucu depo sorumlusunun onayı olmadan kilitlenebilir mi? Hard constraint ile soft warning ayrımında kim override yapabilir? Karışık palet manuel oluşturulabilir mi? Optimal araç/rota garantisi bekleniyor mu? Aks kontrolü için gerçek veri yoksa `NotEvaluated` sonucu kabul edilecek mi? Manuel replan sonrası yeni algorithm/version snapshot ve audit zorunlu mu?

**Kapanış çıktısı:** Otomasyon seviyesi, onay/override rolü, hard/soft kural listesi, algoritma sınırı ve manuel replan politikası.

## 4. Design Gate öncesi ortak UX kararları

Açık kararların yanında aşağıdaki ortak davranışlar da tüm modüllerde aynı şekilde tanımlanmalıdır:

| Konu | Netleşmesi gereken davranış |
|---|---|
| Tarih ve saat | Yerel saat dilimi, gün başlangıcı, vardiya ve teslimat zaman penceresi. |
| Miktar ve yuvarlama | Temel birim ondalık desteği, koli/paket dönüşümü ve miktar yuvarlama. |
| İptal ve ters kayıt | Hangi state’lerde iptal, iade veya reverse transaction yapılabileceği. |
| Yetki | Görüntüleme, oluşturma, onaylama, override, export ve silme yerine iptal yetkileri. |
| Audit | Hangi olayların eski/yeni değer, gerekçe, kullanıcı ve correlation id ile kaydedileceği. |
| Bildirim | Sipariş onayı, risk blokajı, sevkiyat, fatura ve backup başarısızlığında alıcılar. |
| Arama ve barkod | Bilinmeyen barkod, tekrar tarama, aktif depo/durak bağlamı ve hata mesajları. |
| Rapor tanımı | “Günlük/haftalık/aylık” tarih aralığı, iptal kayıtları ve timezone davranışı. |

## 5. Design Gate çıkış kriteri

O-001–O-014 için karar blokajı kaldırılmıştır. Gate değerlendirmesinde artık kararların varlığı değil, kabul edilen değerlerin ilgili canonical artefact’lara eksiksiz yayıldığı ve Architecture aşamasının başlayabileceği doğrulanır.

Design Gate’in `READY FOR ARCHITECTURE` kararı O-001–O-014 kabulüyle açılmıştır. Architecture başlamadan önce kabul edilen değerlerin domain, workflow, database, UI, skill ve test belgelerine yayıldığı doğrulanmalıdır.

Production code yazılmadan önce minimum Design/Architecture kapanış kanıtı şunlardır: kabul edilmiş decision log, çözüm matrisi, etkilenen canonical tasarım dosyaları, permission/state değişiklikleri, migration etkisi, QA test etkisi ve operasyon/backup etkisi. Bu kanıtlar Architecture acceptance ile tamamlanmadan implementation gate açılmaz.

## 6. Toplantı formatı

Karar toplantısında her konu için şu kısa kayıt kullanılmalıdır:

```text
ID:
Seçenek:
Seçilen değer:
Karar sahibi:
Karar tarihi:
Gerekçe:
Etkilenen belgeler:
MVP dışında bırakılanlar:
Risk ve takip tarihi:
```

Bu kayıt doldurulmadan karar `DECIDED` durumuna taşınmamalıdır.
