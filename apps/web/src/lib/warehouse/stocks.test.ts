import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";
import { listStocks, mapStockRow } from "@/lib/warehouse/stocks";

vi.mock("@/lib/api/client", () => ({
  apiRequest: vi.fn(),
}));

describe("mapStockRow", () => {
  it("keeps server availableQtyBase and does not invent on-hand", () => {
    const mapped = mapStockRow({
      id: "s1",
      productCode: "NAP-001",
      productName: "Peçete",
      warehouseCode: "MAIN",
      onHandQtyBase: 100,
      reservedQtyBase: 20,
      availableQtyBase: 80,
      fakeAvailable: 0,
    });
    expect(mapped.availableQtyBase).toBe(80);
    expect(mapped.onHandQtyBase).toBe(100);
    expect(mapped).not.toHaveProperty("fakeAvailable");
  });

  it("leaves totals null when the server omits them", () => {
    const mapped = mapStockRow({ id: "s1", productCode: "NAP-001" });
    expect(mapped.onHandQtyBase).toBeNull();
    expect(mapped.availableQtyBase).toBeNull();
  });
});

describe("listStocks", () => {
  beforeEach(() => {
    vi.mocked(apiRequest).mockReset();
  });

  it("calls GET /stocks and rejects a non-array payload", async () => {
    vi.mocked(apiRequest).mockResolvedValue([{ id: "s1", productCode: "NAP-001" }]);
    const rows = await listStocks();
    expect(apiRequest).toHaveBeenCalledWith({ path: "/stocks", method: "GET" });
    expect(rows[0].productCode).toBe("NAP-001");

    vi.mocked(apiRequest).mockResolvedValue({ items: [] });
    await expect(listStocks()).rejects.toBeInstanceOf(ApiError);
  });
});
