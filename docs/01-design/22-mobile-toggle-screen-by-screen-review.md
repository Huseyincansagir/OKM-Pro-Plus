# Mobil Üçlü Toggle — Ekran Bazlı UX İncelemesi
## Palet · Koli · Paket işlem seviyesi ve Temel Birim · Ambalaj · Kırılım görünümü

**Durum:** Kodlama öncesi UX review
**Kapsam:** Mobil barkod, stok, sayım, transfer, sevkiyat yükleme, rota/durak teslimatı ve üretim
**Arayüz dili:** Türkçe; entity, property, API ve state isimleri İngilizce

## 1. Kritik UX bulgusu

Mevcut tasarımda iki farklı seçim aynı “toggle” gibi algılanabilir:

| Kontrol | Kullanıcı sorusu | Örnek |
|---|---|---|
| `Görünüm` toggle'ı | Miktarı nasıl görmek istiyorum? | Temel Birim / Ambalaj / Kırılım |
| `İşlem seviyesi` toggle/selector'ı | Hangi seviyede işlem yapıyorum? | Palet / Koli / Paket |

Bu iki kontrol aynı alanda üst üste veya aynı görsel hiyerarşide sunulmamalıdır. Önerilen mobil davranış şudur:

> **Liste ve sorgu ekranlarında yalnızca görünüm toggle'ı görünür. Miktar girişi ve işlem ekranlarında görünüm toggle'ının altında ayrı bir `İşlem seviyesi: Palet / Koli / Paket` kontrolü bulunur.**

Böylece kullanıcı `Ambalaj` görünümünü seçtiğinde sistem bunu otomatik olarak `Koli` işlemi olarak yorumlamaz. API’de de bu ayrım `viewMode` ve `operationPackagingId` olarak korunur. API, `operationType` ve ekran bağlamına göre `defaultViewMode`, `allowedViewModes` ve `allowedOperationPackagings` döndürür; ekran matrisi `mobile-toggle-api-and-schema.md` ile birlikte okunmalıdır.

## 2. Ekran bazlı karar matrisi

| Ekran | Toggle görünürlüğü | Varsayılan | İşlem seviyesi | Kritik aksiyon |
|---|---|---|---|---|
| Kamera / barkod tarama | Gizli | Yok | Yok | Barkod çözümleme |
| Barkod sonucu | Görünür, önce read-only | Barkod seviyesine göre `Ambalaj` | Devam edince açılır | Ürün/barkod/context doğrulama |
| Ürün ve stok detayı | Görünür | `Ambalaj` | Quick action açılırsa görünür | Stok sorgu/işlem seçimi |
| Hareket geçmişi | Görünür | `Temel Birim` + satır kırılımı | Yok | Belgeye gitme |
| Sayım | Görünür | `Kırılım` | Ayrı `Palet/Koli/Paket` | Sayılan miktar ve fark |
| Depo transferi | Görünür | `Ambalaj` | Ayrı `Palet/Koli/Paket` | Kaynak-hedef miktar doğrulama |
| Sevkiyat yükleme | Görünür | `Ambalaj` | Ayrı `Palet/Koli/Paket` | Beklenen/okutulan/kalan |
| Rota durağı teslimatı | Koşullu; paket tarama öncelikli | `Kırılım` | Barkod paketi belirler | Aktif durağa teslim |
| Sevkiyat/rota listesi | Toggle yok | Yok | Yok | Kompakt `3 Koli` etiketi |
| Üretim çıktısı | Görünür | `Temel Birim` | Ayrı seçim, gerekirse | Üretim miktarı ve fire |
| Dashboard / ana menü | Toggle yok | Yok | Yok | Görev kartlarına geçiş |

## 3. Ekran akışları

### 3.1 Kamera ve barkod tarama

Kamera ekranında miktar toggle'ı gösterilmemelidir. Kullanıcı bu aşamada yalnızca barkod çözmektedir; erken gösterilen Palet/Koli/Paket seçimi yanlış bağlamda işlem başlatma riskini artırır.

```text
Kamera aç
→ Barkod tara
→ Barkod tipini çöz
→ Ürün/ambalaj/yük birimi bul
→ İşlem context'ini doğrula
→ Barkod sonucu ekranına geç
```

Aynı barkodun tekrarlı taranması ikinci hareket üretmez. Bilinmeyen veya ambiguous barkodda toggle açılmaz; önce ürün/barkod eşleşmesi çözülür.

### 3.2 Barkod sonucu ve ürün doğrulama

Bu ekran iki aşamalı tasarlanmalıdır. İlk aşamada kullanıcı okunan seviyeyi görür; ikinci aşamada işlem yapacaksa miktar kontrolü açılır.

```text
Barkod doğrulandı
Ürün: Premium Napkin 33x33
Okunan seviye: Koli
1 Koli = 2.000 adet
Kullanılabilir: 18 Koli

Görünüm: [Temel Birim] [Ambalaj] [Kırılım]
[İşleme Devam Et] [Yeniden Tara]
```

`İşleme Devam Et` sonrasında:

