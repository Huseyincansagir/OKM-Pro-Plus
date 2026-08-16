# Mobil Barkod Okuma ve Miktar Görünümü UX Tasarımı
## Temel Birim · Ambalaj · Kırılım

**Durum:** Kodlama öncesi canonical mobil UX tasarımı
**Kapsam:** Barkod tarama, ürün doğrulama, miktar girişi, üçlü görünüm toggle'ı, sayım/transfer/sevkiyat ve teslimat
**Arayüz dili:** Türkçe; entity, property, API ve state isimleri İngilizce
**İlgili belgeler:** `mobile-design.md`, `product-packaging-and-uom.md`, `shipment-logistics-ui-design.md`

## 1. İnceleme sonucu ve ana UX kararı

Mevcut akış doğru domain ayrımına sahiptir; ancak saha kullanımında toggle'ın yalnızca görsel bir seçenek olduğu, miktar girişinin hangi ambalaj seviyesinde yapılacağı ve barkodun hangi bağlama ait olduğu daha açık hale getirilmelidir.

Bu nedenle üçlü kontrolün iki ayrı sorumluluğu vardır:

| Kontrol | Sorumluluk |
|---|---|
| `Temel Birim / Ambalaj / Kırılım` toggle'ı | Miktarın ekranda nasıl okunacağını belirler |
| `Miktar gir` alanı + seçili ambalaj seviyesi | Kullanıcının hangi seviyede işlem yaptığını belirler |

> **Toggle miktarı dönüştürmez veya stok hareketi oluşturmaz; yalnızca görünümü değiştirir. İşlem seviyesi, miktar alanının hemen yanında açıkça yazılır.**

Örnek:

```text
Görünüm: [ Temel Birim ] [ Ambalaj ] [ Kırılım ]
İşlem seviyesi: [ Koli ▼ ]
Miktar: [ 5 ]
Karşılık: 5 Koli = 10.000 adet
```

## 2. Barkod okuma ana akışı

```text
İşlem seç
→ Kamera açılır
→ Barkod çerçeveye alınır
→ Barkod çözülür
→ Ürün + ambalaj + bağlam doğrulanır
→ Tekrarlı okuma kilidi kontrol edilir
→ Ürün doğrulama özeti
→ Miktar/ambalaj seviyesi
→ Stok veya sevkiyat etkisi önizlemesi
→ Kullanıcı onayı
→ İşlem sonucu ve kayıt no
```

### 2.1 Kamera ekranı

Kamera ekranı yalnızca tarama görevi için sade tutulur:

```text
┌──────────────────────────────┐
│ Barkod Tara             [×]  │
│ Depo: Merkez · İşlem: Sayım  │
│                              │
│        ┌────────────┐        │
│        │            │        │
│        │  ÇERÇEVE   │        │
│        │            │        │
│        └────────────┘        │
│ Barkodu çerçeve içine alın   │
│                              │
│ [Flaş] [Manuel Kod] [Ara]   │
└──────────────────────────────┘
```

Kamera başarılı okuduğunda kısa titreşim ve metinli başarı bildirimi gösterilir. Aynı barkod kısa süre içinde tekrar okunursa ikinci hareket oluşturulmaz; kullanıcıya “Bu barkod az önce okutuldu” mesajı ve `Yine de okut` seçeneği verilir.

### 2.2 Barkod çözümleme sonuçları

Barkod üç tipten biri olabilir:

| Barkod tipi | Sonuç |
|---|---|
| Ürün barkodu | Ürün ana kartı ve varsayılan ambalaj seviyesi açılır |
| Ambalaj barkodu | İlgili ürün + `ProductPackaging` seviyesi açılır |
| Yük birimi barkodu | Palet/koli/paket, bağlı ürünler ve sevkiyat/rota bağlamı açılır |

Barkod bilinmiyorsa kullanıcıya doğrudan işlem formu açılmaz. Önce `Manuel ürün ara`, `Yeni barkod tanımla` yetkisi varsa `Barkodu ürüne bağla` seçenekleri gösterilir.

## 3. Ürün doğrulama kartı

Başarılı taramadan sonra kartın ilk bakışta şu soruları cevaplaması gerekir: Ne okutuldu, hangi ürüne ait, hangi ambalaj seviyesi, hangi işlem bağlamı ve kullanılabilir miktar ne kadar?

```text
┌──────────────────────────────┐
│ ✓ Barkod doğrulandı           │
│ Premium Napkin 33x33          │
│ NAP-3333-PREM                 │
│ Barkod: 869...0042            │
│                              │
│ Okunan seviye: Koli           │
│ 1 Koli = 2.000 adet           │
│ Kullanılabilir: 18 Koli       │
│                              │
│ [ İşleme Devam Et ]           │
│ [ Yeniden Tara ]              │
└──────────────────────────────┘
```

