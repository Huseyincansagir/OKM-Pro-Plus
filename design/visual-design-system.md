# Görsel Tasarım Sistemi ve İlk Ekran Kararları

## 1. Görsel yön

İlk görsel yön, kurumsal ERP kullanımına uygun **açık zemin + derin lacivert navigasyon + teal ana aksiyon rengi** yaklaşımıdır. Arayüz dekoratif değil, operasyonel ve bilgi yoğun olacaktır. Kartlar hafif gölgeli veya ince çerçeveli, köşeler orta derecede yuvarlatılmış, grafikler sade ve metin hiyerarşisi güçlü kullanılacaktır.

Bu yön, üretim ve depo ortamındaki yoğun bilgiye rağmen kullanıcıya sakin bir çalışma yüzeyi sunar. Kritik uyarılar amber ve kırmızıyla ayrılır; olumlu durumlar yeşil veya teal ile belirtilir. Renk tek başına anlam taşımayacak, her durumda metin etiketi kullanılacaktır.

## 2. Renk sistemi

| Kullanım | Renk yönü | Arayüz kullanımı |
|---|---|---|
| Ana navigasyon | Derin lacivert | Sol menü, mobil üst alan, kritik başlık alanları |
| Birincil aksiyon | Teal | Kaydet, onayla, oluştur, seçili menü |
| İkincil aksiyon | Açık mavi / soğuk gri | Bilgi, filtre ve yardımcı işlemler |
| Bekleyen / dikkat | Amber | Onay bekliyor, düşük stok, vadesi yaklaşan |
| Hata / kritik | Kırmızı | Reddedildi, gecikmiş, yetersiz stok, iptal |
| Başarılı | Yeşil | Tamamlandı, ödendi, stokta |
| Pasif | Gri | Pasif ürün, iptal edilmiş, kullanılmayan seçenek |
| Zemin | Açık gri-beyaz | Uygulama içerik alanı |

## 3. Tipografi ve bilgi hiyerarşisi

Başlıklar güçlü ve kısa tutulacaktır. Sayfa başlığı 28–32 px aralığında, kart başlıkları 15–18 px, tablo metinleri 13–14 px ve yardımcı metinler 12–13 px seviyesinde ele alınacaktır. Sayısal KPI'larda rakamlar metinden daha baskın olacak; para, miktar ve gün bilgileri birimlerinden ayrılmayacaktır.

## 4. Bileşen kararları

| Bileşen | Tasarım kararı |
|---|---|
| Sidebar | Daraltılabilir, ikon + Türkçe metin; aktif öğe teal arka planla belirgin |
| Topbar | Tarih / depo seçimi, global arama, bildirim ve kullanıcı menüsü |
| KPI kartı | Başlık, büyük değer, dönem karşılaştırması ve küçük trend göstergesi |
| Durum rozeti | Renk + Türkçe metin; örnek “Onay Bekliyor” |
| Data table | Yoğun bilgi, sabit başlık, satır aksiyonu, sayfalama |
| Drawer | Hızlı düzenleme, ürün önizleme, kısa detay |
| Modal | Onay, reddetme, kritik stok veya finansal işlem teyidi |
| Timeline | Belge hareketleri, onay geçmişi, audit özeti |
| Stepper | Siparişten faturaya veya sevkiyat durumuna ilerleme |
| Quantity view toggle | `Temel Birim / Ambalaj / Kırılım`; görünümü değiştirir, ledger değerini değiştirmez |
| Packaging filter | `Tümü / Palet / Koli / Paket / Temel Birim`; liste ve rapor filtreleri |
| Capacity summary | kg, m³, palet adedi ve doluluk; kullanılan/maksimum karşılaştırması |
| Load unit card | Palet/koli/yük birimi, barkod, içeriği, alıcı durakları ve istifleme durumu |
| Route stop list | Durak sırası, müşteri/adres, zaman, paket sayısı ve teslim durumu |
| Package trace drawer | Barkod, ürün, ambalaj, temel miktar, alıcı, adres, durak, durum ve kanıt |
| Delivery proof panel | Teslim alan, imza/fotoğraf/not, kısmi teslim ve istisna nedeni |
| Status badge | Araç, sevkiyat, durak ve paket durumlarında renk + Türkçe metin + ikon |
| Toast | Başarılı, uyarı ve hata durumları; doğrudan sonraki adıma bağlantı |
| Empty state | Açıklama + ilk yapılacak işlem butonu |

