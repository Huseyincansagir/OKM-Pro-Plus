import { describe, expect, it, vi } from "vitest";
import { apiRequest } from "@/lib/api/client";
import { completeStockCount, mapStockCount } from "@/lib/warehouse/counts";

vi.mock("@/lib/api/client", () => ({
  apiRequest: vi.fn(),
}));

describe("stock counts", () => {
  it("keeps server variance and does not invent on-hand", () => {
    const mapped = mapStockCount({
      id: "c1",
      documentNumber: "CNT-1",
      status: "Draft",
      items: [{ id: "i1", countedQtyBase: 10, systemOnHandQtyBase: 8, varianceQtyBase: 2, onHand: 0 }],
    });
    expect(mapped.items[0].varianceQtyBase).toBe(2);
    expect(mapped.items[0]).not.toHaveProperty("onHand");
  });

  it("completes POST /stock-counts/{id}/complete", async () => {
    vi.mocked(apiRequest).mockResolvedValue({ id: "c1", status: "Completed", items: [] });
    await completeStockCount("c1");
    expect(apiRequest).toHaveBeenCalledWith({
      path: "/stock-counts/c1/complete",
      method: "POST",
      idempotent: true,
    });
  });
});
