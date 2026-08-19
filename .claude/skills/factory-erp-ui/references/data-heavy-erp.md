# Data-heavy ERP

## Kolon hiyerarşisi

İlk bakışta: kimlik (belge no), özne (müşteri/ürün), miktar veya tutar, durum, sonraki işlem.
Teknik id, audit, iç not sonraya veya drawer’a.

## Tablo davranışı

- Sticky header
- Satır yüksekliği yoğun; marketing padding yok
- Sort kontrollü callback veya açıkça belgelenmiş local state — ikisini karıştırma
- Filtre aktifken “sonuç yok” ile “hiç kayıt yok” ayrılır
- Selection + bulk action: seçim sayısı görünür; bulk destructive ayrıca onay ister
- Satır aksiyonu birincil işlemi gömerse PageHeader primary’yi kaldır
- Expandable row ayrıntı içindir; ledger kolonunu gizleme
- Overflow: yatay scroll veya öncelikli kolon; 320px’te kart fallback

## Filtre ve toplu işlem

Filtre özeti görünür ve tek tıkla temizlenir. Toplu işlem seçimsiz disabled. İşlem sonrası seçim sıfırlanır.

## KPI ve dashboard

KPI: başlık + büyük değer + birim + kısa karşılaştırma veya uyarı.
Sahte trend uydurma. Veri yoksa empty/error; iskelet loading.

## Finans UI

Para birimi ve vergi satırları hizalı. Onay öncesi eski bakiye / yeni bakiye yan yana. İptal ve iade confirmation’sız olmaz.
