# Factory ERP — Design Source of Truth

Bu klasör, kodlama öncesi `DISCOVER → DESIGN` aşamasının **canonical çıktılarıdır**. Production code, database migration veya API endpoint bu tasarım setinden sonra; `implementation-ready.md` başarı durumuna geçtiğinde başlatılmalıdır.

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
| [`open-decisions-solution-matrix.md`](./open-decisions-solution-matrix.md) | O-001–O-014 için önerilen çözüm, sahip, risk ve etki matrisi |
| [`open-decisions-workshop.md`](./open-decisions-workshop.md) | Karar atölyesi için yönetim özeti, karar formu ve Design Gate kapanış planı |
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
| [`decision-log.md`](./decision-log.md) | DECIDED, ASSUMED ve OPEN DECISION kayıtları |
| [`decision-clarification-backlog.md`](./decision-clarification-backlog.md) | O-001–O-014 için karar toplantısı alt soruları ve Design Gate kapanış çıktıları |
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
BLOCKED
```

Tasarım artefact'ları büyük ölçüde tamamlanmıştır. Implementation'a geçişi bloke eden kararlar `decision-log.md` ve `implementation-readiness.md` içindedir. Öncelikli sonraki skill `factory-erp-architecture`'dır.
