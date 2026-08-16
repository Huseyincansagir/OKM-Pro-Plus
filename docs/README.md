# Factory ERP-Lite Dokümantasyon Paketi

Bu klasör, projenin paylaşım, sunum, görsel asset, özgün gereksinim ve süreç arşividir. **Canonical tasarım ve Design Gate kaynağı repository kökündeki `/design` klasörüdür.** Numaralı klasörler teslim ve geriye dönük izlenebilirlik için korunur; aynı ekran veya domain kuralı değiştiğinde önce `/design` güncellenir, ardından ilgili numbered docs kopyası senkronize edilir.

## Klasörler

| Klasör | İçerik |
|---|---|
| [`00-project-brief`](./00-project-brief/) | Özgün proje gereksinimleri ve discovery arşivi |
| [`01-design`](./01-design/) | Senkronize ekran, UX, workflow, visual design ve gate kopyaları |
| [`02-architecture`](./02-architecture/) | Senkronize domain model ve PostgreSQL/teknik mimari kopyaları |
| [`03-production-warehouse`](./03-production-warehouse/) | Üretim, depo, barkod, sayım, transfer ve sevkiyat operasyon derinlemesi |
| [`04-presentation`](./04-presentation/) | Proje yönetimi sunumu, konuşma notları ve HTML slayt kaynakları |
| [`05-assets/mockups`](./05-assets/mockups/) | Arayüz ve mobil görsel referanslar |
| [`06-process-skill`](./06-process-skill/) | Agent talimatları, skill'ler, workflow ve skill sistem incelemesi |

## Canonical tasarım başlangıcı

1. [`../design/project-discovery-report.md`](../design/project-discovery-report.md)
2. [`../design/README.md`](../design/README.md)
3. [`../design/decision-log.md`](../design/decision-log.md)
4. [`../design/implementation-readiness.md`](../design/implementation-readiness.md)
5. [`../design/ui-mockup-review.md`](../design/ui-mockup-review.md)
6. [`../design/grok-session-review.md`](../design/grok-session-review.md) — Grok notlarının karşılaştırmalı incelemesi.

## Arşiv okuma sırası

1. [`00-project-brief/01-project-discovery-report.md`](./00-project-brief/01-project-discovery-report.md)
2. [`01-design/00-complete-ui-design-package.md`](./01-design/00-complete-ui-design-package.md)
3. [`01-design/01-master-screen-inventory.md`](./01-design/01-master-screen-inventory.md)
4. [`01-design/12-business-workflows.md`](./01-design/12-business-workflows.md) ve [`01-design/13-decision-log.md`](./01-design/13-decision-log.md)
5. [`01-design/16-ui-mockup-review.md`](./01-design/16-ui-mockup-review.md)
6. [`02-architecture/00-domain-model.md`](./02-architecture/00-domain-model.md)
7. [`02-architecture/01-database-technical-architecture.md`](./02-architecture/01-database-technical-architecture.md)
8. [`01-design/14-implementation-readiness.md`](./01-design/14-implementation-readiness.md)

## Aşama sonucu

```text
DISCOVER → DESIGN
DESIGN STATUS: BLOCKED
RECOMMENDED NEXT SKILL: factory-erp-architecture
```

Blocker kararlar ve sahipleri canonical [`../design/decision-log.md`](../design/decision-log.md) içinde tutulur. `/design/implementation-ready.md` `NOT READY` kaldığı sürece business feature implementasyonu başlatılmaz.
