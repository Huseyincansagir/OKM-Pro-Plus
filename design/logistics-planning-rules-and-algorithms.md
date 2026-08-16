# Lojistik Kural Seti ve Kargo Planlama Algoritmaları
## Karışık Palet · Araç Kapasitesi · Çok Duraklı Sevkiyat

**Durum:** Kodlama öncesi teknik/operasyonel tasarım
**Kapsam:** `LoadPlan`, `LoadUnit`, `LoadUnitItem`, `VehicleType`, `VehicleCapacity`, `RoutePlan`, `RouteStop`, `ShipmentPackage`
**MVP otomasyon seviyesi:** Uygunluk doğrulaması + açıklanabilir sezgisel öneri + depo sorumlusu manuel onayı
**Önemli sınır:** İlk sürüm matematiksel olarak optimal 3D yükleme veya kesin en kısa rota garantisi vermez.

## 1. Tasarım hedefi

Algoritmanın amacı “en iyi planı bulduğunu iddia etmek” değil, **taşınabilir, fiziksel olarak uyumlu, müşteri duraklarına göre erişilebilir ve depo sorumlusu tarafından denetlenebilir bir plan önermek** olmalıdır.

Planlama çıktısı şu soruları cevaplamalıdır:

1. Hangi araç bu sevkiyatı fiziksel olarak taşıyabilir?
2. Hangi palet/koli/paket hangi yük birimine atanmalıdır?
3. Karışık palet içeriği fiziksel ve operasyonel olarak uyumlu mudur?
4. Çok duraklı rotada hangi yük önce/sonra boşaltılacağı için yükleme sırası nasıl olmalıdır?
5. Kapasite, istifleme, ölçü, alıcı veya miktar kaynaklı hangi uyarılar vardır?
6. Depo sorumlusu öneriyi değiştirdiyse fark ve gerekçe nedir?

## 2. Ölçü ve ana veri standardı

### 2.1 Normalize saklama birimleri

| Değer | Saklama standardı | UI gösterimi |
|---|---|---|
| Uzunluk/genişlik/yükseklik | `mm` | `cm` veya `mm` |
| Ağırlık | `g` veya `kg`; tek sistem seçilmeli | `kg` |
| Hacim | `mm³` veya `m³`; tek sistem seçilmeli | `m³` |
| Miktar | Ürünün `base_uom` değeri | adet, kg vb. |
| Zaman | UTC | Türkiye lokal zamanı |

Aynı sistemde karışık ölçü birimleri kullanılmamalıdır. Dönüşüm backend'de yapılır; frontend'den gelen `quantity_base`, ağırlık veya hacim tek başına doğruluk kaynağı kabul edilmez.

### 2.2 Ürün ve ambalaj lojistik profili

Her ürün veya satılabilir ambalaj seviyesi için aşağıdaki alanlar tanımlanmalıdır:

| Alan grubu | Örnek alanlar | Zorunluluk |
|---|---|---|
| Geometri | `length`, `width`, `height`, `volume` | Yük planında zorunlu |
| Ağırlık | `net_weight`, `tare_weight`, `gross_weight` | Yük planında zorunlu |
| İstifleme | `is_stackable`, `max_stack_count`, `max_load_above` | Zorunlu |
| Yön | `allowed_orientations`, `keep_upright` | Gerekiyorsa zorunlu |
| Fiziksel hassasiyet | `is_crushable`, `is_fragile`, `max_compression` | Ürüne göre |
| Uyumluluk | `compatibility_group`, `incompatible_groups` | Karışık palet için |
| Yükleme | `can_be_top_loaded`, `can_be_bottom_loaded`, `loading_access_class` | Operasyon için |
| Parçalanma | `allow_partial`, `split_allowed_for_shipment` | Ambalaj kuralına bağlı |

Ürün fiziksel profili eksikse algoritma “uygun” sonucu üretmemeli; **“Fiziksel ana veri eksik”** durumunu bloke veya yapılandırılabilir uyarı olarak göstermelidir.

## 3. Kural sınıfları

### 3.1 Hard constraint — ihlal edilirse plan geçersiz

Aşağıdaki kontroller server-side yapılır:

