# Factory ERP-Lite UI Reference Set

**Amaç:** Web, mobil ve public katalog arayüzlerinin kodlama sırasında aynı görsel dil, component ölçüsü ve durum hiyerarşisini koruması.

Bu klasördeki HTML dosyaları **editable static reference**; `png/` altındaki görseller ise 1440×900 sabit karşılaştırma artboard’larıdır. Kodlanan ekranlar aynı viewport ve aynı veri yoğunluğuyla render edilerek bu görsellerle karşılaştırılacaktır.

## Referans görsel manifesti

| Dosya | Yüzey | Kapsam | Kilitlenen noktalar |
|---|---|---|---|
| `png/web-dashboard.png` | Internal web | Yönetici dashboard | Sidebar, topbar, KPI, chart, görev listesi, sipariş tablosu, risk listesi |
| `png/web-order-detail.png` | Internal web | Sipariş detay + onay | Breadcrumb, stepper, tabs, quantity görünümü, approval summary, critical modal |
| `png/web-warehouse-logistics.png` | Internal web | Depo + kargo planı + rota | Stok table, quantity controls, capacity summary, load unit, route stop, right drawer |
| `png/web-production-finance.png` | Internal web | Üretim + finans özeti | KPI, kanban, machine performance, finance/report filter |
| `png/web-component-states.png` | Shared UI | Component/state atlas | Tabs, toggle, filters, badges, forms, popover, drawer, modal levels, empty/error/permission/toast |
| `png/public-catalog-quote.png` | Public | Katalog + teklif sepeti | Public brand separation, product cards, packaging quantity, cart drawer, quote-only message |
| `png/mobile-barcode-quantity.png` | Mobile | Barkod → ürün → işlem | Scan state, product result, quantity toggle, action list, persistent bottom nav |
| `png/mobile-production-delivery.png` | Mobile | Üretim → kayıt → durak teslimatı | Progress, short form, stock effect, package verification, partial delivery, proof/exception |

## Ortak component sözleşmesi

`reference.css` bütün referansların ortak token kaynağıdır. Kod tarafında aynı değerler design token olarak tanımlanacak ve sayfa özelinde renk/radius/spacing override edilmesine izin verilmeyecektir.

| Component | Davranış |
|---|---|
| `AppShell` | Internal web’de 248px sidebar + 73px topbar; public surface ayrı navigation; mobile phone header + bottom navigation |
| `SidebarNav` | Yetkiye duyarlı menü, aktif teal route, bekleyen iş badge’i |
| `Topbar` | Global arama, depo/çalışma alanı, bildirim, yardım, avatar |
| `PageHeader` | Breadcrumb, başlık, açıklama, ana işlem ve ikincil işlem |
| `KpiCard` | Label, baskın sayı, dönem karşılaştırması ve semantic trend |
| `StatusBadge` | Renk + Türkçe metin + durum ikonu; renk tek başına anlam taşımaz |
| `DataTable` | Sabit başlık, yoğun satır, satır aksiyonu, pagination ve export |
| `Tabs` | Aktif teal underline; sekme sayısı gerekiyorsa badge |
| `QuantityViewToggle` | `Temel Birim / Ambalaj / Kırılım`; ledger değerini değiştirmez |
| `PackagingFilter` | `Tümü / Palet / Koli / Paket / Temel Birim` |
| `QuantityInput` | Girilen miktar + giriş birimi + temel karşılık aynı blokta |
| `Stepper` | Belgenin state ilerlemesi ve sonraki işlem |
| `Timeline` | Belge/audit hareketleri; actor ve timestamp görünür |
| `Drawer` | Sağdan açılır desktop detail/trace; backdrop ve sticky footer |
| `Modal` | Standart, warning ve destructive seviyeleri ayrı arka plan/action diliyle gösterir |
| `Popover` | Anchor’a yakın açılır; kısa action menu veya açıklama içindir |
| `Toast` | Başarı/uyarı/hata ve sonraki adıma bağlantı |
| `EmptyState` | Neden, açıklama ve ilk işlem |
| `ErrorState` | Teknik olmayan açıklama, tekrar dene ve destek yönlendirmesi |
| `PermissionState` | Yetki gerekçesi ve yetki isteme/yönlendirme aksiyonu |
| `CapacitySummary` | kg, m³, palet ve doluluk kullanım/maksimum karşılaştırması |
| `LoadUnitCard` | Palet/koli/yük birimi, barkod, içerik, durak ve istifleme |
| `RouteStopList` | Durak sırası, müşteri, adres, paket ve teslim durumu |
| `PackageTraceDrawer` | Barkod, ürün, packaging, base quantity, müşteri, adres, durak ve durum |
| `DeliveryProofPanel` | Teslim alan, imza/fotoğraf/not, kısmi teslim ve istisna |
| `ProductCard` | Görsel, ad, kod, kısa bilgi, ambalaj miktarı ve tek primary action |
| `QuoteCartDrawer` | Ürün, miktar, base equivalent, not, quote-only açıklama ve sonraki adım |
| `MobileScanner` | Kamera/USB sonucu, tarama state’i ve yeniden tarama |
| `MobileActionSheet` | Barkoddan sonra yetki bazlı operasyon seçenekleri |
| `MobileBottomNav` | Ana sayfa, barkod, görev/operasyon, profil; aktif teal state |

