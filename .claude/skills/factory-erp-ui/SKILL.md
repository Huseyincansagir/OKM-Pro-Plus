---
name: factory-erp-ui
description: Factory ERP web ve mobile-web UI kuralları. AppShell, design system, DataTable, form, quantity toggle, state, accessibility, responsive ve UX review için kullan. Tetikleyiciler: UI, UX, AppShell, DataTable, QuantityViewToggle, design system, erişilebilirlik, responsive, WEB SLICE. /factory-erp-ui
---

# Factory ERP UI

Web ve ilerideki mobile-web ekranlarını aynı tasarım diliyle üret. Bu skill kod şablonu değildir; ajanın uyması gereken tekrar kullanılabilir kurallardır.

## Kaynak hiyerarşisi

Çelişkide sırayı bozma:

1. `AGENTS.md`
2. `design/decision-log.md` ve ADR
3. Canonical design: `design/visual-design-system.md`, `design/web-ux-architecture.md`, `design/master-screen-inventory.md`, `design/mobile-toggle-api-and-schema.md`, `design/ui-reference/COMPONENT-IMPLEMENTATION.md`
4. Slice planı: `design/implementation-web-mobile-slice.md`
5. Mevcut production kod (`apps/web` token ve bileşenleri)
6. `design/ui-reference/tokens.json` (mockup ölçü/referans; WEB 001 token’larını sessizce değiştirme)
7. Bu skill

Renk, spacing veya component semantiğini yeniden icat etme. Hex değerlerinin kod kaynağı `apps/web/tailwind.config.ts` içindeki WEB 001 token’larıdır. `tokens.json` ile fark görürsen yeni palet ekleme; `DESIGN CONFLICT` olarak raporla.

## Okuma sırası

İşin türüne göre yalnızca ilgili referansı aç:

| İş | Referans |
|---|---|
| Hiyerarşi, yoğunluk, primary/destructive, status, tutarlılık | `references/design-principles.md` |
| AppShell, nav, sayfa iskeleti, kırılımlar | `references/layout.md` |
| Button, form, tablo, overlay, badge | `references/components.md` |
| Yoğun tablo, filtre, toplu işlem, KPI, finans UI | `references/data-heavy-erp.md` |
| `viewMode` / miktar / ambalaj | `references/quantity.md` |
| Form etkileşimi ve async state | `references/interaction-and-states.md` |
| Klavye, focus, ARIA, kontrast | `references/accessibility.md` |
| Implementation sonrası inceleme | `references/review.md` |

## Sert kurallar

- UI Türkçe; entity, prop, route parametresi ve API alanı İngilizce.
- Her ekranda kullanıcı nerede olduğunu, ne gördüğünü, ne yapabileceğini ve işlemin sonucunu tek bakışta anlamalı.
- Tek dominant primary action. Destructive işlem açık confirmation ister.
- Status = metin + renk + ikon. Yalnızca renk yasak.
- `viewMode ≠ operationPackagingId`. `QuantityViewToggle` yalnızca görünümü değiştirir. Ayrıntı: `references/quantity.md`.
- Frontend conversion, `quantityBase` veya ledger değeri üretemez.
- Frontend permission check UX içindir; yetki backend’dedir.
- Backend’de olmayan endpoint için sahte başarı gösterme.
- Her feature: loading, empty, error, permission, conflict, submitting, success. Happy path yetmez.
- Native Flutter bu skill’in kapsamı dışındadır; mobile-web drawer/responsive kapsamdadır.

## Bileşen evı

Yeni görsel primitive eklemeden önce `apps/web/src/components/` altında mevcut bileşeni ara. Yoksa `design/ui-reference/COMPONENT-IMPLEMENTATION.md` sözleşmesine uygun ekle; kopya variant üretme.

## Definition of Done (UI)

- Canonical design kararına uyuyor
- Quantity invariantı bozulmuyor
- Keyboard + focus + görünür focus-visible
- 320 / 768 / 1024 / 1280 davranışları tanımlı
- State’ler (loading/empty/error/permission) var
- Test yoksa kritik kural PASS sayılmaz
- `design/implementation-web-mobile-slice.md` güncellendi
