# Fabrika ERP-Lite
## Tüm Bölümler İçin Eksiksiz Arayüz Tasarım Paketi

## 1. Kapsam

Bu paket, peçete üretimi yapan fabrikanın web, public müşteri kataloğu ve mobil operasyon uygulamasının kodlama öncesi arayüz tasarımını kapsar. Tasarım; satış, ürün, depo, üretim, sevkiyat, fatura, cari, ödeme, risk, personel, rapor, bildirim, yönetim, yetki, audit, ayarlar, public teklif talebi ve mobil barkod işlemlerini aynı merkezi iş akışı içinde ele alır.

Sistemin temel ortak dili Türkçedir. Arayüzler bilgi yoğun, hızlı taranabilir, rol bazlı ve işlem sonucunu açıkça gösteren kurumsal ERP yaklaşımıyla tasarlanmıştır.

## 2. Tasarım paketi içeriği

| Dosya | Kapsam |
|---|---|
| `uretim-depo-master-screen-inventory.md` | Tüm modüllerin, route'ların, ekranların ve rol kapsamının envanteri |
| `uretim-depo-web-ux-architecture.md` | Web bilgi mimarisi, dashboard'lar, liste/detay şablonları ve ana operasyon akışları |
| `uretim-depo-web-finance-production-hr-design.md` | Üretim, sevkiyat, fatura, cari, ödeme, risk, rapor ve personel tasarımı |
| `uretim-depo-management-public-system-design.md` | Kullanıcılar, roller, yetkiler, bildirimler, audit, ayarlar, backup ve public alan |
| `uretim-depo-mobile-complete-design.md` | Mobil giriş, ana sayfa, barkod, stok, sayım, transfer, sevkiyat, üretim ve durumlar |
| `uretim-depo-public-katalog-final.md` | Public ürün kataloğu, ürün detayı, teklif sepeti ve teklif talebi akışı |
| `uretim-depo-visual-design-system.md` | Renk, tipografi, bileşen, durum ve görsel kabul kriterleri |
| `uretim-depo-ui-design-final.md` | Önceki tasarım aşamasının genel özeti |

## 3. Tamamlanan modüller

| Modül | Tasarım kapsamı | Temel ekranlar |
|---|---|---|
| Dashboard | Rol bazlı KPI, görev ve grafikler | Yönetici, satış, depo, üretim, muhasebe, İK |
| Satış | Tekliften siparişe akış | Teklif talepleri, teklifler, siparişler, onay paneli |
| Ürünler | Görsel katalog ve ürün kartı | Kart, tablo, detay, kategori, barkod |
| Depo | Stok ve fiziksel hareketler | Stok, hareket, transfer, sayım, depo, konum, barkod |
| Üretim | İş emri ve gerçekleşme | İş emri, kanban, makine, üretim kaydı, fire, duruş |
| Sevkiyat | İrsaliye ve teslimat | İrsaliye, sevkiyat, araç, şoför, teslim durumu |
| Muhasebe | Belge ve finans | Fatura, ödeme, cari hesap, cari ekstre, risk |
| Personel | İK operasyonu | Personel, puantaj, izin, mesai, maaş |
| Raporlar | Ortak filtreli analiz | Satış, üretim, stok, cari, fatura, irsaliye, İK |
| Bildirim | Görev ve kritik uyarı | Bildirim merkezi, bildirim ayarları |
| Yönetim | Güvenlik ve işletim | Kullanıcı, rol, izin, audit, ayar, backup, health |
| Public | Dış müşteri talebi | Katalog, ürün detay, sepet, bilgi formu, başarı |
| Mobil | Saha işlemleri | Barkod, stok, sayım, transfer, sevkiyat, üretim |

## 4. Ana bilgi mimarisi

```text
Dashboard
├── Satış
│   ├── Teklif Talepleri
│   ├── Teklifler
│   └── Siparişler
├── Ürünler
│   ├── Ürün Kataloğu
│   ├── Kategoriler
│   └── Barkodlar
├── Depo
│   ├── Stok
│   ├── Stok Hareketleri
│   ├── Depolar / Konumlar
│   ├── Transfer
│   ├── Sayım
│   └── Barkod Merkezi
├── Üretim
│   ├── İş Emirleri
│   ├── Üretim Kayıtları
│   ├── Makineler
│   └── Üretim Raporları
├── Sevkiyat
│   ├── İrsaliyeler
│   ├── Sevkiyatlar
│   ├── Araçlar
│   └── Şoförler
├── Cari ve Muhasebe
│   ├── Faturalar
│   ├── Cari Hesaplar
│   ├── Cari Ekstre
│   ├── Ödemeler
│   └── Risk Analizi
├── Personel
│   ├── Personeller
│   ├── Puantaj
│   ├── İzinler
│   ├── Mesai
│   └── Maaş
├── Raporlar
├── Bildirimler
└── Yönetim
    ├── Kullanıcılar
    ├── Roller ve Yetkiler
    ├── Audit Log
    ├── Ayarlar
    ├── Backup
    └── Sistem Sağlığı
```

## 5. Ana uçtan uca akışlar

### Satış ve tahsilat

```text
Public Ürün Kataloğu
→ Teklif Talebi
→ Satış İncelemesi
→ Teklif
→ Sipariş Taslağı
→ Sorumlu Onayı
→ Stok Rezervasyonu
→ İrsaliye
→ Sevkiyat
→ Fatura
→ Cari Borç
→ Ödeme
→ Güncel Bakiye
```

### Üretim ve stok

```text
Üretim Planı
→ İş Emri
→ Makine Ataması
→ Personel Ataması
→ Üretim Başlangıcı
→ Miktar / Fire / Duruş
→ Üretim Tamamlama
→ Depo Üretim Girişi
→ Stok Güncellemesi
```

