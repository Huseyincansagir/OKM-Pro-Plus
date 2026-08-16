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
7. [`../design/open-decisions-solution-matrix.md`](../design/open-decisions-solution-matrix.md) — O-001–O-014 çözüm önerileri ve karar sahipleri.
8. [`../design/open-decisions-workshop.md`](../design/open-decisions-workshop.md) — Kabul edilen karar paketi, karar kanıtı ve Architecture handoff özeti.
9. [`../design/decision-clarification-backlog.md`](../design/decision-clarification-backlog.md) — O-001–O-014 karar alt soruları ve artefact yayılım kontrolü.
10. [`../design/product-packaging-and-uom.md`](../design/product-packaging-and-uom.md) — Palet-koli-paket-temel birim hiyerarşisi ve miktar dönüşüm kuralları.
11. [`../design/mobile-toggle-api-and-schema.md`](../design/mobile-toggle-api-and-schema.md) — Mobil Palet/Koli/Paket toggle database, API ve idempotency sözleşmesi.
12. [`../design/mobile-toggle-screen-by-screen-review.md`](../design/mobile-toggle-screen-by-screen-review.md) — Toggle'ın ekran bazlı görünürlük, varsayılan ve aksiyon UX incelemesi.
13. [`../design/logistics-planning-rules-and-algorithms.md`](../design/logistics-planning-rules-and-algorithms.md) — Hard/soft lojistik kuralları, araç kapasite eşleştirme ve karışık palet algoritması.
14. [`../design/vehicle-capacity-matching.md`](../design/vehicle-capacity-matching.md) — Araç kapasite eşleştirme, aday eleme, fit skoru ve yük dağılımı detayları.
15. [`../design/shipment-logistics-ui-design.md`](../design/shipment-logistics-ui-design.md) — Araç, kargo planı, rota/durak, karışık palet ve paket izleme UI tasarımı.
16. [`../design/partial-shipment-invoicing-workflow.md`](../design/partial-shipment-invoicing-workflow.md) — O-002/O-003 kısmi sevkiyat ve kısmi fatura iş akışı, state, transaction ve audit tasarımı.
17. [`../design/quantity-error-handling-and-allocation-sql.md`](../design/quantity-error-handling-and-allocation-sql.md) — Quantity mismatch/concurrency API hata sözleşmesi, allocation DDL ve PostgreSQL sorguları.
18. [`../design/p0-p1-decision-recommendations.md`](../design/p0-p1-decision-recommendations.md) — Kabul edilen P0/P1 baseline, MVP sınırları, gerekçeler, riskler ve Architecture handoff listesi.
19. [`../design/architecture-api-contracts.md`](../design/architecture-api-contracts.md) — ASP.NET Core endpoint, DTO, ProblemDetails, idempotency, authorization ve state command sözleşmeleri.
20. [`../design/architecture-efcore-and-migration-plan.md`](../design/architecture-efcore-and-migration-plan.md) — EF Core aggregate/entity mapping, PostgreSQL migration sırası, constraint ve seed planı.
21. [`../design/postgresql-18-migration-sql-specification.md`](../design/postgresql-18-migration-sql-specification.md) — 0001–0018 migration adımlarının ayrıntılı PostgreSQL SQL şeması.
22. [`../design/mvp-test-strategy.md`](../design/mvp-test-strategy.md) — MVP unit, application, PostgreSQL integration, API, security, concurrency, backup ve release test stratejisi.
23. [`../design/github-actions-cicd-plan.md`](../design/github-actions-cicd-plan.md) — MVP GitHub Actions CI/CD, migration/test/image/deployment gate’leri ve self-hosted on-prem release planı.
24. [`../design/aspnet-clean-architecture-and-cqrs.md`](../design/aspnet-clean-architecture-and-cqrs.md) — ASP.NET Core Clean Architecture klasör yapısı, dependency yönü, CQRS handler ve architecture test sözleşmeleri.
25. [`../design/factoryerp-domain-code-design.md`](../design/factoryerp-domain-code-design.md) — FactoryErp.Domain temel entity, aggregate, quantity/packaging value object ve allocation invariant kod tasarımı.
26. [`../design/allocation-cqrs-unit-test-code-design.md`](../design/allocation-cqrs-unit-test-code-design.md) — Allocation ve CQRS transaction unit test blueprint’leri, idempotency, rollback ve error branch testleri.
27. [`../design/docker-compose-deployment-plan.md`](../design/docker-compose-deployment-plan.md) — Docker Compose servisleri, network, HTTPS, backup/restore ve deployment kabul kriterleri.

