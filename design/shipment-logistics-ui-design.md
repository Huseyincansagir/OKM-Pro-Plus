# Sevkiyat Lojistik UI Tasarım Paketi
## Araç · Rota · Kargo Planı · Karışık Palet · Paket İzleme

**Durum:** Kodlama öncesi canonical UI tasarımı
**Marka:** Karar verilene kadar nötr `Factory ERP`
**Arayüz dili:** Türkçe; entity, property, route ve API isimleri İngilizce
**İlgili ana tasarım:** `visual-design-system.md`, `master-screen-inventory.md`, `production-warehouse-deep-dive.md`

## 1. Tasarım amacı

Bu paket, son eklenen fiziksel lojistik kararlarını sevkiyatın tamamına yayar. Kullanıcı tek bir ekranda yalnızca “araç seçmez”; aracın kapasitesini, içindeki palet/koli/paketleri, hangi müşteri adreslerine gideceklerini, durak sırasını, teslim durumunu ve planlanan-gerçekleşen farkını izleyebilir.

Tasarımın temel ayrımı şudur:

> **Araç durumu, sevkiyat durumu, rota durağı durumu ve paket durumu birbirinden ayrı gösterilir.**

Stok ve belge doğruluğu temel birim miktarında kalır. Operasyon ekranlarında palet/koli/paket görünümü, fiziksel ölçü, kg, hacim ve barkod bağlamı birlikte gösterilir.

## 2. Ortak görsel ve etkileşim dili

### 2.1 Ortak üst alan

Her ekranın üstünde şu bağlam korunur:

```text
Sevkiyat no · Müşteri özeti · Araç/plaka · Rota durumu · Son güncelleme
```

Global arama şu değerleri arayabilir: sevkiyat no, irsaliye no, araç plakası, palet/koli/paket barkodu, müşteri adı ve adres.

### 2.2 Ortak miktar görünümü

```text
[ Temel Birim ]  [ Ambalaj ]  [ Kırılım ]
```

| Görünüm | Örnek | Kullanım |
|---|---|---|
| Temel Birim | `10.000 adet` / `300 kg` | Doğruluk, finans ve karşılaştırma |
| Ambalaj | `5 Koli` | Depo, yükleme ve sevkiyat |
| Kırılım | `1 Palet + 4 Koli + 6 Paket` | Fiziksel yük ve karma palet |

Toggle yalnızca görünümü değiştirir. `quantity_base` backend doğruluk kaynağıdır.

### 2.3 Durum renkleri ve metinleri

| Domain | Durum örnekleri | UI etiketi |
|---|---|---|
| Araç | `Available`, `Assigned`, `Loading`, `InTransit`, `Maintenance`, `OutOfService` | Müsait, Atandı, Yükleniyor, Yolda, Bakımda, Kullanım Dışı |
| Sevkiyat | `Preparing`, `Loaded`, `InTransit`, `PartiallyDelivered`, `Delivered`, `Exception`, `Returned` | Hazırlanıyor, Yüklendi, Yolda, Kısmi Teslim, Teslim Edildi, İstisna, İade |
| Rota durağı | `Pending`, `InProgress`, `Delivered`, `Partial`, `Failed`, `Skipped` | Sırada, Devam Ediyor, Teslim Edildi, Kısmi, Başarısız, Atlandı |
| Paket | `Planned`, `Assigned`, `Loaded`, `InTransit`, `Delivered`, `Missing`, `Returned` | Planlandı, Eşleştirildi, Yüklendi, Yolda, Teslim Edildi, Eksik, İade |

Renk tek başına kullanılmaz; her rozet metin, ikon ve gerekirse açıklama ile birlikte gösterilir.

## 3. Ekran 1 — Kargo planlama çalışma alanı

**Route:** `/sevkiyatlar/{shipmentId}/kargo-plani`

**Roller:** Depo, sevkiyat sorumlusu, yönetici, görüntüleyici

**Amaç:** Sevkiyat kalemlerini araç kapasitesine göre palet/koli/yük birimlerine dağıtmak ve planı kilitlemek.

### Yerleşim