### İnsan kaynakları

```text
Personel Kartı
→ Puantaj
→ Mesai
→ İzin Talebi
→ Yönetici Onayı
→ Aylık Çalışma Özeti
→ Maaş Kaydı
→ İK Raporu
```

## 6. Görsel mockup seti

| Mockup | Bölüm |
|---|---|
| `uretim-depo-erp-dashboard-reference.png` | Yönetici dashboard |
| `uretim-depo-order-detail-mockup.png` | Sipariş ve sorumlu onayı |
| `uretim-depo-product-catalog-mockup.png` | İç ürün kataloğu |
| `uretim-depo-mobile-barcode-mockup.png` | Mobil barkod ve stok |
| `uretim-depo-public-catalog-desktop-mockup.png` | Public ürün kataloğu |
| `uretim-depo-quote-cart-desktop-mockup.png` | Public teklif sepeti |
| `uretim-depo-quote-form-mobile-mockup.png` | Mobil teklif formu |
| `uretim-depo-production-work-order-mockup.png` | Üretim iş emri |
| `uretim-depo-shipment-mockup.png` | İrsaliye ve temel sevkiyat |
| `shipment-cargo-planning-desktop.png` | Araç kapasitesi, kargo planlama ve karışık palet |
| `shipment-route-board-desktop.png` | Araç durumu, rota ve çok duraklı teslimat |
| `shipment-package-tracking-desktop.png` | Barkod, müşteri/adres ve paket izleme |
| `vehicle-detail-capacity-desktop.png` | Araç tipi, kapasite, aktif rota ve yük |
| `mobile-shipment-stop-delivery.png` | Mobil durak teslimatı ve teslim kanıtı |
| `mobile-barcode-quantity-toggle-flow.png` | Mobil barkod sonucu, üçlü toggle ve işlem seviyesi |
| `uretim-depo-accounting-current-account-mockup.png` | Cari hesap ve ödeme |
| `uretim-depo-hr-attendance-mockup.png` | Personel ve puantaj |

## 7. Ortak görsel sistem

Derin lacivert, uygulama navigasyonunu ve public üst barı; teal, ana işlemleri ve aktif durumları; amber, bekleyen ve dikkat gerektiren kayıtları; kırmızı, gecikme, hata ve kritik durumları; yeşil ise tamamlanan işlemleri temsil eder. Renkler her zaman metin ve ikonla desteklenir.

Tüm web listelerinde standart olarak arama, gelişmiş filtre, durum rozeti, tarih aralığı, sıralama, sayfalama ve dışa aktarma bulunur. Miktar içeren listelerde `Temel Birim / Ambalaj / Kırılım` toggle'ı ve ambalaj filtresi bulunur. Sevkiyat listelerinde araç/kargo tipi, kapasite kullanımı, rota/durak ve paket durumları filtrelenebilir. Detay ekranları özet kartları, sekmeler, bağlı belgeler, aktivite zaman çizelgesi ve sonraki işlem alanından oluşur. Kritik işlemler onay penceresinde işlem sonucunu ve stok/cari/personel etkisini gösterir.

Mobilde aynı renk dili korunur; ancak ekranlar görev ve operasyon odaklıdır. Barkod tarama, stok sorgu, sayım, transfer, sevkiyat doğrulama, rota/durak seçimi ve teslim kanıtı büyük dokunma alanlarıyla tasarlanır. Kullanıcı aktif durak dışındaki paketleri teslim edemez.

## 8. Kodlama öncesi tasarım kabul kriterleri

| Kriter | Tasarım kararı |
|---|---|
| Kapsam | Envanterdeki bütün modüllerin liste, detay, form ve durumları tanımlandı |
| Akış | Tekliften ödemeye ve üretimden stoğa uçtan uca bağlantı kuruldu |
| Yetki | Rol ve işlem bazlı görünürlük ile kritik buton davranışları belirlendi |
| Belge bağlantısı | Sipariş, irsaliye, sevkiyat, fatura, cari ve ödeme ilişkisi görünür |
| Mobil | Barkod, stok, sayım, transfer, sevkiyat ve üretim akışları tanımlı |
| Public | Ürün kataloğu ve hesapsız teklif talebi akışı tamamlandı |
| Hata durumları | Boş, loading, hata, yetki yok, ağ yok ve başarılı durumlar tanımlı |
| Görsel bütünlük | Ortak renk, tipografi, kart, tablo, form, modal ve badge sistemi belirlendi |

## 9. Kodlamaya geçiş için önerilen sıra

İlk olarak ortak layout, sidebar, topbar, breadcrumb, data table, form, status badge, modal, drawer, timeline ve bildirim bileşenleri uygulanmalıdır. Ardından kimlik, kullanıcı ve izin altyapısı; ürün ve müşteri; depo ve stok; satış ve sipariş; irsaliye ve sevkiyat; fatura ve cari; üretim; personel; rapor ve bildirim; son olarak mobil uygulama geliştirilmelidir.

Frontend kodlamasına başlanmadan önce bu dokümandaki route, alan ve yetki listesi backend API tasarımındaki endpoint'lerle eşleştirilmelidir. Böylece tasarımda görünen her işlem hangi API ve hangi izinle çalışacağı belli olacak şekilde implementasyona aktarılır.

## 10. Sonuç

Tasarım aşamasında sistemin bütün ana bölümleri için kapsam, kullanıcı akışı, ekran envanteri, durum davranışı, yetki modeli, mobil operasyon yaklaşımı ve görsel yön hazırlanmıştır. Bundan sonraki adım tasarımın kodlanmasıdır; bu aşamada özellikle ortak bileşen sistemi ve satıştan tahsilata uzanan ana senaryo referans alınmalıdır.