| Kural | Kontrol |
|---|---|
| Araç uygunluğu | Araç aktif, bakımda değil ve plan tarih/saatinde başka rota ile çakışmıyor |
| Brüt ağırlık | Toplam yük + palet/yük birimi darası ≤ araç maksimum brüt ağırlığı |
| Hacim | Toplam yük hacmi ≤ araç kullanılabilir hacmi |
| İç ölçü | Her palet/yük birimi araç iç ölçüsü ve kapı açıklığına sığıyor |
| Palet kapasitesi | Palet/yük birimi sayısı ve izin verilen palet tipi kapasiteyi aşmıyor |
| Yük birimi | Her `LoadUnit` kendi maksimum ağırlık, ölçü ve hacim sınırlarını aşmıyor |
| Miktar | Atanan temel miktar, shipment/irsaliye kalan miktarını aşmıyor |
| Barkod bütünlüğü | Aynı `ShipmentPackage` iki yük birimine veya iki alıcıya atanamıyor |
| Alıcı bağlantısı | Her paket müşteri, teslim adresi ve `RouteStop` ile eşleşmiş olmalı |
| Uyumluluk | `incompatible_groups` aynı karışık palete alınamıyor |
| İstifleme | İstiflenemeyen ürünün üzerine başka yük konulamıyor; kat/yük limiti aşılamıyor |
| Yön | `keep_upright` veya izin verilen yön kuralları ihlal edilemiyor |
| Durak erişimi | Yükleme planı, daha erken boşalacak yükü daha sonra boşalacak yükün arkasında erişilemez bırakmamalı; istisna açıkça işaretlenmeli |
| Durum | `LoadPlan.Locked` ve geçerli rota olmadan araç çıkışı yapılamıyor |

### 3.2 Soft constraint — ihlal edilirse skor/uyarı üret

Soft kurallar planı otomatik olarak geçersiz kılmaz; kullanıcıya gerekçe ve etki gösterilir:

- Aynı müşteri ve aynı durak yüklerini aynı palet/yük biriminde toplamak.
- Aynı ürün ailesini mümkün olduğunca bir arada tutmak.
- Kullanılabilir hacim ve ağırlığı dengeli kullanmak.
- Palet sayısını azaltmak; ancak paket ayrıştırma ve erişilebilirliği bozmamak.
- Daha erken durak yükünü kapıya yakın tutmak.
- Karışık palet sayısını azaltmak.
- Ağır yükleri altta, hafif ve ezilebilir yükleri üstte taşımak.
- Araç içinde boşlukları ve beklenmeyen erişim koridorlarını azaltmak.
- Aynı araçta çoklu sevkiyat varsa farklı sevkiyatların karışmasını önlemek.

## 4. Araç kapasite eşleştirme

### 4.1 Araç ana verisi

`VehicleType` kapasite şablonudur; `Vehicle` gerçek plakalı araçtır. Eşleştirme için en az şu veriler gerekir:

| Veri | Açıklama |
|---|---|
| İç ölçüler | `inner_length`, `inner_width`, `inner_height` |
| Kapı açıklığı | `door_width`, `door_height` |
| Maksimum brüt ağırlık | Yük + palet + ambalaj dahil limit |
| Kullanılabilir hacim | m³ |
| Palet kapasitesi | Maksimum palet veya yük slotu |
| İzin verilen palet tipleri | Euro, standart, şirket içi vb. |
| İstifleme sınırı | Maksimum kat veya yükseklik |
| Boşaltma yüzü | Arka, yan, iki taraflı veya üstten |
| Opsiyonel aks limitleri | Girilmişse aks ağırlığı kontrolü yapılır |
| Tarihsel durum | Bakım, kullanım dışı, başka rota çakışması |

Araç ana verisinde aks veya yük dağılımı bilgisi yoksa sistem aks güvenliği garantisi vermemeli; yalnızca toplam kapasite kontrolü yaptığını açıkça belirtmelidir.

### 4.2 Eşleştirme sırası

```text
1. Aktif ve tarih/saat açısından müsait araçları al
2. Bakımda veya kullanım dışı araçları ele
3. Toplam brüt kg ön kontrolü
4. Toplam m³ ön kontrolü
5. Palet adedi ve palet tipi kontrolü
6. En büyük yük birimi + kapı açıklığı kontrolü
7. Tüm yük birimlerinin iç ölçülere sığma kontrolü
8. İstifleme ve durak erişimi kontrolü
9. Uygun araçları skorla
10. En açıklanabilir adayı öner
```

### 4.3 Önerilen aday skorlaması

Bu ağırlıklar MVP başlangıç parametresidir; gerçek operasyon verisiyle değiştirilebilir ve karar loguna bağlı olarak sabit kabul edilmemelidir.

| Skor bileşeni | Önerilen ağırlık | Açıklama |
|---|---:|---|
| Ağırlık kullanım dengesi | 25 | Ne aşırı boş ne de sınıra aşırı yakın kullanım |
| Hacim kullanım dengesi | 25 | Kullanılabilir hacmin verimli kullanılması |
| Palet/slot kullanımı | 15 | Gereksiz araç veya palet slotu oluşturmama |
| Durak erişilebilirliği | 15 | Yükleme/boşaltma sırasının uygulanabilirliği |
| Karışık palet karmaşıklığı | 10 | Farklı müşteri/ürünleri gereksiz karıştırmama |
| Operasyonel uygunluk | 10 | Kapı, palet tipi, istif ve yükleme süresi uyumu |

