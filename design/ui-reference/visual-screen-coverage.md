# Factory ERP-Lite — Görsel Ekran ve Popup Coverage Matrix

**Amaç:** Master screen inventory’deki her ekran ailesinin hangi pixel-reference görseli, ortak component’i ve durum seti ile uygulanacağını sabitlemek.

## 1. Internal web ekran aileleri

| Modül | Ekranlar | Ana component seti | Reference |
|---|---|---|---|
| Kimlik ve başlangıç | Giriş, ilk parola, parola sıfırlama, profil/oturum | `AuthForm`, `PasswordField`, `SessionList`, `ErrorState`, `SuccessState` | `web-component-states.png` |
| Dashboard | Yönetici, satış, depo, üretim, muhasebe, İK dashboard’ları | `AppShell`, `KpiCard`, `Chart`, `TaskList`, `RiskList`, `StatusBadge`, `QuantityViewToggle` | `web-dashboard.png` |
| Satış | Teklif talepleri, teklif listesi, teklif formu, sipariş listesi, sipariş formu, sipariş detay, müşteri listesi/detayı/formu | `DataTable`, `ProductPicker`, `QuantityInput`, `Stepper`, `ApprovalSummary`, `Drawer`, `Tabs`, `Timeline` | `web-order-detail.png`, `public-catalog-quote.png` |
| Ürünler | Kart görünümü, tablo görünümü, ürün detay, ürün formu, packaging hierarchy, physical profile, kategori, barkod | `ProductCard`, `DataTable`, `ProductImage`, `PackagingTree`, `QuantityViewToggle`, `PackagingFilter`, `Drawer`, `Modal` | `web-dashboard.png`, `web-component-states.png` |
| Depo | Stok listesi/detayı, hareketler, depolar, konumlar, transfer formu, sayım, stok düzeltme, barkod merkezi | `DataTable`, `BarcodeResult`, `QuantityInput`, `QuantityViewToggle`, `PackagingFilter`, `CountSummary`, `CriticalStockBadge`, `Drawer` | `web-warehouse-logistics.png`, `mobile-barcode-quantity.png` |
| Üretim | İş emri listesi, kanban, iş emri formu/detayı, üretim kaydı, makineler, makine detayı, üretim raporu | `KanbanBoard`, `ProgressCard`, `MachineCard`, `ProductionEntryForm`, `Timeline`, `Modal` | `web-production-finance.png`, `mobile-production-delivery.png` |
| Sevkiyat ve belgeler | İrsaliye listesi/formu/detayı, sevkiyat listesi/detayı, rota planı, durak detayı, paket izleme, kargo planı, karışık palet, yükleme doğrulama, araç/kargo tipleri, palet tipleri, yük planı özeti, yükleme farkı, araçlar, araç detayı, şoförler | `Stepper`, `QuantityViewToggle`, `PackagingFilter`, `CapacitySummary`, `LoadUnitCard`, `RouteStopList`, `PackageTraceDrawer`, `DeliveryProofPanel`, `Drawer`, `Modal` | `web-warehouse-logistics.png`, `mobile-production-delivery.png` |
| Fatura/cari/ödeme | Fatura listesi, allocation, fatura detayı/formu, cari liste/detayı/ekstre, ödeme listesi/formu, risk analizi, ödeme tipleri | `DataTable`, `LedgerSummary`, `AllocationGrid`, `CurrentStatement`, `RiskBadge`, `PaymentModal`, `Timeline`, `ExportMenu` | `web-production-finance.png`, `web-component-states.png` |
| Personel/İK | Personel listesi/detayı/formu, puantaj, mesai, izin listesi/talebi, maaş, İK raporları | `DataTable`, `EmployeeCard`, `CalendarGrid`, `ApprovalModal`, `MaskedFinancialField`, `ExportMenu` | `web-component-states.png`, `web-production-finance.png` |
| Raporlar | Satış, üretim, stok, cari, fatura, irsaliye, sevkiyat/kargo, personel raporları | `ReportFilterBar`, `Chart`, `DataTable`, `QuantityViewToggle`, `PackagingFilter`, `ExportMenu` | `web-production-finance.png`, `web-dashboard.png` |
| Bildirim/yönetim | Bildirim merkezi/ayarları, kullanıcılar/detayı, roller, yetkiler, audit log, sistem ayarları, backup, sağlık | `TaskList`, `PermissionMatrix`, `AuditTimeline`, `HealthCard`, `ConfirmationModal`, `EmptyState` | `web-component-states.png`, `web-dashboard.png` |

