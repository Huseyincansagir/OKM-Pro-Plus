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

## 9. Ortak arayüz deseni: miktar görünümü ve ambalaj filtresi

Ambalaj bilgisi tek bir ürün ekranına hapsedilmez. Barkod, stok, sayım, transfer, sipariş, irsaliye, sevkiyat, fatura allocation, üretim ve rapor ekranlarında ortak bir **üçlü segmented toggle** kullanılır:

```text
[ Temel Birim ]  [ Ambalaj ]  [ Kırılım ]
```

| Görünüm | Kullanıcıya gösterilen | Kullanım amacı |
|---|---|---|
| **Temel Birim** | `10.000 adet` veya `300 kg` | Stok doğruluğu, finansal kontrol, miktar karşılaştırması |
| **Ambalaj** | `5 Koli` | Depo, satış, sevkiyat ve saha operasyonu |
| **Kırılım** | `2 Palet + 5 Koli + 6 Paket` | Karma/açılmış ambalajların fiziksel açıklaması |

Bu toggle **miktarı değiştirmez; yalnızca görünümü değiştirir.** Miktar girişi için ayrıca ambalaj seviyesi seçici kullanılır:

```text
Miktar: [ 5 ]
Giriş birimi: [ Koli ▼ ]
Karşılığı: 10.000 adet
Gösterim: [ Temel Birim | Ambalaj | Kırılım ]
```

Liste ve raporlarda segmented toggle'a ek olarak ambalaj filtresi bulunur:

```text
Ambalaj filtresi: [ Tümü ] [ Palet ] [ Koli ] [ Paket ] [ Temel Birim ]
```

Filtre yalnızca ilgili ambalaj seviyesinde kaydedilmiş veya dönüştürülebilen kayıtları süzer; ledger miktarı değişmez. Kullanıcının seçimi sayfa bazında saklanabilir, ancak kritik belge ekranlarında temel miktar ve seçili ambalaj karşılığı aynı anda görünür kalır.

### Ekranlara yayılım

| Ekran/işlem | Varsayılan görünüm | Özel davranış |
|---|---|---|
| Barkod okuma | `Ambalaj` | Koli barkodu `1 Koli`, paket barkodu `1 Paket` ekler; art arda okumalar temel miktarda toplanır |
| Stok listesi ve detay | `Kırılım` | Tümü/Palet/Koli/Paket/Temel Birim filtresi ve görünüm toggle'ı |
| Sayım | `Kırılım` | Sistem miktarı ve sayılan miktar aynı görünümde; düzeltme temel birimde kaydedilir |
| Transfer | `Ambalaj` | Giriş birimi seçilir, temel miktar önizlenir; kaynak kontrolü temel birimde yapılır |
| Sipariş/teklif | `Ambalaj` | `5 Koli` girilir, `10.000 adet` yardımcı bilgi olarak gösterilir |
| İrsaliye | `Ambalaj` | Sipariş/rezerve/sevk edilen/sevk edilecek kolonları aynı ambalaj görünümünde gösterilir |
| Sevkiyat doğrulama | `Kırılım` | Beklenen, okutulan ve kalan değerler temel + fiziksel kırılım olarak görünür |
| Fatura allocation | `Temel Birim` | Tahsis ve kalan kontrolü temel miktarda yapılır; ambalaj karşılığı yardımcı gösterilir |
| Üretim | Ürün politikasına göre | Hedef/gerçekleşen temel miktar; saha için ambalaj karşılığı |
| Raporlar | Kullanıcı seçimi | Ambalaj filtresi ve `group by` görünümü; rapor dipnotunda temel birim belirtilir |

### Barkod tarama sonrası örnek

```text
Ürün: Premium Napkin 33x33
Okunan barkod: Koli barkodu
Sonuç: +1 Koli = +2.000 adet

[ Temel Birim ] [ Ambalaj ] [ Kırılım ]
Mevcut: 18.600 adet | 9 Koli + 6 Paket
İşlem miktarı: 3 Koli | 6.000 adet

[ Sayıma Ekle ] [ Sevkiyata Ekle ] [ Transfer Başlat ]
```

