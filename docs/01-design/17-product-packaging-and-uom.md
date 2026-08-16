# Product Packaging and Unit of Measure
## Palet → Koli → Paket → Temel Birim

**Durum:** Tasarım gereksinimi ve domain model önerisi

## 1. Amaç

Factory ERP-Lite ürünlerinde miktar, yalnızca tek bir `unit` alanıyla ifade edilmez. Aynı ürünün palet, koli, paket ve temel ölçü birimi seviyelerinde satın alınması, üretilmesi, sayılması, rezerve edilmesi ve sevk edilmesi gerekir.

Bu modelin amacı, kullanıcının günlük operasyon dilini korurken stok ve finansal hareketlerde tek, ölçülebilir ve denetlenebilir bir doğruluk kaynağı oluşturmaktır.

> **Ana kural:** Kullanıcı ambalaj birimiyle giriş yapabilir; stok ledger'ı her zaman ürünün temel biriminde tutulur.

## 2. Hiyerarşi

```text
Palet
  └─ Koli
      └─ Paket
          └─ Temel Birim (adet, kg, metre, litre vb.)
```

Bu hiyerarşi ürün bazlıdır. Global olarak `1 Koli = X` varsayımı yapılmaz; her ürünün koli ve paket içeriği farklı olabilir.

| Seviye | Örnek | Açıklama |
|---|---|---|
| `BaseUnit` | `adet`, `kg` | Stok ledger'ının temel ölçüsü |
| `Package` | `1 Paket = 100 adet` | Küçük satış/üretim ambalajı |
| `Case` | `1 Koli = 20 Paket` | Depo ve sevkiyat ambalajı |
| `Pallet` | `1 Palet = 40 Koli` | Toplu depo/sevkiyat ambalajı |

## 3. Önerilen domain nesneleri

### `UnitOfMeasure`

Ölçü birimi sözlüğüdür. Örnek kodlar: `Piece`, `Kilogram`, `Meter`, `Liter`.

| Alan | Açıklama |
|---|---|
| `code` | İngilizce sistem kodu; örneğin `Piece`, `Kilogram` |
| `display_name` | Türkçe kullanıcı etiketi; örneğin `Adet`, `kg` |
| `dimension` | `Count`, `Weight`, `Length`, `Volume` |
| `decimal_scale` | Ölçünün kaç ondalık basamağa izin verdiği |
| `is_active` | Kullanılabilirlik durumu |

### `Product`

Ürün ana kartıdır ve `base_uom_id` ile temel ölçü birimini belirler.

| Alan | Açıklama |
|---|---|
| `id` | Ürün kimliği |
| `code` | Ürün kodu |
| `name` | Ürün adı |
| `base_uom_id` | Stok doğruluk birimi |
| `is_active` | Ürünün aktiflik durumu |
| `is_public` | Public katalog görünürlüğü |

### `ProductPackaging`

Aynı ürünün palet, koli ve paket seviyelerini tanımlar.

| Alan | Açıklama |
|---|---|
| `product_id` | Bağlı ürün |
| `level` | `BaseUnit`, `Package`, `Case`, `Pallet` |
| `name` | `Paket`, `Koli`, `Palet` |
| `parent_packaging_id` | Bir üst ambalaj seviyesi |
| `units_per_parent` | Üst ambalajdaki alt ambalaj sayısı |
| `quantity_in_base_uom` | Bir ambalajın temel birim karşılığı |
| `is_sellable` | Teklif/sipariş/sevkiyat için seçilebilir mi |
| `allow_partial` | Ambalaj açılarak parçalı işlem yapılabilir mi |
| `barcode` | Bu ambalaj seviyesine ait barkod |
| `effective_from`, `effective_to` | Tarihsel dönüşüm sürümü |

## 4. 5 koli örneği

Bir peçete ürünü için:

| Ambalaj | İçerik | Temel karşılık |
|---|---:|---:|
| 1 Paket | 100 adet | 100 adet |
| 1 Koli | 20 Paket | 2.000 adet |
| 1 Palet | 40 Koli | 80.000 adet |

Kullanıcı satış, sevkiyat veya transfer ekranında `5 Koli` seçerse:

```text
entered_quantity       = 5
entered_packaging      = Case / Koli
quantity_in_base_uom   = 2.000 adet
quantity_base          = 5 × 2.000 = 10.000 adet
packaging_snapshot     = 5 Koli (20 Paket/Koli, 100 adet/Paket)
```

Kullanıcıya gösterilecek ifade:

> **5 Koli (10.000 adet)**

Bu işlem için `5 Koli` adında ayrı bir ürün, stok kartı veya Product kaydı oluşturulmaz. Tek `Product` altında ambalaj seviyesi seçilir.

Ağırlık bazlı örnekte `1 Koli = 60 kg` ise `5 Koli` işlemi `300 kg` temel miktar olarak kaydedilir:

```text
5 Koli × 60 kg = 300 kg
```

## 5. Karma ve açılmış ambalajlar

Kolinin açıldığı durumlarda `0,5 Koli` gibi operasyonel olarak belirsiz bir gösterim yerine alt seviyeler açıkça ifade edilir.

```text
4 Koli + 6 Paket = 4 × 2.000 + 6 × 100 = 8.600 adet
```

