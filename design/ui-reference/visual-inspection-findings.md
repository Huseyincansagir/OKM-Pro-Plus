# UI Reference Visual Inspection Findings

**İncelenen görseller:** `web-dashboard.png`, `web-order-detail.png`
**Tarih:** 2026-08-16

## Dashboard

The dashboard renders at 1440×900 without visible clipping. The deep navy sidebar, white topbar, teal primary action and pale canvas establish a clear visual hierarchy. KPI cards have consistent height and readable numeric emphasis. The chart, pending-work list, recent-orders table and risk list align correctly and preserve a calm information density.

The implementation reference must preserve the 248px sidebar, 73px topbar, 31px desktop content gutter, 5-column KPI rhythm, 16px card gaps, 14–16px card radius and the teal/amber/red/green semantic status language. Table and task-list rows remain scannable at the reference size.

## Order detail + approval modal

The approval modal correctly establishes backdrop separation, centered white modal, close affordance, two-column summary fields, warning strip and a clear primary confirmation action. The underlying page remains legible enough to preserve context: document number, order state, stepper, tabs, items and approval summary are still recognizable under the dim layer.

The implementation reference must preserve the modal hierarchy and must not move business impact information below the primary action. The warning strip must remain visually distinct from the standard form fields, and the approval action must use the same teal primary button as the dashboard.

## General decision

The fixed desktop reference is suitable as a pixel comparison baseline. Future coded screens should be captured at 1440×900 for desktop and the fixed phone artboard size used by the mobile references. All common components must derive from the same tokens and spacing values rather than receive page-specific variants.

## Warehouse and logistics workspace

The logistics reference preserves the same shell and desktop scale while introducing a full-height right drawer with a dimmed workspace backdrop. The stock table, capacity summary and route board remain visible behind the drawer, which keeps operational context while allowing package-level verification. The drawer’s sections are separated by thin rules and its footer stays action-oriented.

The implementation reference must preserve the drawer width, top offset below the topbar, backdrop opacity and sticky footer behavior. Capacity values, route stops and package contents must remain visible as separate information groups; no single color or badge should carry multiple meanings.

## Component atlas

The component atlas renders the shared interaction language coherently: active tabs have a teal underline, toggles use a light neutral track with a raised white selected item, form focus is teal, form error is red, and popovers stay visually close to their anchor. Empty, error and permission states share the same explanatory structure and primary next action.

The implementation reference must treat the atlas as a component contract. Page-specific components should compose these primitives rather than create visually similar one-off versions. Critical, warning and destructive modal levels must remain distinguishable through background treatment, copy and action color.

## Mobile barcode and quantity flow

The mobile reference renders three phones as a single flow: scan → product result → action selection. The phone header remains deep navy, the bottom navigation remains persistent, and the primary action stays within the lower thumb-reach area. The quantity toggle is compact but readable, and the base quantity is always shown beside the selected packaging quantity.

The implementation reference must keep the flow linear, avoid hidden quantity conversion, and preserve the distinction between view mode and entered quantity. Permission-gated stock correction remains visible as a disabled/limited action rather than disappearing from the mental model.

## Public catalog and quote basket

The public reference is visually separated from the internal ERP through a lighter, more editorial surface and larger hero typography while retaining the same teal accent family. Product cards use a clear image area, short metadata, packaging-aware quantity and a single primary add-to-quote action. The right cart drawer maintains the product context and ends with the next-step CTA.

The implementation reference must preserve the quote-only message, show packaging and base-unit equivalents, and never imply that a public quote request is a confirmed order. The cart drawer must remain usable without hiding the page context entirely.

## Mobile production and delivery

The production and delivery references keep operational forms short, vertical and action-focused. Progress is shown before the entry form; the form exposes only the fields needed for the next record; stock impact is explained before save. Delivery keeps package verification, partial-delivery and exception actions visible together.

The implementation reference must preserve this sequence and avoid converting the mobile app into a compressed desktop table. Primary actions occupy the lower action zone, while destructive and exception actions remain visually separate.

## Production and finance desktop

The production reference confirms that the common shell supports a high-density kanban without losing readability. Status columns have light semantic backgrounds, active progress cards use teal emphasis, and machine/finance summaries remain below the primary operational workspace.

The implementation reference must preserve the five-column kanban rhythm, compact card hierarchy and independent filters. Finance summaries should remain auditable and visually secondary to the operational state when shown on the same page.
