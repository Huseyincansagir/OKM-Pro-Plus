# Fabrika ERP-Lite
## Eksiksiz Arayüz Tasarım Envanteri ve Kapsam Matrisi

## 1. Tasarım kapsamı

Bu envanter, üretim yapan fabrikanın şirket içi web uygulamasını, dış müşteriye açık ürün kataloğunu ve operasyon çalışanlarının mobil uygulamasını kapsar. Amaç, kod yazmadan önce bütün ekranları, ekranlar arasındaki bağlantıları, temel alanları, durumları, rollerin görebileceği işlemleri ve eksik kalmaması gereken ortak davranışları tanımlamaktır.

Sistem üç kullanıcı yüzeyinden oluşacaktır:

| Yüzey | Kullanıcı | Ana amaç |
|---|---|---|
| İç web uygulaması | Yönetici, satış, depo, üretim, muhasebe, İK, sistem yöneticisi | Şirket operasyonunun merkezi yönetimi |
| Public katalog | Dış müşteri ve teklif isteyen ziyaretçi | Ürün seçmek ve teklif talebi göndermek |
| Mobil operasyon | Depo, üretim, sevkiyat ve saha kullanıcıları | Barkod, üretim, stok ve sevkiyat işlemlerini sahada yapmak |

## 2. Tüm sistem ortak ekran kuralları

Her ekran sayfa başlığı, kısa açıklama, aktif kullanıcı rolü, varsa depo/şube bağlamı ve açık işlem adımını göstermelidir. Liste ekranlarında arama, sıralama, gelişmiş filtre, tarih aralığı, durum filtresi, sayfalama ve dışa aktarma; detay ekranlarında sekmeler, belge bağlantıları, aktivite geçmişi ve sonraki işlem alanı bulunacaktır.

Aşağıdaki durumlar bütün modüllerde ayrıca tasarlanacaktır:

| Durum | Beklenen arayüz |
|---|---|
| Yükleniyor | Tablo iskeleti, kart iskeleti veya form bekleme durumu |
| Boş | Neden açıklaması ve ilk kayıt/işlem butonu |
| Sonuç yok | Arama veya filtreyi temizleme önerisi |
| Hata | Teknik olmayan açıklama, tekrar dene ve destek bağlantısı |
| Yetki yok | Erişim gerekçesi ve işlemi yapabilecek role yönlendirme |
| Kaydetme | Buton kilidi, işlem göstergesi ve duplicate gönderim önleme |
| Başarılı | Toast, kayıt numarası ve sonraki adıma geçiş |
| Kritik işlem | Etkilenecek stok/cari değerini gösteren onay penceresi |

## 3. Rol ve yetki matrisi

| Rol | Görüntüleme | Oluşturma/değiştirme | Onay/finans |
|---|---|---|---|
| Sistem yöneticisi | Tüm modüller | Sistem, kullanıcı ve yetkiler | Tüm yönetim işlemleri |
| Yönetici | Tüm operasyon ve raporlar | Gerekli operasyon kayıtları | Sipariş, izin ve kritik onaylar |
| Satış | Ürün, müşteri, teklif, sipariş | Teklif, müşteri, sipariş taslağı | Sipariş onaya gönderme |
| Depo | Ürün, stok, irsaliye, sevkiyat | Stok operasyonu, hazırlık, sayım | Stok düzeltme talebi |
| Üretim | Ürün, iş emri, makine, personel | Üretim kaydı, duruş, fire | İş emri tamamlama |
| Muhasebe | Fatura, cari, ödeme, müşteri | Fatura, ödeme, cari kayıt | Finansal kayıt ve iptal |
| İnsan kaynakları | Personel, puantaj, izin, mesai | Personel ve İK kayıtları | İzin ve maaş onayı |
| Görüntüleyici | Yetkili rapor ve listeler | Değişiklik yapamaz | Onay ve finans işlemi yok |

## 4. Ana route ve modül envanteri

### 4.0 Ortak miktar görünümü ve ambalaj kontrolleri

