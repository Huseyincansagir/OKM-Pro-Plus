# Factory ERP — Implementation Readiness

## Durum

```text
DESIGN STATUS:
READY FOR ARCHITECTURE
IMPLEMENTATION:
NOT READY
NEXT SKILL:
factory-erp-architecture
```

Proje sahibinin 2026-08-16 tarihli kabulüyle O-001–O-014 karar blokajı kaldırılmıştır. Bu dosya **implementation’a hazır** anlamına gelmez; Design aşamasının tamamlandığını ve Architecture aşamasının başlayabileceğini gösterir.

## 1. Design Gate kontrolü

| Kontrol | Durum | Açıklama |
|---|---|---|
| Her önemli domain tanımlı mı? | PASS | Identity, ürün, müşteri, satış, depo, üretim, sevkiyat, fatura, cari, ödeme, İK, bildirim, rapor, audit ve dosya domainleri mevcut. |
| Her workflow uçtan uca tanımlı mı? | PASS | Satış, üretim, personel, kısmi sevkiyat, kısmi fatura ve lojistik akışları actor/input/state/effect/audit alanlarıyla işlendi. |
| Workflow state’leri belli mi? | PASS | O-001–O-014 kabul edilen MVP değerleri state, transition ve reversal kurallarına yayıldı. |
| State transition yetkileri belli mi? | PASS | Permission, approval, issue, override, reverse ve export sınırları tanımlıdır. |
| Stock effect tanımlı mı? | PASS | Reservation, movement, shipment, production receipt, partial shipment ve reversal etkileri tanımlıdır. |
| Financial effect tanımlı mı? | PASS | Invoice allocation, current transaction debit/credit, payment, tax snapshot ve reversal sınırları tanımlıdır. |
| Audit requirements tanımlı mı? | PASS | Kritik belge, stok, cari, ödeme, üretim, risk, override, public consent, backup ve yetki geçişleri belirlenmiştir. |
| Screen inventory tamam mı? | PASS | Web, public ve mobil modüller; liste, detay ve işlem ekranlarıyla envanterlenmiştir. |
| API/data source belirsizlikleri giderildi mi? | PASS WITH ARCHITECTURE FOLLOW-UP | Canonical source of truth ve seçilmiş karar etkileri tanımlıdır; gerçek DTO/API sözleşmeleri Architecture aşamasında üretilecektir. |
| Source of truth çakışmaları giderildi mi? | PASS | `domain-model.md`, `business-workflows.md`, `database-technical-architecture.md` ve `decision-log.md` ile canonical entity’ler belirlenmiştir. |
| Database domain sınırları mantıklı mı? | PASS | Modüler monolith sınırları, allocation tabloları, transaction ve index ön taslağı mevcuttur. |
| Mobile kritik operasyonlar tanımlı mı? | PASS | Barkod, stok, sayım, transfer, sevkiyat, miktar toggle ve üretim akışları tanımlıdır. |
| Public katalog/internal ERP ayrılmış mı? | PASS | Public katalog minimum veri, abuse controls ve iç ERP endpoint izolasyonu ile ayrılmıştır. |
| Deployment/backup sınırı belli mi? | PASS | Ubuntu LTS, Docker Compose, LAN HTTPS, public route ayrımı, RPO/RTO ve restore politikası seçilmiştir. |
| Marka/görsel sınırı belli mi? | PASS | Tek production marka ve asset manifest zorunluluğu seçilmiştir; gerçek asset dosyaları Architecture/UI uygulamasında alınacaktır. |

## 2. Karar kapanış kontrolü

O-001–O-014 maddelerinin tamamı proje sahibi tarafından 2026-08-16 tarihinde kabul edilmiştir. Karar sahibi, seçilen MVP değeri, gerekçe ve etki alanları `decision-log.md` içinde kayıtlıdır.

| Karar grubu | Durum | Seçilen kapsam |
|---|---|---|
| O-001 Vergi/e-belge | DECIDED | Vergi snapshot + adapter/stub; gerçek entegrasyon sonraki sınır |
| O-002 Kısmi sevkiyat | DECIDED | Kalem allocation, çoklu irsaliye, remainder/backorder |
| O-003 Kısmi fatura | DECIDED | Issued delivery allocation, çoklu fatura, cari debit on issue |
| O-004 BOM/hammadde | DECIDED | MVP dışında; finished-good receipt kapsamda |
| O-005 Lot/seri | DECIDED | MVP dışında; ihtiyaç çıkarsa yeniden karar |
| O-006 Müşteri kabulü | DECIDED | Public talep satış incelemesi olmadan aktif müşteri oluşturmaz |
| O-007 Risk | DECIDED | Soft block + gerekçeli override; kritik eşiklerde hard block |
| O-008 Maaş/bordro | DECIDED | Özet + kontrollü export; yasal bordro motoru dışında |
| O-009 Public/KVKK | DECIDED | Minimum veri + consent + rate/bot controls + public API izolasyonu |
| O-010 Backup/RPO/RTO | DECIDED | Günlük full, ayrı hedef, 14 gün, aylık restore, RPO ≤24s/RTO ≤8s önerisi |
| O-011 Server/LAN/HTTPS | DECIDED | Ubuntu LTS + Docker Compose + LAN HTTPS + route isolation |
| O-012 Fiyatlandırma | DECIDED | PriceList + CustomerPriceGroup + belge snapshot |
| O-013 Marka/assets | DECIDED | Tek production marka + asset manifest |
| O-014 Kargo otomasyonu | DECIDED | Hard validation + FFD öneri + manuel depo onayı |

**Açık karar sayısı: 0.** Yeni kapsam veya karar değişikliği gelirse ilgili O-ID yeniden açılır ve Design Gate yeniden değerlendirilir.

## 3. Architecture’a geçiş kriteri

Architecture aşamasına geçişe izin verilmiştir. `factory-erp-architecture` skill’i aşağıdaki çıktıları üretmelidir:

- Entity aggregate sınırları ve domain command/query sözleşmeleri.
- ASP.NET Core API endpoint, DTO, validation, error ve idempotency sözleşmeleri.
- EF Core/PostgreSQL migration planı ve allocation/quantity constraint uygulaması.
- RBAC/permission policy, state transition authorization ve audit event matrisi.
- Web, mobile ve public yüzeyleri için karar uyumlu data contract’ları.
- Docker Compose, network, HTTPS, backup/restore ve health-check ayrıntıları.
- O-001–O-014 kararlarına karşı architecture acceptance checklist.

## 4. Implementation’a geçiş kriteri

Implementation’a geçmek için ayrıca Architecture skill’inin gerçek API sözleşmeleri, migration planı, permission policy’leri, deployment/backup belgeleri ve ilgili acceptance testleri tamamlanmalıdır. `implementation-ready.md` bu nedenle hâlâ `NOT READY` kalır.

Aşağıdaki belgeler tamamlanmadan production code başlatılmamalıdır:

- `decision-log.md`
- `domain-model.md`
- `business-workflows.md`
- `database-technical-architecture.md`
- `master-screen-inventory.md`
- `public-catalog-design.md`
- `mobile-design.md`
- ilgili architecture/QA/security/operations skill-impact review’ları
- Architecture çıktıları ve migration/API acceptance testleri
