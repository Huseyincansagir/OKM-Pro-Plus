import { describe, expect, it } from "vitest";
import { requiresIdempotencyKey } from "@/lib/api/headers";

describe("requiresIdempotencyKey", () => {
  it("requires the header only on mutating command prefixes", () => {
    expect(requiresIdempotencyKey("POST", "/customers")).toBe(true);
    expect(requiresIdempotencyKey("POST", "/quotes")).toBe(true);
    expect(requiresIdempotencyKey("POST", "/price-lists")).toBe(true);
    expect(requiresIdempotencyKey("POST", "/customers/1/contacts")).toBe(true);
    expect(requiresIdempotencyKey("POST", "/quotes/1/issue")).toBe(true);
    expect(requiresIdempotencyKey("POST", "/orders")).toBe(true);
    expect(requiresIdempotencyKey("POST", "/orders/1/approve")).toBe(true);
    expect(requiresIdempotencyKey("POST", "/stock-counts")).toBe(true);
    expect(requiresIdempotencyKey("POST", "/stock-counts/1/complete")).toBe(true);
    expect(requiresIdempotencyKey("POST", "/employees")).toBe(true);
    expect(requiresIdempotencyKey("POST", "/warehouse-transfers/1/complete")).toBe(true);
    expect(requiresIdempotencyKey("POST", "/dispatch-runs/1/confirm")).toBe(true);
    expect(requiresIdempotencyKey("GET", "/employees")).toBe(false);
    expect(requiresIdempotencyKey("GET", "/orders/1")).toBe(false);
    expect(requiresIdempotencyKey("POST", "/auth/login")).toBe(false);
    expect(requiresIdempotencyKey("POST", "/public/quote-requests")).toBe(false);
    expect(requiresIdempotencyKey("POST", "/public/catalog/products")).toBe(false);
  });
});