Ambalaj hiyerarşisi ilgili tüm ekranlarda ortak bir bileşen setiyle görünür:

```text
[ Temel Birim ]  [ Ambalaj ]  [ Kırılım ]
Ambalaj filtresi: [ Tümü ] [ Palet ] [ Koli ] [ Paket ] [ Temel Birim ]
```

Bu üçlü toggle yalnızca gösterimi değiştirir; stok ve belge miktarını değiştirmez. Miktar girişi gereken ekranlarda toggle'dan ayrı olarak şu kontrol kullanılır:

```text
Miktar: [ 5 ]   Giriş birimi: [ Koli ▼ ]   Karşılığı: 10.000 adet
```

| Ortak kontrol | Kullanıldığı yerler | Kural |
|---|---|---|
| Görünüm toggle'ı | Barkod sonucu, stok, sayım, transfer, sipariş, irsaliye, sevkiyat, üretim, rapor | `Temel Birim`, `Ambalaj`, `Kırılım` görünümü arasında geçer |
| Ambalaj filtresi | Ürün, stok, hareket, belge ve rapor listeleri | `Tümü`, `Palet`, `Koli`, `Paket`, `Temel Birim` filtreleri |
| Giriş birimi seçici | Teklif, sipariş, transfer, sayım, irsaliye, üretim | Girilen miktarı seçilen ambalaj seviyesinden temel birime çevirir |
| Temel miktar yardımcı metni | Kritik tüm işlem formları | `5 Koli` yanında `10.000 adet` veya `300 kg` gösterilir |

Kritik belge ekranlarında seçilen görünüm ne olursa olsun temel miktar karşılığı gizlenmez. `quantity_base` backend tarafından hesaplanır; frontend görünümü veya filtresi ledger değerini değiştiremez.

### 4.1 Kimlik ve başlangıç

| Ekran | Route önerisi | Temel alan / işlem |
|---|---|---|
| Giriş | `/giris` | E-posta/kullanıcı adı, parola, beni hatırla |
| İlk parola değişimi | `/ilk-parola` | Eski/geçici parola, yeni parola, tekrar |
| Parolamı unuttum | `/parola-sifirla` | E-posta, doğrulama ve yeni parola |
| Dashboard | `/dashboard` | Role göre KPI, uyarı, görev ve grafikler; miktar görünümü seçimi |
| Profil ve oturum | `/profil` | Kullanıcı bilgileri, parola, aktif oturumlar |

### 4.2 Yönetici dashboard'ları

| Dashboard | İçerik |
|---|---|
| Yönetici | Bugünkü satış, üretim, bekleyen sipariş, onaylar, tahsilat, gecikmeler, kritik stok, riskli müşteriler, faturalaşmamış irsaliyeler; miktar görünümü toggle'ı |
| Satış | Açık teklif talepleri, hazırlanan teklifler, onay bekleyen siparişler, aylık satış, en çok talep edilen ürünler; ambalaj filtresi |
| Depo | Kritik stok, bugün giriş/çıkış, hazırlanacak sevkiyat, bekleyen sayım, son barkod işlemleri; temel/ambalaj/kırılım toggle'ı |
| Üretim | Aktif iş emirleri, makine durumu, üretim miktarı, fire, duruş, hedefe ilerleme; hedef/gerçekleşen görünüm seçimi |
| Muhasebe | Tahsilat, toplam borç, toplam alacak, geciken faturalar, vadesi yaklaşanlar, faturalaşmamış irsaliyeler; temel miktar ve ambalaj karşılığı |
| İK | Bugün çalışan personel, devamsızlık, izinli personel, fazla mesai, onay bekleyen izinler |

### 4.3 Satış ve müşteri modülü