## Arşiv okuma sırası

1. [`00-project-brief/01-project-discovery-report.md`](./00-project-brief/01-project-discovery-report.md)
2. [`01-design/00-complete-ui-design-package.md`](./01-design/00-complete-ui-design-package.md)
3. [`01-design/01-master-screen-inventory.md`](./01-design/01-master-screen-inventory.md)
4. [`01-design/02-web-ux-architecture.md`](./01-design/02-web-ux-architecture.md)
5. [`01-design/05-mobile-complete-design.md`](./01-design/05-mobile-complete-design.md)
6. [`01-design/16-ui-mockup-review.md`](./01-design/16-ui-mockup-review.md)
7. [`02-architecture/00-domain-model.md`](./02-architecture/00-domain-model.md)
8. [`02-architecture/01-database-technical-architecture.md`](./02-architecture/01-database-technical-architecture.md)
9. [`02-architecture/02-architecture-api-contracts.md`](./02-architecture/02-architecture-api-contracts.md)
10. [`02-architecture/03-architecture-efcore-and-migration-plan.md`](./02-architecture/03-architecture-efcore-and-migration-plan.md)
11. [`02-architecture/04-docker-compose-deployment-plan.md`](./02-architecture/04-docker-compose-deployment-plan.md)
12. [`01-design/14-implementation-readiness.md`](./01-design/14-implementation-readiness.md)
13. [`01-design/17-product-packaging-and-uom.md`](./01-design/17-product-packaging-and-uom.md)
14. [`01-design/18-shipment-logistics-ui-design.md`](./01-design/18-shipment-logistics-ui-design.md)
15. [`01-design/19-mobile-barcode-and-quantity-ux.md`](./01-design/19-mobile-barcode-and-quantity-ux.md)
16. [`01-design/20-logistics-planning-rules-and-algorithms.md`](./01-design/20-logistics-planning-rules-and-algorithms.md)
17. [`01-design/21-mobile-toggle-api-and-schema.md`](./01-design/21-mobile-toggle-api-and-schema.md)
18. [`01-design/22-mobile-toggle-screen-by-screen-review.md`](./01-design/22-mobile-toggle-screen-by-screen-review.md)
19. [`01-design/23-vehicle-capacity-matching.md`](./01-design/23-vehicle-capacity-matching.md)
20. [`01-design/24-decision-clarification-backlog.md`](./01-design/24-decision-clarification-backlog.md)
21. [`01-design/25-partial-shipment-invoicing-workflow.md`](./01-design/25-partial-shipment-invoicing-workflow.md)
22. [`01-design/26-quantity-error-handling-and-allocation-sql.md`](./01-design/26-quantity-error-handling-and-allocation-sql.md)
23. [`01-design/27-p0-p1-decision-recommendations.md`](./01-design/27-p0-p1-decision-recommendations.md)
24. [`02-architecture/02-architecture-api-contracts.md`](./02-architecture/02-architecture-api-contracts.md)
25. [`02-architecture/03-architecture-efcore-and-migration-plan.md`](./02-architecture/03-architecture-efcore-and-migration-plan.md)
26. [`02-architecture/04-docker-compose-deployment-plan.md`](./02-architecture/04-docker-compose-deployment-plan.md)
27. [`02-architecture/05-postgresql-18-migration-sql-specification.md`](./02-architecture/05-postgresql-18-migration-sql-specification.md)
28. [`02-architecture/06-mvp-test-strategy.md`](./02-architecture/06-mvp-test-strategy.md)
29. [`02-architecture/07-github-actions-cicd-plan.md`](./02-architecture/07-github-actions-cicd-plan.md)
30. [`02-architecture/08-aspnet-clean-architecture-and-cqrs.md`](./02-architecture/08-aspnet-clean-architecture-and-cqrs.md)
31. [`02-architecture/09-factoryerp-domain-code-design.md`](./02-architecture/09-factoryerp-domain-code-design.md)
32. [`02-architecture/10-allocation-cqrs-unit-test-code-design.md`](./02-architecture/10-allocation-cqrs-unit-test-code-design.md)

## Aşama sonucu

```text
DISCOVER → DESIGN → DESIGN GATE → ARCHITECTURE
DESIGN STATUS: READY FOR ARCHITECTURE
ARCHITECTURE: IN PROGRESS
IMPLEMENTATION: NOT READY
```

Karar baseline’ı canonical [`../design/decision-log.md`](../design/decision-log.md) içinde tutulur. `/design/implementation-ready.md` `IMPLEMENTATION: NOT READY` kaldığı sürece business feature implementasyonu başlatılmaz.
