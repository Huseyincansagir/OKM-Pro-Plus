# Üretim ve Depo Yönetim Modülleri
## Detaylı Ekran, Veri ve Etkileşim İncelemesi

## 1. İncelemenin amacı

Üretim ve depo modülleri sistemin fiziksel gerçekliğini temsil eder. Satış ekranında verilen siparişin gerçekten hazırlanabilmesi, üretilen ürünün güvenilir biçimde stoğa girmesi ve sevkiyat sırasında doğru ürünün doğru miktarda çıkması bu iki modülün doğruluğuna bağlıdır.

Bu inceleme; her ekranın amacı, ana alanları, kullanıcı aksiyonları, durumları, yetki sınırları, veri etkileri ve web–mobil ilişkisini detaylandırır.

## 2. Modül sınırları

| Modül | Sistem gerçeği |
|---|---|
| Üretim | Hangi ürünün, hangi iş emriyle, hangi makinede, hangi personelle, ne kadar ve ne fireyle üretildiğini kaydeder. |
| Depo | Ürünün hangi depoda/konumda olduğunu, ne kadar mevcut/rezerve/kullanılabilir olduğunu ve hangi hareketlerle değiştiğini yönetir. |
| Barkod | Fiziksel ürün ile sistemdeki ürün kaydını hızlı ve kontrollü şekilde eşleştirir. |
| Sevkiyat | Siparişten çıkan ürünlerin irsaliye ve barkod doğrulamasıyla müşteriye hazırlanmasını yönetir. |

## 3. Üretim dashboard'u

### Ekranın amacı

Üretim sorumlusuna vardiyanın ve iş emirlerinin genel durumunu tek ekranda göstermek. Dashboard bir rapor ekranı değil, günlük iş önceliklendirme ekranıdır.

### Üst KPI alanı

| KPI | Açıklama | Etkileşim |
|---|---|---|
| Aktif İş Emirleri | Planned, Released, InProgress ve Paused kayıtlar | İş emri listesine filtreli geçiş |
| Bugünkü Üretim | Gün içinde tamamlanan veya kaydedilen miktar | Üretim raporuna geçiş |
| Toplam Fire | Bugünkü veya seçili dönemdeki fire | Fire raporuna geçiş |
| Makine Duruşu | Duruş toplam süresi | Makine/duruş listesine geçiş |
| Ortalama Verimlilik | Hedefe göre üretim performansı | Makine performans raporu |

### Ana çalışma alanı

Sol tarafta önceliklendirilmiş aktif iş emirleri, sağ tarafta makine durumları gösterilir. İş emri kartında iş emri no, ürün, hedef, gerçekleşen, kalan, makine, plan tarihi, öncelik ve durum bulunur. Kartın ana aksiyonu mevcut duruma göre değişir.

```text
Planned      → Serbest Bırak
Released     → Üretimi Başlat
InProgress   → Üretim Kaydı Ekle / Duraklat
Paused       → Devam Et
Tamamlama    → Tamamla
```

## 4. İş emirleri listesi ve kanban

### Liste görünümü

Tablo kolonları; iş emri no, ürün, hedef miktar, gerçekleşen, kalan, planlanan tarih, makine, öncelik, sorumlu, durum ve son kayıt zamanıdır. Kullanıcı ürün, makine, tarih, öncelik ve durum filtrelerini birlikte kullanabilir.

### Kanban görünümü

Kanban sütunları Planned, Released, InProgress, Paused, Completed ve Cancelled durumlarını temsil eder. Kart sürükleme özelliği iş kuralına göre sınırlandırılır; örneğin Completed bir iş emri doğrudan InProgress'e taşınamaz. Durum değişimi gerekiyorsa açıklama veya yetkili onayı istenir.

### İş emri oluşturma ekranı

| Alan | Zorunluluk | Açıklama |
|---|---|---|
| İş Emri No | Sistem üretir | `URE-2026-000021` gibi sequence |
| Ürün | Zorunlu | Ürün kartından seçilir |
| Hedef Miktar | Zorunlu | Üretim birimiyle birlikte girilir; `base_uom` ve gerekiyorsa palet/koli/paket görünümü gösterilir |
| Planlanan Tarih | Zorunlu | Vardiya/takvim kontrolü yapılır |
| Makine | Başlangıçta opsiyonel | İş serbest bırakılmadan önce atanabilir |
| Öncelik | Zorunlu | Düşük, normal, yüksek, kritik |
| Açıklama | Opsiyonel | Üretim talimatı veya not |

## 5. İş emri detay ekranı

### Üst özet

Üstte iş emri numarası, ürün adı, durum rozeti, planlanan tarih, makine, sorumlu ve ilerleme çubuğu bulunur. İlerleme çubuğu yalnızca yüzde değil, `32.400 / 50.000 adet` biçiminde temel birimle; gerekiyorsa `16,2 Koli / 25 Koli` gibi ambalaj görünümüyle de gösterilir. Üretim kaydının doğruluk kaynağı temel birim miktarıdır.

