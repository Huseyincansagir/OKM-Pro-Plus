# UI tasarım ilkeleri

Canonical görsel yön: `design/visual-design-system.md`.

## Bilgi hiyerarşisi

Kullanıcı tek bakışta şunları okuyabilmeli:

1. Nerede olduğu (modül, breadcrumb, sayfa başlığı)
2. Ne gördüğü (kayıt kümesi, belge, görev)
3. Ne yapabileceği (tek primary action + ikinciller)
4. İşlemin sonucu (etki özeti, sonraki adım)

Sayfa başlığı 28–32px, kart başlığı 15–18px, tablo 13–14px, yardımcı metin 12–13px. KPI rakamı metinden baskın; para, miktar ve gün biriminden ayrılmaz.

## ERP yoğunluğu

Bilgi yoğun olacak; pazarlama/sosyal UI olmayacak. Yoğunluk kaos değildir:

- İlk bakışta kritik kolonlar
- İkincil ayrıntı progressive disclosure (drawer, satır genişletme, sekme)
- Boş dekoratif kart, büyük kahraman alanı, gereksiz illüstrasyon yok

## Primary ve destructive

- Ekranda mümkünse tek teal primary (`Kaydet`, `Onayla`, `Oluştur`)
- Sil / iptal / reddet / sevkiyat kesinleştir / ödeme: kırmızı veya ayrı destructive + confirmation
- Confirmation etki özetini gizlemez (stok, cari, belge numarası)

## Status

`StatusBadge`: Türkçe etiket + semantic renk + ikon.

| Anlam | Yön |
|---|---|
| Bekleyen | Amber |
| Kritik / hata | Kırmızı |
| Başarılı | Yeşil |
| Aktif / birincil ilerleme | Teal |
| Pasif | Gri |

Renk tek başına anlam taşımaz.

## Tutarlılık

Aynı kavram aynı bileşen, spacing, tipografi ve etkileşimle gösterilir. Yeni spacing/radius/renk uydurma.

## Progressive disclosure

İlk anda teknik ID, raw JSON, internal error kodu gösterme. ERP kullanıcısının ihtiyaç duyduğu kalem, miktar, durum ve sonraki adım bir tıkta erişilebilir olsun.
