# Mobil Operasyon Uygulaması
## Eksiksiz Ekran ve Akış Tasarımı

## 1. Mobil tasarım amacı

Mobil uygulama şirket içindeki depo, üretim ve sevkiyat kullanıcılarının sahadaki işlerini hızlı ve kontrollü biçimde yapması için tasarlanacaktır. Mobil uygulama web panelinin bütün fonksiyonlarını taşımayacak; barkod, stok sorgu, sayım, transfer, sevkiyat, üretim ve görev tamamlama üzerine odaklanacaktır.

Mobilde kesin stok veya finans işlemi ağ bağlantısı olmadan sessizce kaydedilmeyecektir. Okunabilir veriler önbellekten gösterilebilir; ancak işlem oluşturacak kullanıcıya bağlantı durumu ve kayıt sonucunun kesin olup olmadığı açıkça anlatılacaktır.

## 2. Navigasyon

Ana navigasyon beş bölümlüdür:

| Sekme | İçerik |
|---|---|
| Ana Sayfa | Kullanıcıya atanmış görevler, hızlı işlemler, kritik uyarılar |
| İşlerim | Sevkiyat, sayım, üretim ve onay görevleri |
| Tara | Kamera barkod tarama ve USB/klavye destekli hızlı giriş |
| Bildirimler | İşlem bekleyen ve kritik bildirimler |
| Daha Fazla | Stok, transfer, geçmiş, profil ve uygulama ayarları |

Tara düğmesi alt navigasyonda merkezde daha belirgin olabilir. Kullanıcının rolüne göre Ana Sayfa üzerindeki hızlı işlem kartları değişecektir.

## 3. Mobil giriş ve bağlantı durumu

Giriş ekranında şirket logosu, kullanıcı adı/e-posta, parola, giriş butonu ve parola sıfırlama bağlantısı bulunur. İlk girişte parola değiştirme ekranı açılır. Giriş sonrasında üst alanda kullanıcı adı ve bağlantı durumu gösterilir.

Bağlantı durumları `Bağlı`, `Bağlantı zayıf`, `Çevrimdışı` ve `Senkronizasyon bekliyor` şeklinde metin ve ikonla gösterilir. Stok/finans işlemleri çevrimdışında pasif görünür; kullanıcıya “Bu işlem için şirket ağı bağlantısı gerekir” açıklaması verilir.

## 4. Ana sayfa

Ana sayfanın üstünde “Günaydın, [Ad]” karşılama alanı, bağlantı göstergesi ve bildirim ikonu bulunur. Ardından kullanıcının bekleyen görev sayısı ve kritik uyarılar gelir. Hızlı işlemler büyük kartlar halinde gösterilir.

### Depo kullanıcısı

```text
Barkod Tara | Sevkiyat Hazırla
Stok Sorgula | Sayım Başlat
```

### Üretim kullanıcısı

```text
Aktif İş Emirleri | Üretim Kaydı
Makine Durumu | Duruş Bildir
```

### Yönetici

```text
Onay Bekleyenler | Kritik Bildirimler
Riskli Müşteriler | Rapor Özeti
```

## 5. Barkod akışı

```text
Tara sekmesi
→ Kamera izni
→ Tarama alanı
→ Barkod bulundu
→ Ürün sorgusu
→ Ürün ve stok sonucu
→ Yetkiye göre işlem seçimi
→ İşlem formu
→ Özet ve onay
→ Kayıt numarası
```

Tarama ekranında kamera görüntüsü, çerçeve, flaş, manuel kod girişi ve “Barkodu okutun” açıklaması bulunur. Barkod bulunamazsa kullanıcı manuel ürün aramasına geçebilir. Aynı barkodun art arda okutulması duplicate hareket oluşturmayacak şekilde kısa süreli kilitlenir.

Ürün sonuç ekranında ürün görseli, ürün adı, ürün kodu, barkod, toplam stok, seçili depo, rezerve miktar ve kullanılabilir miktar gösterilir. Hızlı işlemler role göre değişir:

| Rol | İşlemler |
|---|---|
| Depo | Stok gör, sayım yap, transfer, sevkiyatta doğrula |
| Üretim | İş emrine ekle, üretim girdisi, stok gör |
| Yönetici | Stok gör, hareket geçmişi, düzeltme incele |
| Görüntüleyici | Yalnızca stok ve ürün bilgisi |

## 6. Stok sorgu ve hareket geçmişi

Stok detayında depo bazlı kartlar, konum, mevcut, rezerve ve kullanılabilir miktar görünür. Hareket geçmişi son giriş/çıkışları tarih, tip, miktar, belge ve kullanıcı ile listeler.

Mobil tablolar yerine dikey hareket kartları kullanılacaktır. Kullanıcı hareket kartına dokunarak bağlı üretim, irsaliye, transfer veya sayım belgesine gider.

## 7. Sayım akışı

```text
Sayım görevi seç
→ Depo/konum seç
→ Barkodla ürün okut
→ Sistem miktarını gör
→ Sayılan miktarı gir
→ Farkı göster
→ Gerekçe seç
→ Onaya gönder veya yetkiliyse tamamla
```

