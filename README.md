# OKM Pro Plus

## Factory ERP-Lite Tasarım ve Mimari Paketi

Bu repository şu anda kodlama öncesi Factory ERP-Lite tasarım temelidir. Dosyalar konu ve aşama sırasını koruyan `docs/00`–`docs/06` yapısında tutulur; aynı domain için ikinci bir source of truth bulunmaz.

Başlangıç noktası: [`docs/00-project-brief/01-project-discovery-report.md`](./docs/00-project-brief/01-project-discovery-report.md)

Önerilen okuma sırası:

1. [`docs/00-project-brief/`](./docs/00-project-brief/) — özgün gereksinim ve discovery raporu.
2. [`docs/01-design/`](./docs/01-design/) — ekran, UX, workflow, public katalog ve karar dosyaları.
3. [`docs/02-architecture/`](./docs/02-architecture/) — domain modeli ve teknik veritabanı mimarisi.
4. [`docs/03-production-warehouse/`](./docs/03-production-warehouse/) — üretim/depo derinlemesine operasyon tasarımı.
5. [`docs/06-process-skill/`](./docs/06-process-skill/) — skill sistemi ve zorunlu çalışma sırası.

Sunum kaynakları ve görsel referanslar [`docs/04-presentation/`](./docs/04-presentation/) ile [`docs/05-assets/mockups/`](./docs/05-assets/mockups/) altındadır.

Mevcut aşama: `DISCOVER → DESIGN`. Production code, migration veya API implementasyonu henüz yoktur. Design Gate, açık kararlar nedeniyle `BLOCKED` durumundadır; sonraki adım `factory-erp-architecture` skill'idir.
