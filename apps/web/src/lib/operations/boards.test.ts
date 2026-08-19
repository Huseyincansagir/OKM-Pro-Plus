import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";
import { listProductionOrders, listShipments, listTransfers, mapProduction } from "@/lib/operations/boards";

vi.mock("@/lib/api/client", () => ({
  apiRequest: vi.fn(),
}));

describe("operations boards", () => {
  beforeEach(() => {
    vi.mocked(apiRequest).mockReset();
  });

  it("lists transfers from GET /warehouse-transfers", async () => {
    vi.mocked(apiRequest).mockResolvedValue([
      { id: "t1", productId: "p1", status: "Draft", quantityBase: 10, enteredQuantity: 1 },
    ]);
    const rows = await listTransfers();
    expect(apiRequest).toHaveBeenCalledWith({ path: "/warehouse-transfers", method: "GET" });
    expect(rows[0].quantityBase).toBe(10);
  });

  it("maps remainingQuantityBase from the server", () => {
    const mapped = mapProduction({
      id: "po1",
      plannedQuantityBase: 100,
      completedQuantityBase: 40,
      remainingQuantityBase: 60,
      onHand: 999,
    });
    expect(mapped.remainingQuantityBase).toBe(60);
    expect(mapped).not.toHaveProperty("onHand");
  });

  it("rejects a non-array shipment payload", async () => {
    vi.mocked(apiRequest).mockResolvedValue({ items: [] });
    await expect(listShipments()).rejects.toBeInstanceOf(ApiError);
    vi.mocked(apiRequest).mockResolvedValue([]);
    await expect(listProductionOrders()).resolves.toEqual([]);
  });
});
