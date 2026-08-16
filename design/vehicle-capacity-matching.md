# Araç Kapasite Eşleştirme ve Yük Uygunluğu
## Araç tipi · gerçek araç · kapasite profili · mixed-pallet · çok duraklı yük

**Durum:** Kodlama öncesi canonical lojistik tasarımı
**Kapsam:** `VehicleType`, `Vehicle`, `VehicleCapacity`, `LoadPlan`, `LoadUnit`, `LoadUnitItem`, `RoutePlan`, `RouteStop`
**Amaç:** Bir sevkiyatın hangi araçla güvenli ve operasyonel olarak taşınabileceğini açıklanabilir biçimde belirlemek
**MVP sınırı:** Hard constraint doğrulaması + açıklanabilir aday önerisi + manuel depo onayı; optimal 3D bin-packing veya kesin trafik optimizasyonu yoktur.

## 1. Kapasite eşleştirmenin temel yaklaşımı

Araç seçimi yalnızca “kaç kilo taşır?” sorusuna indirgenmemelidir. Bir araç ancak aşağıdaki beş katmanın tamamı uyumluysa aday olarak kabul edilir:

```text
VehicleType kapasite şablonu
  → Vehicle gerçek araç ve çalışma durumu
      → VehicleCapacity tarihsel kapasite profili
          → LoadPlan fiziksel yük planı
              → RoutePlan/RouteStop boşaltma erişimi
```

| Katman | Sorduğu soru | Sonuç |
|---|---|---|
| Ticari miktar | Shipment miktarı doğru mu? | Atanan temel miktar kalan miktarı aşmamalı |
| Fiziksel kapasite | Yük kg, m³, ölçü ve palet sınırına sığıyor mu? | Hard fit veya elenme |
| Yükleme uyumluluğu | Palet, kapı, yön ve istifleme uygulanabilir mi? | Hard fit veya warning |
| Araç kullanılabilirliği | Araç aktif, bakımsız değil ve zaman aralığı müsait mi? | Aday veya elenme |
| Rota erişimi | Durak sırasına göre paketlere erişilebilir mi? | Skor, warning veya elenme |

## 2. Araç ana veri modeli

### 2.1 `VehicleType`

`VehicleType` aynı tip araçların kapasite şablonudur. Örnek adlar `Panelvan`, `Kamyonet`, `Kamyon` veya `Tır` olabilir; gerçek kapasite değerleri şirketin araç ana verisinden girilir.

| Alan grubu | Alanlar | Kullanım |
|---|---|---|
| Kimlik | `code`, `name` | Tipin seçimi ve raporlanması |
| İç hacim | `inner_length`, `inner_width`, `inner_height` | Yükün fiziksel sığması |
| Kapı | `door_width`, `door_height`, `door_depth` | Paletin/kutunun kapıdan girişi |
| Ağırlık | `max_gross_weight`, `tare_weight`, `max_payload_weight` | Brüt ve net taşıma sınırı |
| Hacim | `max_usable_volume` | Kullanılabilir m³ sınırı |
| Zemin | `floor_length`, `floor_width`, `floor_load_limit` | Palet izi ve zemin yükü |
| Palet | `max_pallet_count`, `allowed_pallet_type_ids`, `pallet_slot_count` | Palet standardı ve slotları |
| Yükseklik | `max_load_height`, `max_stack_count` | İstifleme ve iç yükseklik |
| Erişim | `loading_sides`, `unloading_sides`, `door_count` | Durak bazlı boşaltma |
| Dağılım | `axle_count`, `axle_limits`, `load_zone_limits` | Veri varsa aks/yük bölgesi kontrolü |
| Operasyon | `loading_time_factor`, `is_active` | Planlama ve kapasite tahmini |

`max_payload_weight` doğrudan verilmemişse sistem bunu `max_gross_weight - tare_weight` üzerinden türetir; ancak hesaplanan değerin araç ana verisiyle ayrıca doğrulanması gerekir.

### 2.2 `Vehicle`

`Vehicle` plakalı gerçek araçtır. Tip kapasitesini miras alır, fakat gerçek araçta bakım veya farklı ekipman nedeniyle kullanılabilir kapasite değişebilir.

