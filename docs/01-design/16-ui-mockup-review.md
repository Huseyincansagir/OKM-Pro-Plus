# Manus UI Mockup İncelemesi

**Kaynak:** `docs/05-assets/mockups/` altındaki Manus ekran referansları  
**Kapsam:** Ortak görsel dil, operasyonel kullanılabilirlik ve uygulama öncesi açık kararlar

## 1. Ortak görsel dil doğrulaması

İncelenen dashboard, sipariş, ürün, mobil barkod, public katalog, teklif, üretim, sevkiyat, cari ve personel ekranları aynı temel yönü destekliyor:

- Derin lacivert navigasyon ve açık içerik yüzeyi.
- Birincil aksiyonlarda teal; bekleyen durumlarda amber; kritik durumda kırmızı; tamamlanan durumda yeşil.
- KPI kartları, yoğun veri tabloları, durum rozetleri, stepper ve aktivite zaman çizelgesi.
- İç ERP'de sol menü + üst bağlam çubuğu; mobilde görev odaklı alt navigasyon.
- Kritik işlemlerde açık durum, belge ilişkisi ve bir sonraki aksiyonun görünür olması.
- Public katalogda iç stok, maliyet ve risk bilgisinin gizlenmesi; teklif akışının siparişten ayrılması.

Bu ortaklık, `/design/visual-design-system.md` içindeki renk, bileşen ve bilgi yoğunluğu kararlarıyla uyumludur.

## 2. Ekran bazlı gözlemler

| Referans | Doğrulanan UX kararı |
|---|---|
| Dashboard | Rol bazlı KPI, üretim/satış trendi, kritik risk listesi ve son hareketler tek taramada görünür. |
| Sipariş detay | Siparişten faturaya stepper, stok uygunluğu, onay paneli ve aktivite geçmişi aynı bağlamda tutulur. |
| Ürün kataloğu | Liste/kart görünümü, filtreler ve hızlı ürün drawer'ı operasyon hızını destekler. |
| Mobil barkod | Kamera taraması sonrasında ürün, stok özeti ve sayım/transfer aksiyonları tek görev akışında verilir. |
| Public katalog ve teklif | Ürün keşfi, sepet ve teklif talebi birbirinden ayrılır; “sipariş oluşturmaz” uyarısı görünürdür. |
| Üretim iş emri | Hedef/gerçekleşen/kalan/fire, personel, makine ve zaman akışı birlikte izlenir. |
| Sevkiyat | İrsaliye, barkod doğrulama, araç/şoför ve teslim bilgileri; yükleme adımıyla ilişkilidir. Yeni turda kapasite, rota/durak, paket alıcısı ve teslim kanıtı ayrıca görünür olmalıdır. |
| Cari hesap | Borç/alacak/bakiye/gecikme KPI'ları, ekstre ve ödeme girişi aynı ekrandadır. |
| Personel ve puantaj | Günlük puantaj, izin onayları ve departman dağılımı aynı yönetim yüzeyinde görünür. |

## 3. Yeni lojistik mockup kabul kriterleri

Yeni kargo ve sevkiyat ekranları aşağıdaki ortak görsel kararları uygulamalıdır:

| Ekran | Görsel zorunluluk |
|---|---|
| Kargo planlama | Araç kapasitesi, kullanılan kg/m³/palet, palet/yük birimleri ve bloke uyarıları aynı ekranda |
| Rota/durak panosu | Araç durumu, durak sırası, müşteri/adres, planlanan-gerçekleşen zaman ve teslim durumu |
| Paket izleme | Barkod, ürün/ambalaj, temel miktar, müşteri, adres, durak ve paket durumu |
| Araç detayı | Araç tipi, kapasite, aktif rota, mevcut yük, bakım ve son durum değişikliği |
| Mobil durak teslimatı | Yalnızca aktif durağın paketleri, barkod doğrulama, teslim kanıtı ve istisna |

Kapasite ve miktar kartlarında sayılar birimleriyle birlikte yazılmalı; `426 / 1.200 kg`, `2,4 / 8,0 m³`, `2 / 4 palet` gibi karşılaştırmalar görsel olarak ayrıştırılmalıdır. Karışık palet içindeki farklı alıcılar renk ile değil, müşteri adı, adres ve durak etiketiyle ayrılmalıdır.

## 4. Uygulama öncesi görsel kararlar

Mockup'larda marka adı ve logo üç farklı biçimde kullanılmıştır (`MaviKağıt`, `NAVIS`, `Napkinova`). Bunlar referans görseli olarak kabul edilir; üretim arayüzüne doğrudan taşınmamalıdır. Kodlama öncesinde tek bir marka adı, logo, favicon ve renk token seti seçilmelidir. Bu karar verilene kadar arayüzlerde nötr `Factory ERP` adı kullanılmalıdır.

Ürün görselleri de mockup referansıdır. Üretim asset'i olarak kullanılmadan önce lisans/kaynak, dosya adlandırma, kırpma oranı ve eksik görsel için placeholder davranışı tanımlanmalıdır.

## 5. Yeni mockup çıktıları

| Mockup | Dosya | İlk kabul notu |
|---|---|---|
| Kargo planlama | `docs/05-assets/mockups/shipment-cargo-planning-desktop.png` | Araç kapasitesi, karışık palet ve durak dağılımı aynı çalışma alanında |
| Rota ve teslimat panosu | `docs/05-assets/mockups/shipment-route-board-desktop.png` | Durak sırası, aktif müşteri, araç içeriği ve teslim istisnaları görünür |
| Paket izleme | `docs/05-assets/mockups/shipment-package-tracking-desktop.png` | Barkod, müşteri, adres, durak, durum ve detay drawer'ı görünür |
| Araç detayı | `docs/05-assets/mockups/vehicle-detail-capacity-desktop.png` | Araç tipi, kapasite, aktif rota, yük ve bakım geçmişi görünür |
| Mobil durak teslimatı | `docs/05-assets/mockups/mobile-shipment-stop-delivery.png` | Aktif durak sınırı, barkod, teslim kanıtı ve istisna aksiyonları görünür |

Bu görseller UI kararlarını doğrulayan mockup'lardır; production code veya gerçek veri seed'i değildir. Marka kararı kesinleşene kadar nötr `Factory ERP` adı korunur.

## 6. Uygulama kabul kontrolü

- Aynı durum her modülde aynı metin + renk + ikon kombinasyonuyla gösterilir; renk tek başına anlam taşımaz.
- Masaüstü detay sayfalarında birincil aksiyonlar üst alanda, bağlı belge ve audit bilgisi içerik akışında korunur.
- Mobil operasyonlarda barkod, sayım, transfer, sevkiyat ve üretim kayıtları üç ana adımdan fazla olmadan tamamlanabilir.
- Public ve iç ERP markalama/erişim sınırları ayrıdır.
- Mockup'taki örnek tarih ve tutarlar yalnızca görsel test verisidir; domain seed verisi olarak kullanılmaz.
