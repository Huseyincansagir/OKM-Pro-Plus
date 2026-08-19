import { describe, expect, it, vi } from "vitest";
import { apiRequest } from "@/lib/api/client";
import { mapBarcodeResolution, resolveBarcode } from "@/lib/catalog/barcode";

vi.mock("@/lib/api/client", () => ({
  apiRequest: vi.fn(),
}));

describe("barcode resolve", () => {
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

  it("posts to /mobile/barcodes/resolve", async () => {
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
  });
});
