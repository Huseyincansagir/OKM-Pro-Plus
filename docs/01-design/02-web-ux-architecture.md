# Web Arayüz Bilgi Mimarisi ve Ekran Akışları

## 1. Web uygulaması ana navigasyonu

Sol menü, kullanıcının yetkisine göre bölümleri gösterecek ve her bölümün yanında bekleyen iş sayısını rozet olarak taşıyacaktır. Menüde gereksiz teknik ayrıntı yerine kullanıcıların günlük işlerine göre isimlendirme kullanılacaktır.

| Menü bölümü | Alt ekranlar | Bekleyen iş rozeti |
|---|---|---|
| Dashboard | Rol bazlı özet | Uyarı sayısı |
| Satış | Teklif talepleri, teklifler, siparişler | Onay bekleyen sipariş |
| Ürünler | Ürün kataloğu, kategoriler, barkodlar | Kritik stok bağlantısı |
| Depo | Stok, hareketler, transferler, sayım, barkod | Hazırlanacak sevkiyat |
| Üretim | İş emirleri, üretim kayıtları, makineler, üretim raporları | Aktif iş emirleri |
| Sevkiyat | İrsaliyeler, sevkiyatlar, araçlar, şoförler | Sevke hazır kayıt |
| Cari ve Muhasebe | Faturalar, cari hesaplar, ödemeler, ekstre, risk | Geciken ödeme |
| Personel | Personeller, puantaj, izin, mesai, maaş | Onay bekleyen izin |
| Raporlar | Satış, üretim, stok, cari, fatura, sevkiyat, personel | — |
| Bildirimler | Tümü, bana atananlar, kritik | Okunmamış bildirim |
| Yönetim | Kullanıcılar, roller, yetkiler, ayarlar, audit log | — |

## 2. Genel sayfa şablonu

Her liste ekranı aynı zihinsel modeli koruyacaktır. Kullanıcı, hangi modülde olduğunu, hangi kayıtları gördüğünü, hangi filtrelerin aktif olduğunu ve bir sonraki işlemi her zaman anlayabilmelidir.

```text
Üst bar: Global arama | Bildirimler | Yardım | Kullanıcı menüsü
Sol menü: Modül navigasyonu
İçerik:
  Breadcrumb
  Sayfa başlığı + açıklama + ana işlem
  KPI kartları
  Arama + gelişmiş filtreler + dışa aktarma
  Veri tablosu
  Sayfalama
```

Detay ekranları için standart yapı şöyledir:

```text
Başlık: Belge numarası + durum rozeti + ana işlem
Özet kartları: müşteri / tutar / tarih / sorumlu
Sekmeler: Genel | Kalemler | Hareketler | Belgeler | Aktivite
Sağ panel: işlem geçmişi ve sonraki adım
Alt sabit alan: Kaydet | Onaya gönder | İptal / geri dön
```

## 3. Rol bazlı dashboard tasarımları

### Yönetici dashboard'u

İlk satırda bugünkü satış, bugün üretilen miktar, bekleyen sipariş, onay bekleyen işlem ve tahsilat gösterilecektir. İkinci satırda satış trendi, üretim performansı ve stok uyarıları yer alacaktır. Sağ tarafta riskli müşteriler, geciken ödemeler, faturalaşmamış irsaliyeler ve vadesi yaklaşan faturalar görev listesi olarak gösterilecektir.

### Depo dashboard'u

Depo kullanıcısı için ana odak stok miktarı değil, yapılacak iş sırasıdır. Üstte kritik stok, bugün bekleyen giriş, hazırlanacak sevkiyat ve son stok hareketi gösterilecektir. Hızlı işlem alanında barkod tara, stok sorgula, sevkiyat hazırla ve sayım başlat seçenekleri bulunacaktır.

### Üretim dashboard'u

Aktif iş emirleri, makine durumları, bugün üretilen miktar, fire oranı ve duruş süresi ilk alanda gösterilecektir. İş emirleri makine veya öncelik bazında filtrelenebilecek ve devam eden iş emri tek tıklamayla üretim kaydı ekranına açılacaktır.

### Muhasebe dashboard'u

Tahsilat toplamı, toplam borç, toplam alacak, geciken fatura ve faturalaşmamış irsaliye gösterilecektir. Geciken müşteriler tutar ve gecikme gününe göre sıralanacak; her satır doğrudan cari ekstreye açılacaktır.

### İnsan kaynakları dashboard'u

Bugün çalışan personel, devamsızlık, izinli personel, fazla mesai ve onay bekleyen izin talebi gösterilecektir. Aylık puantaj görünümüne ve personel detayına hızlı geçiş bulunacaktır.