```text
┌──────────────────────────────────────────────────────────────────────────┐
│ SHP-2026-000142 · Müşteri özeti · Araç/rota bağlamı                      │
├──────────────────────────────────────────────────────────────────────────┤
│ [Araç/kargo tipi] [Araç] [Şoför] [Durak planı] [Planı kilitle]           │
├──────────────────────┬───────────────────────────────────────────────────┤
│ SEVKİYAT KALEMLERİ    │ KAPASİTE ÖZETİ                                    │
│ Ürün / ambalaj        │ 426 / 1.200 kg                                   │
│ Beklenen / atanan     │ 2,4 / 8,0 m³                                     │
│ Kalan temel miktar    │ 2 / 4 palet                                      │
│ [Kalem ata]           │ Uygunluk: Uygun / Uyarı / Bloke                  │
├──────────────────────┼───────────────────────────────────────────────────┤
│ PALLET-001 · Karışık │ PALLET-002 · Tek ürün                            │
│ A ürünü · 3 Koli     │ C ürünü · 1 Palet                                │
│ B ürünü · 6 Koli     │ 320 kg · 1,8 m³                                  │
│ 114 kg · 0,684 m³    │                                                   │
└──────────────────────┴───────────────────────────────────────────────────┘
```

### Ana aksiyonlar

`Uygunluğu Hesapla`, `Palet Ekle`, `Kalem Ata`, `Duraklara Dağıt`, `Barkod Yazdır`, `Planı Kilitle`.

Plan kilitleme öncesi ağırlık, hacim, palet sayısı, ölçü, istifleme, kalan miktar ve alıcı durağı kontrolleri yapılır. Bloke uyarı varsa plan kilitlenemez; warning varsa yetkili override ve açıklama istenir.

## 4. Ekran 2 — Rota ve teslimat panosu

**Route:** `/sevkiyatlar/{shipmentId}/rota`

**Amaç:** Aracın ve çok duraklı teslimatların tek ekranda izlenmesi.

### Yerleşim

```text
┌──────────────────────────────────────────────────────────────────────────┐
│ 34 ABC 123 · Panelvan · 650 kg / 4,5 m³ · Yolda · Durak 2 / 4            │
├──────────────────────────────────────────────────────────────────────────┤
│ [Rota özeti] [Yük planı] [Paketler] [Aktivite]                          │
├───────────────┬───────────────────────────┬──────────────────────────────┤
│ DURAKLAR        │ AKTİF DURAK               │ ARAÇ İÇERİĞİ                │
│ 1 A ✓           │ Müşteri B                 │ PALLET-001                  │
│ 2 B ●           │ Adres B                   │ A: 3 Koli                   │
│ 3 C ○           │ Planlanan 14:30           │ B: 1 Palet + 4 Paket        │
│ 4 D ○           │ [Varış bildir]            │ C: 6 Paket                  │
│                 │ [Paketleri gör]           │ [Barkodla ara]              │
├───────────────┴───────────────────────────┴──────────────────────────────┤
│ Son hareketler · Araç durumu · Teslim istisnaları                        │
└──────────────────────────────────────────────────────────────────────────┘
```

Durak kartı müşteri, seçilmiş adres, iletişim, teslimat penceresi, paket sayısı, toplam temel miktar ve teslim durumunu gösterir. Adres değişikliği plan kilitlendikten sonra yetki ve audit gerektirir.

## 5. Ekran 3 — Paket izleme ve alıcı eşleştirme

**Route:** `/sevkiyatlar/{shipmentId}/paketler`

**Amaç:** Her palet/koli/paket barkodunun hangi müşteriye, hangi adrese ve hangi duruma ait olduğunu göstermek.

### Tablo kolonları

| Kolon | İçerik |
|---|---|
| Barkod | Palet/koli/paket barkodu |
| Ürün ve ambalaj | Ürün adı, `Palet/Koli/Paket` |
| Miktar | Seçili görünüm + temel miktar |
| Yük birimi | `PALLET-001`, koli veya bağımsız paket |
| Alıcı | Müşteri ve iletişim |
| Teslim adresi | Seçilen `CustomerAddress` |
| Rota durağı | Sıra ve durak durumu |
| Durum | Planlandı, yüklendi, yolda, teslim edildi, eksik, iade |
| Kanıt | İmza, fotoğraf, teslim notu |

### Paket detay drawer'ı

```text
Barkod: BOX-0042
Ürün: Kokteyl Napkin 24x24
Miktar: 6 Paket (600 adet)
Yük birimi: PALLET-001 / Karışık

Alıcı: Müşteri C
Teslim adresi: Adres C
Rota durağı: 3 / 4
Durum: Yolda

[Durak değişikliği] [Teslim edildi] [Eksik bildir]
```