Sevkiyat veya teslimat bağlamı varsa müşteri, adres, aktif rota durağı ve paket durumu bu kartta ayrıca görünür. Aktif durağa ait olmayan paketler “Bu paket aktif durağa ait değil” uyarısıyla bloke edilir.

## 4. Üçlü toggle davranışı

### 4.1 Kontrol yerleşimi

Toggle, miktar gösteriminin üstünde ve işlem alanından önce yer alır. Küçük ekranlarda yatay üç segment korunur; metin sığmazsa `Temel`, `Ambalaj`, `Kırılım` kısa etiketleri ve erişilebilirlik açıklamaları kullanılır.

```text
Miktar görünümü
[ Temel Birim ] [ Ambalaj ] [ Kırılım ]
```

Seçili segment teal zemin ve beyaz metinle; seçili olmayan segmentler açık zemin ve koyu metinle gösterilir. Seçim yalnızca görsel sunumu değiştirir; backend'e gönderilen `quantity_base` değişmez.

### 4.2 Görünümler

| Seçim | Ana gösterim | Yardımcı bilgi |
|---|---|---|
| Temel Birim | `10.000 adet` veya `300 kg` | `5 Koli` |
| Ambalaj | `5 Koli` | `10.000 adet` |
| Kırılım | `1 Palet + 5 Koli + 6 Paket` | `10.600 adet` |

Kırılım yalnızca sistemde geçerli ambalaj ağacı ve açılmış miktar varsa gösterilir. Kırılım yoksa segment pasif değil, açıklamalı boş durumla gösterilir: “Bu ürün için kırılım bulunmuyor.”

### 4.3 İşlem seviyesi ile görünümün ayrılması

Kullanıcı `Ambalaj` görünümünü seçtiğinde sistem otomatik olarak miktarı değiştirmez. İşlem yapılacak seviye ayrı kontrolden seçilir:

```text
İşlem seviyesi
[ Palet ] [ Koli ] [ Paket ]

Miktar
[ 5 ] Koli

Önizleme
5 Koli × 2.000 adet = 10.000 adet
```

Bu ayrım yanlışlıkla `5 Paket` yerine `5 Koli` işlemi yapılmasını önler.

## 5. Sayım akışında toggle

```text
Sayım görevi seç
→ Konum seç
→ Barkod okut
→ Ürün doğrula
→ Görünüm seç
→ İşlem seviyesi seç
→ Sayılan miktarı gir
→ Sistem miktarıyla karşılaştır
→ Fark ve temel miktar etkisini gör
→ Gerekçe seç
→ Onaya gönder / tamamla
```

Sayım ekranı aynı anda iki değeri gösterir:

```text
Sistem: 18 Koli (36.000 adet)
Sayılan: 17 Koli + 8 Paket (35.600 adet)
Fark: -400 adet
```

Farkın temel birimde hesaplandığı açıkça yazılır. Gerekçe seçilmeden `Onaya Gönder` veya `Tamamla` aktif olmaz.

## 6. Transfer akışında toggle

Transfer ekranında kullanıcı kaynak ve hedef konumu seçtikten sonra barkod okutur. Miktar girişi seçili ambalaj seviyesinde yapılır; kaynak kullanılabilir temel stok ve hedef beklenen miktar önizlenir.

```text
Kaynak: Merkez Depo / Raf A-12
Hedef: Sevkiyat Alanı / S-03

Görünüm: [ Temel Birim ] [ Ambalaj ] [ Kırılım ]
İşlem seviyesi: [ Koli ▼ ]
Miktar: 5 Koli
Temel etki: 10.000 adet

Kaynakta kalan: 13 Koli (26.000 adet)
[ Transferi Onayla ]
```

Kapasite veya stok yetersizliği varsa buton pasifleştirilmez; neden açıklanır ve işlem engellenir.

## 7. Sevkiyat yükleme ve teslimatta toggle

Yükleme sırasında varsayılan görünüm `Ambalaj`; teslimat sırasında varsayılan görünüm `Kırılım` olur. Bunun nedeni depo kullanıcısının koli/paletle, teslimat kullanıcısının ise alıcıya giden gerçek paketlerle çalışmasıdır.

Barkod okuma sonrası doğrulama sırası:

```text
1. Barkod ve paket kimliği
2. Ürün ve ambalaj seviyesi
3. Müşteri ve teslim adresi
4. Rota durağı
5. Beklenen / okutulan / kalan miktar
6. Araç ve yük birimi
7. Teslim veya yükleme onayı
```

