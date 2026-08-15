# OKM Pro Plus

## Factory ERP-Lite Tasarım ve Mimari Paketi

Bu repository şu anda kodlama öncesi Factory ERP-Lite tasarım temelidir. **Canonical tasarım ve Design Gate kaynağı `/design` klasörüdür.** Numaralı `docs/00`–`docs/06` yapısı paylaşım, sunum, görsel asset ve senkronize arşiv paketini korur.

Başlangıç noktaları:

1. [`design/project-discovery-report.md`](./design/project-discovery-report.md) — repository, domain, workflow, risk ve Design Gate raporu.
2. [`design/README.md`](./design/README.md) — canonical tasarım artefact indeksi.
3. [`design/decision-log.md`](./design/decision-log.md) — source of truth, varsayım ve açık kararlar.
4. [`design/implementation-readiness.md`](./design/implementation-readiness.md) — Design Gate sonucu.
5. [`docs/00-project-brief/`](./docs/00-project-brief/) — özgün gereksinim ve discovery arşivi.
6. [`docs/01-design/`](./docs/01-design/) ve [`docs/02-architecture/`](./docs/02-architecture/) — senkronize teslim kopyaları.
7. [`docs/06-process-skill/`](./docs/06-process-skill/) — skill sistemi ve zorunlu çalışma sırası.

Sunum kaynakları ve görsel referanslar [`docs/04-presentation/`](./docs/04-presentation/) ile [`docs/05-assets/mockups/`](./docs/05-assets/mockups/) altındadır.

Mockup incelemesinde üç farklı marka adı görüldüğü için uygulama kodlamasına kadar marka adı, logo, favicon, renk token'ları ve ürün görseli lisans/placeholder politikası kesinleştirilmelidir. Bu konu `/design/decision-log.md` içinde `O-013` olarak kayıtlıdır.

Mevcut aşama: `DISCOVER → DESIGN`. Production code, migration veya API implementasyonu henüz yoktur. Design Gate, açık kararlar nedeniyle `BLOCKED` durumundadır; sonraki adım `factory-erp-architecture` skill'idir.
