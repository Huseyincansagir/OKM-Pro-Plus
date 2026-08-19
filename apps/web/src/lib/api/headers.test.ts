import { describe, expect, it } from "vitest";
import { requiresIdempotencyKey } from "@/lib/api/headers";

describe("requiresIdempotencyKey", () => {
  it("requires the header only on mutating command prefixes", () => {
    expect(requiresIdempotencyKey("POST", "/customers")).toBe(true);
    expect(requiresIdempotencyKey("POST", "/orders")).toBe(true);
    expect(requiresIdempotencyKey("POST", "/orders/1/approve")).toBe(true);
    expect(requiresIdempotencyKey("GET", "/orders/1")).toBe(false);
    expect(requiresIdempotencyKey("POST", "/auth/login")).toBe(false);
    expect(requiresIdempotencyKey("POST", "/public/quote-requests")).toBe(false);
    expect(requiresIdempotencyKey("POST", "/public/catalog/products")).toBe(false);
  });
});
