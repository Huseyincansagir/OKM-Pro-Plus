# Üretim, Sevkiyat, Muhasebe, Cari, Rapor ve Personel Arayüz Tasarımı

## 1. Üretim modülü

Üretim arayüzünün ana amacı, planlanan iş emrini makine ve personel ile ilişkilendirerek gerçek üretim, fire, duruş ve çalışma süresini güvenilir biçimde kaydetmektir. Üretim ekranları “hedef”, “gerçekleşen”, “kalan” ve “durum” kavramlarını her zaman birlikte göstermelidir.

### Üretim dashboard'u

Üst KPI alanında aktif iş emirleri, bugün üretilen miktar, toplam fire, makine duruşu ve ortalama verimlilik yer alır. Orta alanda makine durum kartları bulunur: çalışıyor, bekliyor, duruşta, bakımda veya pasif. Alt bölümde önceliğe göre iş emri listesi ve makine/personel performans grafikleri yer alır.

### İş emri listesi

Tablo kolonları iş emri no, ürün, hedef miktar, gerçekleşen miktar, kalan, planlanan tarih, makine, öncelik, sorumlu ve durumdur. Kullanıcı liste ve kanban görünümü arasında geçiş yapabilir. Kanban kartında ürün görseli, ilerleme çubuğu, makine, plan tarihi ve aciliyet rozeti görünür.

### İş emri detayı

Üst bölümde iş emri numarası, ürün, durum, hedef miktar ve üretim ilerlemesi gösterilir. İlerleme `Gerçekleşen / Hedef` biçiminde verilir. Alt sekmeler şunlardır:

| Sekme | İçerik |
|---|---|
| Genel | Ürün, hedef, plan tarihi, öncelik, açıklama, sorumlu |
| Üretim Kayıtları | Başlangıç, bitiş, makine, miktar, fire, duruş |
| Personeller | Çalışan, rol, vardiya, süre |
| Makine | Makine durumu, duruş geçmişi, verimlilik |
| Malzeme | Kullanılan malzeme ve üretim bağlantıları |
| Depo Hareketi | Tamamlama sonrası üretim girişi |
| Aktivite | Durum ve kayıt değişiklikleri |

Ana işlem sırası durumla değişir. Planned durumunda “Serbest Bırak”, Released durumunda “Üretimi Başlat”, InProgress durumunda “Üretim Kaydı Ekle” ve “Duraklat”, Paused durumunda “Devam Et”, tamamlanmaya uygun durumda “Tamamla” görünür.

### Üretim gerçekleşmesi formu

Form; iş emri, makine, başlangıç, bitiş, üretilen miktar, fire, duruş nedeni/süresi ve açıklama alanlarından oluşur. Personel seçimi ayrı bir tablo ile yapılır. Her personel satırında rol, vardiya ve çalışma süresi bulunur. Üretilen miktar ve fire sıfırdan büyük veya iş kuralına uygun olacak şekilde doğrulanır.

Üretim tamamlanırken kullanıcıya “Depoya girecek miktar”, “Fire miktarı” ve “İş emri ilerlemesi” özeti gösterilir. Tamamlama sonrasında üretim girişi stok hareketi olarak görünür.

## 2. Makine takip ekranları

Makine listesi kod, ad, departman, model, seri no, aktif/pasif ve anlık durum kolonlarını gösterir. Makine detayında toplam üretim, toplam fire, çalışma süresi, duruş süresi, verimlilik ve son iş emirleri KPI olarak yer alır.

Makine duruşu başlatılırken neden, başlangıç zamanı, tahmini bitiş ve açıklama istenir. Duruş tamamlandığında gerçek süre hesaplanır ve üretim raporlarına yansır. Makineyi pasife alma işleminde aktif iş emri varsa kullanıcıya açık uyarı gösterilir.

## 3. İrsaliye ve sevkiyat modülü

### İrsaliye listesi

İrsaliye listesinde irsaliye no, sipariş no, müşteri, tarih, kalem sayısı, sevk durumu, fatura durumu ve gün sayısı gösterilir. Faturalaşmamış irsaliyeler amber rozetle ayrılır; uzun süre bekleyenler dashboard uyarı listesine taşınır.