`Durak değişikliği` yalnızca sevkiyat sorumlusu veya yönetici yetkisiyle yapılır ve audit kaydı üretir.

## 6. Ekran 4 — Araç tipleri ve araç detayı

**Routes:** `/ayarlar/arac-tipleri`, `/sevkiyat/araclar`, `/sevkiyat/araclar/{vehicleId}`

### Araç tipi alanları

`code`, `name`, `inner_length`, `inner_width`, `inner_height`, `max_gross_weight`, `max_volume`, `max_pallet_count`, `is_active`.

### Gerçek araç alanları

`plate_number`, `vehicle_type_id`, `status`, `current_route_plan_id`, `last_known_location_text`, `last_status_at`, bakım ve açıklama alanları.

Araç detayında aktif rota, mevcut yük, kapasite kullanım grafikleri, duraklar, son durum değişiklikleri ve bakım uyarıları görünür.

## 7. Ekran 5 — Mobil yükleme ve durak teslimatı

**Mobil akış:**

```text
İşlerim → Sevkiyat seç
→ Araç/rota özeti
→ Durak seç
→ Yalnızca o durağın paketlerini gör
→ Paket/palet barkodu tara
→ Miktar ve alıcı doğrula
→ Teslim edildi / kısmi / edilemedi
→ İmza, fotoğraf, not
→ Durak durumunu güncelle
```

Mobilde kullanıcı başka bir durağın paketini yanlışlıkla teslim edemez. Barkod okutulduğunda ürün, ambalaj, temel miktar, müşteri, adres ve rota durağı birlikte doğrulanır. Eksik, hasarlı, yanlış adres ve iade durumlarında neden zorunludur.

## 8. Ekran durumları ve güvenlik

| Durum | Davranış |
|---|---|
| Kapasite yetersiz | Plan kilitleme engellenir; hangi sınırın aşıldığı gösterilir |
| Paket durağa bağlı değil | Yükleme/teslim aksiyonu engellenir |
| Araç bakımda | Yeni sevkiyata atanamaz |
| Araçta başka rota var | Çakışan tarih ve kapasite uyarısı gösterilir |
| Kısmi teslim | Teslim edilen barkodlar kapanır; kalan paketler durakta kalır |
| Teslim edilemedi | Neden, fotoğraf/not ve takip görevi oluşturulur |
| Bağlantı yok | Okuma geçici gösterilebilir; teslim ve stok etkisi kesinleşmiş sayılmaz |
| Yetki yok | Buton gizlenmek yerine açıklamalı yetki mesajı gösterilir |

## 9. UI mockup üretim seti

Yeniden üretilecek görsel mockup'lar:

1. `shipment-cargo-planning-desktop.png`
2. `shipment-route-board-desktop.png`
3. `shipment-package-tracking-desktop.png`
4. `vehicle-detail-capacity-desktop.png`
5. `mobile-shipment-stop-delivery.png`

Mockup'larda marka kararı kesinleşene kadar yalnızca nötr `Factory ERP` kullanılmalı; önceki `MaviKağıt`, `NAVIS` ve `Napkinova` adları tekrar edilmemelidir.

## 10. Kabul kriterleri

- [ ] Kargo planı kapasite, palet ve durak dağılımını aynı bağlamda gösteriyor.
- [ ] Karışık palet farklı ürünleri ve farklı alıcı duraklarını taşıyabiliyor.
- [ ] Her barkod müşteri, teslim adresi ve rota durağıyla sorgulanabiliyor.
- [ ] Araç tipi ve gerçek araç durumu ayrı ekranlarda yönetiliyor.
- [ ] Rota panosu durak sırasını ve teslimat durumunu gösteriyor.
- [ ] Mobil kullanıcı yalnızca aktif durağa atanmış paketleri teslim edebiliyor.
- [ ] Kısmi teslim, eksik, hasarlı, teslim edilemedi ve iade durumları destekleniyor.
- [ ] Temel birim, ambalaj ve kırılım toggle'ı tüm ilgili ekranlarda tutarlı çalışıyor.
- [ ] UI'da görünen her kritik aksiyonun permission ve audit karşılığı tanımlı.

**Hazırlayan:** Manus AI
**Tarih:** 16 Ağustos 2026