### Sekmeler

| Sekme | İçerik |
|---|---|
| Genel | Ürün, hedef, plan, öncelik, açıklama, sorumlu |
| Üretim Kayıtları | Her gerçekleşme, miktar, fire, başlangıç, bitiş, operatör |
| Personeller | Personel, rol, vardiya, çalışma süresi |
| Makine | Makine durumu, OEE/performans özeti, duruş geçmişi |
| Fire ve Duruş | Fire nedeni, miktarı, duruş nedeni ve süresi |
| Depo Hareketi | Üretim tamamlanınca oluşan stok girişleri |
| Aktivite | Durum, atama, düzeltme ve onay geçmişi |

### Üretim kaydı ekleme

Formun üstünde iş emri ve makine kilitli özet olarak görünür. Kullanıcı başlangıç/bitiş zamanını, üretilen miktarı, fireyi, duruş süresini ve notu girer. Personel tablosunda çalışan, rol, vardiya ve çalışma süresi satır bazında eklenir.

Form kapanmadan önce canlı özet gösterilir:

```text
Hedef miktar: 50.000 adet (25 Koli)
Yeni kayıt: 6.200 adet (3 Koli + 200 adet)
Toplam gerçekleşen: 32.400 adet (16 Koli + 400 adet)
Kalan: 17.600 adet (8 Koli + 1.600 adet)
Fire: 150 adet / %2,4
```

Ekran, kullanıcıya ambalaj kırılımını gösterse de backend'e temel birim miktarı gönderir. Ambalaj dönüşüm katsayısı üretim kaydı tarihindeki packaging snapshot'ından alınır.

## 6. Makine ve duruş yönetimi

Makine listesinde makine kodu, makine adı, üretim hattı/departman, model, seri no, aktiflik ve anlık durum görünür. Makine detayında aktif iş emri, son üretim, toplam çalışma süresi, duruş süresi, fire ve verimlilik gösterilir.

Duruş ekleme ekranında başlangıç zamanı otomatik gelir. Kullanıcı duruş nedeni, tahmini süre ve açıklama girebilir. Duruş bitirilince gerçek süre hesaplanır. İş emri ve makine istatistikleri aynı kayıt üzerinden güncellenir.

Makine pasife alınırken aktif iş emri varsa şu uyarı gösterilir:

> Bu makineye bağlı aktif iş emirleri var. Makineyi pasife almak üretim planını etkileyebilir. Devam etmek için aktif iş emirleri için yeni makine seçin veya yetkili onayı alın.

## 7. Üretim–stok entegrasyonu

Üretim kaydı eklemek ile stoğa giriş yapmak aynı işlem değildir. Ara kayıtlar üretim gerçekleşmesini temsil eder; yalnızca iş emri tamamlandığında veya tanımlı bir ara üretim kuralı varsa stok hareketi oluşturulur.

### Tamamlama öncesi kontrol

| Kontrol | Sonuç |
|---|---|
| Hedef/gerçekleşen ilişkisi | Aşım varsa yetki veya açıklama ister |
| Fire oranı | Eşik üstündeyse kalite/üretim uyarısı |
| Personel süresi | Çalışan ve vardiya bilgisi tamamlanmış olmalı |
| Makine | Aktif/uygun durumda olmalı |
| Depo | Giriş yapılacak depo belirlenmiş olmalı |

### Tamamlama sonrası

```text
ProductionRecord close
→ ProductionOrder = Completed
→ StockMovement = ProductionIn
→ Stock quantity increase
→ Machine statistics update
→ Audit log
→ Notification to warehouse
```

Kullanıcıya tamamlama sonrası “Depoya aktarılacak miktar” ve “Fire miktarı” ayrı gösterilir. Fire stoğa sağlam ürün olarak eklenmez.

## 8. Depo dashboard'u

Depo dashboard'u stok miktarından çok günlük görevleri öne çıkarır. Üstte kritik stok, bekleyen giriş, hazırlanacak sevkiyat ve açık sayım KPI'ları bulunur. Ana işlem kartları `Barkod Tara`, `Stok Sorgula`, `Sevkiyat Hazırla`, `Sayım Başlat` ve `Transfer Oluştur` şeklindedir.

Kritik stok listesinde ürün, depo, kullanılabilir miktar, minimum stok, son hareket ve bekleyen üretim bilgisi görünür. Kullanıcı kritik ürün üzerinden üretim planına veya satın alma/tedarik notuna geçebilir.

## 9. Stok listesi ve stok detay ekranı

### Stok listesi kolonları

