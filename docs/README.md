# Factory ERP-Lite Dokümantasyon Paketi

Bu klasör projenin tek kanonik tasarım ve mimari kaynağıdır. Numaralı klasörler aşama ve konu sırasını gösterir; aynı ekran veya domain kuralı ayrı bir paralel pakette tutulmaz.

## Klasörler

| Klasör | İçerik |
|---|---|
| [`00-project-brief`](./00-project-brief/) | Özgün proje gereksinimleri ve discovery raporu |
| [`01-design`](./01-design/) | Master screen inventory, web/mobile/public UX, workflow, visual design ve karar/gate dosyaları |
| [`02-architecture`](./02-architecture/) | Domain model ve PostgreSQL/teknik mimari ön taslağı |
| [`03-production-warehouse`](./03-production-warehouse/) | Üretim, depo, barkod, sayım, transfer ve sevkiyat operasyon derinlemesi |
| [`04-presentation`](./04-presentation/) | Proje yönetimi sunumu, konuşma notları ve HTML slayt kaynakları |
| [`05-assets/mockups`](./05-assets/mockups/) | Arayüz ve mobil görsel referanslar |
| [`06-process-skill`](./06-process-skill/) | Agent talimatları, skill'ler, workflow ve skill sistem incelemesi |

## Önerilen okuma sırası

1. [`00-project-brief/01-project-discovery-report.md`](./00-project-brief/01-project-discovery-report.md)
2. [`01-design/00-complete-ui-design-package.md`](./01-design/00-complete-ui-design-package.md)
3. [`01-design/01-master-screen-inventory.md`](./01-design/01-master-screen-inventory.md)
4. [`01-design/12-business-workflows.md`](./01-design/12-business-workflows.md) ve [`01-design/13-decision-log.md`](./01-design/13-decision-log.md)
5. [`01-design/16-ui-mockup-review.md`](./01-design/16-ui-mockup-review.md) — Manus mockup doğrulaması ve görsel açık kararlar
6. [`02-architecture/00-domain-model.md`](./02-architecture/00-domain-model.md)
7. [`02-architecture/01-database-technical-architecture.md`](./02-architecture/01-database-technical-architecture.md)
8. [`01-design/14-implementation-readiness.md`](./01-design/14-implementation-readiness.md)

## Aşama sonucu

```text
DISCOVER → DESIGN
DESIGN STATUS: BLOCKED
RECOMMENDED NEXT SKILL: factory-erp-architecture
```

Blocker kararlar ve sahipleri [`01-design/13-decision-log.md`](./01-design/13-decision-log.md) içinde tutulur. `implementation-ready.md` `NOT READY` kaldığı sürece business feature implementasyonu başlatılmaz.