## Ekran family → reference mapping

### Internal web

Dashboard, satış listeleri, teklif/sipariş detayları, ürün/katalog, depo/stok, üretim, sevkiyat, cari/muhasebe, personel, rapor, bildirim ve yönetim ekranları `AppShell`, `PageHeader`, `KpiCard`, `DataTable`, `Tabs`, `StatusBadge`, `Drawer`, `Modal`, `Timeline` ve ortak empty/error/permission states bileşenlerini kullanacaktır.

Sipariş, irsaliye, fatura ve sevkiyat detaylarında `Stepper`, `QuantityViewToggle`, `PackagingFilter`, `QuantityInput`, `ApprovalSummary`, `CapacitySummary`, `RouteStopList` ve `PackageTraceDrawer` birlikte çalışır. Görsel baseline için `web-order-detail.png` ve `web-warehouse-logistics.png` kullanılır.

### Mobile

Giriş, ana sayfa, barkod, stok, sayım, transfer, sevkiyat, üretim, bildirim ve profil ekranları `MobileHeader`, `MobileBottomNav`, `MobileCard`, `MobileActionSheet`, `QuantityViewToggle`, `MobileScanner`, `DeliveryProofPanel` ve kısa form bileşenlerini kullanacaktır. Görsel baseline için `mobile-barcode-quantity.png` ve `mobile-production-delivery.png` kullanılır.

### Public katalog

Public ana sayfa, ürün listesi, ürün detayı, teklif sepeti, firma bilgileri, talep özeti, anti-abuse states ve başarı ekranı `PublicNav`, `Hero`, `ProductCard`, `PackagingQuantity`, `QuoteCartDrawer`, `ConsentBlock`, `Captcha/RateLimitError` ve `SuccessSummary` bileşenlerini kullanacaktır. Görsel baseline `public-catalog-quote.png` dosyasıdır.

## State matrix

Her route için aşağıdaki durumlar kodlanmadan önce tasarlanacak ve en az bir kez görsel regression testinden geçirilecektir:

| State | Web | Mobile | Public |
|---|---:|---:|---:|
| Loading/skeleton | Evet | Evet | Evet |
| Empty/ilk kayıt | Evet | Evet | Evet |
| Filter sonucu yok | Evet | Gerekli ekranlarda | Evet |
| API hata/retry | Evet | Evet | Evet |
| Permission denied | Evet | Evet | İç route yok; public error sözleşmesi |
| Kaydetme/duplicate prevention | Evet | Evet | Talep gönderimi |
| Critical confirmation | Evet | Evet | Talep gönderim teyidi |
| Success + next step | Evet | Evet | Talep numarası + şirket dönüşü |
| Offline/reconnect | Opsiyonel | Zorunlu operasyon ekranlarında | Gerekli değil |

## Pixel-sadakatli uygulama kuralı

Kodlanan ekranlar `reference.css` token’larını kullanacak ve referans görsellerle aynı artboard üzerinde render edilecektir. Kabul sırasında yalnızca renk benzerliği değil; sidebar/topbar ölçüsü, content gutter, card radius, gaps, typography scale, table row height, modal/drawer width, action position, mobile bottom-nav yüksekliği, text hierarchy ve state visibility kontrol edilecektir.

Bir component farklı bir modülde yeniden yazılmayacak. Yeni bir ihtiyaç varsa önce component atlasına eklenip referans görseli güncellenecek, sonra tüm surface’lerde kullanılacaktır. Bu yöntem, web ve mobil tarafın aynı bilgiyi farklı cihazlara uyarlanmış ama aynı anlam hiyerarşisiyle göstermesini sağlar.