| Kolon | Anlam |
|---|---|
| Ürün | Ürün kodu, ad, görsel ve barkod |
| Depo | Ürünün bulunduğu depo |
| Konum | Raf veya depo lokasyonu |
| Mevcut | Temel birimde fiziksel sistem miktarı; ambalaj kırılımı ayrıca gösterilebilir |
| Rezerve | Temel birimde sipariş/işlem için ayrılmış miktar |
| Kullanılabilir | Temel mevcut eksi temel rezerve |
| Ambalaj görünümü | `5 Koli`, `10 Paket` veya `4 Koli + 6 Paket` gibi açıklanabilir kırılım |
| Minimum Stok | Uyarı eşiği, temel birimle tanımlanır |
| Durum | Normal, düşük, kritik, stokta yok |

### Stok detay sekmeleri

`Özet`, `Depo/Konum Dağılımı`, `Hareketler`, `Rezervasyonlar`, `Sayım Geçmişi`, `Bağlı Belgeler` sekmeleri kullanılacaktır. Stok hareketleri kronolojik olarak listelenir; her hareketin kaynak belgesi ve kullanıcı bağlantısı bulunur.

## 10. Stok hareketleri

Stok hareket tipleri üretim girişi, satış çıkışı, transfer, sayım, iade ve düzeltmedir. Her hareket için ürün, depo, konum, temel birim miktarı, yön, belge, kullanıcı, tarih ve açıklama saklanır. Kullanıcının girdiği ambalaj seviyesi ve belge tarihindeki dönüşüm snapshot'ı da korunur; örneğin `5 Koli (10.000 adet)`.

Bir hareketin yanlış olması durumunda eski satır silinmez. Yetkili kullanıcı ters hareket veya düzeltme hareketi oluşturur. Arayüzde ters kayıt ilişkisi görünür:

```text
Yanlış çıkış: STK-2026-00128
Düzeltme girişi: STK-2026-00141
Gerekçe: Sevk miktarı hatalı girildi
Onaylayan: Depo Sorumlusu
```

## 11. Sayım ekranı

Sayım başlığı depo, konum, sayım tarihi, sorumlu, durum ve açıklamayı taşır. Sayım kalemlerinde ürün, barkod, sistem miktarı, sayılan miktar, fark ve fark gerekçesi bulunur.

### Web sayım akışı

```text
Sayım oluştur
→ Depo/konum seç
→ Ürün listesi oluşur
→ Sayılan miktarları gir
→ Farkları incele
→ Gerekçe ekle
→ Onaya gönder veya tamamla
```

### Mobil sayım akışı

```text
Sayım görevi aç
→ Barkod okut
→ Sistem miktarını gör
→ Sayılan miktarı gir
→ Farkı gör
→ Gerekçe seç
→ Sonraki ürüne geç
```

Farklılık yüksekse kullanıcı işlemi tamamlayamaz; sayım sorumlusuna veya yöneticiye inceleme görevi gönderilir.

## 12. Depo transferi

Transfer ekranında kaynak depo, hedef depo, kaynak konum, hedef konum, ürün, miktar ve ambalaj seviyesi bulunur. Kullanıcı `5 Koli` girebilir; sistem temel miktarı önizler ve kaynakta temel kullanılabilir stoğu kontrol eder. İki hareket aynı transaction içinde oluşturulur: kaynak çıkışı ve hedef girişi. Transferin doğruluk kaynağı temel birim, operasyon görünümü ambalaj kırılımıdır.

Transfer durumları `Taslak`, `Hazırlanıyor`, `Yolda`, `Tamamlandı`, `İptal` olarak tasarlanabilir. “Yolda” durumunda kaynak miktar düşmüş, hedef miktar henüz kullanılabilir stoğa eklenmemiş olarak gösterilebilir.

## 13. Barkod deneyimi

### Web USB barkod

USB okuyucu klavye gibi giriş yaptığında odaklanmış barkod alanı kısa sürede gelen karakterleri okuyup Enter sinyalinde arama yapar. Kullanıcı manuel yazı ile cihaz çıktısını ayırt edebilmesi için son tarama zamanı ve barkod uzunluğu sistemde tutulabilir.

### Mobil kamera

Mobilde kamera açılır, barkod çerçeve içinde yakalanır, API ürün sorgusu yapılır ve ürün sonucu açılır. Kullanıcı ürün bulunduktan sonra yalnızca yetkili işlemleri görür.

```text
Camera
→ Barcode decoder
→ Product lookup
→ Product / stock result
→ Role-based operation
```

Ağ yoksa kamera okuması ürün kodunu geçici olarak gösterebilir; stok hareketi kesinleşmiş gibi gösterilmez.

## 14. Sevkiyat hazırlama ile depo bağlantısı