## 4. Ürün kataloğu ve ürün detayı

Ürün kataloğu masaüstünde filtrelenebilir kart ve tablo görünümünü destekleyecektir. Görseli güçlü olan ürünlerde kart görünümü, hızlı operasyonlarda tablo görünümü kullanılacaktır.

| Ürün kartı alanı | Gösterim |
|---|---|
| Fotoğraf | Kare küçük görsel, yoksa nötr placeholder |
| Ürün adı | Birincil metin |
| Ürün kodu | İkincil metin |
| Barkod | Barkod rozeti veya kodu |
| Stok | Mevcut / rezerve / kullanılabilir |
| Fiyat | TRY formatında |
| Durum | Aktif veya pasif |

Ürün detayında üst bölümde fotoğraf, ürün adı, kod ve stok özeti; alt bölümde bilgiler, barkodlar, fiyatlar, stok hareketleri ve bağlı üretim kayıtları sekmeleri bulunacaktır. “Teklife ekle” işlemi public katalogda birincil, şirket içi katalogda ikincil işlem olacaktır.

## 5. Public ürün kataloğu ve teklif sepeti

Public katalog, şirket içi ERP menüsünden görsel olarak ayrılacak ve müşterinin teknik sistem hissine kapılmadan ürün seçmesini sağlayacaktır. Ürün kartında fotoğraf, ürün adı, kodu, kısa açıklaması ve “Teklife ekle” butonu bulunacaktır. Filtreler kategori, ürün tipi ve arama alanından oluşacaktır.

Teklif sepeti sağdan açılan panel veya ayrı sayfa olarak tasarlanacaktır. Her satırda ürün, miktar, müşteri notu ve kaldırma işlemi yer alacaktır. Son adımda firma, yetkili, telefon ve e-posta alanları istenecek; gönderimden sonra talep numarası ve “Şirketimiz inceleyip sizinle iletişime geçecektir” mesajı gösterilecektir.

## 6. Sipariş akışı

Sipariş ekranı üç ana katmandan oluşacaktır: liste, detay ve onay paneli.

### Sipariş listesi

Tabloda sipariş numarası, müşteri, tarih, toplam, ödeme şartı, sevk durumu, onay durumu ve sorumlu gösterilecektir. Varsayılan filtre “Benim işlem bekleyenlerim” olacak; kullanıcı tüm siparişlere geçebilecektir.

### Sipariş detay sekmeleri

| Sekme | İçerik |
|---|---|
| Genel | Müşteri, adres, ödeme şartı, teslim tarihi, notlar |
| Ürünler | Ürün, miktar, fiyat, iskonto, vergi, toplam |
| Stok ve rezervasyon | Mevcut, rezerve, eksik ve kullanılabilir miktar |
| Belgeler | Teklif, irsaliye, sevkiyat, fatura bağlantıları |
| Onay geçmişi | Onaylayan kişi, tarih, karar ve açıklama |
| Aktivite | Kayıt üzerinde yapılan değişikliklerin zaman çizelgesi |

### Sipariş onay paneli

Sorumlu “Onayla” butonuna bastığında ayrı bir onay paneli açılacak ve sistem toplam tutarı, stok yeterliliğini, teslim tarihini ve ödeme şartını özetleyecektir. Kullanıcı açıklama yazabilir. Onay sonrası sipariş durumu “Onaylandı”, stok durumu “Rezerve edildi” ve sonraki işlem “İrsaliye oluştur” olarak gösterilecektir. Reddetme işleminde açıklama zorunlu olacaktır.

## 7. Depo ve stok ekranları

Stok listesi ürün, depo, konum, mevcut miktar, rezerve miktar ve kullanılabilir miktar kolonlarını gösterecektir. Kritik stok satırlarında görsel uyarı ve minimum stok değeri görünür olacaktır. Stok detayında giriş, çıkış, transfer, sayım, iade ve düzeltme hareketleri zaman sırasıyla listelenecektir.

Barkod okuyucu, web üzerinde klavye girdisi gibi çalıştığında odaklanmış barkod alanı otomatik olarak ürünü bulacak; mobilde kamera taraması aynı ürün detayına bağlanacaktır. Ürün bulunduğunda kullanıcıya “Stok görüntüle”, “Transfer başlat”, “Sayım yap” ve yetkisi varsa “Düzeltme talebi oluştur” işlemleri sunulacaktır.

## 8. Üretim ekranları

İş emirleri liste ekranında iş emri numarası, ürün, hedef, gerçekleşen, makine, öncelik, plan tarihi ve durum gösterilecektir. Kanban görünümü planlandı, serbest bırakıldı, devam ediyor, duraklatıldı ve tamamlandı sütunlarını destekleyecektir.

