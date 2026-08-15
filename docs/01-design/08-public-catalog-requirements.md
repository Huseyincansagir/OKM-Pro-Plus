# Public Ürün Kataloğu ve Teklif Sepeti
## Detaylı Tasarım Gereksinimleri

## 1. Amaç

Public katalog, şirket dışındaki müşterinin hesap açmadan ürünleri incelemesini, ilgilendiği ürünleri teklif sepetine eklemesini ve şirketten fiyat teklifi istemesini sağlayacaktır. Bu alan şirket içi ERP ekranlarından görsel ve işlevsel olarak ayrılacak; ziyaretçi kendisini bir yönetim panelinde değil, güvenilir bir kurumsal ürün kataloğunda hissetmelidir.

Akışın temel amacı doğrudan satış yapmak değil, **ürün seçimini ve nitelikli teklif talebini kolaylaştırmaktır**. Bu nedenle fiyat gösterimi varsayılan olarak zorunlu tutulmayacak; ürün kartında “Teklif isteyin” veya “Fiyat bilgisi için teklif talebi oluşturun” mesajı kullanılacaktır. Eğer şirket isterse ileride müşteri grubuna göre fiyat gösterimi eklenebilecek şekilde tasarımda alan bırakılacaktır.

## 2. Ziyaretçi profili

| Ziyaretçi ihtiyacı | Arayüz karşılığı |
|---|---|
| Ürünü hızlı bulmak | Arama, kategori, ölçü, paket içeriği ve ürün tipi filtreleri |
| Ürünü karşılaştırmak | Kart üzerinde temel özellikler ve karşılaştırmaya ekleme seçeneği |
| Ürünü anlamak | Fotoğraf galerisi, açıklama, ölçü, paket/koli bilgisi |
| Teklif istemek | Görünür “Teklife ekle” butonu ve sepet rozeti |
| Miktar belirtmek | Sepette ürün bazlı miktar alanı ve birim gösterimi |
| Özel ihtiyacı aktarmak | Ürün notu ve genel talep notu |
| Güvenli iletişim kurmak | Firma, yetkili, telefon ve e-posta alanları; açık gizlilik metni |

## 3. Public alanın ana sayfa yapısı

```text
Üst bar:
  Logo / şirket adı
  Ürünler
  Hakkımızda veya Kurumsal
  İletişim
  Teklif Sepeti (rozet)

Hero alanı:
  "İhtiyacınıza uygun ürünler için teklif alın"
  Kısa açıklama
  Ürünleri İncele butonu

Kategori alanı:
  Peçeteler | Dispenser Ürünleri | Özel Üretim | Tüm Ürünler

Öne çıkan ürünler:
  Görsel kartlar + Teklife Ekle

Güven alanı:
  Üretim kapasitesi / kalite / hızlı teklif / kurumsal iletişim

Alt bölüm:
  İletişim bilgileri, adres, e-posta, yasal metinler
```

## 4. Varsayılan tasarım kararları

| Konu | Varsayılan karar |
|---|---|
| Fiyat gösterimi | Fiyatlar public katalogda gösterilmez; “Teklif isteyin” mesajı kullanılır. |
| Üyelik | Hesapsız teklif talebi desteklenir. |
| Sepet | Kullanıcı sayfadan ayrılmadan ürün ekleyebilir; sepet sağdan açılır veya ayrı sayfaya geçer. |
| Miktar | Ürün birimine göre adet, paket, koli veya özel miktar kabul edilir. |
| Zorunlu bilgiler | Firma, yetkili, telefon ve e-posta. |
| Ürün notu | Her ürün için isteğe bağlı not; sepet genelinde ayrıca genel not. |
| Talep sonrası | Talep numarası, başarı mesajı ve şirketin dönüş süresi bilgisi gösterilir. |
| Görsel ton | Temiz, güvenilir, ürün fotoğraflarını öne çıkaran kurumsal katalog. |
| Mobil davranış | Tek sütun kartlar, sabit sepet özeti ve alt tarafta ana CTA. |
| Spam önleme | Kontrollü public form, rate limit ve gerektiğinde CAPTCHA/manuel inceleme altyapısı. |