Sevkiyat ekranı irsaliyeden gelen beklenen kalemleri gösterir. Her kalemde sipariş miktarı, daha önce sevk edilen, sevk edilecek ve barkodla doğrulanan miktar; hem temel birimde hem de seçilen ambalaj seviyesinde gösterilir. Örneğin `5 Koli (10.000 adet)`.

```text
İrsaliye aç
→ Ürün kalemlerini gör
→ Barkodla doğrula
→ Okutulan / beklenen karşılaştır
→ Eksik/fazla varsa açıklama
→ Yüklemeyi tamamla
→ Sevk edilecek durumuna geçir
```

Depo kullanıcıları finansal fiyat veya cari bilgilerini varsayılan olarak görmez. Sevkiyat tamamlandığında irsaliye ve stok hareketi arasındaki bağlantı detay ekranında gösterilir.

## 15. Yetki matrisi

| İşlem | Depo | Üretim | Yönetici | Muhasebe |
|---|---:|---:|---:|---:|
| Stok görüntüleme | ✓ | ✓ | ✓ | Sınırlı |
| Stok düzeltme oluşturma | ✓ | — | ✓ | — |
| Stok düzeltme onayı | — | — | ✓ | — |
| Sayım başlatma | ✓ | — | ✓ | — |
| Transfer oluşturma | ✓ | — | ✓ | — |
| Üretim kaydı girme | — | ✓ | ✓ | — |
| İş emri tamamlama | — | ✓ | ✓ | — |
| İrsaliye hazırlama | ✓ | — | ✓ | Görüntüleme |
| Fatura oluşturma | — | — | ✓ | ✓ |

Yetki yok ekranı kullanıcıya yalnızca “erişim yok” dememeli; işlemi yapabilecek departmanı veya rolü de açıklamalıdır.

## 16. Tasarımda özel hata ve güvenlik durumları

| Durum | Kullanıcıya mesaj |
|---|---|
| Kullanılabilir stok yetersiz | Sevk edilebilir miktar: X. Eksik miktar: Y. |
| Aynı ürün tekrar okutuldu | Mevcut okutulan miktara eklendi. |
| Barkod ürünü bulamadı | Barkod kayıtlı değil. Manuel ürün arayın veya yöneticinize bildirin. |
| İş emri tamamlanmadan stok | Üretim tamamlanmadan stoğa giriş yapılamaz. |
| Makine çakışması | Makinede aktif başka iş emri bulunuyor. |
| Sayım farkı | Fark için gerekçe ve yetkili onayı gerekir. |
| Ağ bağlantısı yok | İşlem sunucuya kaydedilmedi. Bağlantı gelince tekrar deneyin. |
| Yetki yok | Bu işlemi yapma yetkiniz bulunmuyor. |

## 17. Üretim ve depo için kritik raporlar

### Üretim

Makine bazında toplam üretim, toplam fire, çalışma/duruş süresi, verimlilik; personel bazında çalışma süresi ve üretim katkısı; ürün bazında hedef-gerçekleşen; günlük/aylık/yıllık trend raporları.

### Depo

Mevcut stok, kritik stok, depo bazlı stok, ürün bazlı hareket, rezerve stok, sayım farkları, transfer bekleyenler, üretim girişleri ve sevkiyat çıkışları.

Rapor ekranı her zaman tarih aralığı, ürün, depo, makine, personel, durum ve belge filtresi taşımalıdır.

## 18. Kabul senaryoları

### Üretim

```text
İş emri oluştur
→ Makine ata
→ Personel ata
→ Üretimi başlat
→ Miktar/fire/duruş gir
→ Üretim kaydını kaydet
→ İş emrini tamamla
→ Üretim stok girişini doğrula
→ Makine istatistiğini kontrol et
```

### Depo

```text
Ürün barkodu okut
→ Depo/konum stokunu gör
→ Sevkiyat aç
→ Barkodla ürün doğrula
→ Miktarı tamamla
→ İrsaliyeyi hazırla
→ Stok çıkışını doğrula
```

### Sayım farkı

```text
Sayım aç
→ Barkodla say
→ Fark oluşur
→ Gerekçe gir
→ Onaya gönder
→ Yetkili onaylar
→ Ters/düzeltme hareketi oluşur
```

## 19. Tasarım kararlarının geliştirmeye etkisi

Üretim ve depo modüllerinde arayüz, yalnızca form ve liste üretmek için değil, domain kurallarını kullanıcıya görünür kılmak için tasarlanmıştır. Stok yetersizliği, iş emri tamamlanmadan stok girişi, barkod bulunamaması, sayım farkı ve makine çakışması gibi durumlar backend kuralı olarak kalmayacak; kullanıcıya anlaşılır biçimde gösterilecektir.

Bu nedenle frontend geliştirme başlamadan önce bu dokümandaki işlem durumları, alan zorunlulukları, yetki matrisi, bağlı belge ve transaction etkileri API sözleşmesine bağlanmalıdır.