## 5. İlk mockup seti

İlk görsel referans seti dört temel ekranı doğrulamaktadır:

| Ekran | Tasarım amacı | Dosya |
|---|---|---|
| Yönetici dashboard'u | Rol bazlı özet, KPI, grafik ve risk listesi | `../docs/05-assets/mockups/uretim-depo-erp-dashboard-reference.png` |
| Sipariş detay ve onay | Durum akışı, ürünler, stok özeti ve sorumlu onayı | `../docs/05-assets/mockups/uretim-depo-order-detail-mockup.png` |
| Ürün kataloğu | Görsel ürün kartları, filtreler ve hızlı ürün detayı | `../docs/05-assets/mockups/uretim-depo-product-catalog-mockup.png` |
| Mobil barkod | Kamera taraması, ürün sonucu ve operasyon butonları | `../docs/05-assets/mockups/uretim-depo-mobile-barcode-mockup.png` |
| Kargo planlama | Araç kapasitesi, palet/yük birimleri, ağırlık/hacim ve uygunluk | Yeni mockup turu |
| Rota ve durak panosu | Araç durumu, durak sırası, müşteri/adres ve teslimat ilerlemesi | Yeni mockup turu |
| Paket izleme | Barkod, alıcı, adres, durak ve teslim durumu | Yeni mockup turu |
| Araç detayı | Araç tipi, kapasite, aktif rota, yük ve bakım durumu | Yeni mockup turu |
| Mobil durak teslimatı | Aktif durak, barkod doğrulama, teslim kanıtı ve istisna | Yeni mockup turu |
| Mobil barkod ve miktar | Barkod sonucu, üçlü toggle, işlem seviyesi ve temel miktar önizlemesi | `../docs/05-assets/mockups/mobile-barcode-quantity-toggle-flow.png` |

## 6. Görsel kabul kriterleri

İlk ekran tasarımları şu kriterleri karşılamalıdır:

| Kriter | Kabul koşulu |
|---|---|
| Dil | Kullanıcıya görünen bütün metinler Türkçe |
| Hızlı anlama | Kullanıcı sayfaya girdiğinde ana amacı ve sonraki adımı anlayabilmeli |
| Durum görünürlüğü | Belge durumu ve bekleyen onay açıkça görünmeli |
| Bilgi yoğunluğu | Masaüstünde kritik alanlar tek ekranda taranabilmeli |
| Mobil operasyon | Barkod ve üretim işlemi az adımla tamamlanabilmeli |
| Yetki güvenliği | Kullanıcı yetkisiz finans veya stok işlemi başlatamamalı |
| Tutarlılık | Aynı buton, rozet, tablo ve kart davranışı tüm modüllerde korunmalı |
| Okunabilirlik | Renk, kontrast ve metin boyutu saha kullanımına uygun olmalı |

## 7. Bir sonraki görsel tasarım turu

Bir sonraki turda public teklif kataloğu, teklif sepeti, depo stok detay ekranı, üretim iş emri, irsaliye–sevkiyat ekranı ve cari ekstre ekranına ek olarak yeni lojistik ekranları hazırlanacaktır: kargo planlama çalışma alanı, rota/durak panosu, paket izleme, araç tipi/araç detayı ve mobil durak teslimatı. Bu ekranlar aynı görsel sistemle hazırlanacak; miktar toggle'ı, kapasite özeti, durum rozetleri ve barkod doğrulama bileşenleri ortak kullanılacaktır. Bundan sonra tüm öncelikli ekranlar tek bir tasarım el kitabında birleştirilecek ve kodlama öncesi route haritasıyla eşleştirilecektir.