İş emri detayında üstte hedefe ilerleme çubuğu, makine kartı, planlanan tarih ve öncelik bulunacaktır. “Üretim kaydı ekle” ekranında başlangıç, bitiş, üretilen miktar, fire, duruş, not ve çalışan personeller girilecektir. Personel seçiminde her kişi için rol, vardiya ve çalışma süresi ayrı satırda tutulacaktır.

## 9. İrsaliye ve sevkiyat ekranları

Onaylanmış siparişten irsaliye oluşturulurken kullanıcı, her ürün için sipariş miktarı, rezerve miktar, daha önce sevk edilen miktar ve sevk edilecek miktarı görecektir. Stok yetersizse sistem irsaliyeyi kesinleştirmeden önce açık bir uyarı verecektir.

Sevkiyat detayında irsaliye ve müşteri özeti üstte; araç, şoför, yükleme tarihi, çıkış tarihi ve teslim durumu ortada; teslim belgesi ve notlar altta yer alacaktır. Sevkiyat durumu yatay bir adım göstergesiyle hazırlanıyor, sevke hazır, sevk edildi, teslim edildi ve iptal şeklinde izlenecektir.

## 10. Fatura ve cari ekranları

Fatura listesinde fatura numarası, müşteri, tarih, bağlı irsaliye, toplam, vade ve ödeme durumu görünür olacaktır. Fatura detayında cari etkisi ayrı bir özet kartında gösterilecektir. Fatura oluşturma işlemi öncesinde bağlı irsaliye, vergi, iskonto, vade ve genel toplam kontrol panelinde özetlenecektir.

Müşteri detayının üst kısmında cari özet kartları bulunacaktır:

| Kart | İçerik |
|---|---|
| Borç | Açık borç toplamı |
| Alacak | Müşteriden beklenen veya müşteriye ait alacak |
| Bakiye | Net cari bakiye |
| Geciken | Vadesi geçen toplam |
| Risk | Düşük, orta, yüksek veya kritik |

Cari ekstre sekmesinde tarih, işlem, belge no, borç, alacak ve bakiye kolonları bulunacaktır. Ödeme girişi tamamlanmadan önce eski bakiye ve işlem sonrası bakiye yan yana gösterilecektir.

## 11. Rapor ekranı standartları

Rapor sayfası, sol tarafta rapor kategorileri ve sağ tarafta seçilen raporun sonuç alanından oluşacaktır. Ortak filtre çubuğunda tarih aralığı, müşteri, ürün, depo, makine, personel, durum ve ödeme tipi bulunacaktır. Grafik ile tablo her zaman aynı filtreleri kullanacaktır.

Rapor çıktılarında “PDF”, “Excel” ve “CSV” seçenekleri sayfanın sağ üstünde bulunacak; dışa aktarma sırasında aktif filtre özeti dosyada yer alacaktır.

## 12. Bildirim ve görev merkezi

Bildirimler yalnızca bilgi mesajı değil, işlem bekleyen görev olarak da tasarlanacaktır. Bildirim satırında olay, ilgili kayıt, gönderen sistem bölümü, zaman ve doğrudan işlem butonu bulunacaktır. Örneğin “3 sipariş onay bekliyor” bildirimi kullanıcıyı filtrelenmiş sipariş listesine götürecektir.

## 13. Ortak durum ekranları

Her ana ekranda aşağıdaki durumlar önceden tasarlanacaktır:

| Durum | Arayüz davranışı |
|---|---|
| Yükleniyor | Tablo iskeleti ve butonlarda pasif görünüm |
| Sonuç yok | Neden ve ilk işlem önerisi |
| Filtre sonucu yok | Filtreleri temizle bağlantısı |
| Hata | Kısa açıklama ve tekrar dene |
| Yetki yok | Erişim gerekçesi ve yöneticiden yetki isteme seçeneği |
| Başarılı işlem | Toast ve ilgili sonraki adıma bağlantı |
| Kritik işlem | Açıklama isteyen onay penceresi |

## 14. Tasarımın doğrulanacağı ana uçtan uca senaryo

İlk web prototipi aşağıdaki senaryoyu kesintisiz gösterecek şekilde hazırlanacaktır:

```text
Ürün kataloğu
→ Teklif sepeti
→ Teklif talebi
→ Teklif hazırlama
→ Sipariş oluşturma
→ Sorumlu onayı
→ Stok rezervasyonu
→ İrsaliye hazırlama
→ Sevkiyat
→ Fatura
→ Cari borç
→ Ödeme
→ Güncel bakiye
```

Bu akışın her adımında belge bağlantısı, durum rozeti, yetkili kullanıcı ve sonraki işlem görünür olacaktır.