Hard constraint ihlali olan aday skorlanmaz. Skor yalnızca uygun adaylar arasında karşılaştırma yapar. UI şu açıklamayı vermelidir: “Bu araç seçildi çünkü kapasite kontrollerinden geçti ve durak erişimi en düşük operasyonel riskli adaydır.”

## 5. Karışık palet planlama algoritması

### 5.1 Girdi hazırlama

Her shipment kalemi şu planlama kaydına dönüştürülür:

```text
PlanningItem
- shipment_item_id
- product_id
- packaging_id
- package_count
- quantity_base
- net_weight
- gross_weight
- volume
- dimensions
- compatibility_group
- stackability_rules
- customer_id
- route_stop_id
- delivery_sequence
- split_allowed
```

Kalemler önce şu gruplara ayrılır:

1. Sert uyumluluk grubu (`compatibility_group`).
2. Müşteri/durak grubu.
3. Ambalaj ve fiziksel boyut grubu.
4. İstifleme/hassasiyet grubu.

### 5.2 MVP sezgisel yöntem

İlk sürüm için açıklanabilir **First Fit Decreasing + kısıt kontrolü** yaklaşımı önerilir:

```text
1. Planlama kalemlerini normalize et
2. Hard constraint verisi eksik olanları hata listesine al
3. Kalemleri önce uyumluluk/hassasiyet, sonra hacim ve ağırlığa göre sırala
4. Her kalem için mevcut LoadUnit adaylarını dolaş
5. Hard constraint ihlali olmayan adayları çıkar
6. Adayları soft-constraint ceza skoruna göre sırala
7. En düşük cezalı LoadUnit'e ata
8. Uygun aday yoksa yeni LoadUnit oluştur
9. Aynı kalem bölünebiliyorsa kalan miktar için devam et
10. Bölünemiyorsa kalemi bütün olarak yeni birime taşı
11. Durak erişim sırasını kontrol et
12. Araç kapasitesini tekrar hesapla
13. ValidationSummary ve öneri skorunu kaydet
14. Depo sorumlusuna manuel düzenleme sun
```

### 5.3 Aday LoadUnit ceza skoru

Hard constraint'i geçen her aday için aşağıdaki ceza bileşenleri hesaplanabilir:

```text
Penalty =
  NewLoadUnitPenalty
+ RemainingSpacePenalty
+ MixedCustomerPenalty
+ StopAccessPenalty
+ StackRiskPenalty
+ CompatibilityPenalty
```

`CompatibilityPenalty` veya `StackRiskPenalty` hard kural kapsamındaysa aday doğrudan elenir. Soft kabul edilen durumlarda ceza olarak gösterilir. Amaç algoritmanın neden bir palet seçtiğinin açıklanabilmesidir.

### 5.4 Karışık palet izin matrisi

| Durum | Varsayılan davranış |
|---|---|
| Aynı ürün, aynı müşteri, aynı durak | Aynı palete al; en düşük karmaşıklık |
| Farklı ürün, aynı müşteri/durak | Fiziksel uyum varsa al; uyarı yok veya düşük |
| Aynı ürün, farklı durak | Miktar ve erişim uygunsa al; boşaltma sırası zorunlu |
| Farklı ürün, farklı durak | Uygunluk + erişim kontrolü; MVP’de uyarı |
| Uyumsuzluk grupları | Bloke et |
| Ezilebilir ürün üstüne ağır ürün | Bloke et |
| Kırılabilir ve titreşim/ezilme riski | Ayrı yük birimi öner |
| Paket barkodu olmayan gevşek yük | Yükleme öncesi barkod/etiket üret veya manuel kontrol iste |

### 5.5 Çok duraklı yükleme sırası

Rota durakları `1..N` şeklinde sıralıysa, arka kapıdan boşaltılan standart araç varsayımında önerilen yükleme mantığı şöyledir:

```text
Son durak yükleri önce arka iç bölgeye / en derine
İlk durak yükleri kapıya yakın bölgeye
```

Ancak karışık palet tek parça taşınıyorsa, palet içindeki erken durak paketlerine erişim zorlaşabilir. Sistem bu durumu `StopAccessWarning` olarak işaretler ve şu seçenekleri sunar:

- Paleti durak bazında ayır.
- Paketleri barkodlu alt yük birimlerine böl.
- Farklı boşaltma yüzü olan araç seç.
- Yetkili override ile planı kilitle; açıklama zorunlu olsun.

## 6. Çoklu sevkiyat ve araç paylaşımı

Bir araç aynı zaman aralığında birden fazla sevkiyat taşıyacaksa:

