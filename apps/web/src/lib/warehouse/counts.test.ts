import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";
import { addStockCountItem, completeStockCount, listStockCounts, mapStockCount } from "@/lib/warehouse/counts";

vi.mock("@/lib/api/client", () => ({
  apiRequest: vi.fn(),
}));

describe("stock counts", () => {
  beforeEach(() => {
    vi.mocked(apiRequest).mockReset();
  });

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

  it("adds an item with countedQtyBase only", async () => {
    vi.mocked(apiRequest).mockResolvedValue({
      id: "c1",
      status: "Draft",
      items: [{ id: "i1", countedQtyBase: 12, systemOnHandQtyBase: 10, varianceQtyBase: 2 }],
    });
    await addStockCountItem("c1", { productId: "p1", countedQtyBase: 12 });
    expect(apiRequest).toHaveBeenCalledWith({
      path: "/stock-counts/c1/items",
      method: "POST",
      body: { productId: "p1", countedQtyBase: 12 },
      idempotent: true,
    });
    const body = vi.mocked(apiRequest).mock.calls[0][0].body as Record<string, unknown>;
    expect(body).not.toHaveProperty("onHandQtyBase");
    expect(body).not.toHaveProperty("varianceQtyBase");
  });

  it("lists GET /stock-counts and rejects a wrapper payload", async () => {
    vi.mocked(apiRequest).mockResolvedValue([{ id: "c1", documentNumber: "CNT-1", items: [] }]);
    const rows = await listStockCounts();
    expect(apiRequest).toHaveBeenCalledWith({ path: "/stock-counts", method: "GET" });
    expect(rows[0].documentNumber).toBe("CNT-1");
    vi.mocked(apiRequest).mockResolvedValue({ items: [] });
    await expect(listStockCounts()).rejects.toBeInstanceOf(ApiError);
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