Aynı ürünün paket barkodu daha sonra okutulursa işlem `3 Koli + 1 Paket` olarak görünür; backend toplamı temel birimde tutar. Toggle veya filtre, kullanıcıya farklı bir okuma sağlasa da `quantity_base` değerini değiştiremez.

Public katalogda fiyat ve şirket içi stok gösterilmez; ancak ürünün paket/koli içeriği ve teklif talebinin temel miktar karşılığı kullanıcıya açıklanabilir.

## 10. Fiziksel ölçü, ağırlık ve istifleme modeli

Ambalaj miktarı ile fiziksel lojistik bilgisi ayrı fakat ilişkili tutulur. `5 Koli` demek yalnızca 5 adet koli değildir; kargonun kapladığı hacim, brüt ağırlık ve istifleme davranışı da hesaplanabilmelidir.

### Ürün ve ambalaj fiziksel alanları

| Alan grubu | Ürün temel birimi | Paket/koli/palet ambalajı |
|---|---|---|
| Boyut | `length`, `width`, `height` ve `dimension_uom` | Ambalajın dış ölçüsü; örneğin mm |
| Ağırlık | Net birim ağırlığı veya kg başına ürün bilgisi | `net_weight`, `tare_weight`, `gross_weight` |
| Hacim | Birim hacmi veya hesaplanabilir ölçüler | `volume` veya boyutlardan hesaplanan hacim |
| Fiziksel kurallar | Kırılabilirlik, yön, taşınabilirlik | `is_stackable`, `max_stack_count`, `max_load_kg` |
| Ambalaj tipi | Ürün formu | Kutu, koli, palet, shrink, çuval vb. |

Sistem içinde ölçüleri normalize etmek için uzunluk `mm`, ağırlık `g` veya `kg`, hacim `mm³` veya `m³` olarak saklanabilir; kullanıcı arayüzü şirket standardına göre `cm`, `kg` ve `m³` gösterebilir. Birim kodu ve precision her kayıtta açık olmalıdır.

Önerilen fiziksel nesneler:

| Entity | Sorumluluk |
|---|---|
| `ProductPhysicalProfile` | Temel ürünün fiziksel ölçüsü, net ağırlığı, hacmi ve taşıma kuralları |
| `PackagingPhysicalProfile` | Paket/koli/palet dış ölçüsü, dara ağırlığı, brüt ağırlığı ve istifleme kuralları |
| `PalletType` | Euro palet, standart palet veya şirket içi palet ölçüsü; kapasite ve dara |
| `LoadPlan` | Bir sevkiyatın araç/kargo yükleme planı |
| `LoadUnit` | Tek palet, kafes, koli grubu veya bağımsız yük birimi |
| `LoadUnitItem` | Yük birimindeki ürün/ambalaj miktarı ve temel karşılığı |

### Fiziksel hesap kuralları

Bir yük satırı için sistem şu değerleri üretmelidir:

```text
base_quantity
packaging_quantity
net_weight
packaging_tare
gross_weight = net_weight + packaging_tare
volume
```

`5 Koli` için örnek:

```text
1 Koli = 600 mm × 400 mm × 300 mm
1 Koli net ağırlığı = 12 kg
Koli dara ağırlığı = 0,5 kg
5 Koli net ağırlığı = 60 kg
5 Koli brüt ağırlığı = 62,5 kg
Toplam hacim = 5 × 0,072 m³ = 0,36 m³
```

Bu değerler ürünün gerçek ana verisinden hesaplanır; örnekteki sayılar yalnızca modelin nasıl çalışacağını göstermek içindir.

## 11. Karışık palet ve kargo planlama

Aynı palet üzerinde farklı ürün veya farklı ambalaj seviyeleri taşınabilir. Karışık palet için `Pallet` ayrı bir ürün olarak oluşturulmaz; sevkiyata bağlı bir `LoadUnit` olarak planlanır.

```text
Shipment
  └─ LoadPlan
      ├─ LoadUnit: Palet-001
      │   ├─ Product A / 3 Koli
      │   └─ Product B / 6 Koli
      └─ LoadUnit: Palet-002
          └─ Product C / 1 Palet
```