- Her shipment ayrı `LoadPlanSegment` veya ayrı renk/etiketle ayrıştırılır.
- Araç toplam kapasitesi tüm sevkiyatların toplamı üzerinden kontrol edilir.
- Farklı müşterilere ait paketler barkod seviyesinde izlenir.
- Rota ve durak sıraları çakışıyorsa plan bloke edilir.
- Bir shipment'ın teslim kanıtı diğer shipment'ın durumunu değiştirmez.

İlk sürümde aynı araçta çoklu shipment desteği aktif edilecekse planlama ekranı shipment bazlı alt bölümler göstermelidir; aksi halde MVP’de tek araç–tek aktif plan varsayımıyla başlanmalıdır. Bu konu karara bağlıdır ve `decision-log.md` içinde açıkça tutulmalıdır.

## 7. Plan sonucu ve açıklanabilirlik

`LoadPlan` çıktısı yalnızca palet listesi olmamalıdır. En az şu bilgileri üretmelidir:

| Çıktı | İçerik |
|---|---|
| Feasibility | `Feasible`, `FeasibleWithWarnings`, `Infeasible` |
| VehicleFit | Seçilen araç, kapasite snapshot'ı ve uygunluk gerekçesi |
| LoadUnits | Palet/yük birimleri ve iç kalemler |
| StopAccess | Durak bazlı erişim uyarıları |
| Utilization | kg, m³, palet ve ölçü kullanım oranları |
| ValidationSummary | Error/warning kodları ve açıklamaları |
| AlgorithmMetadata | Algoritma sürümü, parametre seti, oluşturma zamanı |
| ManualChanges | Kullanıcı değişikliği, eski/yeni atama ve gerekçe |

Öneri ekranında “neden bu araç/palet?” açıklaması bulunmalıdır. Kullanıcı yalnızca renkli bir skor görmemeli; bloke veya uyarının hangi fiziksel kuraldan kaynaklandığını okuyabilmelidir.

## 8. Replan ve kilit kuralları

Aşağıdaki değişiklikler planı yeniden doğrulamayı zorunlu kılar:

- Shipment miktarı veya irsaliye kalemi değişikliği.
- Araç tipi, araç, kapasite veya palet tipi değişikliği.
- Rota durağı veya teslimat sırası değişikliği.
- Ürün/ambalaj fiziksel profilinin etkili katsayılarının değişmesi.
- Manuel palet kalemi ekleme/çıkarma.
- Barkodlu gerçek yük ile plan arasındaki fark.
- Yükleme tarih/saatinin araç çakışması oluşturması.

`LoadPlan.Locked` sonrasında sessiz düzenleme yapılmaz. Değişiklik yeni plan versiyonu, validation sonucu ve audit kaydı üretir. `shipment.depart` yalnızca geçerli ve kilitli planla çalışır.

## 9. QA kabul senaryoları

- Ağırlık kapasitesini aşan araç adayları elenir.
- Hacim yeterli görünse bile kapı açıklığına sığmayan palet adayı elenir.
- Palet kapasitesi uygun olsa bile istiflenemeyen ürünün üstüne yük konulamaz.
- Farklı `incompatible_groups` aynı karışık palete atanamaz.
- İlk durak yükünün son durak yükünün arkasında kalması uyarı üretir veya bloklanır.
- Aynı barkod ikinci `LoadUnit` veya farklı müşteriyle eşleşemez.
- Shipment miktarı aşılırsa plan kilitlenemez.
- Araç bakımda veya başka rota ile çakışıyorsa aday olamaz.
- Manuel override gerekçe ve yetki üretir.
- Plan kilitlendikten sonra yapılan değişiklik yeni versiyon ve audit üretir.
- Algoritma aynı girdi ve parametrelerle tekrar çalıştırıldığında aynı öneriyi veya sürüm farkını açıklayabilir.
- Gerçek yükleme farkları planned-versus-actual raporuna düşer.

## 10. Karar ve kapsam notu

Bu belge **MVP için açıklanabilir heuristik planlama** önerir. Gerçek 3D bin-packing, aks ağırlığı optimizasyonu, trafik bazlı kesin rota optimizasyonu veya otomatik araç rezervasyonu ancak saha verisi, araç ana verisi ve proje sahibi kararı sonrası ayrı kapsam olarak ele alınmalıdır.

**Hazırlayan:** Manus AI
**Tarih:** 16 Ağustos 2026

## References

Bu belge harici veri kullanmaz; repository içindeki canonical domain, database, workflow ve UI kararlarını teknik bir algoritma kural setinde birleştirir:

- [`domain-model.md`](./domain-model.md)
- [`database-technical-architecture.md`](./database-technical-architecture.md)
- [`business-workflows.md`](./business-workflows.md)
- [`product-packaging-and-uom.md`](./product-packaging-and-uom.md)
- [`shipment-logistics-ui-design.md`](./shipment-logistics-ui-design.md)