| Alan | Açıklama |
|---|---|
| `vehicle_type_id` | Kapasite şablonu |
| `plate_number` | Plaka; aktif kayıtlar arasında unique |
| `status` | `Available`, `Assigned`, `Loading`, `InTransit`, `Maintenance`, `OutOfService` |
| `maintenance_until` | Yeni sevkiyata atanamayacağı zaman |
| `capacity_override` | Yetkili ve tarihli araç özel kapasite farkı |
| `current_route_plan_id` | Aktif rota |
| `last_known_location` | Operasyonel takip bilgisi |
| `last_status_at` | Durum değişikliği zamanı |
| `row_version` | Concurrency kontrolü |

`capacity_override` sessiz bir sayı değişikliği olmamalıdır. Override nedeni, kullanıcı, tarih, eski değer ve yeni değer audit kaydına yazılmalıdır.

### 2.3 `VehicleCapacity`

Kapasite profili tarihsel ve geçerlilik dönemli tutulmalıdır. Araç lastiği, kasa, raf veya ekipman değişiklikleri geçmiş planların kapasite snapshot'ını bozmayacak şekilde yeni profile dönüşür.

```text
VehicleType
  └─ VehicleCapacity [effective_from, effective_to]
       └─ Vehicle
```

Plan oluşturulduğunda kullanılan kapasite profili `capacity_snapshot` olarak `LoadPlan` içine kopyalanır.

## 3. Yük tarafının normalize edilmesi

Her shipment kalemi araç eşleştirme öncesinde fiziksel `PlanningItem` kaydına çevrilir.

| Hesap | Formül |
|---|---|
| Temel miktar | `entered_quantity × quantity_in_base_uom` |
| Net ağırlık | `base_quantity × net_weight_per_base_unit` veya ambalaj net ağırlığı |
| Ambalaj darası | Ambalaj/koli/palet daralarının toplamı |
| Brüt ağırlık | `net_weight + packaging_tare + load_unit_tare` |
| Hacim | `length × width × height × quantity` veya paket profili hacmi |
| Palet ayak izi | `pallet_length × pallet_width` |
| Yükseklik | Palet darası + katmanlar + üst yük |
| Durak sırası | `RouteStop.sequence_no` |

Aynı yükün hem ürün profilinden hem ambalaj profilinden ağırlık/hacim üretildiği durumlarda hangi profilin source of truth olduğu önceden tanımlanmalıdır. Çift sayım yapılmamalıdır.

## 4. Kapasite kullanım hesapları

### 4.1 Ağırlık

```text
payload_used = Σ(load_unit.gross_weight)
weight_usage_ratio = payload_used / usable_payload_weight
remaining_weight = usable_payload_weight - payload_used
```

`usable_payload_weight`, araç brüt ağırlığından aracın gerçek dara ağırlığı ve gerekirse sabit ekipman/şoför ağırlığı düşülerek hesaplanır. Şoför veya ekipman bilgisi sistemde yoksa bu eksiklik açık uyarı olarak gösterilir; sistem yasal/teknik güvence iddiasında bulunmaz.

### 4.2 Hacim

```text
volume_used = Σ(load_unit.volume)
volume_usage_ratio = volume_used / usable_volume
remaining_volume = usable_volume - volume_used
```

Düz toplam hacim, gerçek yerleşim garantisi değildir. Düzensiz şekilli, erişim koridoru gerektiren veya istiflenemeyen yükler için `space_penalty` ve `access_penalty` uygulanır.

### 4.3 Palet ve zemin alanı

```text
pallet_count_used = count(load_units where unit_type = Pallet)
pallet_usage_ratio = pallet_count_used / max_pallet_count
floor_area_used = Σ(load_unit.floor_footprint)
floor_area_ratio = floor_area_used / usable_floor_area
```

Palet adedi kapasiteye sığsa bile palet ayak izleri araç zeminine yerleşmiyorsa aday elenir. Dönme yönleri `allowed_orientations` ile kontrol edilir.

### 4.4 İç yükseklik ve katlar

```text
stack_height = pallet_height + Σ(layer_height)
height_usage_ratio = stack_height / max_load_height
```

`is_stackable = false`, `max_stack_count = 1`, `max_load_above = 0` veya `keep_upright = true` olan ürünler için üst yük ve yön kontrolü hard constraint'tir.

## 5. Araç adaylarının elenme sırası

Sistem önce ucuz ve kesin kontrolleri, sonra yerleşim ve rota kontrollerini çalıştırmalıdır. Her elenme adayı kullanıcıya neden kodu ile gösterilir.

