/**
 * Canonical viewMode values from design/mobile-toggle-api-and-schema.md.
 * Do not substitute Agent-prompt aliases (base/transaction/packaging).
 */
export type QuantityViewMode = "BaseUnit" | "Packaging" | "Breakdown";

export const QUANTITY_VIEW_MODES: readonly QuantityViewMode[] = [
  "BaseUnit",
  "Packaging",
  "Breakdown",
] as const;

export const QUANTITY_VIEW_MODE_LABELS: Record<QuantityViewMode, string> = {
  BaseUnit: "Temel Birim",
  Packaging: "Ambalaj",
  Breakdown: "Kırılım",
};
