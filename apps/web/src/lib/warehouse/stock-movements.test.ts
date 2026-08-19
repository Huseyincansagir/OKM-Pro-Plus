import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";
import {
  getStockMovement,
  listStockMovements,
  mapStockMovement,
  movementEffectKind,
  movementEffectLabel,
  movementTypeLabel,
} from "@/lib/warehouse/stock-movements";

vi.mock("@/lib/api/client", () => ({
  apiRequest: vi.fn(),
}));

describe("mapStockMovement", () => {
  it("keeps server quantityBase and does not invent a signed total", () => {
    const mapped = mapStockMovement({
      id: "m1",
      productCode: "NAP-001",
      movementType: "ProductionIn",
      effect: "In",
      quantityBase: 40,
      fakeSigned: -40,
    });
    expect(mapped.quantityBase).toBe(40);
    expect(mapped.effect).toBe("In");
    expect(mapped).not.toHaveProperty("fakeSigned");
  });

  it("leaves quantity null when the server omits it", () => {
    const mapped = mapStockMovement({ id: "m1", movementType: "DeliveryIssue" });
    expect(mapped.quantityBase).toBeNull();
    expect(mapped.effect).toBe("");
    expect(mapped.reversedFromId).toBeNull();
  });
});

describe("movement labels", () => {
  it("maps known types and leaves unknown types as-is", () => {
    expect(movementTypeLabel("WarehouseTransferOut")).toBe("Transfer çıkışı");
    expect(movementTypeLabel("WarehouseTransferIn")).toBe("Transfer girişi");
    expect(movementTypeLabel("ProductionIn")).toBe("Üretim girişi");
    expect(movementTypeLabel("DeliveryIssue")).toBe("İrsaliye çıkışı");
    expect(movementTypeLabel("CountIn")).toBe("Sayım girişi");
    expect(movementTypeLabel("CountOut")).toBe("Sayım çıkışı");
    expect(movementTypeLabel("FutureAdjust")).toBe("FutureAdjust");
    expect(movementEffectLabel("In")).toBe("Giriş");
    expect(movementEffectLabel("Out")).toBe("Çıkış");
    expect(movementEffectLabel("Unknown")).toBe("Yön yok");
    expect(movementEffectKind("In")).toBe("success");
    expect(movementEffectKind("Out")).toBe("pending");
    expect(movementEffectKind("Unknown")).toBe("info");
  });
});

describe("listStockMovements", () => {
  beforeEach(() => {
    vi.mocked(apiRequest).mockReset();
  });

  it("calls GET /stock-movements and rejects a non-array payload", async () => {
    vi.mocked(apiRequest).mockResolvedValue([{ id: "m1", quantityBase: 12 }]);
    const rows = await listStockMovements();
    expect(apiRequest).toHaveBeenCalledWith({ path: "/stock-movements", method: "GET" });
    expect(rows[0].quantityBase).toBe(12);

    vi.mocked(apiRequest).mockResolvedValue({ items: [] });
    await expect(listStockMovements()).rejects.toBeInstanceOf(ApiError);
  });
});

describe("getStockMovement", () => {
  beforeEach(() => {
    vi.mocked(apiRequest).mockReset();
  });

  it("calls GET /stock-movements/{id}", async () => {
    vi.mocked(apiRequest).mockResolvedValue({
      id: "m1",
      movementType: "DeliveryIssue",
      effect: "Out",
      quantityBase: 8,
    });
    const row = await getStockMovement("m1");
    expect(apiRequest).toHaveBeenCalledWith({ path: "/stock-movements/m1", method: "GET" });
    expect(row.effect).toBe("Out");
    expect(row.quantityBase).toBe(8);
  });
});
