# Factory ERP — Implementation Readiness

## Durum

```text
DESIGN STATUS:
BLOCKED
```

Bu sonuç production code kalitesiyle değil, bootstrap promptunun istediği Design Gate kriterleriyle ilgilidir. Tasarım kapsamı geniş ölçüde tamamlanmış olsa da bazı domain kararları implementasyonun veri ve workflow davranışını doğrudan etkilediği için Architecture/Implementation aşamasına geçmeden önce netleştirilmelidir.

## 1. Design Gate kontrolü

| Kontrol | Durum | Açıklama |
|---|---|---|
| Her önemli domain tanımlı mı? | PASS | Identity, ürün, müşteri, satış, depo, üretim, sevkiyat, fatura, cari, ödeme, İK, bildirim, rapor, audit ve dosya domainleri mevcut. |
| Her workflow uçtan uca tanımlı mı? | PASS | Satış, üretim ve personel akışları actor/input/state/effect/audit alanlarıyla işlendi. |
| Workflow state'leri belli mi? | PASS WITH ASSUMPTION | Ana belge durumları tanımlı; kısmi sevkiyat/fatura gibi kararlar açık. |
| State transition yetkileri belli mi? | PASS | Permission örnekleri ve rol sınırları ekran envanterinde belirtilmiştir. |
| Stock effect tanımlı mı? | PASS | Reservation, movement, shipment, production receipt ve count etkileri tanımlıdır. |
| Financial effect tanımlı mı? | PASS WITH ASSUMPTION | Fatura/cari/ödeme ilişkisi tanımlı; VAT ve bordro kapsamı açıktır. |
| Audit requirements tanımlı mı? | PASS | Kritik belge, stok, cari, ödeme, üretim ve yetki geçişleri belirlenmiştir. |
| Screen inventory tamam mı? | PASS | Web, public ve mobil modüller; liste, detay ve işlem ekranlarıyla envanterlenmiştir. |
| API/data source belirsizlikleri giderildi mi? | PASS WITH ASSUMPTION | Ana kaynaklar domain modelinde belirtilmiştir; API kontratları Architecture aşamasında üretilecektir. |
| Source of truth çakışmaları giderildi mi? | PASS | `domain-model.md` ve `decision-log.md` ile canonical entity'ler belirlendi. |
| Database domain sınırları mantıklı mı? | PASS | Modüler monolith sınırları ve tablo grupları mevcut teknik taslakta vardır. |
| Mobile kritik operasyonlar tanımlı mı? | PASS | Barkod, stok, sayım, transfer, sevkiyat ve üretim akışları tanımlıdır. |
| Public katalog/internal ERP ayrılmış mı? | PASS | Public katalog maliyet, risk ve iç operasyon bilgilerini göstermeyecek şekilde ayrılmıştır. |

## 2. Blocking open decisions

Aşağıdaki kararlar schema ve domain davranışını değiştirebileceği için Architecture aşamasına geçişi bloke eder:

1. Vergi/VAT ve e-belge entegrasyonu.
2. Kısmi sevkiyat ve kısmi fatura kuralları.
3. Üretim BOM/reçete ve hammadde tüketimi.
4. Lot/seri/parti takip gereksinimi.
5. Fiyat listesi ve müşteri bazlı fiyatlandırma.
6. Public katalog erişim, doğrulama ve KVKK metin politikası.
7. Risk skorunun yalnızca uyarı mı, sipariş blokajı mı olacağı.
8. Maaş/bordro kapsamı ve harici sistem entegrasyonu.
9. Şirket içi server işletim sistemi, HTTPS/LAN modeli ve RPO/RTO hedefleri.
10. Final marka adı, logo, favicon, renk token'ları ve ürün görseli lisans/placeholder politikası.

Bu kararlar `/design/decision-log.md` içinde `OPEN DECISION` olarak tutulmaktadır.

## 3. Architecture'a geçiş kriteri

Architecture aşamasına geçmeden önce açık kararların her biri için karar sahibi, son tarih, seçilen değer ve etkilenen artefact'lar yazılmalıdır. Sonrasında bu dosya `READY FOR ARCHITECTURE` olarak güncellenmelidir.

## 4. Implementation'a geçiş kriteri

Implementation'a geçmek için ayrıca Architecture skill'in üreteceği gerçek API sözleşmeleri, migration planı, permission policy'leri ve deployment/backup belgeleri bulunmalıdır. `implementation-ready.md` dosyası yalnızca bu Design Gate ve gerekli Architecture çıktıları birlikte tamamlandığında başarı durumuna çekilmelidir.
