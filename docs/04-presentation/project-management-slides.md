# Fabrika ERP-Lite Arayüz Tasarımı
## Proje Yönetimi Ekibi Sunumu

---

## Slayt 1 — Proje vizyonu

**Fabrika operasyonlarını tek merkezde birleştiren üretim, depo, satış, cari ve personel platformu**

Peçete üretiminden teklif talebine, sipariş onayından sevkiyat ve tahsilata kadar tüm operasyon; ortak veri modeli, rol bazlı yetkiler ve web–mobil deneyim ile yönetilecektir.

**Tasarım aşaması tamamlandı. Sıradaki aşama: ortak bileşen sistemi ve öncelikli iş akışlarının kodlanması.**

Görsel: `uretim-depo-erp-dashboard-reference.png`

---

## Slayt 2 — Yönetici özeti

Bu proje, farklı departmanların ayrı tablolar veya kopuk uygulamalar üzerinden yürüttüğü süreçleri tek bir operasyon ekranında birleştirmek üzere tasarlanmıştır. Tasarımın odağında görsel efekt değil; veri doğruluğu, işlem izlenebilirliği, güvenli onay, hızlı kullanım ve uçtan uca belge bağlantısı bulunmaktadır.

| Tasarım sonucu | Proje etkisi |
|---|---|
| Ortak web bilgi mimarisi | Departmanlar arası aynı dili kullanma |
| Rol bazlı dashboard ve yetki | Kullanıcıya yalnızca gerekli işlemi gösterme |
| Siparişten tahsilata bağlantı | Satış, depo ve muhasebe kopukluğunu azaltma |
| Mobil barkod ve üretim akışı | Saha işlemlerinde veri girişini hızlandırma |
| Public katalog ve teklif sepeti | Dış müşteri talebini standartlaştırma |

---

## Slayt 3 — Kapsam ve hedeflenen operasyon

Sistem üç kullanıcı yüzeyinden oluşur: şirket içi web uygulaması, dış müşteriye açık public katalog ve depo/üretim/sevkiyat çalışanları için mobil operasyon uygulaması.

**Web:** Dashboard, satış, ürünler, depo, üretim, sevkiyat, cari/muhasebe, personel, raporlar, bildirimler ve yönetim.

**Public:** Ürünleri inceleme, ürün seçme, miktar ve not belirtme, teklif talebi gönderme.

**Mobil:** Barkod tarama, stok sorgulama, sayım, transfer, sevkiyat doğrulama ve üretim gerçekleşmesi.

---

## Slayt 4 — Uçtan uca değer akışı

```text
Public Ürün Kataloğu
        ↓
Teklif Talebi
        ↓
Teklif ve Sipariş
        ↓
Sorumlu Onayı
        ↓
Stok Rezervasyonu
        ↓
İrsaliye ve Sevkiyat
        ↓
Fatura
        ↓
Cari Borç ve Ödeme
        ↓
Raporlama ve Risk Analizi
```

Üretim tarafında paralel akış; iş emri, makine, personel, üretim gerçekleşmesi, fire/duruş ve tamamlanan miktarın depoya girişi üzerinden ilerler.

Görsel önerisi: Akışın yatay süreç diyagramı ve her adımın altında sorumlu departman etiketi.

---

## Slayt 5 — Modül haritası

| Operasyon | Modüller |
|---|---|
| Gelir ve satış | Teklif talepleri, teklifler, siparişler, müşteri |
| Fiziksel akış | Depo, stok, barkod, irsaliye, sevkiyat |
| Üretim | İş emirleri, makineler, üretim kayıtları, fire, duruş |
| Finans | Fatura, cari hesap, cari ekstre, ödemeler, risk |
| İnsan kaynağı | Personel, puantaj, izin, mesai, maaş |
| Yönetim ve analiz | Dashboard, raporlar, bildirimler, roller, audit, ayarlar |

Bu modüllerin tamamı aynı müşteri, ürün, belge, stok, personel ve cari verisini kullanacak şekilde tasarlanmıştır.

---

## Slayt 6 — Kullanıcı rolleri ve karar noktaları

Sistemde kullanıcıların yalnızca rolü değil, işlem bazlı izinleri de belirleyicidir. Bu yaklaşım; ürün görüntüleme, sipariş onayı, stok düzeltme, fatura iptali, ödeme oluşturma ve izin onayı gibi kritik işlemleri birbirinden ayırır.

| Rol | Birincil karar veya görev |
|---|---|
| Yönetici | Sipariş, risk, kritik uyarı ve performans yönetimi |
| Satış | Teklif, müşteri ve sipariş hazırlama |
| Depo | Stok, barkod, irsaliye ve yükleme |
| Üretim | İş emri, makine, personel, fire ve duruş |
| Muhasebe | Fatura, cari, ödeme ve tahsilat |
| İK | Puantaj, izin, mesai ve maaş |
| Sistem yöneticisi | Kullanıcı, rol, yetki, audit ve sistem sağlığı |

---

## Slayt 7 — Ortak UX ve görsel tasarım sistemi

Tasarım dili açık içerik alanı, derin lacivert navigasyon, teal ana aksiyonlar, amber bekleyen durumlar ve kırmızı kritik durumlar üzerine kuruludur. Arayüz dekoratif değil; bilgi yoğun, hızlı taranabilir ve tutarlı olacak şekilde tasarlanmıştır.

