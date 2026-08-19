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

## WEB SLICE 002 — AppShell & Design System

**Tarih:** 2026-08-19
**Durum:** PASS (web gates). Backend compile PASS. Postgres entegrasyon testleri bu makinede koşmadı.
**Kapsam:** `apps/web` AppShell, responsive layout, ortak bileşenler, quantity UI ve Vitest foundation. Auth, API client, public catalog, dashboard iş verisi ve native mobile yok.
**Baseline:** WEB SLICE 001 PASS (`3da3bfc`). Sonraki commit’ler yalnızca docs idi; WEB 002 bu oturumda uygulandı.

## 1. Uygulanan yüzey

| Alan | Karar |
|---|---|
| AppShell | Sidebar + Topbar + Breadcrumb + PageHeader + content |
| Responsive | ≥1024 expanded; 768–1023 collapsible (viewport değişince default collapsed); <768 drawer + Escape + inert + focus trap |
| Design tokens | WEB 001 Tailwind ad/değerleri korundu; yeni palet yok |
| Quantity | Canonical `BaseUnit \| Packaging \| Breakdown` |
| DataTable | Controlled foundation; API yok |
| Preview route | `/` tasarım sistemi önizlemesi; iş verisi bağlı değil |
| Test | Vitest + Testing Library (`pnpm --dir apps/web test`) |

Vitest, Agent A promptundaki “yeni framework kurma” yasağına karşı master prompt ve quantity invariant test zorunluluğu için slice altyapısı olarak eklendi.

## 2. Bileşenler

AppShell: `AppShell`, `Sidebar`, `Topbar`, `Breadcrumb`, `PageHeader`, `UserMenu`, `NotificationArea`, `ConnectionStatus`.

Ortak: `Button`, `IconButton`, `Input`, `Select`, `Checkbox`, `Badge`, `StatusBadge`, `Card`, `Divider`, `Tooltip`, `DropdownMenu`, `Dialog`, `Drawer`, `Tabs`, `Skeleton`, `Spinner`, `Toast`, `Alert`, `DataTable`.

Durum: `EmptyState`, `ErrorState`, `PermissionDenied`.

Miktar: `QuantityViewToggle`, `QuantityEntryPreview`.

Dialog/Drawer: focus trap, Escape, `aria-modal`, `useId`, focus return, body scroll lock.

## 3. Quantity invariantı

`QuantityViewToggle` yalnızca `viewMode` değiştirir. `onViewModeChange` yeni mode alır. `operationPackagingId` prop olarak kabul edilir, okunmaz, mutate edilmez. Girilen miktar, `quantityBase` ve stok değişmez. Ok tuşları da yalnızca `viewMode` değiştirir.

`QuantityEntryPreview` çarpma/bölme/yuvarlama yapmaz; verilen canonical sonucu gösterir.

Önizleme sayfası `viewMode` için önceden hesaplanmış `display.*` fixture string’leri seçer; `5 × katsayı` üretmez.

Agent A promptundaki `base | transaction | packaging` kullanılmadı. Kaynak: `design/mobile-toggle-api-and-schema.md`.

## 4. DESIGN CONFLICT — token paleti

`design/ui-reference/tokens.json` hex değerleri WEB 001 `tailwind.config.ts` ile farklıdır. Bu slice yeni palet eklemedi; runtime token kaynağı WEB 001’dir. Palette birleştirme ayrı tasarım kararı ister.

| Anlamsal | WEB 001 (korunan) | tokens.json (benimsenmedi) |
|---|---|---|
| Navy | `#102A43` | `#10263F` |
| Teal | `#0F9D9A` | `#10B5A2` |
| Amber | `#D97706` | `#ECAC3C` |
| Danger | `#DC2626` | `#E15C63` |
| Success | `#16A34A` | `#29A36A` |

## 5. UI skill sistemi

`.claude/skills/factory-erp-ui/` eklendi. Tek skill, referans dosyalarıyla: design-principles, layout, components, data-heavy-erp, quantity, interaction-and-states, accessibility, review. Canonical design belgelerini tekrar etmez; ajan kurallarını taşır.

## 6. Test kapsamı

Komut: `pnpm --dir apps/web test` (Vitest). İzleme: `pnpm --dir apps/web test:watch`.
Paylaşılan jsdom yardımcısı: `apps/web/src/test/viewport.ts`. Quantity harness kalıcı state kullanır; her render’da sıfırlanan object ile invariant gizlenmez.

| Dosya | Doğrulama |
|---|---|
| `viewport.test.ts` | 320 / 768 / 1024 kırılımları |
| `quantity-view-toggle.test.tsx` | Yalnızca viewMode; ok tuşu; disabled; aynı moda tıklama |
| `quantity-entry-preview.test.tsx` | Client conversion yok; çelişkili canonical değer korunur |
| `design-system-preview.test.tsx` | Preview display.* seçer; işlem miktarı/stok aynı kalır |
| `app-shell.test.tsx` | Render, desktop collapse, tablet collapsed, mobile Escape |
| `connection-status.test.tsx` | Bağlı / çevrimdışı |
| `dialog.test.tsx` / `drawer.test.tsx` | Focus, Escape, focus return |
| `dropdown-menu.test.tsx` | Aç / seç / Escape |
| `data-table.test.tsx` | loading / empty / error / sort / selection / pagination |
| `form-controls.test.tsx` | aria-invalid, describedby, hint gizleme |
| `button.test.tsx` | loading disabled + aria-busy |
| `tabs.test.tsx` | aria-selected değişimi |
| `status-badge.test.tsx` | Altı status: metin + ikon |
| `states.test.tsx` | Empty / retry / permission |