Aktif durak dışındaki paketlerde teslim butonu açılmaz. Yükleme planı kilitliyse miktar veya alıcı durağı değişikliği yapılamaz; yalnızca fark/istisna akışı açılır.

## 8. Varsayılan görünüm matrisi

| İşlem | Varsayılan görünüm | Neden |
|---|---|---|
| Genel ürün sorgu | Ambalaj | Saha kullanıcısının fiziksel diline yakın |
| Stok sayımı | Kırılım | Açılmış ambalajı açık gösterir |
| Transfer | Ambalaj | Kaynak/hedef fiziksel hareketi anlaşılır |
| Sevkiyat yükleme | Ambalaj | Koli/palet barkodlarıyla uyumludur |
| Durak teslimatı | Kırılım | Müşteriye giden paketleri ayırır |
| Üretim kaydı | Temel Birim | Üretim çıktısı ve stok doğruluğu |
| Finansal özet | Temel Birim | Belge ve cari doğruluğu |

Son kullanılan görünüm cihazda kalabilir; ancak farklı kullanıcı/işlem bağlamında yanlış anlamaya yol açmaması için işlem seviyesi her yeni barkodda yeniden doğrulanır.

## 9. Hata önleme ve saha durumları

| Durum | Kullanıcıya gösterilecek davranış |
|---|---|
| Bilinmeyen barkod | Ürün arama veya yetkili barkod bağlama; işlem formu açılmaz |
| Aynı barkod tekrarlandı | Kısa süreli duplicate kilidi ve açık uyarı |
| Barkod başka depoda | Konum bilgisi ve “Bu konumda değil” uyarısı |
| Ambalaj seviyesi kapalı | Alt seviyeye geçiş veya yetkili override |
| Ondalık miktar yasak | Alan sayısal doğrulama ile reddeder; kabul edilen seviyeleri gösterir |
| Aktif durağa ait değil | Teslim/teslim kanıtı butonları açılmaz |
| Plan kilitli | Miktar, alıcı ve durak değişikliği engellenir |
| Bağlantı kesildi | Son doğrulama zamanı gösterilir; stok/teslim kaydı sunucu onayı olmadan kesinleşmez |
| Kullanıcı yetkisiz | Veri görüntülenebilir, kritik aksiyon açıklamalı yetki mesajıyla engellenir |

## 10. Kabul kriterleri

- [ ] Barkod okuma, ürün/ambalaj/yük birimi türünü ayırt edebiliyor.
- [ ] Üçlü toggle yalnızca görünümü değiştiriyor; `quantity_base` korunuyor.
- [ ] İşlem seviyesi görünüm toggle'ından ayrı ve açıkça gösteriliyor.
- [ ] `5 Koli` girişi `5 Koli (10.000 adet)` önizlemesiyle doğrulanıyor.
- [ ] Kırılım, açılmış ambalajları açık biçimde gösteriyor.
- [ ] Duplicate barkod okumaları ikinci stok/teslim hareketi oluşturmuyor.
- [ ] Aktif durak dışındaki paketler teslim edilemiyor.
- [ ] Sayım farkı temel birimde hesaplanıyor ve gerekçe gerektiriyor.
- [ ] Offline durumda stok ve teslim hareketi kesinleşmiş sayılmıyor.
- [ ] Mobilde kritik aksiyonlar üç ana adımdan fazla olmayan görev akışıyla tamamlanabiliyor.

**Hazırlayan:** Manus AI
**Tarih:** 16 Ağustos 2026

## 11. Görsel mockup referansı

Barkod sonucu, üçlü görünüm toggle'ı, ayrı işlem seviyesi seçimi ve temel birim önizlemesini birlikte gösteren referans ekran:

`docs/05-assets/mockups/mobile-barcode-quantity-toggle-flow.png`

Bu mockup, toggle ile işlem seviyesinin ayrıştırılması kararını görsel olarak doğrular. Görsel, production code veya gerçek veri seed'i değildir.

**Güncelleme tarihi:** 16 Ağustos 2026

**Hazırlayan:** Manus AI

---

## References

Bu belge harici kaynak kullanmaz; sistemin canonical domain ve ambalaj tasarım kararlarını referans alır:

- [`product-packaging-and-uom.md`](./product-packaging-and-uom.md)
- [`mobile-design.md`](./mobile-design.md)
- [`shipment-logistics-ui-design.md`](./shipment-logistics-ui-design.md)
- [`ui-mockup-review.md`](./ui-mockup-review.md)

> Not: Mockup içindeki ürün, müşteri, miktar ve tarih değerleri yalnızca görsel test verisidir; gerçek işletme verisi değildir.
