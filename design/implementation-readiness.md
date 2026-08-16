# Factory ERP — Implementation Readiness

## Durum

```text
DESIGN STATUS:
READY FOR ARCHITECTURE
ARCHITECTURE:
ACCEPTED FOR MVP HANDOFF
IMPLEMENTATION:
DOMAIN SLICE COMPLETE — NEXT: INFRASTRUCTURE/PERSISTENCE
NEXT SLICE:
FactoryErp.Infrastructure + PostgreSQL migration/integration
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

## 4. Implementation slice kabulü

İlk implementation slice tamamlanmış ve sonraki persistence slice’ına devredilmeye hazırdır. Gerçekleştirilen kapsam şudur:

1. `FactoryErp.Domain` common types ve framework bağımsız aggregate temeli.
2. `PositiveQuantity`, `NonNegativeQuantity`, `UomCode`, `PackagingSnapshot` ve `QuantitySnapshot`.
3. `SalesOrder`/`SalesOrderItem` state geçişleri, reservation ve partial shipment invariant’ları.
4. `DeliveryNoteItem` invoiceable quantity ve source-scoped allocation invariant’ları.
5. Pozitif reversal kaydı ve `reversed_from_id` semantiği.
6. Domain event collection ve typed `DomainError`/`DomainException` sözleşmesi.
7. xUnit Domain unit testleri ve NetArchTest dependency boundary testleri.

Kabul kanıtı olarak Release build başarılıdır; 28 Domain unit testi ve 2 architecture testi geçmiştir. Domain projesinde ASP.NET Core, EF Core, PostgreSQL, Dapper veya `System.Data` bağımlılığı bulunmamaktadır. API, EF migration, web, mobile, Worker ve external adapter kapsamı bilinçli olarak sonraki slice’a bırakılmıştır. Ayrıntılı kanıt ve kapsam [`implementation-domain-slice.md`](./implementation-domain-slice.md) içinde tutulur.

Production implementation devam ederken karar değişirse ilgili ADR yeniden açılır, implementation durdurulur ve canonical artefact’lar güncellenir.