### Kargo planlama akışı

1. Sistem seçilen sevkiyatın irsaliye kalemlerini ve temel miktarlarını toplar.
2. Her kalem için ambalaj ölçüsü, brüt ağırlık, hacim ve istifleme kuralını hesaplar.
3. Kullanıcı araç veya kargo tipi seçer; araç kapasitesi ağırlık, hacim, palet adedi ve ölçü sınırlarıyla tanımlıdır.
4. Sistem önce bir **uygunluk ön kontrolü** yapar: toplam ağırlık, toplam hacim, palet kapasitesi, ürün taşma riski ve istiflenemeyen ürünler.
5. Sistem önerilen bir yük planı oluşturabilir; ancak ilk sürümde sonuç “optimal” kabul edilmez. Depo sorumlusu öneriyi düzenleyip manuel olarak onaylar.
6. Karışık palet satırları bir `LoadUnit` altında toplanır. Her satırda ürün, ambalaj, miktar, temel miktar, ağırlık ve hacim görünür.
7. Plan kesinleştiğinde palet etiketi/barkodu üretilir ve sevkiyat doğrulamasına bağlanır.

### Planlama ekranı

```text
Sevkiyat: SHP-2026-000142       Araç: Kamyon-03
Kapasite: 1.200 kg | 8,0 m³ | 4 palet
Kullanım: 426 kg | 2,4 m³ | 2 palet
Durum: Uygun

PALLET-001  Karışık Palet
├─ Premium Napkin 33x33   3 Koli   36 kg   0,216 m³
└─ Kokteyl Napkin 24x24   6 Koli   78 kg   0,468 m³

[ Uygunluğu Hesapla ] [ Palet Ekle ] [ Kalem Ata ] [ Planı Kilitle ]
```

### Karışık palet kuralları

- Aynı palete farklı ürünler ancak fiziksel uyumluluk ve istifleme kuralları izin veriyorsa eklenebilir.
- Kırılabilir, ezilebilir veya üstüne yük konulamaz ürünler için `max_stack_count = 1` veya `is_stackable = false` dikkate alınır.
- Palet kapasitesi ağırlık, hacim ve maksimum ölçü açısından ayrı ayrı kontrol edilir; yalnızca toplam koli adedi yeterli kontrol değildir.
- Bir yük birimindeki miktarların toplamı bağlı irsaliye/sevkiyat miktarını aşamaz.
- Kargo planı sevkiyatı değiştirmez; yalnızca sevkiyat kalemlerinin fiziksel yük birimlerine nasıl dağıtıldığını belirler.
- Plan kilitlendikten sonra değişiklik yeni versiyon/audit kaydı üretir.
- Gerçek yükleme sırasında barkodla doğrulama yapılır; planlanan ve gerçekleşen yük farkı açıklama gerektirir.

### 11.1 Detaylı lojistik kural ve algoritma referansı

Ayrıntılı hard constraint, soft constraint, araç kapasite eşleştirme, karışık palet, durak erişimi ve açıklanabilir heuristik planlama kuralları `logistics-planning-rules-and-algorithms.md` dosyasında tanımlıdır.

MVP planlama sırası:

```text
Normalize et
→ Hard constraint verisini kontrol et
→ Uygun araç adaylarını çıkar
→ Kalemleri fiziksel uyumluluk ve hacim/ağırlığa göre sırala
→ First Fit Decreasing + kısıt kontrolü ile LoadUnit ata
→ Durak erişimini kontrol et
→ Uygunluk ve kullanım özetini üret
→ Depo sorumlusuna manuel düzenleme sun
→ Planı versiyon/audit ile kilitle
```

Hard constraint ihlali planı bloke eder. Soft constraint ihlali açıklanabilir uyarı veya skor üretir. Sistem ilk sürümde optimal 3D yükleme veya kesin rota optimizasyonu garantisi vermez.

## 12. Araç, rota ve alıcı paket takibi