## 7. Gate sonuçları

| Kontrol | Sonuç |
|---|---|
| `pnpm --dir apps/web typecheck` | PASS |
| `pnpm --dir apps/web lint` | PASS |
| `pnpm --dir apps/web test` | PASS; 40 test |
| `pnpm --dir apps/web build` | PASS; Next.js 15.5.23 (önceki oturum doğrulaması + bu oturum typecheck/lint/test) |
| `dotnet build FactoryErp.sln --configuration Release` | PASS; 0 warning / 0 error |
| `dotnet test FactoryErp.sln` | Domain 122 PASS, Architecture 5 PASS. Infrastructure 48 PASS / 30 FAIL: `127.0.0.1:5432` yok. WEB 002 backend’e dokunmadı. Bu makinede .NET 8 runtime yok; testler `DOTNET_ROLL_FORWARD=LatestMajor` ile koştu. |
| `git diff --check` | Bu oturum commit öncesi doğrulanır |

## 8. Sınır

- `/giris`, auth/session, API client: WEB SLICE 003
- Public catalog: WEB SLICE 004
- Dashboard business data: WEB SLICE 005
- Native Flutter: ayrı mobile slice
- `tokens.json` hex migration: açık DESIGN CONFLICT
- Tabs ok-tuşu / dropdown menü roving tabindex: sonraki UI polish
- İlk boyamada JS viewport (SSR default desktop): bilinen responsive sınır

## Final report

```text
WEB SLICE 002

STATUS: PASS

Typecheck: PASS
Lint: PASS
Tests: PASS (40)
Build: PASS
Backend Build: PASS
Backend Tests: Domain+Architecture PASS; Infrastructure integration BLOCKED (no local Postgres)

Next Slice:
WEB SLICE 003 — Auth & API Client
```

## References

[1]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/design/visual-design-system.md "Factory ERP visual design system"

[2]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/design/web-ux-architecture.md "Factory ERP web UX architecture"

[3]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/design/architecture-api-contracts.md "Factory ERP API contract architecture"

[4]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/design/mobile-toggle-api-and-schema.md "Canonical QuantityViewMode"

[5]: https://github.com/Huseyincansagir/OKM-Pro-Plus/blob/main/AGENTS.md "Factory ERP repository agent instructions"

## WEB SLICE 003 — Auth & API Client

**Tarih:** 2026-08-19
**Durum:** PASS (web gates)
**Kapsam:** `/giris`, session bootstrap, BFF token storage, typed API client, error normalizer, 401 single-flight refresh. Public catalog, dashboard iş verisi ve native mobile yok.
**Baseline:** WEB SLICE 002 PASS.

### 1. Sözleşme

Kaynak: mevcut `AuthController` ve `design/web-slice-003-api-contract-review.md`.

| Endpoint | Client yüzeyi |
|---|---|
| `POST /api/v1/auth/login` | Next BFF `POST /api/auth/login` |
| `POST /api/v1/auth/refresh` | Next BFF `POST /api/auth/refresh` |
| `POST /api/v1/auth/logout` | Next BFF `POST /api/auth/logout` |
| `GET /api/v1/auth/me` | Next BFF `GET /api/auth/me` |
| Diğer `/api/v1/*` | Next proxy `app/api/v1/[...path]`; `auth/login` ve `auth/refresh` proxy’den kapalı |

Backend CORS yok; tarayıcı doğrudan API origin’ine gitmez.

### 2. Token kararı

Ham `refreshToken` JSON’da mevcuttur (backend). WEB 003 bunu tarayıcıya iletmez.

- Access ve refresh: `HttpOnly`, `SameSite=Lax` cookie (`fe_access`, `fe_refresh`)
- `localStorage` / `sessionStorage` yok
- Zustand yalnızca user summary + auth status tutar
- Cookie `secure` yalnızca production
- Refresh cookie path: `/api/auth`

Sayfa yenilemede BFF `/api/auth/me` access yoksa refresh cookie ile döner.

### 3. Auth lifecycle

Login → cookie set + memory user → `/`
Access 401 → tek shared refresh → aynı Idempotency-Key ile bir replay
Refresh 401 → session clear → `/giris`
403 → refresh yok; `permission_denied`
Logout → backend revoke denemesi; hata olsa da cookie ve client state temizlenir

### 4. Test

51 web testi. Yeni: error normalizer, secret-stripping, idempotency prefix, 401 single-flight/replay, 403 no-refresh, login form boş/401.

### 5. Gate

| Kontrol | Sonuç |
|---|---|
| `pnpm --dir apps/web typecheck` | PASS |
| `pnpm --dir apps/web lint` | PASS |
| `pnpm --dir apps/web test` | PASS; 51 test |
| `pnpm --dir apps/web build` | PASS; Next.js 15.5.23; `/giris` + auth BFF route’ları |
| Backend kod | Değiştirilmedi |

### 6. Sınır

- Public catalog: WEB SLICE 004
- Dashboard iş verisi: WEB SLICE 005
- CORS/HSTS/rate-limit backend: operations/security slice
- Public quote idempotency backend gap açık
- Signing key production override backend sorumluluğu

```text
WEB SLICE 003
STATUS: PASS
Next Slice: WEB SLICE 004 — Public Catalog
```