Fark oluştuğunda sistem miktarı, sayılan miktar ve fark büyük puntolarla gösterilir. Gerekçe zorunlu olur. Kullanıcının düzeltme yetkisi yoksa “Onaya Gönder” görünür; yetkiliyse kesinleştirmeden önce stok etkisi özetlenir.

## 8. Transfer akışı

Transfer ekranında kaynak depo, hedef depo ve konum seçilir. Ürün barkodla eklenir, miktar girilir ve transfer özeti gösterilir. Kaynakta kullanılabilir stok yeterli değilse işlem tamamlanmaz. Transfer kayıt numarası, kaynak çıkışı ve hedef giriş durumu başarılı sonuç ekranında gösterilir.

## 9. Sevkiyat operasyonu

İşlerim ekranında sevke hazır ve hazırlanmakta olan sevkiyatlar kart halinde listelenir. Kartta sevkiyat no, müşteri, irsaliye, teslim tarihi, ürün adedi ve öncelik bulunur.

Sevkiyat detayı ürün doğrulama ekranına açılır. Her satırda beklenen, okutulan ve kalan miktar gösterilir. Barkod okutuldukça okutulan miktar artar. Fark varsa sistem uyarır ve sevkiyatı tamamlamadan önce açıklama ister.

```text
Sevkiyat seç
→ İrsaliye ürünlerini gör
→ Ürün barkodlarını doğrula
→ Miktarları tamamla
→ Araç/şoför bilgisini kontrol et
→ Yükleme tamamlandı
→ Sevkiyata hazır / sevk edildi
```

## 10. Üretim operasyonu

Aktif iş emirleri ürün, hedef, gerçekleşen, kalan, makine ve durum bilgisiyle listelenir. İş emri detayında ilerleme çubuğu, makine durumu, plan tarihi ve atanmış personeller bulunur.

### Üretimi başlatma

Kullanıcı makineyi doğrular ve “Üretimi Başlat” butonuna basar. Başlangıç zamanı otomatik alınır. Makinede başka aktif iş emri varsa kullanıcıya çakışma uyarısı gösterilir.

### Üretim kaydı

Büyük sayısal giriş alanları üretilen miktar ve fire için kullanılır. Duruş nedeni seçilebilir, açıklama girilebilir, personeller ve çalışma süreleri eklenebilir. Kullanıcı üretimi kaydeder veya hedef tamamlandıysa “Üretimi Tamamla” ile sonlandırır.

```text
İş emri seç
→ Başlat
→ Üretilen miktar
→ Fire
→ Duruş
→ Personel ve süre
→ Ön izleme
→ Kaydet / Tamamla
→ Depo giriş özeti
```

## 11. Bildirim ve görev ekranı

Bildirim listesinde görev bekleyen, bilgi ve kritik bildirimler ayrı sekmelerde gösterilir. Bildirim kartında olay, ilgili belge, zaman, öncelik ve “Aç” işlemi bulunur. Kullanıcı bildirimi okundu işaretleyebilir; işlem tamamlanmadıysa görev listesinde kalır.

## 12. Geçmiş ve profil

Daha Fazla bölümünde kullanıcının yaptığı son işlemler; barkod sorguları, sayımlar, transferler, üretim kayıtları ve sevkiyat doğrulamaları listelenir. Profil ekranında ad, rol, departman, parola değişikliği, bildirim tercihleri ve uygulamadan çıkış bulunur.

## 13. Mobil durumlar

| Durum | Mobil davranış |
|---|---|
| Kamera izni yok | Ayarlara gitme açıklaması |
| Barkod bulunamadı | Tekrar tara veya manuel ara |
| Ürün pasif | İşlem kapalı, neden açıklaması |
| Stok yetersiz | Kullanılabilir miktar ve eksik miktar |
| Ağ yok | Hareket kaydedilmedi açıklaması |
| İşlem bekliyor | Sunucu yanıtı bekleniyor, tekrar gönderim kilidi |
| Başarılı | Kayıt no, belge bağlantısı ve yeni işlem |
| Yetki yok | İşlemi yapabilecek role yönlendirme |
| Sunucu hatası | Veriler korunur, tekrar dene |

## 14. Mobil erişilebilirlik

Mobilde tüm kritik butonlar geniş dokunma alanına sahip olacaktır. Önemli durumlar renk yanında metin ve ikonla ifade edilir. Saha ışığında okunabilir kontrast, sayısal alanlarda sayısal klavye, uzun listelerde arama ve sabit ana CTA kullanılır.

## 15. Mobil kabul senaryoları

### Depo

```text
Giriş
→ Barkod tara
→ Ürünü bul
→ Stok miktarını gör
→ Sevkiyat görevi aç
→ Ürünü barkodla doğrula
→ Miktarı tamamla
→ Yüklemeyi bitir
```

### Üretim

```text
Giriş
→ Aktif iş emrini aç
→ Makineyi doğrula
→ Üretimi başlat
→ Miktar/fire/duruş gir
→ Personel ekle
→ Üretimi tamamla
→ Depo giriş özetini gör
```

### Sayım

```text
Sayım görevini aç
→ Barkodla ürünleri okut
→ Sistem ve sayım miktarını karşılaştır
→ Fark gerekçesi gir
→ Onaya gönder
```