**Standart bileşenler:** Sidebar, topbar, breadcrumb, KPI kartları, data table, durum rozetleri, form, modal, drawer, timeline, stepper, toast ve empty state.

**Ortak ekran davranışı:** Her detay ekranı özet bilgiyi, mevcut durumu, bağlı belgeleri, aktivite geçmişini ve bir sonraki işlemi aynı zihinsel model içinde gösterir.

Görsel: `uretim-depo-erp-dashboard-reference.png`

---

## Slayt 8 — Web satış ve onay deneyimi

Satış ekranları, public teklif talebinden kesin siparişe kadar tek akış içinde tasarlanmıştır. Sipariş ilk oluşturulduğunda kesin sipariş sayılmaz; sorumlu onayı ve stok uygunluğu görünür bir kontrol panelinde birlikte gösterilir.

**Sipariş detayında:** Müşteri, teslim tarihi, toplam tutar, ürünler, stok rezervasyonu, belgeler, onay geçmişi ve aktivite bulunur.

**Onay panelinde:** Stok uygunluğu, ödeme şartı, teslim tarihi ve toplam etki gösterilir. Onay veya ret kararı açıklama ve kullanıcı bilgisiyle kaydedilir.

Görsel: `uretim-depo-order-detail-mockup.png`

---

## Slayt 9 — Ürün, public katalog ve teklif sepeti

Public katalog, dış müşterinin hesap açmadan ürünleri seçmesini sağlar. Katalogda ürün fotoğrafı, ürün adı, kodu, ölçüsü, paket/koli içeriği ve açıklama bulunur; stok ve şirket içi maliyet bilgisi gösterilmez.

Teklif sepetinde ürün miktarı, ürün notu ve genel talep notu alınır. Firma, yetkili, telefon ve e-posta bilgileri iki aşamalı form ile toplanır. Gönderim sonrasında şirket içi sisteme `NEW` durumunda teklif talebi ve satış ekibine bildirim düşer.

Görseller: `uretim-depo-public-catalog-desktop-mockup.png`, `uretim-depo-quote-cart-desktop-mockup.png`

---

## Slayt 10 — Üretim, depo ve mobil operasyon

Üretim iş emrinde hedef, gerçekleşen, kalan miktar, makine, personel, fire ve duruş aynı ekran içinde takip edilir. Üretim tamamlandığında gerçekleşen miktar depo stok girişine dönüşür.

Mobil uygulama masaüstünün küçültülmüş kopyası değildir. Ana işlem; barkod tarama, stok sorgulama, sayım, transfer, sevkiyat doğrulama ve üretim kaydıdır. Ağ kesildiğinde stok ve finans işlemleri sessizce gönderilmez; kullanıcıya açık bağlantı durumu gösterilir.

Görseller: `uretim-depo-production-work-order-mockup.png`, `uretim-depo-mobile-barcode-mockup.png`

---

## Slayt 11 — Sevkiyat, cari ve personel görünümü

Sevkiyat ekranında irsaliye, ürün doğrulama, araç, şoför, yükleme ve teslim durumu birlikte izlenir. Cari ekranında borç, alacak, bakiye ve geciken tutar üst düzey KPI olarak görünür; ödeme kaydı işlem sonrası yeni bakiyeyi önceden gösterir.

Personel ekranında günlük puantaj, devamsızlık, izin, fazla mesai ve onay bekleyen izinler tek dashboard'da toplanır.

Görseller: `uretim-depo-shipment-mockup.png`, `uretim-depo-accounting-current-account-mockup.png`, `uretim-depo-hr-attendance-mockup.png`

---

## Slayt 12 — Yol haritası ve proje yönetimi kararları

### Önerilen geliştirme sırası

1. Ortak layout ve tasarım bileşenleri.
2. Kimlik, kullanıcı, rol ve izinler.
3. Ürün, müşteri, public katalog ve teklif talebi.
4. Depo, stok ve barkod.
5. Teklif, sipariş ve onay.
6. İrsaliye, sevkiyat, fatura ve cari.
7. Üretim, makine ve üretim personeli.
8. Personel, puantaj, izin ve mesai.
9. Raporlar, dashboard, bildirim ve audit.
10. Mobil uygulama, backup, deployment ve uçtan uca test.

### Proje yönetimi ekibinden beklenen kararlar

Marka/logo ve kurumsal renkler, sipariş onayının tek kademeli olup olmayacağı, public katalogda fiyat gösterimi, fatura/e-belge entegrasyonu, ilk sürüm mobil kullanıcı kapsamı ve depo/konum modelinin kesinleştirilmesi.

**Önerilen sonraki adım:** Tasarım paketi üzerinden ortak bileşenlerin ve ilk uçtan uca satış akışının geliştirme backlog'una dönüştürülmesi.

---

## Ek not — Tasarım teslim kapsamı

Bu sunum, aşağıdaki detay dokümanlarını temel almaktadır: ekran envanteri, web UX mimarisi, üretim–finans–İK tasarımı, yönetim ve public sistem tasarımı, mobil uygulama tasarımı, public katalog tasarımı ve görsel tasarım sistemi.