| Ekran | Temel içerik | Ana işlemler |
|---|---|---|
| Teklif talepleri listesi | Talep no, firma, iletişim, tarih, ürün sayısı, durum, sorumlu | İncele, sorumlu ata, teklife dönüştür, reddet |
| Teklif talebi detayı | Firma, yetkili, telefon, e-posta, ürünler, miktarlar, notlar, kaynak | İncele, müşteri oluştur, teklif oluştur |
| Teklifler listesi | Teklif no, müşteri, tarih, geçerlilik, toplam, durum | Filtrele, PDF al, siparişe dönüştür |
| Teklif oluştur/düzenle | Müşteri, ürün, miktar, seçilen ambalaj, temel miktar karşılığı, birim fiyat, iskonto, vergi, toplam, geçerlilik, not | Kaydet, PDF üret, gönder, kabul/ret |
| Fiyat listeleri (O-012 seçilirse) | Liste, geçerlilik, para birimi, ürün fiyatları, müşteri grubu, fiyatın hangi ambalaj seviyesine ait olduğu | Oluştur, kopyala, yayına al, pasifleştir |
| Müşteri fiyat grupları (O-012 seçilirse) | Grup, müşteri bağlantıları, varsayılan liste, geçerlilik | Ata, kaldır, geçmişi gör |
| Siparişler listesi | Sipariş no, müşteri, tarih, toplam, onay, sevk, fatura durumu | Filtrele, toplu dışa aktar, detay |
| Sipariş oluştur | Müşteri, teslimat adresi, ürünler, girilen miktar + ambalaj, temel miktar önizlemesi, ödeme şartı, teslim tarihi, not | Taslak kaydet, onaya gönder |
| Sipariş detayı | Genel, ürünler, `5 Koli (10.000 adet)` görünümü, temel miktarlar, stok rezervasyonu, belgeler, onay geçmişi, aktivite | Onayla, reddet, iptal et, irsaliye oluştur |
| Sipariş onay paneli | Toplam, stok uygunluğu, ödeme şartı, teslim tarihi, risk özeti | Onayla veya açıklamalı reddet |
| Müşteri listesi | Kod, firma, yetkili, telefon, bakiye, risk, son işlem | Yeni müşteri, içe/dışa aktar, detaya git |
| Müşteri detayı | Kimlik, adresler, contacts, satışlar, teklifler, siparişler, cari, risk, notlar | Düzenle, not ekle, cari ekstreye git |
| Müşteri oluştur/düzenle | Firma, vergi, yetkili, telefon, e-posta, fatura/teslim adresi, vade, not, aktiflik | Kaydet, pasife al |

### 4.4 Ürün modülü

| Ekran | Temel içerik | Ana işlemler |
|---|---|---|
| Ürün kart görünümü | Fotoğraf, ürün adı, kod, barkod, stok, fiyat, aktiflik | Detay, düzenle, teklife ekle |
| Ürün tablo görünümü | Kod, ad, kategori, temel birim, ambalaj özeti, stok, minimum stok, fiyat, durum | Filtrele, dışa aktar, toplu işlem |
| Ürün detayı | Görsel, kod, barkodlar, temel birim, palet-koli-paket hiyerarşisi, dönüşüm katsayıları, fiziksel ölçüler, net ağırlık, hacim, istifleme, fiyat, maliyet, minimum stok, hareketler | Düzenle, ambalaj ekle/sürümle, fiziksel profil düzenle, barkod ekle, görsel yükle |
| Ürün oluştur/düzenle | Ürün ana bilgileri, `base_uom`, ambalaj seviyeleri, koli/paket içerikleri, parçalı işlem izni, temel birim ölçü/ağırlık | Kaydet, aktif/pasif yap, dönüşüm ve fiziksel profil doğrula |
| Ambalaj hiyerarşisi | Seviye, ad, üst ambalaj, alt ambalaj adedi, temel birim karşılığı, satılabilirlik, parçalı işlem | Ekle, sıralamayı değiştir, effective date ile sürümle |
| Fiziksel profil | Boyutlar, ölçü birimi, net/brüt/dara ağırlık, hacim, kırılabilirlik, yön, istiflenebilirlik, maksimum istif sayısı | Kaydet, geçerlilik sürümle, doğrulama uyarılarını çöz |
| Kategori listesi | Kategori adı, üst kategori, ürün sayısı, durum | Ekle, düzenle, arşivle |
| Barkod yönetimi | Barkod, ürün, ambalaj seviyesi, barkod tipi, aktiflik | Ekle, değiştir, pasifleştir |