Kargo planı tamamlandığında sevkiyata bir `VehicleType`, gerçek `Vehicle`, `Driver` ve çok duraklı `RoutePlan` atanır. Araç tipi kapasite şablonunu; gerçek araç plaka, aktiflik ve anlık durum bilgisini taşır.

```text
Shipment
  ├─ Vehicle: 34 ABC 123 / Panelvan / InTransit
  ├─ RoutePlan
  │   ├─ RouteStop 1: Müşteri A / Teslim edildi
  │   ├─ RouteStop 2: Müşteri B / Sırada
  │   └─ RouteStop 3: Müşteri C / Sırada
  └─ ShipmentPackage
      ├─ PALLET-001 / Müşteri A / 3 Koli
      ├─ PALLET-001 / Müşteri B / 1 Palet + 4 Paket
      └─ BOX-0042 / Müşteri C / 6 Paket
```

`ShipmentPackage` tek bir palet, koli, paket veya barkodlanabilir yük birimidir. Her kayıt müşteri, teslim adresi, rota durağı, bağlı ürün/ambalaj, temel miktar ve durum bilgisi taşır. Bu sayede karışık bir paletin içindeki farklı müşterilere giden parçalar birbirinden ayrıştırılabilir.

| Seviye | İzlenen bilgi |
|---|---|
| Araç | Plaka, araç tipi, kapasite, araç durumu, aktif rota |
| Rota | Durak sırası, planlanan/gerçekleşen zaman, toplam durum |
| Durak | Müşteri, seçilmiş adres, iletişim, teslim alan, kanıt, istisna |
| Paket | Barkod, ürün, ambalaj, temel miktar, alıcı durak, teslim durumu |

Araç ve sevkiyat durumları aynı şey değildir. Araç `Available`, `Assigned`, `Loading`, `InTransit`, `Maintenance` veya `OutOfService`; sevkiyat `Preparing`, `Loaded`, `InTransit`, `PartiallyDelivered`, `Delivered`, `Exception` veya `Returned` olabilir. Durak ve paket durumları ayrıca izlenir.

Kullanıcı arayüzünde şu sorgu desteklenir:

> **Barkod, müşteri, adres, araç veya sevkiyat numarasıyla arama yapıldığında yükün nerede olduğu, kime gideceği, hangi durakta bulunduğu ve teslim edilip edilmediği gösterilir.**

## 13. Uygulama öncesi kabul kriterleri

- [ ] Her ürün için `base_uom` tanımlanabiliyor.
- [ ] Ürün altında birden fazla ambalaj seviyesi tanımlanabiliyor.
- [ ] `5 Koli` gibi bir giriş doğru temel miktara dönüşüyor.
- [ ] Aynı ürün için farklı ambalaj barkodları ayırt ediliyor.
- [ ] Stok ledger'ı yalnızca temel birimde hareket oluşturuyor.
- [ ] Sipariş, irsaliye ve sevkiyat ekranlarında ambalaj + temel miktar birlikte görünüyor.
- [ ] Parçalı ambalaj açık kırılımla gösteriliyor.
- [ ] Ambalaj katsayısı değiştiğinde geçmiş belge snapshot'ı bozulmuyor.
- [ ] Public katalog kullanıcıya paket/koli içeriğini anlaşılır biçimde gösteriyor.
- [ ] Araç tipi ve gerçek araç kapasitesi tanımlanabiliyor.
- [ ] Bir sevkiyata araç, şoför ve çok duraklı rota atanabiliyor.
- [ ] Her palet/koli/paket barkodu müşteri ve teslim adresiyle eşleştirilebiliyor.
- [ ] Araç, sevkiyat, durak ve paket durumları ayrı ayrı takip edilebiliyor.
- [ ] Kısmi teslim, teslim edilememe, iade ve teslim kanıtı kaydedilebiliyor.

**Kapsam notu:** Bu belge yeni bir ürün/ambalaj gereksinimi olarak canonical tasarıma eklenmiştir. Ürünlerin gerçek ambalaj katsayıları, ürün ana verisi hazırlanırken operasyon sorumlusu tarafından doldurulmalıdır.

**Hazırlayan:** Manus AI
**Tarih:** 16 Ağustos 2026
