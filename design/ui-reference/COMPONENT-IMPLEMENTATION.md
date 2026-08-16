# Factory ERP-Lite — Component Implementation Contract

Bu belge, `ui-reference/png/` altındaki görsellerin kod tarafındaki karşılığıdır. Uygulama sırasında bir ekran ile görsel arasında fark oluşursa önce bu sözleşme, sonra ilgili reference HTML/PNG incelenir. Yeni bir component ancak bu belgeye eklenip görseli güncellendikten sonra feature içine alınabilir.

## 1. Surface shells

### Internal web

`AppShell` 248px genişlikte koyu lacivert sidebar ve 73px yüksekliğinde beyaz topbar kullanır. Content alanı 31px yatay gutter ile başlar. Sidebar menüleri permission sonucuna göre gelir; aktif route teal arka plan, bekleyen iş sayısı sağ badge ve tüm görünen metinler Türkçe’dir.

### Mobile

`MobileShell` koyu lacivert 64px header, açık canvas ve 67px sabit bottom navigation kullanır. Operasyonel primary action ekranda alt aksiyon bölgesinde görünür. Büyük tablolara masaüstü görünümünü küçülterek değil, `MobileCard`, `MobileList`, `BottomSheet` ve `ActionSheet` ile karşılık verilir.

### Public

`PublicShell` internal sidebar kullanmaz. Beyaz public navigation, daha geniş hero typography ve daha ferah product grid ile internal ERP’den görsel olarak ayrılır; teal mark/primary aksiyon ailesi korunur.

## 2. Component rules

| Component | Zorunlu davranış | Yasak davranış |
|---|---|---|
| `StatusBadge` | Metin + semantic renk + ikon; Türkçe label | Sadece renk noktasıyla state anlatmak |
| `QuantityViewToggle` | Görüntüyü değiştirir, `quantity_base` değiştirmez | Base quantity’yi client’ta yeniden hesaplamak |
| `PackagingFilter` | Liste/rapor görünümünü filtreler | Packaging master katsayısını mutasyona uğratmak |
| `QuantityInput` | Giriş miktarı, giriş birimi ve temel karşılığı beraber gösterilir | Temel karşılığı gizlemek |
| `DataTable` | Header, loading skeleton, empty/error/filter-empty, pagination | Loading sırasında boş beyaz alan bırakmak |
| `Drawer` | Backdrop, close, section separators, sticky footer | Drawer açıkken arka işlemleri aktif bırakmak |
| `Modal` | Header, açıklama, etki özeti, explicit cancel/confirm | Kritik effect’i yalnızca buton label’ına saklamak |
| `Toast` | Kayıt numarası ve sonraki adıma deep link | Başarılı işlemi yalnızca renk ile belirtmek |
| `MobileActionSheet` | Permission-aware action rows, bottom sheet, one primary CTA | Desktop dropdown’u aynen mobile taşımak |
| `ProductCard` | Image, product title, code, short metadata, packaging amount, one primary action | Ürün kartına birden fazla eşdeğer primary CTA koymak |
| `CapacitySummary` | kg, m³, palet, kullanılan/maksimum ve warning listesi | Doluluğu tek yüzde ile açıklamak |
| `DeliveryProofPanel` | Alıcı, imza/fotoğraf/not, partial/exception reason | Teslim kanıtını yalnızca checkbox yapmak |

## 3. State contract

Her async component şu durumları göstermelidir: `loading`, `success`, `empty`, `filter-empty`, `error`, `permission-denied`, `saving`, `critical-confirmation`. Backend typed errors UI’da Türkçe açıklama, kullanıcıya uygulanabilir sonraki adım ve gerekiyorsa retry ile gösterilir. Teknik exception, stack trace veya internal identifier kullanıcı metnine taşınmaz.

### State visual mapping

| Durum | Teal | Amber | Red | Neutral |
|---|---|---|---|---|
| Loading | Aktif skeleton | — | — | Disabled controls |
| Pending | — | Onay bekliyor | — | — |
| Success | Tamamlandı / sonraki adım | — | — | — |
| Warning | — | Risk veya limit | — | — |
| Error | — | — | Hata/critical | Retry action |
| Permission | — | — | — | Yetki açıklaması + talep |

## 4. Pixel comparison protocol

Kodlanan her ana ekran aşağıdaki sabitlerle render edilir:

| Surface | Viewport | Karşılaştırılacak referans |
|---|---|---|
| Internal web | 1440×900 | `web-dashboard.png`, `web-order-detail.png`, `web-warehouse-logistics.png`, `web-production-finance.png` |
| Component atlas | 1440×900 | `web-component-states.png` |
| Public | 1440×900 | `public-catalog-quote.png` |
| Mobile | 338×760 phone artboard, 3-up export 1440×900 | `mobile-barcode-quantity.png`, `mobile-production-delivery.png` |

Karşılaştırma kontrolü shell ölçüsü, component geometry, typography, spacing, semantic colors, overlay opacity, button placement, text hierarchy, quantity base-equivalent visibility, mobile bottom navigation ve modal/drawer footer konumunu kapsar. Her PR, görsel değişiklik varsa baseline PNG ve kısa change note ekler.

## 5. Responsive adaptation

Desktop’ta üç kolon veya dense table olarak görünen bilgi mobilde öncelik sırasına göre tek kolon kartlara ayrılır. Miktar gösteriminde ana değer üstte, base equivalent hemen altında ve packaging level badge’i aynı grup içinde kalır. Action button’lar mobilde en az 43px yüksekliğinde, desktop’ta 37px yüksekliğinde kullanılır.

Drawer desktop’ta sağdan açılır; mobile karşılığı bottom sheet’tir. Modal desktop’ta merkezlenir; mobile karşılığı full-width bottom sheet veya full-screen route olur. Popover desktop’ta anchor-aware açılır; mobile’da aynı içeriğin action sheet veya tooltip karşılığı seçilir.

## 6. Code review checklist

Kod incelemesi sırasında reviewer şu soruların tamamına olumlu cevap vermelidir:

1. Ekran doğru reference PNG ile eşleşiyor mu?
2. Ortak component yeni bir kopya ile çoğaltılmamış mı?
3. `tokens.json` değerleri dışında özel renk, radius veya spacing eklenmiş mi?
4. Türkçe görünen metinler, status label’ları ve error copy tamam mı?
5. Quantity toggle ve packaging filter ledger semantiğini değiştirmiyor mu?
6. Loading, empty, error, permission, saving ve success durumları var mı?
7. Kritik işlem modalı effect summary ve açıklamalı confirmation içeriyor mu?
8. Mobile’da primary action thumb reach içinde mi?
9. Görsel baseline screenshot’ı değiştiyse neden açıklanmış mı?
10. UI permission görünürlüğü backend authorization’ın yerine geçirilmemiş mi?