```text
İşlem seviyesi: [Palet] [Koli] [Paket]
Miktar: [ 5 ]
Önizleme: 5 Koli = 10.000 adet
```

Okutulan barkod bir `ShipmentPackage` ise ürün seviyesine geçmeden müşteri, teslim adresi, aktif durak ve yük birimi bağlamı gösterilir.

### 3.3 Ürün ve stok detayı

Stok detayı sorgu ekranıdır; burada işlem seviyesi seçicisi başlangıçta gösterilmemelidir. Kullanıcı `Sayım`, `Transfer` veya `Sevkiyata ekle` gibi bir aksiyon seçtiğinde işlem formuna geçilir ve işlem seviyesi açılır.

```text
Premium Napkin 33x33
Mevcut: 36.000 adet
Görünüm: [Temel Birim] [Ambalaj] [Kırılım]

Depo: Merkez · Raf A-12
[Sayım Yap] [Transfer Başlat] [Sevkiyata Ekle]
```

Varsayılan `Ambalaj` seçimi saha diline uygundur. Kullanıcının son görünüm tercihi cihazda saklanabilir; ancak yeni işlem formu açıldığında `operationPackagingId` yeniden doğrulanmalıdır.

### 3.4 Hareket geçmişi

Hareket geçmişinde işlem oluşturulmaz; bu nedenle `Palet/Koli/Paket` işlem seviyesi kontrolü gösterilmez. Görünüm toggle'ı, geçmiş ledger satırının temel karşılığını ve snapshot bilgisini değiştirir.

```text
Çıkış · 16 Ağustos · Depo kullanıcısı
Görünüm: [Temel Birim] [Ambalaj] [Kırılım]
10.000 adet · 5 Koli
Belge: DN-2026-00142
```

Varsayılan `Temel Birim` olmalıdır; çünkü finansal/stok geçmişinin ana doğruluk kaynağı `quantity_base` değeridir.

### 3.5 Sayım ekranı

Sayımda kullanıcı gerçek fiziksel kırılımı görmeye ihtiyaç duyar. Bu nedenle varsayılan `Kırılım` olmalıdır.

```text
Sistem: 18 Koli (36.000 adet)
Görünüm: [Temel Birim] [Ambalaj] [Kırılım]
İşlem seviyesi: [Palet] [Koli] [Paket]
Sayılan miktar: [17] Koli
Önizleme: 17 Koli = 34.000 adet
Fark: -2.000 adet
Gerekçe: [Eksik ürün ▼]
[Onaya Gönder]
```

Sistem, sayılan miktar ve farkı aynı görünüm seçimine göre yeniden biçimlendirebilir; ancak farkın server-side temel değeri değişmez. `Kırılım` açılmış ambalajı açıkça gösteremiyorsa boş durum metni verilmelidir.

### 3.6 Depo transferi

Transfer ekranında kaynak ve hedef konum sabit bağlam olarak üstte görünür. Toggle, satırın okunma biçimini değiştirir; işlem seviyesi miktar alanının hemen yanında yer alır.

```text
Kaynak: Merkez Depo / A-12
Hedef: Sevkiyat Alanı / S-03
Görünüm: [Temel Birim] [Ambalaj] [Kırılım]
İşlem seviyesi: [Palet] [Koli] [Paket]
Miktar: 5 Koli
Kaynak sonrası: 13 Koli (26.000 adet)
[Transferi Onayla]
```

`Transferi Onayla` öncesinde kaynak kullanılabilir stok, ambalaj katsayısı ve hedef kapasitesi server-side kontrol edilir. Kaynakta yetersizlik varsa kullanıcıya temel ve ambalaj karşılığı birlikte gösterilir.

### 3.7 Sevkiyat yükleme

Yükleme ekranı ambalaj ve barkod operasyonuna en yakın ekrandır. Varsayılan `Ambalaj` olmalı; `Kırılım` karışık palet ve parçalı koli kontrolünde açılmalıdır.

```text
Sevkiyat: SHP-2026-000142
Yük birimi: PALLET-001 · Karışık Palet
Görünüm: [Temel Birim] [Ambalaj] [Kırılım]

Beklenen: 18 Koli
Okutulan: 5 Koli
Kalan: 13 Koli
İşlem seviyesi: [Palet] [Koli] [Paket]

[Paketi Tara] [Kalemi Onayla]
```

Plan kilitliyse işlem seviyesi veya müşteri/durak ataması değiştirilemez; toggle yalnızca görüntüyü değiştirir. Barkod yanlış `LoadUnit`, shipment veya alıcı durağına aitse aksiyon engellenir.

### 3.8 Rota durağı teslimatı

Teslimatta kullanıcı tercihiyle miktar girmek yerine paket barkodunu taramak birincil akış olmalıdır. Bu nedenle toggle koşullu görünür:

- Tek tek `ShipmentPackage` taranıyorsa toggle ikincil veya read-only olabilir.
- Paket grubu teslim ediliyorsa `Kırılım` varsayılan gösterim olur.
- Kısmi teslimde teslim edilen ve kalan paketler aynı görünümde ayrılır.
- Aktif durak dışı paketlerde toggle ve teslim butonu işlem açmamalıdır.