### 4.5 Depo ve stok modülü

| Ekran | Temel içerik | Ana işlemler |
|---|---|---|
| Stok listesi | Ürün, depo, konum, temel birim mevcut/rezerve/kullanılabilir, ambalaj kırılımı, minimum stok | Sorgula, koli/paket görünümü seç, filtrele, dışa aktar |
| Stok detayı | Depo/konum kırılımı, temel miktar, `5 Koli + 6 Paket` ambalaj görünümü, rezervasyonlar, hareket zaman çizelgesi | Görünüm birimi değiştir, hareket gör, sayım başlat, transfer başlat |
| Stok hareketleri | Tarih, hareket tipi, ürün, depo, temel miktar, girilen ambalaj, belge, kullanıcı | Filtrele, belgeye git, dışa aktar |
| Depolar | Depo kodu, adı, sorumlu, konum sayısı, durum | Depo ekle, düzenle, pasif yap |
| Depo konumları | Raf/konum kodu, depo, kapasite, durum | Konum ekle, düzenle |
| Transfer oluştur | Kaynak, hedef, ürün, miktar + ambalaj, temel miktar önizlemesi, açıklama | Taslak, onay, transferi tamamla |
| Sayım | Sayım no, depo, sorumlu, tarih, durum, fark, sayım birimi | Sayım başlat, barkodla koli/paket say, sonuçlandır |
| Stok düzeltme | Ürün, mevcut temel miktar, girilen ambalaj, yeni miktar, fark nedeni, belge | Talep oluştur, yetkili onayı |
| Barkod merkezi | Kamera/USB barkod, ürün sonucu, yapılabilir işlemler | Sorgu, sayım, transfer, sevkiyat |

### 4.6 Üretim modülü

| Ekran | Temel içerik | Ana işlemler |
|---|---|---|
| İş emirleri listesi | İş emri no, ürün, hedef, gerçekleşen, makine, plan tarihi, öncelik, durum | Oluştur, filtrele, kanbana geç |
| İş emri kanbanı | Planned, Released, InProgress, Paused, Completed, Cancelled sütunları | Durum değiştir, detaya git |
| İş emri oluştur | Ürün, hedef miktar, plan tarih, makine, öncelik, açıklama | Kaydet, serbest bırak |
| İş emri detayı | Hedef ilerlemesi, makine, plan, üretim kayıtları, personel, fire, duruş, hareket | Başlat, duraklat, üretim kaydı, tamamla |
| Üretim kaydı | İş emri, makine, başlangıç, bitiş, miktar, fire, duruş, not, personeller | Kaydet, düzeltme talep et |
| Makineler | Kod, ad, departman, model, seri no, durum | Ekle, düzenle, duruş başlat |
| Makine detayı | Durum, aktif iş, üretim toplamı, fire, verimlilik, duruş geçmişi | İş emri ata, duruş kaydet |
| Üretim raporları | Ürün, makine, personel, miktar, fire, süre, verimlilik | Tarih filtresi, grafik, dışa aktar |

### 4.7 Sevkiyat ve belge modülü

