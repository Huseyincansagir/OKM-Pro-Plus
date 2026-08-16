---
name: factory-erp-design-workflow
description: Kodlamadan önce üretim, depo, satış, sevkiyat, cari ve personel süreçlerini; web ERP, public katalog ve mobil operasyon UX'ini, ekran envanterini, kullanıcı rollerini ve proje kararlarını tasarlamak için kullan. ERP-lite, fabrika operasyon sistemi, barkodlu depo, teklif kataloğu veya kodlama öncesi UX/mimari tasarım istendiğinde tetikle.
---

# Factory ERP Design Workflow

## Amaç

Bu skill, ürünün kodunu yazmadan önce iş akışını, kullanıcı deneyimini, bilgi mimarisini ve teknik ön tasarımı tek bir tutarlı tasarım sistemine dönüştürür.

## Çalışma ilkeleri

- Kullanıcı arayüzü Türkçe; entity, property, API ve code isimleri İngilizce.
- Tasarımı yalnızca görsel mockup olarak bırakma. Her ekran için amaç, rol, veri, aksiyon, durum, yetki ve veri etkisini tanımla.
- Modülleri bağımsız ekranlar olarak değil ortak belge ve veri akışları olarak tasarla.
- Public müşteri deneyimini iç ERP'den ayır.
- Mobil uygulamayı masaüstünün küçültülmüş kopyası yapma; görev odaklı tasarla.
- Finansal ve stok hareketlerinde silme yerine iptal/ters kayıt yaklaşımını koru.
- Gereksiz kullanıcı onayı istemeden makul varsayımlar yap.
- Belirsiz veya iş riski oluşturan kararları `/design/decision-log.md` içinde tut.
- Agent, Grok, ChatGPT veya başka bir yardımcı tarafından üretilen öneriyi proje sahibi onayı olmadan `DECIDED` yapma.
- Açık kararlar için `/design/open-decisions-solution-matrix.md` içinde seçenek, varsayılan MVP, sahip, risk ve etki zinciri yaz.

## Uygulanacak sıra

1. Kapsam ve domain modüllerini çıkar.
2. Kullanıcı rollerini ve izin sınırlarını tanımla.
3. Uçtan uca iş akışlarını çıkar.
4. Master screen inventory oluştur.
5. Web bilgi mimarisini oluştur.
6. Mobil operasyon akışlarını oluştur.
7. Public katalog ve teklif akışını oluştur.
8. Veri/teknik ön taslağı oluştur.
9. Visual design system oluştur.
10. Proje yönetimi çıktısını hazırla.
11. Design consistency review yap.
12. Design Gate çalıştır.

## Temel iş akışları

### Satış

`Quote Request → Quote → Sales Order → Approval → Stock Reservation → Delivery Note → Shipment → Invoice → Current Account → Payment`

### Üretim

`Production Plan → Production Order → Machine → Personnel → Production Record → Scrap/Downtime → Stock Receipt`

### Personel

`Employee → Attendance → Leave/Overtime → Production Assignment → Payroll Record`

## Ekran şablonu

Her ekran için:

- Ekran adı
- Amaç
- Kullanıcı rolleri
- Route
- KPI/summary alanları
- Ana tablo ve kolonlar
- Birincil/ikincil aksiyonlar
- Durumlar
- Yetki davranışı
- Bağlı belgeler
- Veri etkisi
- Empty/loading/error/permission/offline durumları
- Mobil uyarlama
- Kabul senaryosu

## Source of Truth

Her domain kavramı için tek kaynak belirle.

Örnek:

- `Product`: ürün ana kaydı
- `ProductBarcode`: barkod kaydı
- `Stock`: mevcut stok durumu
- `StockMovement`: stok tarihçesi
- `Customer`: müşteri ana kaydı
- `CurrentTransaction`: cari hareket kaydı
- `Payment`: ödeme kaydı
- `SalesOrder`: sipariş kaydı
- `DeliveryNote`: sevk belgesi
- `Invoice`: fatura kaydı

Aynı veriyi bağımsız kopyalayan modeller üretme.

## Domain invariants

En az aşağıdakileri tasarımda görünür kıl:

- Onaylanmamış sipariş sevke dönüşemez.
- Aynı irsaliye kalemi için faturalandırılan toplam miktar, sevk edilen ve faturalanmamış kalan miktarı aşamaz; aynı miktar allocation'ı ikinci kez yapılamaz.
- Stok hareketlerinin geçmişi izlenebilir olmalıdır.
- Finansal hareketler fiziksel olarak silinmemelidir.
- Üretim tamamlanması, stoğa giriş davranışı tanımlanmadan geçerli sayılmaz.
- Kritik state transition'lar audit log üretmelidir.

## Design Gate

Kodlama başlamadan önce şu dosyalar mevcut olmalı:

- `/design/master-screen-inventory.md`
- `/design/web-ux-architecture.md`
- `/design/production-warehouse-deep-dive.md`
- `/design/database-technical-architecture.md`
- `/design/mobile-design.md`
- `/design/public-catalog-design.md`
- `/design/visual-design-system.md`
- `/design/decision-log.md`
- `/design/open-decisions-solution-matrix.md`
- `/design/grok-session-review.md`

Aşağıdaki tutarsızlıklardan biri varsa implementation'a geçme:

- Ekrandaki entity database tasarımında yok.
- Workflow state'i domain modelinde yok.
- Aksiyon için permission tanımlı değil.
- Ekranın veri kaynağı belli değil.
- Belgenin önceki/sonraki ilişkisi belli değil.
- Stok/cari etkisi belirtilmemiş.
- Kritik ağ kesintisi veya hata durumu tanımlanmamış.
- Açık karar seçildikten sonra domain, workflow, database, screen inventory ve skill-impact artefact'larına yayılmamış.
- Kararın sahibi, tarihi ve kanıtı yok.

Gate başarılıysa `/design/implementation-ready.md` oluştur.

## Varsayılan tasarım artefact'ları

```text
/design
  master-screen-inventory.md
  web-ux-architecture.md
  production-warehouse-deep-dive.md
  database-technical-architecture.md
  mobile-design.md
  public-catalog-design.md
  visual-design-system.md
  ui-mockup-review.md
  domain-model.md
  business-workflows.md
  decision-log.md
  implementation-readiness.md
  implementation-ready.md
  project-discovery-report.md
  skill-system-review.md

/docs
  00-project-brief/          # synchronized archive
  01-design/                 # synchronized delivery copies
  02-architecture/           # synchronized delivery copies
  03-production-warehouse/
  04-presentation/
  05-assets/
  06-process-skill/
```

## Sunum standardı

En fazla 12 slayt. Her slaytta tek ana mesaj. Teknik kararları iş etkisiyle ilişkilendir. Mockup'ı dekorasyon olarak değil karar ve risk açıklaması için kullan.
