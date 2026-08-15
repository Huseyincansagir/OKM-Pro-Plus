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
| [`public-catalog-design.md`](./public-catalog-design.md) | Public katalog ve teklif sepeti |
| [`visual-design-system.md`](./visual-design-system.md) | Görsel tasarım standardı |
| [`ui-mockup-review.md`](./ui-mockup-review.md) | Mockup UX, marka ve görsel varlık kabul incelemesi |
| [`domain-model.md`](./domain-model.md) | Bounded context ve source-of-truth haritası |
| [`business-workflows.md`](./business-workflows.md) | Sales, production ve personnel workflow'ları |
| [`database-technical-architecture.md`](./database-technical-architecture.md) | PostgreSQL, API, transaction ve deployment ön taslağı |
| [`decision-log.md`](./decision-log.md) | DECIDED, ASSUMED ve OPEN DECISION kayıtları |
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