| Ekran | Temel içerik | Ana işlemler |
|---|---|---|
| İrsaliyeler listesi | İrsaliye no, sipariş, müşteri, tarih, temel miktar toplamı, ambalaj görünümü, durum, fatura durumu | Yeni irsaliye, filtrele, faturala |
| İrsaliye oluştur | Sipariş, müşteri, adres, ürünler, sevk edilecek miktar + ambalaj, temel miktar, tarih, açıklama | Taslak, hazırla, kesinleştir |
| İrsaliye detayı | Ürünler, `5 Koli (10.000 adet)` görünümü, temel stok çıkışı, bağlı sipariş, sevkiyat, fatura, hareket | PDF, iptal, sevkiyat oluştur |
| Sevkiyat listesi | Sevkiyat no, irsaliye, müşteri, araç, şoför, temel/ambalaj toplamı, tarih, teslim durumu | Oluştur, yükle, teslim et |
| Sevkiyat detayı | Araç, şoför, yükleme, çıkış, teslim, belge, not, kapasite özeti | Hazırla, sevk edildi, teslim edildi, kargo planına git |
| Kargo planlama | Sevkiyat kalemleri, araç/kargo kapasitesi, toplam kg, hacim, palet sayısı, doluluk ve uyarılar | Otomatik öneri oluştur, manuel palet ata, uygunluğu hesapla, planı kilitle |
| Karışık palet detayı | Palet barkodu, ürün/ambalaj satırları, temel miktar, kg, hacim, istifleme durumu | Kalem ekle/çıkar, barkodla doğrula, etiketi bas |
| Yükleme doğrulama | Planlanan/gerçekleşen palet, koli, temel miktar, ağırlık ve hacim | Barkod okut, fark açıklaması, yüklemeyi tamamla |
| Araç/kargo tipleri | Tip, iç ölçü, maksimum kg, hacim, palet kapasitesi, ölçü sınırı, istifleme kuralı | Ekle, düzenle, pasifleştir |
| Palet tipleri | Tip, ölçü, dara ağırlığı, maksimum yük, istifleme kuralı | Ekle, düzenle, pasifleştir |
| Yük planı özeti | Toplam net/brüt kg, hacim, palet sayısı, araç doluluk oranları, uyarılar | Uygunluğu hesapla, planı kilitle |
| Yükleme farkı | Planlanan/gerçekleşen koli, temel miktar, palet, kg, hacim farkı | Açıklama gir, yetkiliye gönder, yeniden doğrula |
| Araçlar | Plaka, araç tipi, kapasite, durum | Ekle, düzenle |
| Şoförler | Sicil, ad, telefon, ehliyet, durum | Ekle, düzenle |

### 4.8 Muhasebe, fatura ve cari modülü

| Ekran | Temel içerik | Ana işlemler |
|---|---|---|
| Faturalar listesi | Fatura no, müşteri, tarih, irsaliye, toplam, vade, ödeme durumu, faturalanan/kalan miktar | Oluştur, PDF, filtrele |
| Faturalandırma allocation ekranı (O-003 seçilirse) | İrsaliye kalemi, temel sevk edilen, faturalanan, kalan, ambalaj görünümü, seçilen miktar | Miktar/ambalaj seç, doğrula, faturaya aktar |
| Fatura detayı | Kalemler, ara toplam, iskonto, vergi, genel toplam, vade, bağlı belgeler, cari etkisi | PDF, iptal yetkisi, ödeme ekle |
| Fatura oluştur | İrsaliye/sipariş, müşteri, kalemler, vergi, iskonto, vade | Ön izleme, oluştur |
| Cari hesaplar listesi | Müşteri, borç, alacak, bakiye, geciken, risk | Detaya git, risk filtresi |
| Cari hesap detayı | Açılış bakiyesi, borç, alacak, bakiye, vade, gecikme, hareketler | Ödeme, ekstre, not |
| Cari ekstre | Tarih, işlem, belge, borç, alacak, bakiye | Tarih filtresi, PDF, Excel |
| Ödemeler listesi | Tarih, müşteri, tutar, ödeme tipi, belge, kullanıcı | Yeni ödeme, düzeltme yetkisi |
| Ödeme oluştur | Müşteri, tarih, tutar, ödeme tipi, açıklama, belge | Kaydet, cari etkisini onayla |
| Risk analizi | Risk seviyesi, borç, gecikme, ödeme davranışı, satış hacmi | Filtrele, müşteri detayına git |
| Ödeme tipleri | Nakit, havale, EFT, çek, senet, kredi kartı, diğer | Ayar olarak yönet |

### 4.9 Personel ve İK modülü

