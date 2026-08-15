# Factory ERP Agent Instructions

## Mission

Build and maintain a production-ready factory ERP covering production, warehouse, sales, shipment, invoicing, current accounts, payments, personnel, reports, public product catalog and mobile operations.

## Mandatory workflow

Always reason through the following order for substantial work:

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
TEST
  ↓
SECURITY REVIEW
  ↓
OPERATIONS / DEPLOYMENT
  ↓
RELEASE GATE
```

Use the relevant skills under `.claude/skills/` rather than treating one giant instruction set as the only source of guidance.

## Repository rules

- Read existing code and documentation before changing architecture.
- Keep the numbered `/docs/00`–`/docs/06` package as the design source of truth.
- Keep domain and database decisions synchronized with `docs/01-design/13-decision-log.md`.
- Do not introduce duplicate sources of truth for products, customers, stock, documents or current-account transactions.
- Do not use mock data as the final implementation.
- Do not leave TODO/placeholder business functionality behind.
- Do not silently change established business rules.

## Business integrity

- Never double-invoice a delivery note.
- Never silently double-apply a payment.
- Never permit unauthorized financial or inventory modifications.
- Keep stock and financial history auditable.
- Prefer cancellation/reversal over destructive deletion for financial and stock records.
- Keep critical state transitions transactional and audited.

## UX

- Turkish UI.
- English code/entity/API identifiers.
- Desktop ERP optimized for dense operational information.
- Mobile optimized for barcode, inventory, shipment and production tasks.
- Public catalog must not expose internal cost, risk or operational information.

## Quality gate

A feature is not complete until its database, backend, authorization, UI/mobile, validation, tests, audit behavior and documentation are complete.

Before declaring completion, run the relevant build, type-check, migration, automated tests and integration checks.