## 5. Ürün kartı minimum içeriği

Ürün kartında ürünün fotoğrafı, ürün adı, ürün kodu, kısa açıklaması, ölçüsü veya temel özelliği ve “Teklife ekle” işlemi bulunacaktır. Stok miktarı ve iç fiyat bilgisi public kullanıcıya gösterilmeyecektir. Ürün aktif değilse public katalogda görünmeyecek; ancak eski teklif taleplerinde ürünün kayıtlı adı korunacaktır.

## 6. Ürün detay sayfası minimum içeriği

Ürün detayında büyük ürün görseli, varsa alternatif görseller, ürün adı, ürün kodu, ürün açıklaması, ürün özellikleri, birim, paket içeriği, koli içeriği ve teklif sepetine ekleme alanı bulunacaktır. Kullanıcı isterse detay sayfasında miktarı seçerek doğrudan sepete ekleyecektir.

## 7. Teklif sepeti minimum içeriği

Sepet, teklif talebinin çalışma alanıdır; alışveriş sepeti gibi ödeme veya kesin satış anlamına gelmez. Sepet başlığında “Teklif Sepetiniz” ifadesi bulunacak ve “Bu işlem sipariş oluşturmaz. Şirketimiz talebinizi inceleyerek sizinle iletişime geçer.” açıklaması gösterilecektir.

Her satırda ürün görseli, ürün adı, ürün kodu, miktar, birim, ürün notu, miktar azaltma/artırma ve kaldırma işlemleri bulunacaktır. Sepet altında genel talep notu ve “Bilgilerimi Gir ve Teklif İste” butonu yer alacaktır.

## 8. Müşteri bilgi formu

Form tek uzun sayfa yerine iki adımlı ve kısa tutulacaktır:

| Adım | Alanlar |
|---|---|
| 1. Firma bilgileri | Firma adı, yetkili adı soyadı, telefon, e-posta |
| 2. Talep özeti | Seçilen ürünler, miktarlar, not, iletişim izni/gizlilik onayı |

Form alanlarında örnek metinler gerçek veri formatını tarif edecek; hata mesajları alanın hemen altında Türkçe olarak gösterilecektir. Telefon ve e-posta alanları gönderim öncesi doğrulanacaktır.

## 9. Başarılı gönderim ekranı

Gönderim sonrasında kullanıcıya büyük bir başarı durumu, teklif talep numarası ve talebin alındığı zamanı gösterilecektir. Kullanıcıya “Talebiniz alınmıştır. Satış ekibimiz ürün ve miktar bilgilerinizi inceleyerek sizinle iletişime geçecektir.” mesajı gösterilecektir. İsterse ürün kataloğuna dönme ve talep özetini yazdırma/PDF olarak alma seçenekleri bulunabilir.

## 10. Tasarlanacak durumlar

| Durum | Ekran davranışı |
|---|---|
| Boş sepet | Ürün eklemeye yönlendiren açıklama ve “Kataloğa Git” butonu |
| Ürün eklendi | Sepet rozeti artar, kısa başarı bildirimi gösterilir |
| Aynı ürün tekrar eklendi | Yeni satır açmak yerine miktar artırılır ve bilgi mesajı verilir |
| Zorunlu alan eksik | Alan bazlı hata, form kaybolmadan gösterilir |
| Geçersiz e-posta | E-posta alanında açık format uyarısı |
| Katalogda ürün yok | Arama sonucu yok mesajı ve filtre temizleme seçeneği |
| Ürün pasifleşmiş | Sepette korunmuşsa “Bu ürün artık teklif için aktif değil” uyarısı |
| Gönderim hatası | Veriler korunur, tekrar dene seçeneği gösterilir |
| Başarılı gönderim | Talep numarası ve sonraki adımlar gösterilir |

## 11. Tasarımın başarı ölçütü

Bir ziyaretçi, ana sayfadan başlayarak ürün arama, ürün detayına bakma, en az iki ürün ekleme, miktar ve ürün notu girme, firma iletişim bilgilerini yazma ve teklif talebini gönderme akışını masaüstünde açık yönlendirme ile; mobilde ise minimum kaydırma ve sabit ana buton ile tamamlayabilmelidir.