| Ekran | Temel içerik | Ana işlemler |
|---|---|---|
| Personel listesi | Sicil, ad, departman, pozisyon, giriş tarihi, durum | Ekle, filtrele, dışa aktar |
| Personel detayı | Kimlik, iletişim, görev, maaş özeti, puantaj, izin, mesai, üretim ilişkisi | Düzenle, belge ekle |
| Personel oluştur/düzenle | Sicil, ad, departman, pozisyon, tarih, telefon, e-posta, maaş, durum | Kaydet |
| Puantaj | Günlük giriş, çıkış, çalışma, fazla, eksik, devamsızlık | Düzeltme, aylık onay |
| Mesai | Personel, tarih, süre, neden, onay durumu | Talep, onay, rapor |
| İzinler listesi | Personel, izin tipi, başlangıç, bitiş, gün, durum | Onayla, reddet, detay |
| İzin talebi | Personel, tip, tarih, gün, açıklama | Gönder, geri çek |
| Maaş kayıtları | Dönem, brüt/net, mesai, kesinti, ikramiye, avans, net ödeme | Dönem oluştur, dışa aktar |
| İK raporları | Çalışma, izin, devamsızlık, mesai, personel süresi | Tarih filtresi, grafik |

### 4.10 Rapor modülü

| Rapor grubu | Raporlar |
|---|---|
| Satış | Günlük, haftalık, aylık, yıllık, müşteri bazlı, ürün bazlı, tekliften siparişe dönüşüm |
| Üretim | Makine, personel, ürün, günlük, aylık, yıllık, fire, verimlilik |
| Stok | Mevcut, kritik, hareketler, depo, ürün, rezervasyon |
| Cari | Borç/alacak, ekstre, geciken ödemeler, risk |
| Fatura | Dönem, ödeme durumu, gecikme, müşteri, ürün |
| İrsaliye | Günlük, bekleyen, faturalaşmamış, sevk durumu |
| Sevkiyat ve kargo | Araç doluluk, palet kullanımı, toplam kg/m³, karışık palet, planlanan-gerçekleşen yük farkı |
| Personel | Puantaj, mesai, izin, devamsızlık, çalışma süresi |

Ortak rapor araçları tarih aralığı, müşteri, ürün, depo, makine, personel, durum, ödeme tipi, ambalaj seviyesi ve araç/kargo tipi filtreleridir. Miktar raporlarında `Temel Birim / Ambalaj / Kırılım` toggle'ı bulunur; dipnotta temel birim belirtilir. Grafik ve tablo aynı filtre kümesini kullanır. Dışa aktarma seçenekleri PDF, Excel ve CSV olacaktır.

### 4.11 Bildirim, yönetim ve sistem ayarları

| Ekran | Temel içerik |
|---|---|
| Bildirim merkezi | Yeni sipariş onayı, teklif talebi, geciken ödeme, kritik stok, faturalaşmamış irsaliye, izin, sevkiyat hazır |
| Bildirim ayarları | Kullanıcı veya rol bazında bildirim tercihleri |
| Kullanıcılar | Kullanıcı, e-posta, roller, durum, son giriş, oturumlar |
| Kullanıcı detayı | Roller, izinler, override yetkileri, oturum geçmişi |
| Roller | Rol adı, açıklama, kullanıcı sayısı, aktiflik |
| Yetkiler | Modül/işlem bazında read, create, update, delete, approve, cancel |
| Audit log | Kim, ne yaptı, kayıt, eski/yeni değer, tarih, IP |
| Sistem ayarları | Şirket bilgileri, belge numarası, para birimi, vergi, timezone, bildirim, dosya |
| Depolama ve yedekleme | Yedek durumu, son başarılı yedek, retention, manuel yedek |
| Sağlık durumu | API, database, storage ve arka plan servisleri |

### 4.12 Public katalog

