# Public Ürün Kataloğu ve Teklif Sepeti
## Detaylı Ekran Akışları

## 1. Ana kullanıcı akışı

```text
Public ana sayfa
  ↓
Ürün kataloğu
  ↓
Arama / filtreleme
  ↓
Ürün detay
  ↓
Miktar seçimi
  ↓
Teklife ekle
  ↓
Teklif sepeti
  ↓
Firma bilgileri
  ↓
Talep özeti ve onay
  ↓
Başarılı gönderim
```

Kullanıcı herhangi bir adımda kataloğa geri dönebilecek, sepet içeriği korunacak ve sayfa yenilense dahi teklif sepeti tarayıcı oturumunda kaybolmayacaktır. Bu davranış, hesap açmadan kullanılan public deneyimde temel güven unsurlarından biridir.

## 2. Ekran 1 — Public ana sayfa

### Amaç

Ziyaretçiyi en kısa sürede ürün kataloğuna götürmek ve teklif isteme sürecinin bir sipariş veya ödeme işlemi olmadığını anlaşılır şekilde anlatmak.

### Yerleşim

```text
[Logo]  Ürünler  Kurumsal  İletişim                 [Teklif Sepeti 0]

İhtiyacınıza uygun peçete ürünleri için teklif alın
Ürünleri inceleyin, miktarları belirtin, teklif talebinizi gönderin.
[Ürünleri İncele]       [Nasıl Çalışır?]

[Peçete] [Dispenser] [Kokteyl] [Özel Üretim] [Tüm Ürünler]

Öne Çıkan Ürünler
[Ürün Kartı] [Ürün Kartı] [Ürün Kartı] [Ürün Kartı]

3 adımda teklif alın: Ürünleri seçin → Miktar belirtin → Talebinizi gönderin

[İletişim] [Adres] [Telefon] [E-posta]
```

### Birincil CTA

“Ürünleri İncele” birincil buton; “Nasıl Çalışır?” ikincil buton olacaktır. Üst menüdeki “Teklif Sepeti” sürekli görünür kalacak ve sepet adedi rozet ile gösterilecektir.

## 3. Ekran 2 — Ürün kataloğu

### Başlık ve filtre alanı

Sayfa başlığı “Ürünler” olacak, altında “İhtiyacınız olan ürünleri seçerek teklif talebi oluşturabilirsiniz.” açıklaması yer alacaktır. Arama alanı ürün adı, ürün kodu veya kategori üzerinden çalışacaktır.

| Filtre | Davranış |
|---|---|
| Kategori | Çoklu seçim yapılabilir |
| Ürün tipi | Standart, dispenser, kokteyl, özel üretim |
| Ölçü | 17x17, 24x24, 30x30, 33x33, 40x40 gibi seçenekler |
| Paket içeriği | Aralık veya hazır seçenekler |
| Sıralama | Önerilen, yeni, ada göre |

### Ürün kartı

```text
[Ürün fotoğrafı]
Premium Peçete 33x33
Ürün Kodu: ÜRÜN-001
33x33 cm · 100 yaprak / paket
Kısa açıklama: Yüksek emiş gücüne sahip...
[Favoriye ekle]              [Teklife Ekle]
[Detayları Gör]
```

Public katalogda stok ve şirket içi maliyet bilgisi gösterilmeyecektir. “Teklife Ekle” butonu kart içinde belirgin teal renkte olacak; başarılı ekleme sonrası buton kısa süre “Sepete Eklendi” durumuna geçecektir.

## 4. Ekran 3 — Ürün detay

Ürün detay sayfası veya modalı, karttan daha fazla bilgi verecek ancak kullanıcıyı teklif akışından koparmayacaktır.

```text
[← Ürünlere Dön]

[ Büyük ürün görseli ]  Premium Peçete 33x33
                        Ürün Kodu: ÜRÜN-001
                        33x33 cm
                        100 yaprak / paket
                        Koli içeriği: 20 paket
                        Ürün açıklaması
                        Teknik özellikler

                        Miktar: [-] [ 10 ] [+] [Paket]
                        Ürün notu: [________________]
                        [Teklife Ekle]
```

Miktar alanı ürünün varsayılan birimini gösterir. “Koli” seçeneği varsa kullanıcı paket veya koli bazında seçim yapabilir; dönüşüm bilgisi kullanıcıya açıkça gösterilir.

## 5. Ekran 4 — Teklife ekleme davranışı

Kullanıcı “Teklife Ekle” butonuna bastığında ürün ilk kez ekleniyorsa sepet adedi bir artar ve kısa bir başarı bildirimi gösterilir. Aynı ürün tekrar eklenirse ikinci satır oluşturulmaz; miktar birleştirilir ve “Bu ürün sepetinizdeki miktara eklendi” mesajı gösterilir.

Ürün notu ürün detayında girilmişse ilgili sepet satırına taşınır. Miktar sıfır veya geçersizse ürün eklenmez ve alanın altında “Miktar 0'dan büyük olmalıdır.” uyarısı görünür.

## 6. Ekran 5 — Teklif sepeti

### Masaüstü yerleşim