## 2. Public katalog ekran aileleri

| Ekran | Görsel içerik | Popup/durum |
|---|---|---|
| Public ana sayfa | Hero, öne çıkan ürünler, marka açıklaması, CTA | Cookie/consent paneli, rate-limit durumu |
| Public ürün listesi | Arama, kategori/ölçü filtresi, product card grid | Filter-empty, loading skeleton |
| Public ürün detayı | Görsel, ürün bilgisi, packaging, miktar, not | Quantity picker drawer, image preview |
| Teklif sepeti | Ürün, miktar, giriş birimi, base equivalent, ürün notu | Sağ cart drawer, remove confirm |
| Firma bilgileri | Firma, yetkili, telefon, e-posta, consent | Validation error, privacy consent |
| Talep özeti | Ürünler, miktarlar, iletişim, gönder | Critical submit confirmation |
| Anti-abuse | Rate limit, bot kontrolü, doğrulama | Retry, cooldown, support message |
| Başarı | Talep no, tarih, sonraki iletişim açıklaması | Copy request number toast |

Public surface internal ERP’den daha hafif ve katalog odaklı kalır. Ancak miktar ve packaging karşılıkları internal model ile aynı semantiği kullanır; public talep hiçbir ekranda kesin sipariş gibi gösterilmez.

## 3. Mobil ekran aileleri

| Alan | Ekranlar | Ana component seti | Reference |
|---|---|---|---|
| Giriş | Giriş, parola değişimi, bağlantı durumu | `MobileHeader`, `AuthForm`, `OfflineBanner` | `mobile-barcode-quantity.png` |
| Ana sayfa | Görev özeti, hızlı işlemler, kritik uyarılar | `MobileBottomNav`, `TaskCard`, `AlertCard` | `mobile-production-delivery.png` |
| Barkod | Kamera, barkod sonucu, ürün detayı, işlem seçimi | `MobileScanner`, `BarcodeResult`, `QuantityViewToggle`, `MobileActionSheet` | `mobile-barcode-quantity.png` |
| Stok | Stok sorgu, depo/kırılım, hareket geçmişi | `MobileCard`, `QuantityViewToggle`, `MobileList`, `BottomSheet` | `mobile-barcode-quantity.png` |
| Sayım | Sayım görevi, barkodla sayım, fark/gerekçe, onaya gönder | `CountProgress`, `Scanner`, `QuantityInput`, `ReasonSheet`, `ConfirmationModal` | `mobile-barcode-quantity.png` |
| Transfer | Kaynak/hedef depo, ürün, miktar, özet, tamamla | `WarehouseSelect`, `QuantityInput`, `SummaryCard`, `ConfirmationModal` | `mobile-barcode-quantity.png` |
| Sevkiyat | Görev listesi, irsaliye, ürün doğrulama, yükleme tamamla | `RouteCard`, `BarcodeVerification`, `LoadSummary`, `ExceptionSheet` | `mobile-production-delivery.png` |
| Üretim | Aktif iş emirleri, detay, başlat, üretim kaydı, tamamla | `ProgressCard`, `ProductionForm`, `StockEffectCard`, `PauseSheet` | `mobile-production-delivery.png` |
| Bildirim | Görevler, kritik uyarılar, ilgili kayda git | `NotificationCard`, `PriorityBadge`, `DeepLinkAction` | `mobile-production-delivery.png` |
| Profil | Kullanıcı, uygulama ayarları, çıkış | `ProfileCard`, `SettingsList`, `LogoutModal` | `mobile-barcode-quantity.png` |

