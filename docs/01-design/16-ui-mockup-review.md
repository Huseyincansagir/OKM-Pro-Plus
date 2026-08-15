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

Bu ortaklık, `docs/01-design/09-visual-design-system.md` içindeki renk, bileşen ve bilgi yoğunluğu kararlarıyla uyumludur.

## 2. Ekran bazlı gözlemler

| Referans | Doğrulanan UX kararı |
|---|---|
| Dashboard | Rol bazlı KPI, üretim/satış trendi, kritik risk listesi ve son hareketler tek taramada görünür. |
| Sipariş detay | Siparişten faturaya stepper, stok uygunluğu, onay paneli ve aktivite geçmişi aynı bağlamda tutulur. |
| Ürün kataloğu | Liste/kart görünümü, filtreler ve hızlı ürün drawer'ı operasyon hızını destekler. |
| Mobil barkod | Kamera taraması sonrasında ürün, stok özeti ve sayım/transfer aksiyonları tek görev akışında verilir. |
| Public katalog ve teklif | Ürün keşfi, sepet ve teklif talebi birbirinden ayrılır; “sipariş oluşturmaz” uyarısı görünürdür. |
| Üretim iş emri | Hedef/gerçekleşen/kalan/fire, personel, makine ve zaman akışı birlikte izlenir. |
| Sevkiyat | İrsaliye, barkod doğrulama, araç/şoför ve teslim bilgileri; yükleme adımıyla ilişkilidir. |
| Cari hesap | Borç/alacak/bakiye/gecikme KPI'ları, ekstre ve ödeme girişi aynı ekrandadır. |
| Personel ve puantaj | Günlük puantaj, izin onayları ve departman dağılımı aynı yönetim yüzeyinde görünür. |

## 3. Uygulama öncesi görsel kararlar

Mockup'larda marka adı ve logo üç farklı biçimde kullanılmıştır (`MaviKağıt`, `NAVIS`, `Napkinova`). Bunlar referans görseli olarak kabul edilir; üretim arayüzüne doğrudan taşınmamalıdır. Kodlama öncesinde tek bir marka adı, logo, favicon ve renk token seti seçilmelidir. Bu karar verilene kadar arayüzlerde nötr `Factory ERP` adı kullanılmalıdır.

Ürün görselleri de mockup referansıdır. Üretim asset'i olarak kullanılmadan önce lisans/kaynak, dosya adlandırma, kırpma oranı ve eksik görsel için placeholder davranışı tanımlanmalıdır.

## 4. Uygulama kabul kontrolü

- Aynı durum her modülde aynı metin + renk + ikon kombinasyonuyla gösterilir; renk tek başına anlam taşımaz.
- Masaüstü detay sayfalarında birincil aksiyonlar üst alanda, bağlı belge ve audit bilgisi içerik akışında korunur.
- Mobil operasyonlarda barkod, sayım, transfer, sevkiyat ve üretim kayıtları üç ana adımdan fazla olmadan tamamlanabilir.
- Public ve iç ERP markalama/erişim sınırları ayrıdır.
- Mockup'taki örnek tarih ve tutarlar yalnızca görsel test verisidir; domain seed verisi olarak kullanılmaz.
