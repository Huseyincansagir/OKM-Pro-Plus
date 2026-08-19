import { describe, expect, it, vi } from "vitest";
import { apiRequest } from "@/lib/api/client";
import { mapQuantityPreview, previewQuantity } from "@/lib/catalog/quantity-preview";

vi.mock("@/lib/api/client", () => ({
  apiRequest: vi.fn(),
}));

describe("quantity preview", () => {
  it("keeps server quantityBase and does not multiply packaging", () => {
    const mapped = mapQuantityPreview({
      productId: "p1",
      enteredQuantity: 1,
      quantityBase: 2000,
      displayText: "1 Koli (2.000 adet)",
      enteredPackaging: { name: "Koli" },
      inventedBase: 1,
    });
    expect(mapped.quantityBase).toBe(2000);
    expect(mapped.packagingName).toBe("Koli");
    expect(mapped).not.toHaveProperty("inventedBase");
  });

  it("posts to /mobile/quantity-previews without a client base", async () => {
    vi.mocked(apiRequest).mockReset();
    vi.mocked(apiRequest).mockResolvedValue({
      productId: "p1",
      enteredQuantity: 1,
      quantityBase: 2000,
    });
    await previewQuantity({
      productId: "p1",
      enteredQuantity: 1,
      enteredPackagingId: "pkg",
      viewMode: "Packaging",
      operationType: "WarehouseTransfer",
      warehouseId: "w1",
    });
    const call = vi.mocked(apiRequest).mock.calls[0][0];
    expect(call.path).toBe("/mobile/quantity-previews");
    expect(call.body).not.toHaveProperty("quantityBase");
  });
});
