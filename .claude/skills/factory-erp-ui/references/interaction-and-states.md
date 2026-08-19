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

## Geri bildirim

Başarı yalnızca renk değişimi olamaz. Kullanıcı oluşan belgeyi veya sonraki adımı görmeli.