```text
Durak 2 / 4 · Müşteri B
Görünüm: [Temel Birim] [Ambalaj] [Kırılım]
Atanan: 1 Palet + 4 Paket
Teslim edilen: 3 Paket
Kalan: 1 Palet + 1 Paket

[Paket Tara] [Teslim Et] [İstisna Bildir]
```

Teslim kanıtı, miktar toggle'ından bağımsız olarak `RouteStop` ve `ShipmentPackage` seviyesinde kaydedilir.

### 3.9 Sevkiyat ve rota listeleri

Liste ve dashboard kartlarında toggle kullanılmamalıdır. Kartlar tek bir kompakt ambalaj etiketi ve yardımcı temel miktar gösterir:

```text
Müşteri B · Durak 2
1 Palet + 4 Paket · 10.600 adet
```

Kullanıcı detay ekranına girdiğinde ilgili toggle bağlamı açılır. Bu yaklaşım listeyi gereksiz kontrollerle doldurmaz.

### 3.10 Üretim çıktısı

Üretim doğruluğunun kaynağı temel birimdir. Varsayılan `Temel Birim` olmalıdır. Operasyon sorumlusu fiziksel planlama için `Palet/Koli/Paket` seviyesi seçebilir; sistem üretim miktarını temel birime çevirir.

```text
Üretim çıktısı
Görünüm: [Temel Birim] [Ambalaj] [Kırılım]
İşlem seviyesi: [Palet] [Koli] [Paket]
Üretildi: 5 Koli
Karşılık: 10.000 adet
Fire: 120 adet
```

Fire veya duruş miktarı için ambalaj toggle'ı zorunlu tutulmamalıdır; bu alanlar temel birimde daha güvenli ve anlaşılırdır.

## 4. Ekranlar arası tutarlılık kuralları

| Kural | Uygulama |
|---|---|
| Görünüm ve işlem seviyesi ayrımı | Liste/sorguda görünüm; işlem formunda ayrıca Palet/Koli/Paket |
| Varsayılan bağlama göre | Stok/transfer/yükleme Ambalaj; sayım/teslim Kırılım; üretim/finans Temel Birim |
| Toggle yerleşimi | Her ekranda aynı sıra ve aynı görsel dil |
| Hesaplama | Her ekranda `quantity_base` server-side doğruluk kaynağı |
| Barkod | Barkod çözülmeden toggle veya işlem formu açılmaz |
| Snapshot | Kesinleşmiş harekette görünüm, işlem ambalajı ve katsayı snapshot'ı saklanır |
| Offline | Sunucu onayı olmayan işlem kesinleşmiş gösterilmez |
| Kısmi işlem | Teslim edilen/sayılan/transfer edilen ve kalan miktar aynı görünümde karşılaştırılır |

## 5. Erişilebilirlik ve saha kullanımı

Toggle segmentleri en az parmakla rahat basılabilecek dokunma alanına sahip olmalı, yalnızca renkle ayrıştırılmamalı ve seçili durum metin/kontrast ile anlaşılmalıdır. Seçim değiştiğinde ekranda kısa bir açıklama verilmelidir: `Görünüm Ambalaj olarak değiştirildi; işlem seviyesi Koli.`

İşlem seviyesi değiştiğinde daha güçlü bir önizleme gösterilmelidir: `5 Koli = 10.000 adet`. Bu açıklama, hızlı barkod ve saha kullanımında yanlış miktar girişinin ana önleme noktasıdır.

## 6. Kabul kriterleri

- [ ] Kamera ekranında toggle gereksiz yere görünmüyor.
- [ ] Barkod sonucu ekranı okunan ambalaj seviyesini gösteriyor; işlem seviyesi ancak işlem başlatıldığında açılıyor.
- [ ] Stok detayı sorgu ve işlem bağlamını birbirinden ayırıyor.
- [ ] Sayım `Kırılım`, transfer/yükleme `Ambalaj`, üretim `Temel Birim` varsayılanıyla açılıyor.
- [ ] Rota teslimatında barkod tarama toggle'dan önce geliyor.
- [ ] Liste ve dashboard kartları toggle ile kalabalıklaştırılmıyor.
- [ ] Her işlem ekranında `viewMode` ve `operationPackagingId` ayrı kavramlar olarak gösteriliyor.
- [ ] `quantity_base` ve snapshot değerleri tüm ekranlarda backend tarafından hesaplanıyor.
- [ ] Kısmi miktar ve kalan miktar aynı görünüm seçimiyle karşılaştırılabiliyor.
- [ ] Offline veya hata durumunda işlem kesinleşmiş gibi gösterilmiyor.

## References

- [`mobile-design.md`](./mobile-design.md)
- [`mobile-barcode-and-quantity-ux.md`](./mobile-barcode-and-quantity-ux.md)
- [`mobile-toggle-api-and-schema.md`](./mobile-toggle-api-and-schema.md)
- [`product-packaging-and-uom.md`](./product-packaging-and-uom.md)
- [`business-workflows.md`](./business-workflows.md)

**Hazırlayan:** Manus AI
**Tarih:** 16 Ağustos 2026
