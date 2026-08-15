# Üretim–Depo–Cari Yönetim Sistemi
## Kodlama Öncesi Arayüz Tasarım Brifi

**Sürüm:** Tasarım başlangıcı 0.1  
**Dil:** Türkçe  
**Platformlar:** Web, tablet ve mobil  
**Tasarım yaklaşımı:** Kurumsal, sade, hızlı, bilgi yoğun ve operasyon odaklı

## 1. Ürün vizyonu

Bu uygulama, peçete üretimi yapan fabrikanın üretimden depoya, tekliften siparişe, sipariş onayından irsaliye ve sevkiyata, faturadan cari hesap ve tahsilata kadar bütün operasyonunu tek merkezden yönetmesini sağlayacaktır. Aynı müşteri, ürün, stok, sipariş, irsaliye, fatura ve cari hareket bilgisi modüller arasında ortak kullanılacaktır.

Arayüz tasarımında temel hedef, kullanıcıya yalnızca yetkili olduğu işlemleri göstermek ve her işlemin bir sonraki adımını açıkça belirtmektir. Kullanıcı, kaydın hangi aşamada olduğunu, kimden onay beklediğini, hangi belgenin hangi belgeye bağlı olduğunu ve işlem sonucunda stok veya cari hesapta ne değişeceğini kolayca anlayabilmelidir.

## 2. Tasarım ilkeleri

| İlke | Arayüz karşılığı |
|---|---|
| Netlik | Her ekranda sayfa başlığı, durum, ana işlem ve sonraki adım görünür olur. |
| Hız | Liste ekranlarında arama, hızlı filtre, klavye kullanımı ve toplu işlem desteklenir. |
| Bilgi yoğunluğu | Masaüstünde tablo ağırlıklı, mobilde kart ve görev akışı ağırlıklı görünüm kullanılır. |
| Güvenli işlem | Finansal, stok ve onay işlemlerinde açık uyarı ve ikinci onay adımı bulunur. |
| İzlenebilirlik | Sipariş, irsaliye, sevkiyat, fatura ve ödeme arasındaki bağlantılar zaman çizelgesiyle gösterilir. |
| Rol odaklılık | Dashboard ve menü, kullanıcının rolüne ve izinlerine göre değişir. |
| Türkçe kullanım | Kullanıcıya gösterilen bütün metinler Türkçe; teknik kod isimleri İngilizce olacaktır. |
| Hata toleransı | Boş, yükleniyor, hata, yetki yok, sonuç yok ve başarılı durumlar ayrı tasarlanır. |

## 3. Ana kullanıcı rolleri

| Rol | Ana kullanım alanı | Birincil hedefi |
|---|---|---|
| Sistem yöneticisi | Ayarlar, kullanıcılar, roller, yetkiler, audit log | Sistemi ve erişimleri yönetmek |
| Yönetici / sorumlu | Dashboard, sipariş onayı, risk, raporlar | Şirket operasyonunu kontrol etmek |
| Satış | Ürünler, müşteriler, teklif, sipariş | Talebi satışa dönüştürmek |
| Depo | Stok, barkod, irsaliye, sevkiyat | Doğru ürünü doğru miktarda hazırlamak ve çıkarmak |
| Üretim | İş emirleri, makineler, üretim kayıtları | Üretimi, fireyi ve makine performansını kaydetmek |
| Muhasebe | Fatura, cari, ödeme, ekstre | Borç, alacak, tahsilat ve belge takibini yapmak |
| İnsan kaynakları | Personel, puantaj, izin, mesai, maaş | Personel devam ve özlük süreçlerini yönetmek |
| Görüntüleyici | Yetkili rapor ve listeler | Veri üzerinde değişiklik yapmadan bilgi görmek |
| Dış müşteri | Public ürün kataloğu ve teklif talebi | Ürün seçerek teklif istemek |

## 4. Yetki tasarımının arayüze yansıması

