import { describe, expect, it, vi } from "vitest";
import { apiRequest } from "@/lib/api/client";
import { createDeliveryNote, issueDeliveryNote, mapDeliveryNoteDetail } from "@/lib/shipping/delivery-notes";

vi.mock("@/lib/api/client", () => ({
  apiRequest: vi.fn(),
}));

describe("delivery notes", () => {
  it("keeps server quantityBase", () => {
    const mapped = mapDeliveryNoteDetail({
      id: "d1",
      documentNumber: "DN-1",
      status: "Draft",
      items: [{ id: "i1", quantityBase: 2000, enteredQuantity: 1 }],
    });
    expect(mapped.items[0].quantityBase).toBe(2000);
    expect(mapped.documentNumber).toBe("DN-1");
  });

  it("issues POST /delivery-notes/{id}/issue", async () => {
    vi.mocked(apiRequest).mockReset();
    vi.mocked(apiRequest).mockResolvedValue({ id: "d1", status: "Issued", items: [] });
    await issueDeliveryNote("d1");
    expect(apiRequest).toHaveBeenCalledWith({
      path: "/delivery-notes/d1/issue",
      method: "POST",
      idempotent: true,
    });
  });

  it("creates a note with BaseUnit remaining quantities", async () => {
    vi.mocked(apiRequest).mockReset();
    vi.mocked(apiRequest).mockResolvedValue({ id: "d1", status: "Draft", items: [] });
    await createDeliveryNote({
      salesOrderId: "o1",
      items: [{ salesOrderItemId: "i1", enteredQuantity: 10000, enteredPackagingId: null, viewMode: "BaseUnit" }],
    });
    const body = vi.mocked(apiRequest).mock.calls[0][0].body as { items: Array<{ viewMode: string }> };
    expect(body.items[0].viewMode).toBe("BaseUnit");
  });
});