### İrsaliye oluşturma

Kullanıcı kesinleşmiş siparişten irsaliye başlattığında sistem sipariş bilgilerini ve stok rezervasyonunu getirir. Her satırda sipariş miktarı, önceki sevk, rezerve miktar, kullanılabilir miktar ve sevk edilecek miktar bulunur. Kullanıcı kısmi sevk yapabilir; ancak stoktan fazla miktar giremez.

Kesinleştirme öncesi özet panelinde müşteri, teslimat adresi, ürün adedi, sevk toplamı ve stok çıkışının etkisi gösterilir. Kesinleştirmede stok çıkışı ve irsaliye durumu birlikte değişir.

### Sevkiyat listesi ve detay

Sevkiyat listesi sevkiyat no, irsaliye, müşteri, araç, şoför, yükleme tarihi, çıkış tarihi, teslim durumu ve sorumlu kolonlarından oluşur. Detay ekranında yatay durum adımları ve teslimat zaman çizelgesi yer alır.

| Durum | Kullanıcıya görünen sonraki işlem |
|---|---|
| Hazırlanıyor | Ürünleri doğrula, araç/şoför ata |
| Sevk Edilecek | Yüklemeyi tamamla |
| Sevk Edildi | Teslim bilgisini bekle |
| Teslim Edildi | Belgeyi ekle, kapat |
| İptal | Salt okunur geçmiş ve iptal gerekçesi |

Mobil barkod doğrulama ile web sevkiyat hazırlama ekranı aynı sevk miktarlarını kullanır. Beklenen miktardan farklı barkod okutulursa sevkiyat tamamlanmadan fark açıklaması istenir.

## 4. Fatura modülü

Fatura listesinde fatura no, müşteri, tarih, bağlı irsaliye, toplam, vade tarihi, ödeme durumu ve kalan bakiye bulunur. Filtreler müşteri, dönem, ödeme durumu, vade, irsaliye durumu ve tutar aralığıdır.

Fatura detayının üst kısmında fatura numarası, müşteri, durum ve genel toplam; orta bölümde kalemler ve vergi özeti; sağ bölümde cari etkisi, vade ve ödeme özeti bulunur. Alt sekmeler bağlı irsaliye, sipariş, cari hareket, ödemeler, dosyalar ve aktivitedir.

Fatura oluşturma ekranı irsaliye veya siparişten açılır. Kullanıcı kalemleri, iskonto, vergi ve vade tarihini kontrol eder. Fatura oluşturulmadan önce “Bu işlem müşterinin cari hesabına borç oluşturacaktır” uyarısı gösterilir. Faturalanmış irsaliye tekrar faturalanamaz; kullanıcıya bağlı fatura numarası gösterilir.

## 5. Cari hesap ve ödeme modülü

### Cari hesap dashboard'u

Üstte toplam borç, toplam alacak, net bakiye, vadesi geçen tutar ve bu ay tahsilat KPI'ları yer alır. Altında müşteri risk dağılımı, vade takvimi, tahsilat trendi ve en yüksek gecikmeler gösterilir.

### Cari hesap detayı

Cari detay ekranı müşterinin satış ve finans geçmişini tek sayfada birleştirir. Üst özet kartlarında açılış bakiyesi, borç, alacak, bakiye, vade ve gecikme görünür. Sekmeler; Cari Ekstre, Faturalar, Ödemeler, Siparişler, Risk, Notlar ve Belgeler şeklindedir.

Cari ekstre tablosunda tarih, işlem, belge no, borç, alacak ve bakiye bulunur. Bakiye satırdan satıra izlenebilir olmalı; finansal hareketler silinmek yerine ters kayıt veya iptal statüsüyle korunmalıdır.

### Ödeme oluşturma

Ödeme formunda tarih, müşteri, tutar, ödeme tipi, açıklama ve belge ekleme alanları bulunur. Ödeme tipi; nakit, havale, EFT, çek, senet, kredi kartı ve diğer seçeneklerini içerir. Kaydetme öncesi eski bakiye, ödeme tutarı ve yeni bakiye yan yana gösterilir.