Yetki yalnızca menüyü gizlemekle sınırlı kalmayacaktır. Kullanıcının erişemediği sayfalarda işlem butonları gösterilmeyecek, doğrudan bağlantı ile erişimde de yetki ekranı açılacaktır. Örneğin muhasebe kullanıcısı siparişleri görebilir; ancak üretim kaydı değiştiremez. Depo kullanıcısı irsaliye hazırlayabilir; ancak faturayı iptal edemez.

Kritik işlemlerde arayüz aşağıdaki bilgileri zorunlu olarak gösterecektir:

| Kritik işlem | Gösterilecek kontrol bilgisi |
|---|---|
| Sipariş onayı | Müşteri, toplam tutar, stok durumu, teslim tarihi, ödeme şartı |
| İrsaliye oluşturma | Sevk miktarı, mevcut stok, rezerve stok, bağlı sipariş |
| Fatura oluşturma | İrsaliye, vergi, vade tarihi, cari etkisi |
| Ödeme kaydı | Müşteri, tutar, ödeme tipi, belge, yeni bakiye |
| Stok düzeltmesi | Ürün, depo, mevcut miktar, yeni miktar, gerekçe |
| İzin onayı | Personel, izin türü, tarih aralığı, iş gücü etkisi |

## 5. Ortak ekran iskeleti

### Web

Web uygulamasında sol tarafta daraltılabilir bir **ana menü**, üstte global arama, bildirimler, kullanıcı menüsü ve aktif şirket/depo bilgisi bulunacaktır. İçerik alanının başında breadcrumb ve sayfa başlığı, hemen altında sayfanın birincil işlemi yer alacaktır.

Liste ekranlarının standart yapısı; başlık alanı, KPI özetleri, arama ve gelişmiş filtreler, tablo, sayfalama ve dışa aktarma bölümlerinden oluşacaktır. Kayıt detayları için ayrı sayfa tercih edilecek; hızlı düzenlemelerde sağdan açılan drawer kullanılacaktır.

### Mobil

Mobil uygulama masaüstü menüsünü küçültülmüş biçimde kopyalamayacaktır. Ana ekran görev odaklı olacaktır: **Barkod Tara**, **Stok Kontrolü**, **Sevkiyat**, **Üretim Kaydı**, **Bildirimler**. Alt navigasyonda en fazla beş ana hedef bulunacak, diğer işlemler “Diğer” bölümünde gruplanacaktır.

## 6. Ana iş akışları

### Teklif talebinden kesin siparişe

Dış müşteri ürün kataloğunda ürünleri görür, ürünleri teklif sepetine ekler, miktar ve not girer, firma ve iletişim bilgilerini yazarak talebi gönderir. Şirket içindeki satış kullanıcısı talebi inceler ve teklife dönüştürür. Kabul edilen teklif sipariş taslağına dönüşür. Sipariş sorumluya onay için gönderilmeden kesin sipariş sayılmaz. Sorumlu onayından sonra stok kontrolü ve rezervasyon adımı görünür hale gelir.

### Siparişten sevkiyata ve faturaya

Onaylanmış sipariş detayında kullanıcı, stok durumunu ve rezerve miktarı görür. Depo kullanıcısı sevk edilecek miktarları seçerek irsaliye taslağı hazırlar. İrsaliye kesinleştirildiğinde stok çıkışıyla ilişkilendirilir. Sevkiyat ekranında araç, şoför, yükleme ve teslim bilgileri takip edilir. Muhasebe, irsaliyeden fatura oluşturur ve fatura cari hesaba borç olarak işlenir.

### Üretimden depoya

Üretim kullanıcısı iş emrini açar, ürün, hedef miktar, makine ve önceliği görür. Üretim başladığında başlangıç zamanı ve makine durumu kaydedilir. Üretim kaydında üretilen miktar, fire, duruş ve çalışan personeller girilir. İş emri tamamlandığında yalnızca gerçekleşen miktar üretim girişi olarak depoya aktarılır.

### Cari ve ödeme

