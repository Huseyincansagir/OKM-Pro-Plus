import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";
import { mapBarcodeResolution, resolveBarcode } from "@/lib/catalog/barcode";

vi.mock("@/lib/api/client", () => ({
  apiRequest: vi.fn(),
}));

describe("barcode resolve", () => {
  beforeEach(() => {
    vi.mocked(apiRequest).mockReset();
  });

  it("maps product and packaging without inventing stock", () => {
    const mapped = mapBarcodeResolution({
      barcode: "869000000001",
      productId: "p1",
      productCode: "NAP-001",
      packagingId: "pkg",
      quantityInBaseUom: 100,
      onHand: 9,
    });
    expect(mapped.productCode).toBe("NAP-001");
    expect(mapped.quantityInBaseUom).toBe(100);
    expect(mapped).not.toHaveProperty("onHand");
  });

  it("posts to /mobile/barcodes/resolve without quantityBase", async () => {
    vi.mocked(apiRequest).mockResolvedValue({
      barcode: "869000000001",
      productId: "p1",
      productCode: "NAP-001",
    });
    await resolveBarcode("869000000001");
    expect(apiRequest).toHaveBeenCalledWith({
      path: "/mobile/barcodes/resolve",
      method: "POST",
      body: { barcode: "869000000001" },
    });
    const body = vi.mocked(apiRequest).mock.calls[0][0].body as Record<string, unknown>;
    expect(body).not.toHaveProperty("quantityBase");
  });

  it("rejects a resolution without productId", async () => {
    vi.mocked(apiRequest).mockResolvedValue({ barcode: "869000000099" });
    await expect(resolveBarcode("869000000099")).rejects.toBeInstanceOf(ApiError);
  });
});