```text
1. Araç aktif mi?
2. MaintenanceUntil geçmiş mi?
3. Plan tarihi başka route plan ile çakışıyor mu?
4. Araçta kapasite override veya geçerli kapasite profili var mı?
5. Toplam brüt ağırlık sığıyor mu?
6. Toplam hacim sığıyor mu?
7. Palet adedi ve izin verilen palet tipleri uyuyor mu?
8. En büyük yük birimi kapıdan geçiyor mu?
9. Yük birimleri iç ölçü ve zemin ayak izine sığıyor mu?
10. İç yükseklik ve istifleme izinleri geçerli mi?
11. Uyumluluk grupları ve yükleme yönleri uygun mu?
12. Durak sırasına göre paket erişimi mümkün mü?
13. Aks/yük bölgesi verisi varsa dağılım kontrolü
14. Uygun adayları skorla ve açıkla
```

### 5.1 Aday elenme kodları

| Kod | Anlam | UI sonucu |
|---|---|---|
| `VEHICLE_INACTIVE` | Araç aktif değil | Aday listesinde elendi |
| `VEHICLE_MAINTENANCE` | Bakım aralığında | Bakım tarihi gösterilir |
| `VEHICLE_SCHEDULE_CONFLICT` | Başka rota ile zaman çakışması | Çakışan rota gösterilir |
| `WEIGHT_EXCEEDED` | Brüt/net ağırlık aşıldı | Kırmızı hard error |
| `VOLUME_EXCEEDED` | Hacim aşıldı | Kırmızı hard error |
| `PALLET_CAPACITY_EXCEEDED` | Palet/slot sayısı aşıldı | Kırmızı hard error |
| `DIMENSION_MISMATCH` | Yük iç ölçülere sığmıyor | Kırmızı hard error |
| `DOOR_OPENING_MISMATCH` | Yük kapı açıklığından geçmiyor | Kırmızı hard error |
| `STACKING_NOT_ALLOWED` | İstifleme kuralı ihlali | Kırmızı hard error |
| `COMPATIBILITY_BLOCK` | Uyumsuz ürün/ambalaj | Kırmızı hard error |
| `STOP_ACCESS_WARNING` | Durak erişimi operasyonel riskli | Sarı warning veya policy'ye göre block |
| `AXLE_DATA_MISSING` | Aks verisi yok | Gri bilgi/uyarı; aks güvenliği hesaplanmadı |
| `PHYSICAL_PROFILE_MISSING` | Ürün/ambalaj ölçüsü eksik | Kırmızı veya policy'ye göre warning |

## 6. Yerleşim ve yön kontrolü

### 6.1 Boyut yönleri

Bir yük birimi için izin verilen yönler oluşturulur:

```text
allowed_orientations = [
  (L, W, H),
  (W, L, H)
]
```

`keep_upright = true` ise yükseklik ekseni değiştirilemez. Her yön için şu kontrol yapılır:

```text
unit.length <= vehicle.inner_length
unit.width  <= vehicle.inner_width
unit.height <= vehicle.inner_height
unit.door_width  <= vehicle.door_opening_width
unit.height      <= vehicle.door_opening_height
```

En az bir izinli yön geçerli değilse yük birimi o araca atanmaz.

### 6.2 Zemin slotları

MVP’de tam 3D optimizasyon yerine araç zemini basit slotlara bölünebilir:

```text
Vehicle floor
├─ Zone A: kapıya yakın
├─ Zone B: orta
└─ Zone C: iç/arka
```

Her `LoadUnit` için `placement_zone`, `floor_footprint`, `unloading_priority` ve `route_stop_sequence` saklanır. Bu yaklaşım fiziksel planın açıklanabilir olmasını sağlar; gerçek 3D optimum yerleşim iddiası taşımaz.

### 6.3 Durak erişimi

Standart arka kapı boşaltması varsayımında:

```text
Son durak → aracın en iç bölgesi
İlk durak → kapıya en yakın bölge
```

Daha erken durak yükünün daha geç durak yükünün arkasında kalması `STOP_ACCESS_WARNING` üretir. Eğer ürün veya palet parçalanamıyorsa sistem:

1. Farklı araç erişim yüzü önerir.
2. Ayrı palet/yük birimi önerir.
3. Kullanıcıdan erişim override gerekçesi ister.

## 7. Ağırlık dağılımı ve aks kontrolü

Aks veya yük bölgesi bilgisi araç ana verisinde mevcutsa sistem yükü yalnızca toplam ağırlıkla değil, bölgesel dağılımla da kontrol eder.