Müşteri detayında özet bakiye, borç, alacak, vadesi geçen tutar ve risk seviyesi üst bölümde gösterilir. Alt bölümde cari ekstre zaman akışı bulunur. Ödeme girişi sırasında eski bakiye, işlem tutarı ve yeni bakiye aynı panelde gösterilir. Böylece kullanıcı yaptığı işlemin finansal etkisini kaydetmeden önce kontrol edebilir.

## 7. İlk tasarlanacak öncelikli ekranlar

| Öncelik | Ekran | Neden önce tasarlanacak |
|---|---|---|
| 1 | Rol bazlı dashboard | Tüm kullanıcıların giriş deneyimini belirler. |
| 2 | Ürün kataloğu | Hem şirket içi satış hem public teklif talebi için temel ekran. |
| 3 | Sipariş listesi ve sipariş detay akışı | Onay sürecinin merkezidir. |
| 4 | Depo stok ekranı | Üretim, rezervasyon ve sevkiyatın ortak veri alanıdır. |
| 5 | Barkod tarama ekranı | Mobil operasyonun ana hareket noktasıdır. |
| 6 | Üretim iş emri ve gerçekleşme ekranı | Üretim çıktısının depoya aktarımını yönetir. |
| 7 | İrsaliye–sevkiyat ekranı | Fiziksel sevkiyatın operasyon ekranıdır. |
| 8 | Fatura–cari detay ekranı | Muhasebe ve tahsilatın ana ekranıdır. |
| 9 | Raporlar ekranı | Yönetim kararları için ortak filtre ve grafik yapısını belirler. |
| 10 | Kullanıcı, rol ve yetki ekranları | Diğer ekranlardaki görünürlük kurallarını belirler. |

## 8. Tasarımda kullanılacak durum dili

Durumlar yalnızca renk ile anlatılmayacak; metin ve ikonla birlikte gösterilecektir. Yeşil, tamamlanan veya onaylanan işlemler; sarı, bekleyen veya dikkat gerektiren işlemler; kırmızı, gecikmiş, reddedilmiş veya kritik durumlar; gri ise taslak, pasif veya iptal edilmiş kayıtlar için kullanılacaktır. Renklerin tek başına anlam taşımaması erişilebilirlik açısından zorunludur.

## 9. İlk tasarım turunun çalışma biçimi

İlk turda önce web için masaüstü dashboard, sol menü ve sipariş detay ekranı tasarlanacaktır. Ardından ürün kataloğu ve public teklif sepeti tasarlanacaktır. İkinci turda depo, üretim ve irsaliye ekranları; üçüncü turda mobil barkod, stok ve üretim ekranları ele alınacaktır. Bu sıralama, uygulamanın en kritik uçtan uca akışını erken doğrulamayı sağlar.

## 10. Tasarım kararı gerektiren konular

| Konu | Tasarım başlamadan netleştirilecek karar |
|---|---|
| Marka | Şirket adı, logo, ana renk ve varsa kurumsal renkler |
| Depolar | İlk sürümde kaç depo ve depo içi raf/konum kullanımı |
| Onay | Tek sorumlu mu, tutara göre çok kademeli onay mı |
| Fatura | Fatura sistem içinde mi üretilecek, yoksa harici e-belge sistemine mi aktarılacak |
| Mobil kullanıcı | Tüm roller mi, yoksa öncelikle depo ve üretim personeli mi |
| Public katalog | Herkese açık mı, parola/özel bağlantı ile mi erişilecek |
| Ürün bilgileri | Gösterilecek fiyatların müşteri bazlı veya tek liste fiyatı olması |
| Raporlar | İlk sürümde yönetim için en kritik beş rapor |

## Tasarım çıktısı

Bu belge, görsel ekran üretimine geçmeden önceki kapsam ve UX temelidir. Bir sonraki aşamada bu kararlar üzerinden web route haritası, her ekranın alanları, tablo kolonları, buton durumları, modal/drawer kullanımları ve kullanıcı akış diyagramları hazırlanacaktır.