Bu hareketin temel miktarı `8.600 adet` olur. Ekranda hem temel miktar hem ambalaj kırılımı gösterilir:

> **4 Koli + 6 Paket (8.600 adet)**

`allow_partial = false` olan kapalı ambalajlarda yalnızca tam koli/paket kabul edilir. Parçalı işlem gerekiyorsa kullanıcı bir alt ambalaj seviyesine indirilir.

## 6. İşlem alanları

Sipariş, teklif, irsaliye, sevkiyat, stok hareketi, rezervasyon, sayım ve üretim kaydında aşağıdaki alanlar birlikte değerlendirilir:

```text
entered_quantity
entered_packaging_id
quantity_base
packaging_snapshot
packaging_breakdown
```

`quantity_base` backend tarafından hesaplanır. Frontend'in gönderdiği temel miktar tek başına güvenilir kabul edilmez.

### Doğruluk ve görünüm ayrımı

| Alan | Rol |
|---|---|
| `quantity_base` | Stok, rezervasyon, sevkiyat ve allocation için doğruluk kaynağı |
| `entered_quantity` | Kullanıcının girdiği sayı; örneğin `5` |
| `entered_packaging_id` | Kullanıcının seçtiği seviye; örneğin `Koli` |
| `packaging_snapshot` | İşlem tarihindeki dönüşüm bilgisi; geçmiş belgeleri korur |
| `packaging_breakdown` | `4 Koli + 6 Paket` gibi insan-okur görünüm |

## 7. Stok ve belge davranışı

`Stock`, `StockMovement` ve `StockReservation` kayıtlarında miktar temel birimde tutulur. Kullanıcı arayüzü varsayılan olarak ambalaj görünümünü, ayrıntı veya tooltip alanında temel karşılığı gösterir.

| İşlem | Kullanıcı girişi | Ledger etkisi |
|---|---|---|
| Sipariş | `5 Koli` | `10.000 adet` rezervasyon adayı |
| İrsaliye | `5 Koli` | `10.000 adet` stok çıkışı |
| Transfer | `5 Koli` | Kaynak `-10.000`, hedef `+10.000 adet` |
| Sayım | `4 Koli + 6 Paket` | `8.600 adet` sayım sonucu |
| Üretim | `5 Koli` veya temel miktar | `10.000 adet` üretim girişi |
| Fatura allocation | İrsaliye kaleminden seçilen miktar | Temel miktar üzerinden kalan kontrolü |

Kullanılabilir stok hesabı:

```text
AvailableBaseQuantity = OnHandBaseQuantity - ReservedBaseQuantity
```

## 8. Barkod davranışı

Aynı ürünün farklı ambalaj seviyeleri farklı barkodlara sahip olabilir. Barkod çözümlemesi yalnızca ürünü değil, mümkünse ambalaj seviyesini de döndürür:

```text
Barcode → Product → Packaging Level → Base Quantity
```

Barkod koliye aitse okutma varsayılan olarak `1 Koli`; paket barkodu ise `1 Paket` ekler. Kullanıcı miktarı artırabilir veya yetkili işlemde ambalaj seviyesini değiştirebilir.

## 9. Ekran kuralları

Ürün kartında temel birim ve ambalaj özeti görünür. Sipariş, irsaliye, transfer ve sevkiyat ekranlarında miktar girişi iki alanla yapılır:

```text
Miktar: [ 5 ]
Ambalaj: [ Koli ▼ ]
Karşılığı: 10.000 adet
```

Stok ekranlarında kullanıcı görünümü değiştirebilir:

```text
Temel birim: 18.600 adet
Ambalaj görünümü: 9 Koli + 6 Paket
```

Public katalogda fiyat ve şirket içi stok gösterilmez; ancak ürünün paket/koli içeriği ve teklif talebinin temel miktar karşılığı kullanıcıya açıklanabilir.

## 10. Uygulama öncesi kabul kriterleri

- [ ] Her ürün için `base_uom` tanımlanabiliyor.
- [ ] Ürün altında birden fazla ambalaj seviyesi tanımlanabiliyor.
- [ ] `5 Koli` gibi bir giriş doğru temel miktara dönüşüyor.
- [ ] Aynı ürün için farklı ambalaj barkodları ayırt ediliyor.
- [ ] Stok ledger'ı yalnızca temel birimde hareket oluşturuyor.
- [ ] Sipariş, irsaliye ve sevkiyat ekranlarında ambalaj + temel miktar birlikte görünüyor.
- [ ] Parçalı ambalaj açık kırılımla gösteriliyor.
- [ ] Ambalaj katsayısı değiştiğinde geçmiş belge snapshot'ı bozulmuyor.
- [ ] Public katalog kullanıcıya paket/koli içeriğini anlaşılır biçimde gösteriyor.

**Kapsam notu:** Bu belge yeni bir ürün/ambalaj gereksinimi olarak canonical tasarıma eklenmiştir. Ürünlerin gerçek ambalaj katsayıları, ürün ana verisi hazırlanırken operasyon sorumlusu tarafından doldurulmalıdır.

**Hazırlayan:** Manus AI
**Tarih:** 16 Ağustos 2026