```text
front_axle_load <= front_axle_limit
rear_axle_load  <= rear_axle_limit
zone_load       <= zone_load_limit
```

Yük merkezi yaklaşık olarak şu şekilde izlenebilir:

```text
load_center_x = Σ(load_weight × load_position_x) / Σ(load_weight)
```

Aks sınırı veya zemin yük bölgesi verisi yoksa sonuç `AxleCheck = NotEvaluated` olarak kaydedilmelidir. Sistem “akslar uygundur” dememeli; yalnızca “toplam kapasite doğrulandı, aks dağılımı hesaplanmadı” demelidir.

## 8. Karışık palet ve araç eşleştirmesi

Karışık palet kararında araç kapasitesi, paletin kendi kapasitesinden ayrı kontrol edilir:

```text
Product/Packaging fit
  → Mixed pallet compatibility
      → PalletType capacity
          → Vehicle floor/door/volume/weight fit
              → Route stop access
```

| Kontrol | Palet içinde | Araç içinde |
|---|---|---|
| Ağırlık | `LoadUnit.max_gross_weight` | `VehicleCapacity.max_payload_weight` |
| Hacim | `LoadUnit.max_volume` | `VehicleCapacity.max_usable_volume` |
| Ölçü | Palet dış ölçüsü | Araç iç ölçü ve kapı açıklığı |
| İstif | Üst/alt yük kuralları | Araç iç kat ve yükseklik |
| Erişim | Palet içi paket sırası | Araç içi durak sırası |
| Barkod | `ShipmentPackage` alt bağlantısı | Shipment/RouteStop bağlantısı |

Aynı araçta farklı müşterilerin paletleri taşınabilir. Ancak karışık palet içindeki her alt paket müşteri, adres ve `RouteStop` ile eşleştirilmeli; fiziksel olarak erişilemeyen paketler warning/block olarak gösterilmelidir.

## 9. Aday skorlaması

Hard constraint geçen adaylar arasında skor üretilir. Skor tek başına karar değildir; gerekçelerle birlikte gösterilir.

### 9.1 Kullanım oranı cezaları

```text
capacity_penalty =
  weight_balance_penalty
+ volume_balance_penalty
+ pallet_slot_penalty
+ floor_area_penalty
+ height_penalty
```

Kapasiteyi yüzde yüz doldurmak her zaman iyi değildir. Operasyon için güvenlik payı ve yükleme erişim alanı bırakılmalıdır. Güvenlik payları şirket politikasıyla parametreleştirilir; kod içine sabitlenmez.

### 9.2 Başlangıç puan bileşenleri

| Bileşen | Ağırlık | Açıklama |
|---|---:|---|
| Ağırlık dengesi | 20 | Sınırı aşmadan dengeli kullanım |
| Hacim dengesi | 20 | Hacim kullanım verimliliği |
| Zemin/slot kullanımı | 15 | Gereksiz palet veya boş slot oluşturmama |
| Durak erişimi | 20 | Önce boşalacak yükün erişilebilirliği |
| Mixed-pallet karmaşıklığı | 10 | Farklı müşteri/ürün karışımını azaltma |
| Kapı/istif/operasyon uyumu | 15 | Depo ve araç operasyonundaki uyum |

```text
candidate_score = 100 - weighted_penalties
```

Ağırlıklar başlangıç parametresidir. Gerçek operasyon verisi olmadan “en doğru” kabul edilmez; `algorithm_version` ve `parameter_set` ile snapshot'lanır.

## 10. Eşleştirme algoritması

```text
Input:
  ShipmentItems
  PhysicalProfiles
  AvailableVehicles
  VehicleCapacityProfiles
  RouteStops
  PlanningPolicy

1. Shipment kalemlerini base quantity, kg, m³, ölçü ve stop sequence ile normalize et
2. Fiziksel profil veya ambalaj katsayısı eksik kalemleri error listesine al
3. Vehicle ve effective VehicleCapacity kayıtlarını getir
4. Maintenance, inactive ve schedule conflict adaylarını ele
5. Her aday için gross weight, volume, pallet, door ve dimension hard check çalıştır
6. Aday yük planı için LoadUnit/LoadUnitItem önerisi üret
7. Mixed-pallet compatibility ve stacking check çalıştır
8. Stop access ve placement zone kontrolü yap
9. Aks verisi varsa axle/zone check; yoksa NotEvaluated yaz
10. Hard failure varsa candidate rejected + reason code
11. Geçen adaylar için weighted penalty ve score hesapla
12. En düşük operasyonel riskli adayı öner
13. Alternatif adayları skor ve elenme/uyarı nedenleriyle göster
14. Depo sorumlusu kabul, manuel düzenleme veya replan seçer
15. Yeni planı tekrar validate et
16. LoadPlan.Locked + capacity snapshot + algorithm metadata + audit üret
```