Ödeme kaydı tamamlandığında cari hareket, bakiye ve audit kaydı aynı işlem sonucunda gösterilir. Duplicate ödeme riski için kullanıcıya işlem numarası ve son kaydedilen ödeme özeti gösterilir.

### Risk analizi

Risk ekranında müşteriler risk seviyesine, gecikme gününe, geciken toplam borca ve son ödeme davranışına göre filtrelenir. Müşteri detayında risk skorunu oluşturan açıklanabilir etkenler ayrı satırlarda gösterilir: toplam borç, geciken borç, en uzun gecikme, ortalama ödeme süresi, vadesi geçmiş fatura, son 12 ay satış ve ödeme düzensizliği.

## 6. Rapor modülü

Rapor ekranı sol tarafta rapor grupları, üstte ortak filtre çubuğu, ortada grafik ve altta veri tablosundan oluşur. Kullanıcı grafik veya tabloyu ayrı ayrı değil, aynı filtrelenmiş veri setini görür.

### Grafik düzeni

| Rapor | Grafik önerisi |
|---|---|
| Satış trendi | Çizgi grafik ve günlük/haftalık toplam |
| Üretim trendi | Sütun grafik, hedef-gerçekleşen karşılaştırması |
| Ürün dağılımı | Yatay çubuk grafik |
| Müşteri satış dağılımı | Sıralı yatay çubuklar |
| Ödeme performansı | Vade ve tahsilat sütunları |
| Stok durumu | Kritik/normal/pasif durum dağılımı |
| Makine performansı | Üretim, fire, duruş ve verimlilik karşılaştırması |

Rapor başlığında aktif tarih aralığı ve filtre özeti görünür. Dışa aktarma işlemi PDF, Excel veya CSV olarak yapılır. Dışa aktarılan dosyada rapor tarihi, filtreler ve oluşturan kullanıcı bilgisi yer alır.

## 7. Personel ve İK modülü

### Personel listesi ve detay

Personel listesi sicil no, ad soyad, departman, pozisyon, işe giriş, durum ve son puantaj bilgilerini gösterir. Personel detayında kimlik, iletişim, görev, maaş özeti, puantaj, izin, mesai, devamsızlık ve üretim katılımı sekmeleri bulunur.

### Puantaj

Puantaj ekranında personel satır, tarih sütunları veya günlük tablo görünümü bulunur. Her gün giriş, çıkış, çalışılan saat, fazla mesai, eksik mesai ve devamsızlık durumu gösterilir. Aylık özet kartları normal çalışma, fazla mesai, izin ve devamsızlık toplamlarını içerir.

### İzin akışı

Personel izin talebinde izin türü, başlangıç, bitiş, gün ve açıklama girer. Talep yöneticinin onay kuyruğuna düşer. Onay panelinde personelin mevcut izin bakiyesi ve tarih aralığındaki iş gücü etkisi gösterilir. Reddetme işleminde açıklama zorunludur.

### Mesai ve maaş

Mesai ekranında personel, tarih, süre, neden, onay durumu ve ödeme dönemine etkisi bulunur. Maaş ekranı gerçek bordro mevzuatı varsaymadan brüt/net, fazla mesai, kesintiler, ikramiye, avans ve net ödeme alanlarını gösterir. İleride harici bordro sistemine aktarım yapılabilmesi için dışa aktarma ve entegrasyon durumu alanları tasarlanır.

## 8. Ortak kritik onay davranışları

Üretim tamamlanması, irsaliye kesinleştirme, fatura oluşturma, ödeme kaydı, stok düzeltme, izin onayı ve maaş dönemini kapatma işlemleri kullanıcıya işlem öncesi etkisini gösteren onay paneliyle sunulur. Panelde kim, hangi belge, hangi miktar/tutar, hangi durum ve sonraki etki açıkça yazılır.

Bu işlemlerde kullanıcı butona birden fazla kez basamaz. Başarılı işlem sonrası kayıt numarası, oluşan belge bağlantısı ve audit geçmişine geçiş görünür.