| Ekran | Temel içerik |
|---|---|
| Public ana sayfa | Logo, ürünler, kurumsal, iletişim, CTA, öne çıkan ürünler |
| Public ürün listesi | Arama, kategori, ölçü, ürün kartları, Teklife Ekle |
| Public ürün detayı | Fotoğraf, ürün bilgisi, paket/koli içeriği, miktar, not |
| Teklif sepeti | Ürün, miktar, birim, ürün notu, genel not |
| Firma bilgileri | Firma, yetkili, telefon, e-posta |
| Talep özeti | Ürünler, miktarlar, iletişim onayı, gönder |
| Public erişim/anti-abuse durumu | Rate limit, bot kontrolü, doğrulama, privacy consent ve hata mesajı (O-009) | Doğrula, yeniden dene, destek mesajı göster |
| Başarı ekranı | Talep numarası, tarih, şirket dönüş açıklaması |

## 5. Mobil ekran envanteri

| Mobil alan | Ekranlar |
|---|---|
| Giriş | Giriş, parola değişimi, bağlantı durumu |
| Ana sayfa | Görev özeti, hızlı işlemler, kritik uyarılar |
| Barkod | Kamera, barkod sonucu, ürün detayı, işlem seçimi |
| Stok | Stok sorgu, depo/kırılım, hareket geçmişi |
| Sayım | Sayım görevi, barkodla sayım, fark ve gerekçe, onaya gönder |
| Transfer | Kaynak depo, hedef depo, ürün, miktar, özet, tamamla |
| Sevkiyat | Görev listesi, irsaliye, ürün doğrulama, yükleme tamamla |
| Üretim | Aktif iş emirleri, iş emri detayı, başlat, üretim kaydı, tamamla |
| Bildirim | Görevler, kritik uyarılar, ilgili kayda git |
| Profil | Kullanıcı, uygulama ayarları, çıkış |

## 6. Temel belge durumları

| Belge | Durumlar |
|---|---|
| Teklif talebi | NEW, REVIEWING, QUOTED, ACCEPTED, REJECTED, EXPIRED |
| Sipariş | Taslak, Onay Bekliyor, Onaylandı, Reddedildi, Hazırlanıyor, Kısmi Sevk, Tamamlandı, İptal |
| Onay | Pending, Approved, Rejected |
| İrsaliye | Draft, Prepared, Issued, Shipped, PartiallyInvoiced (O-003 seçilirse), Invoiced, Cancelled |
| Sevkiyat | Hazırlanıyor, Sevk Edilecek, Sevk Edildi, Teslim Edildi, İptal |
| Fatura | Ödenmedi, Kısmi Ödendi, Ödendi, Gecikmiş |
| Üretim iş emri | Planned, Released, InProgress, Paused, Completed, Cancelled |
| İzin | Bekliyor, Onaylandı, Reddedildi |

## 7. Ana uçtan uca tasarım senaryoları

### Satıştan tahsilata

```text
Public katalog
→ Teklif talebi
→ Satış incelemesi
→ Teklif
→ Sipariş taslağı
→ Sorumlu onayı
→ Stok rezervasyonu
→ İrsaliye
→ Sevkiyat
→ Fatura
→ Cari borç
→ Ödeme
→ Güncel bakiye
```

### Üretimden stoğa

```text
Üretim planı
→ İş emri
→ Makine ataması
→ Personel ataması
→ Üretim başlangıcı
→ Miktar/fire/duruş kaydı
→ Üretim tamamlanması
→ Depo üretim girişi
→ Stok güncellemesi
```

### Personel yönetimi

```text
Personel kartı
→ Puantaj
→ Mesai
→ İzin talebi
→ Onay
→ Aylık çalışma özeti
→ Maaş kaydı
→ İK raporu
```

## 8. Tasarım tamamlanma kriteri

Tasarım seti, her modül için en az liste, oluşturma/düzenleme, detay, işlem onayı ve hata/boş durumlarını açıklamalıdır. Kritik süreçlerde hangi rolün hangi butonu göreceği, işlemin hangi belgeyi oluşturacağı ve stok/cari/personel kaydındaki etkisinin kullanıcıya nasıl anlatılacağı ayrıca belirtilmelidir.

Bu envanter sonraki tasarım dokümanlarının ana kontrol listesi olacaktır. Her ekran görsel prototipe dönüştürüldüğünde ilgili route, alan, rol, durum ve bağlı belge referansı bu envanterden kontrol edilecektir.