## 11. Araç önerisi response örneği

```json
{
  "shipmentId": "SHP-2026-000142",
  "status": "FeasibleWithWarnings",
  "recommendedVehicle": {
    "vehicleId": "vehicle-03",
    "plateNumber": "34 ABC 123",
    "vehicleType": "Panelvan",
    "score": 86.4,
    "reason": "Ağırlık, hacim ve kapı kontrollerinden geçti; durak erişiminde düşük riskli aday."
  },
  "capacity": {
    "weight": { "used": 426, "available": 650, "ratio": 0.655 },
    "volume": { "used": 2.4, "available": 4.5, "ratio": 0.533 },
    "pallets": { "used": 2, "available": 4, "ratio": 0.5 },
    "axleCheck": "NotEvaluated"
  },
  "warnings": [
    {
      "code": "STOP_ACCESS_WARNING",
      "message": "Müşteri C paketleri Müşteri B paketlerinin arkasında kalıyor.",
      "suggestedActions": ["Paleti ayır", "Farklı erişim yüzü olan araç seç"]
    }
  ],
  "alternatives": [
    {
      "vehicleId": "vehicle-07",
      "status": "Rejected",
      "reasonCode": "DOOR_OPENING_MISMATCH"
    }
  ],
  "algorithm": {
    "name": "FirstFitDecreasingConstraint",
    "version": "v1",
    "parameterSet": "default-logistics-2026-08",
    "inputSnapshotHash": "..."
  }
}
```

> Bu response içindeki sayılar yalnızca sözleşme örneğidir; gerçek araç kapasitesi veya saha verisi değildir.

## 12. Yeniden planlama ve kilitleme

Aşağıdaki durumlarda `LoadPlan` yeniden doğrulanmalıdır:

- Araç veya araç tipi değiştirilirse.
- Araç kapasite profili geçerlilik dönemi değişirse.
- Shipment veya irsaliye miktarı değişirse.
- Palet tipi, yük birimi ölçüsü veya ürün fiziksel profili değişirse.
- Rota durak sırası değişirse.
- Manuel palet/kalem ataması yapılırsa.
- Gerçek barkodlu yük planlanan yükten farklıysa.
- Kapasite override eklenirse.

`LoadPlan.Locked` sonrasında sessiz değişiklik yapılmaz. Replan yeni `version`, `replanned_from_id`, validation sonucu, manual change kaydı ve audit üretir.

## 13. QA kabul senaryoları

- Araç `max_payload_weight` sınırını aşan sevkiyatı aday olarak göstermez.
- Araç hacmi yeterli olsa bile kapı açıklığına sığmayan palet `DOOR_OPENING_MISMATCH` ile elenir.
- Palet adedi sığsa bile zemin ayak izi sığmıyorsa araç elenir.
- İstiflenemeyen ürünün üzerine yük konulamaz.
- `AXLE_DATA_MISSING` durumunda sistem aks uygunluğu iddia etmez.
- Maintenance veya route schedule conflict olan araç önerilmez.
- Farklı durak yükleri erişilemez yerleşmişse warning/block politikası çalışır.
- Aynı input snapshot ve algorithm version aynı aday sonuçlarını üretir veya farkı açıklar.
- Manuel araç/palet değişikliği gerekçe, kullanıcı, zaman ve yeni validation sonucu üretir.
- `LoadPlan.Locked` kapasite snapshot'ı olmadan araç çıkışı yapılamaz.

## References

- [`logistics-planning-rules-and-algorithms.md`](./logistics-planning-rules-and-algorithms.md)
- [`product-packaging-and-uom.md`](./product-packaging-and-uom.md)
- [`domain-model.md`](./domain-model.md)
- [`database-technical-architecture.md`](./database-technical-architecture.md)
- [`business-workflows.md`](./business-workflows.md)
- [`shipment-logistics-ui-design.md`](./shipment-logistics-ui-design.md)

**Hazırlayan:** Manus AI
**Tarih:** 16 Ağustos 2026
