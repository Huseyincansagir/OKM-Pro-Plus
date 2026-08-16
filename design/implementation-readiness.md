# Factory ERP — Implementation Readiness

## Durum

```text
DESIGN STATUS:
READY FOR ARCHITECTURE
ARCHITECTURE:
ACCEPTED FOR MVP HANDOFF
IMPLEMENTATION:
READY FOR SCAFFOLD — CONDITIONAL
NEXT SKILL:
factory-erp-implementation
```

Proje sahibinin 2026-08-16 tarihli kabulüyle O-001–O-014 karar blokajı kaldırılmış; araştırma sonrası ADR-001–ADR-011 teknik baseline’ı da kabul edilmiştir. Bu dosya artık **sınırlı MVP scaffold ve test implementation’ına geçiş izni** verir; tüm ERP feature’larının aynı anda kodlanabileceği anlamına gelmez.

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
| API/data source belirsizlikleri giderildi mi? | PASS | Canonical source of truth, API/DTO/error/idempotency sözleşmeleri ve seçilmiş ADR etkileri Architecture artefact’larında tanımlıdır. |
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

## 3. Architecture acceptance sonucu

Architecture aşamasının MVP çıktıları tamamlanmış ve ADR-001–ADR-011 ile kabul edilmiştir. Kabul edilen çıktılar şunlardır:

- Entity aggregate sınırları ve domain command/query sözleşmeleri.
- ASP.NET Core API endpoint, DTO, validation, error ve idempotency sözleşmeleri.
- EF Core/PostgreSQL migration planı ve allocation/quantity constraint uygulaması.
- RBAC/permission policy, state transition authorization ve audit event matrisi.
- Web, mobile ve public yüzeyleri için karar uyumlu data contract’ları.
- Docker Compose, network, HTTPS, backup/restore ve health-check ayrıntıları.
- O-001–O-014 ve ADR-001–ADR-011 kararlarına karşı acceptance checklist.

## 4. Implementation’a geçiş kriteri

Implementation gate **READY FOR SCAFFOLD** durumundadır. İlk implementation slice yalnızca Domain ve test altyapısıdır:

1. `FactoryErp.Domain` common types.
2. `PositiveQuantity`, `NonNegativeQuantity`, `PackagingSnapshot` ve `QuantitySnapshot`.
3. `SalesOrderItem`, `DeliveryNoteItem` ve allocation invariant’ları.
4. Domain unit test project’i.
5. Architecture dependency testleri.

Bu slice’ın build, unit test, architecture test ve documentation acceptance kanıtları alınmadan API, EF migration, web, mobile, production worker veya external adapter feature’ları başlatılmayacaktır. Ayrıntılı kontrol listesi ve test planı [`pre-implementation-readiness-review.md`](./pre-implementation-readiness-review.md) içinde tutulur.

Production implementation devam ederken karar değişirse ilgili ADR yeniden açılır, implementation durdurulur ve canonical artefact’lar güncellenir.
