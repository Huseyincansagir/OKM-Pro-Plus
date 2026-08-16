# Factory ERP — Design Source of Truth

Bu klasör, `DISCOVER → DESIGN → DESIGN GATE → ARCHITECTURE` aşamalarının canonical çıktılarıdır. Production code, gerçek database migration veya API implementation’ı Architecture acceptance ve `implementation-ready.md` başarı durumundan sonra başlatılmalıdır.

Nümara verilmiş `docs/00`–`docs/06` klasörleri mevcut paylaşım ve arşiv paketini korur. Aynı artefact iki yerde bulunuyorsa `/design` canonical kabul edilir; numbered docs kopyası karar değişikliklerinde senkronize edilmelidir.

## Ana artefact'lar

| Dosya | Rol |
|---|---|
| [`project-discovery-report.md`](./project-discovery-report.md) | Repository, domain, workflow, risk ve Design Gate özeti |
| [`skill-system-review.md`](./skill-system-review.md) | Skill amacı, tetikleyici, girdi/çıktı ve aşama ilişkileri |
| [`master-screen-inventory.md`](./master-screen-inventory.md) | Web, public ve mobil ekran envanteri |
| [`web-ux-architecture.md`](./web-ux-architecture.md) | İç web bilgi mimarisi ve ekran akışları |
| [`production-warehouse-deep-dive.md`](./production-warehouse-deep-dive.md) | Üretim ve depo detaylı ekran/veri incelemesi |
| [`mobile-design.md`](./mobile-design.md) | Mobil operasyon ve barkod akışları |
| [`mobile-barcode-and-quantity-ux.md`](./mobile-barcode-and-quantity-ux.md) | Mobil barkod, üçlü miktar toggle'ı ve işlem seviyesi UX akışı |
| [`mobile-toggle-api-and-schema.md`](./mobile-toggle-api-and-schema.md) | Mobil Palet/Koli/Paket toggle database, API ve idempotency sözleşmesi |
| [`mobile-toggle-screen-by-screen-review.md`](./mobile-toggle-screen-by-screen-review.md) | Toggle'ın ekran bazlı görünürlük, varsayılan ve aksiyon UX incelemesi |
| [`public-catalog-design.md`](./public-catalog-design.md) | Public katalog ve teklif sepeti |
| [`visual-design-system.md`](./visual-design-system.md) | Görsel tasarım standardı |
| [`ui-mockup-review.md`](./ui-mockup-review.md) | Mockup UX, marka ve görsel varlık kabul incelemesi |
| [`grok-session-review.md`](./grok-session-review.md) | Grok notlarının karşılaştırmalı teknik ve karar incelemesi |
| [`open-decisions-solution-matrix.md`](./open-decisions-solution-matrix.md) | O-001–O-014 kabul edilen MVP çözümü, sahip, risk ve etki matrisi |
| [`open-decisions-workshop.md`](./open-decisions-workshop.md) | Kabul edilen karar paketi, karar kanıtı ve Architecture handoff özeti |
| [`product-packaging-and-uom.md`](./product-packaging-and-uom.md) | Palet-koli-paket-temel birim hiyerarşisi ve miktar dönüşüm kuralları |
| [`logistics-planning-rules-and-algorithms.md`](./logistics-planning-rules-and-algorithms.md) | Hard/soft lojistik kuralları, araç kapasite eşleştirme ve karışık palet algoritması |
| [`vehicle-capacity-matching.md`](./vehicle-capacity-matching.md) | Araç kapasite eşleştirme, aday eleme, fit skoru ve yük dağılımı detayları |
| [`shipment-logistics-ui-design.md`](./shipment-logistics-ui-design.md) | Araç, kargo planı, rota/durak, karışık palet ve paket izleme UI tasarımı |
| [`domain-model.md`](./domain-model.md) | Bounded context ve source-of-truth haritası |
| [`business-workflows.md`](./business-workflows.md) | Sales, production ve personnel workflow'ları |
| [`partial-shipment-invoicing-workflow.md`](./partial-shipment-invoicing-workflow.md) | O-002/O-003 kısmi sevkiyat ve kısmi fatura diyagramları, state, transaction ve audit akışı |
| [`quantity-error-handling-and-allocation-sql.md`](./quantity-error-handling-and-allocation-sql.md) | Quantity mismatch/concurrency API hata sözleşmesi, allocation DDL ve PostgreSQL sorguları |
| [`o002-partial-shipment-workflow.mmd`](./o002-partial-shipment-workflow.mmd) | O-002 editable Mermaid iş akışı kaynağı |
| [`o003-partial-invoice-workflow.mmd`](./o003-partial-invoice-workflow.mmd) | O-003 editable Mermaid iş akışı kaynağı |
| [`database-technical-architecture.md`](./database-technical-architecture.md) | PostgreSQL, API, transaction ve deployment ön taslağı |
| [`architecture-api-contracts.md`](./architecture-api-contracts.md) | ASP.NET Core endpoint, DTO, ProblemDetails, idempotency, authorization ve state command sözleşmeleri |
| [`architecture-efcore-and-migration-plan.md`](./architecture-efcore-and-migration-plan.md) | EF Core aggregate/entity mapping, PostgreSQL migration sırası, constraint ve seed planı |
| [`postgresql-18-migration-sql-specification.md`](./postgresql-18-migration-sql-specification.md) | 0001–0018 migration adımlarının ayrıntılı PostgreSQL tablo, FK, constraint, index, seed ve rollback SQL şeması |
| [`mvp-test-strategy.md`](./mvp-test-strategy.md) | MVP unit, application, PostgreSQL integration, API, security, concurrency, backup ve release test stratejisi |
| [`docker-compose-deployment-plan.md`](./docker-compose-deployment-plan.md) | Docker Compose servisleri, network, HTTPS, backup/restore ve operasyon kabul kriterleri |
| [`decision-log.md`](./decision-log.md) | DECIDED, ASSUMED ve OPEN DECISION kayıtları |
| [`decision-clarification-backlog.md`](./decision-clarification-backlog.md) | O-001–O-014 için karar toplantısı alt soruları ve Design Gate kapanış çıktıları |
| [`p0-p1-decision-recommendations.md`](./p0-p1-decision-recommendations.md) | Kabul edilen P0/P1 baseline, MVP sınırları, gerekçeler, riskler ve Architecture handoff listesi |
| [`implementation-readiness.md`](./implementation-readiness.md) | Ayrıntılı Design Gate değerlendirmesi |
| [`implementation-ready.md`](./implementation-ready.md) | Implementation skill için resmi gate dosyası |

## Aşama akışı

```text
DISCOVER
  ↓
DESIGN
  ↓
DESIGN GATE
  ↓
ARCHITECTURE
  ↓
IMPLEMENTATION
  ↓
QA / SECURITY
  ↓
OPERATIONS
  ↓
RELEASE
```

## Mevcut sonuç

```text
DESIGN STATUS:
READY FOR ARCHITECTURE
IMPLEMENTATION:
NOT READY
```

O-001–O-014 karar paketi kabul edilmiş, Architecture artefact’ları üretim aşamasına alınmıştır. Design Gate `READY FOR ARCHITECTURE` durumundan Architecture çalışma durumuna geçmiştir; production implementation hâlâ Architecture acceptance tamamlanana kadar kapalıdır.
