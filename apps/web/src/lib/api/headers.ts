export function createCorrelationId(): string {
  return crypto.randomUUID();
}

export function createIdempotencyKey(): string {
  return crypto.randomUUID();
}

export const IDEMPOTENT_PREFIXES = [
  "/orders",
  "/delivery-notes",
  "/invoices",
  "/payments",
  "/shipments",
  "/production",
  "/quote-requests",
  "/warehouse-transfers",
  "/vehicle-types",
  "/vehicles",
  "/drivers",
  "/route-plans",
  "/load-plans",
  "/load-verification",
  "/dispatch-runs",
] as const;

export function requiresIdempotencyKey(method: string, path: string): boolean {
  if (method !== "POST") {
    return false;
  }
  const normalized = path.startsWith("/") ? path : `/${path}`;
  if (normalized.startsWith("/public/")) {
    return false;
  }
  return IDEMPOTENT_PREFIXES.some(
    (prefix) => normalized === prefix || normalized.startsWith(`${prefix}/`),
  );
}