```text
[Logo]  Ürünler  Kurumsal  İletişim              [Teklif Sepeti 2]

Teklif Sepetiniz
Bu işlem sipariş oluşturmaz. Talebinizi inceleyip sizinle iletişime geçeceğiz.

Ürün                         Miktar       Birim       Not       İşlem
[foto] Premium Peçete...     [- 10 +]     Paket       ...       [Sil]
[foto] Kokteyl Peçete...     [- 5 +]      Koli        ...       [Sil]

Genel Talep Notu
[____________________________________________________________]

[Alışverişe Devam Et]                    [Bilgilerimi Gir ve Teklif İste]
```

### Sepet davranışları

Miktar düzenleme satır içinde yapılacaktır. Uzun ürün adı iki satıra taşınabilecek, satır yüksekliği sabitlenmeyecektir. Kaldırma işleminde yanlışlıkla silmeyi önlemek için kısa bir geri alma bildirimi gösterilecektir. Sepet boşaltma işlemi varsa ayrı bir onay gerektirecektir.

## 7. Ekran 6 — Müşteri bilgileri

Bilgi formu iki adıma bölünecektir. Böylece ziyaretçi uzun bir form karşısında zorlanmayacak ve hangi bilgilerin neden istendiğini anlayacaktır.

### Adım 1: Firma bilgileri

```text
Teklif talebi oluşturun
Talebinizi iletmek için iletişim bilgilerinizi bırakın.

Firma adı *
[Örnek: ABC Gıda San. ve Tic. Ltd. Şti.]

Yetkili adı soyadı *
[Ad Soyad]

Telefon *
[05XX XXX XX XX]

E-posta *
[ornek@firma.com]

[Devam Et]
```

### Adım 2: Talep özeti

```text
Talep özeti
2 ürün · Tahmini toplam miktar: 15 birim

[Ürün satırları ve miktarlar]

Ek notunuz
[____________________________________________]

[ ] Gizlilik ve iletişim metnini okudum, kabul ediyorum. *

[Bilgileri Düzenle]                 [Teklif Talebini Gönder]
```

Public formda kullanıcıdan ödeme bilgisi, şifre veya gereksiz muhasebe bilgisi istenmeyecektir. Vergi bilgileri satış ekibinin gerektiğinde sonraki iletişimde alabileceği isteğe bağlı alan olarak bırakılabilir.

## 8. Ekran 7 — Gönderim durumu

### Başarılı gönderim

```text
[Başarı ikonu]
Teklif talebiniz alındı

Talep Numaranız: TLT-2026-000184
Talebiniz 15 Ağustos 2026, 14:32 tarihinde şirketimize iletildi.
Satış ekibimiz ürün ve miktar bilgilerinizi inceleyerek sizinle iletişime geçecektir.

[Talep Özetini Gör]       [Ürünlere Dön]
```

### Hata durumu

Form verileri hata durumunda korunacaktır. Kullanıcıya “Talep gönderilemedi. Bilgileriniz korunuyor; lütfen tekrar deneyin.” mesajı gösterilecek ve formdaki değerler silinmeyecektir.

## 9. Mobil akış

Mobil katalogda üstte logo, sepet ikonu ve arama; içerikte tek sütun ürün kartları kullanılacaktır. Filtreler alttan açılan panel olarak çalışacak, ürün detayında “Teklife Ekle” butonu ekranın altında sabit kalacaktır.

Sepet ekranında ürün satırları dikey kartlara dönüşecektir. “Teklif İste” CTA'sı alt alanda sabit kalacak, klavye açıldığında form alanlarının üzerini kapatmayacaktır. Müşteri bilgi formunda her adım bir ekran olacak ve üstte ilerleme göstergesi bulunacaktır:

```text
1 Firma Bilgileri  →  2 Talep Özeti  →  3 Gönderildi
```

## 10. İç sistemde oluşacak sonuç

Public form gönderildiğinde şirket içi sistemde yeni teklif talebi oluşturulacaktır. Satış kullanıcısının bildirim merkezinde “Yeni teklif talebi geldi” bildirimi, teklif talepleri listesinde ise `NEW` durumlu yeni satır görünecektir. Talep detayında public müşterinin yazdığı firma, iletişim, ürün, miktar ve not bilgileri korunacaktır.

## 11. Ekranlar arası bağlantı matrisi

| Ekran | Birincil çıkış | İkincil çıkış |
|---|---|---|
| Public ana sayfa | Ürün kataloğu | Nasıl çalışır, iletişim |
| Katalog | Ürün detay | Teklif sepeti |
| Ürün detay | Teklife ekle | Kataloğa dön |
| Sepet | Müşteri bilgileri | Alışverişe devam et |
| Firma bilgileri | Talep özeti | Sepete dön |
| Talep özeti | Gönder | Bilgileri düzenle |
| Başarı | Kataloğa dön | Talep özetini gör |

## 12. Tasarım test senaryosu

Bir test kullanıcısı şu akışı tamamlayabilmelidir:

```text
1. Public ana sayfayı açar.
2. Katalogda “33x33” araması yapar.
3. Premium Peçete 33x33 ürün detayını açar.
4. 10 paket seçer ve ürün notu yazar.
5. Teklife ekler.
6. Başka bir ürünü 5 koli olarak ekler.
7. Teklif sepetinde miktarları değiştirir.
8. Genel talep notu ekler.
9. Firma ve iletişim bilgilerini girer.
10. Talep özetini kontrol eder.
11. Gizlilik onayını verir.
12. Teklif talebini gönderir.
13. Talep numarasını görür.
```
