# Factory ERP — Web/Mobile Implementation Slice

## WEB SLICE 001 — Next.js Scaffold & Build Baseline

**Tarih:** 2026-08-18
**Durum:** PASS
**Kapsam:** Yalnızca `apps/web` altında temiz ve build edilebilir Next.js foundation.

Bu slice, gerçek ERP ekranlarını veya mobil uygulamayı başlatmaz. Amaç, sonraki web UI slice’larının üzerine inşa edilebileceği deterministik bir Next.js App Router temelini oluşturmaktır.

## 1. Uygulanan stack

| Alan | Karar |
|---|---|
| Framework | Next.js 15 App Router |
| Dil | TypeScript strict mode |
| Stil | Tailwind CSS + PostCSS |
| Lint | ESLint 9 + `eslint-config-next` |
| Server-state hazırlığı | TanStack Query dependency’si hazır; feature kullanılmadı |
| UI/session-state hazırlığı | Zustand dependency’si hazır; store oluşturulmadı |
| Form hazırlığı | React Hook Form + Zod dependency’leri hazır; form oluşturulmadı |
| İkon/yardımcılar | `lucide-react`, `clsx`, `tailwind-merge` dependency’leri hazır |
| Package manager | Root pnpm workspace, lockfile root’ta |

Kütüphaneler bu slice’ta feature geliştirmek için kullanılmadı. Yalnızca sonraki AppShell, auth, katalog ve operasyon slice’larının aynı foundation üzerinde ilerleyebilmesi için dependency/config seviyesi hazırlandı.

## 2. Değiştirilen dosyalar

| Dosya | Amaç |
|---|---|
| `apps/web/package.json` | Next.js/TypeScript/Tailwind ve gelecek state/form dependency’leri |
| `apps/web/tsconfig.json` | Strict TypeScript ve `@/*` path alias |
| `apps/web/next.config.mjs` | React strict mode, production header ayarı |
| `apps/web/tailwind.config.ts` | Factory ERP renk token’larının başlangıcı |
| `apps/web/postcss.config.mjs` | Tailwind + Autoprefixer pipeline |
| `apps/web/eslint.config.mjs` | Next.js Core Web Vitals ve TypeScript lint kuralları |
| `apps/web/.env.example` | `NEXT_PUBLIC_API_BASE_URL` ve public app adı |
| `apps/web/.gitignore` | Node, Next build ve local env artefact’ları |
| `apps/web/next-env.d.ts` | Next.js TypeScript ambient declarations |
| `apps/web/src/app/layout.tsx` | Türkçe root layout ve metadata |
| `apps/web/src/app/globals.css` | Tailwind directives, global background ve focus görünürlüğü |
| `apps/web/src/app/page.tsx` | Minimal Türkçe scaffold doğrulama sayfası |
| `pnpm-workspace.yaml` | `apps/web` root workspace ve native build allowlist |
| `pnpm-lock.yaml` | Reproducible dependency lockfile |
| `.gitignore` | Root Node/pnpm artefact ignore kuralları |

## 3. Route foundation

Bu slice’ta yalnızca root route `/` uygulanmıştır. Root sayfa, uygulamanın çalıştığını gösteren minimal Türkçe scaffold mesajı sunar. Henüz AppShell, Sidebar, Dashboard, Login, API client, public catalog, Zustand store, TanStack Query feature veya business screen eklenmemiştir.

Sonraki route foundation için önerilen alanlar aşağıdaki slice’larda açılacaktır:

| Sonraki slice | Route kapsamı |
|---|---|
| WEB SLICE 002 | AppShell, responsive layout ve design system |
| WEB SLICE 003 | `/giris`, auth/session ve API client |
| WEB SLICE 004 | `/katalog`, ürün listesi, ürün detayı ve teklif sepeti |
| WEB SLICE 005 | `/dashboard` ve role-aware iç uygulama başlangıcı |

## 4. Environment yaklaşımı

Secret veya production credential repository’ye eklenmemiştir. Web runtime’da public olarak kullanılmasına izin verilen API base URL `NEXT_PUBLIC_API_BASE_URL` ile yönetilir. Production deployment’ta bu değer host-specific environment üzerinden verilmelidir. Access/refresh token davranışı bu slice’ta başlatılmamıştır; auth implementation’ı ayrı slice’ta API contract ve security kararlarına göre yapılacaktır.

## 5. Gate sonuçları

| Kontrol | Sonuç |
|---|---|
| Next.js scaffold | PASS |
| TypeScript | PASS |
| Tailwind | PASS |
| PostCSS | PASS |
| ESLint configuration | PASS |
| Environment example | PASS |
| `pnpm install` | PASS; pnpm native build approvals uygulandı |
| `pnpm typecheck` | PASS |
| `pnpm lint` | PASS |
| `pnpm build` | PASS; Next.js 15.5.23, root route static prerender edildi |
| `dotnet restore FactoryErp.sln` | PASS |
| `dotnet build FactoryErp.sln --configuration Release` | PASS; 0 warning / 0 error |
| `dotnet test FactoryErp.sln` | PASS; 122 domain, 5 architecture, 78 infrastructure |
| `git diff --check` | PASS |

## 6. Kalan işler ve sınır

WEB SLICE 001 sonrasında otomatik olarak feature geliştirmeye geçilmemelidir. Bir sonraki hedef **WEB SLICE 002 — AppShell & Design System** olmalıdır. Bu slice’ta sidebar, topbar, responsive layout, loading/empty/error/permission-denied state’leri ve ortak `QuantityViewToggle` bileşeni uygulanacaktır.

Flutter mobil uygulaması bu slice’ın kapsamına dahil edilmemiştir. Mobil temel proje yapısı, Riverpod state planı, bağlantı durumu, barcode ve quantity state sözleşmeleri ayrı mobile slice olarak ele alınmalıdır. Flutter SDK sandbox’ta mevcut olmadığı için mobil build gate’i ayrıca kurulacaktır.

## Final report

```text
WEB SLICE 001

STATUS: PASS

Install: PASS
Typecheck: PASS
Lint: PASS
Build: PASS
Backend Build: PASS
Backend Tests: PASS

Files Changed:
apps/web/*
pnpm-workspace.yaml
pnpm-lock.yaml
.gitignore
design/implementation-web-mobile-slice.md

Issues:
Flutter SDK ve mobil implementation bu slice’ın dışındadır.
AppShell, auth, API client, public catalog ve business screen henüz yapılmamıştır.

Next Slice:
WEB SLICE 002 — AppShell & Design System
```

## References

[1]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/design/visual-design-system.md "Factory ERP visual design system"

[2]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/design/web-ux-architecture.md "Factory ERP web UX architecture"

[3]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/design/architecture-api-contracts.md "Factory ERP API contract architecture"

[4]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/AGENTS.md "Factory ERP repository agent instructions"
