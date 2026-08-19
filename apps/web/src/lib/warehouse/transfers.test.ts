import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";
import { createTransfer, listTransfers, mapTransfer } from "@/lib/warehouse/transfers";

vi.mock("@/lib/api/client", () => ({
  apiRequest: vi.fn(),
}));

describe("mapTransfer", () => {
  it("keeps server quantityBase and does not invent a signed total", () => {
    const mapped = mapTransfer({
      id: "t1",
      productCode: "NAP-001",
      quantityBase: 2000,
      enteredQuantity: 1,
      fakeSigned: -2000,
    });
    expect(mapped.quantityBase).toBe(2000);
    expect(mapped.enteredQuantity).toBe(1);
    expect(mapped).not.toHaveProperty("fakeSigned");
  });

  it("leaves quantity null when omitted", () => {
    expect(mapTransfer({ id: "t1" }).quantityBase).toBeNull();
  });
});

describe("transfer APIs", () => {
  beforeEach(() => {
    vi.mocked(apiRequest).mockReset();
  });

  it("lists GET /warehouse-transfers and rejects a wrapper payload", async () => {
    vi.mocked(apiRequest).mockResolvedValue([{ id: "t1", quantityBase: 10, enteredQuantity: 1 }]);
    const rows = await listTransfers();
    expect(apiRequest).toHaveBeenCalledWith({ path: "/warehouse-transfers", method: "GET" });
    expect(rows[0].quantityBase).toBe(10);
    vi.mocked(apiRequest).mockResolvedValue({ items: [] });
    await expect(listTransfers()).rejects.toBeInstanceOf(ApiError);
  });

  it("creates without sending quantityBase", async () => {
    vi.mocked(apiRequest).mockResolvedValue({ id: "t1", quantityBase: 2000, enteredQuantity: 1 });
    await createTransfer({
      productId: "p1",
      sourceWarehouseId: "w1",
      sourceLocationId: "l1",
      targetWarehouseId: "w1",
      targetLocationId: "l2",
      enteredQuantity: 1,
      enteredPackagingId: "pkg",
      viewMode: "Packaging",
    });
    const call = vi.mocked(apiRequest).mock.calls[0][0];
    expect(call.path).toBe("/warehouse-transfers");
    expect(call.body).not.toHaveProperty("quantityBase");
  });
});
