# Etkileşim ve state

## Form

- Alanlar gruplanır; her grubun amacı yazılıdır
- Required görsel ve programatik (`required` / `aria-required`)
- Inline hata alanın yanında; sunucu hatası form üstünde + ilgili alanda
- Dirty formdan çıkış uyarılır
- Submit sırasında kontroller kilitlenir
- Disabled nedeni tahmin ettirilmez; kısa açıklama ver

## Onay

Destructive ve geri alınamaz işlemler (sil, iptal, reddet, sevkiyat kesinleştir, öde):

- Dialog
- Etki özeti
- Gerekirse zorunlu açıklama (red)
- Explicit iptal + onay

## State matrisi

Her UI feature:

| State | Davranış |
|---|---|
| initial | Boş iskelet veya önceki cache yok |
| loading | Skeleton; kontroller pasif |
| success | Veri + sonraki adım |
| empty | Neden + ilk işlem |
| filter-empty | Filtreleri temizle |
| validation error | Alan bazlı, focus ilk hataya |
| server error | Türkçe açıklama + retry; stack yok |
| permission denied | Gerekçe + yetkili rol |
| conflict | Yenile / tekrar dene |
| offline | ConnectionStatus + yazma durur |
| submitting | Çift submit yok |
| success feedback | Toast + kayıt no |

Happy path tek başına PASS değildir.

## Operasyonel Sıradaki Adım (Next-Step Pattern)

Kullanıcının her an *"Bu belge/sevkiyat şu anda nerede ve benim sıradaki işim ne?"* sorusuna cevap verebilmesi için:
- Belge kartlarının üst kısmında açık, anlaşılır bir **Sıradaki Adım** bildirim çubuğu gösterilir.
- İlgili adım için gereken önkoşullar (örn. Rota kilitli mi? Yük planı kilitli mi? Yükleme doğrulandı mı?) karşılanmadığında aksiyon butonları yerine veya altında yol gösterici açıklama sunulur.

## Sevkiyat ve Yükleme İş Akışı (Shipping & Load Verification UX)

- **Doğrulama ve Yükleme:** Fiziksel paket barkodu taraması (`LoadVerificationScan`) ile kabul (`Accepted`), mükerrer (`Duplicate`), iptal (`CancelledPackage`) veya yük planı dışı (`Unexpected`) durumları görsel ve sesli/metinsel olarak açıkça bildirilir.
- **Sefer Hazırlama (Dispatch Preparation):** Sefer oluşturulmadan önce araç plakası, şoför adı, rota durak sayısı, kilitli yük planı, paket sayısı ve sevkiyat durumu teyit modalında özetlenir.
- **Yol ve Teslimat (InTransit & POD):** Varış (`Arrived`), teslim (`Delivered` + alıcı adı kanıtı), geçiş (`Departed`) ve istisna (`Skipped` + gerekçe) adımları deterministik sıra ile işletilir.

## Geri bildirim

Başarı yalnızca renk değişimi olamaz. Kullanıcı oluşan belgeyi veya sonraki adımı görmeli.