## 4. Ortak tab, pencere, drawer ve popup kataloğu

| Pattern ID | Pattern | Web | Mobile | Public | Kritik durum |
|---|---|---:|---:|---:|---|
| `TAB-001` | Detay sekmeleri: Genel, Kalemler, Belgeler, Aktivite | Evet | Gerektiğinde | Ürün detayında | Aktif tab teal underline |
| `TAB-002` | Rapor/kanban/liste görünüm switch | Evet | Hayır | Hayır | View change data query’yi bozmaz |
| `TOGGLE-001` | Temel Birim / Ambalaj / Kırılım | Evet | Evet | Evet | Ledger değerini değiştirmez |
| `FILTER-001` | Tümü / Palet / Koli / Paket / Temel Birim | Evet | Evet | Ürün/katalog uyarlaması | Base equivalent görünür kalır |
| `DRAWER-001` | Ürün hızlı detay | Evet | Bottom sheet uyarlaması | Ürün detay | Backdrop + sticky footer |
| `DRAWER-002` | Package trace / mixed pallet | Evet | Bottom sheet | Hayır | Barkod, müşteri, durak, durum |
| `DRAWER-003` | Quote cart | Hayır internal | Hayır | Evet | Talep kesin sipariş değildir |
| `MODAL-001` | Standart oluştur/düzenle onayı | Evet | Evet | Evet | Input validation tamamlanmadan kapanmaz |
| `MODAL-002` | Critical approval | Evet | Evet | Talep submit | Stok/cari/ledger etkisi görünür |
| `MODAL-003` | Destructive cancel/reversal | Evet | Evet | Remove/clear | Açıklama ve typed reason zorunlu |
| `POPOVER-001` | Satır işlem menüsü | Evet | Action sheet | Evet | Anchor’a yakın açılır |
| `POPOVER-002` | Ambalaj katsayısı bilgisi | Evet | Tooltip/bottom sheet | Evet | Snapshot/version bilgisi |
| `TOAST-001` | Başarılı işlem + sonraki adıma git | Evet | Evet | Evet | Kayıt numarası ve deep link |
| `TOAST-002` | Uyarı / retry | Evet | Evet | Evet | Teknik stack trace yok |
| `STATE-001` | Loading/skeleton | Evet | Evet | Evet | Buton duplicate submit’i engeller |
| `STATE-002` | Empty/first action | Evet | Evet | Evet | Neden + ilk işlem |
| `STATE-003` | Error/retry | Evet | Evet | Evet | Kullanıcı dilinde açıklama |
| `STATE-004` | Permission denied | Evet | Evet | Internal route yok | Gerekçe + yetki isteme |
| `STATE-005` | Offline/reconnect | Evet | Evet | Hayır | Mobil operasyonlarda zorunlu |

## 5. Görsel kabul sırası

Görsel uygulama sırası önce `AppShell`, `MobileShell` ve `PublicNav`; sonra common component atlası; ardından dashboard, ürün, sipariş, depo, sevkiyat, üretim, finans, public ve mobile critical paths olacaktır. Her yeni ekran önce bu matrix’te bir route/pattern ile eşleşecek; eşleşmeyen yeni bir popup veya component doğrudan feature içine yazılmayacaktır.

Kodlanan her route için 1440×900 desktop veya mobil phone artboard screenshot’ı alınacak ve ilgili PNG ile karşılaştırılacaktır. Kritik farklar; shell ölçüsü, spacing, typography, component radius, overlay opacity, drawer/modal geometry, button placement, base quantity visibility ve state copy başlıklarıyla kaydedilecektir.
