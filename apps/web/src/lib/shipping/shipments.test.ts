import { describe, expect, it, vi } from "vitest";
import { apiRequest } from "@/lib/api/client";
import { createShipment, mapShipmentDetail, shipmentStatusKind } from "@/lib/shipping/shipments";

vi.mock("@/lib/api/client", () => ({
  apiRequest: vi.fn(),
}));

describe("shipments", () => {
  it("maps Preparing/Loaded/InTransit without inventing Delivered", () => {
    expect(shipmentStatusKind("Preparing")).toBe("pending");
    expect(shipmentStatusKind("Loaded")).toBe("active");
    expect(shipmentStatusKind("InTransit")).toBe("active");
    const mapped = mapShipmentDetail({
      id: "s1",
      status: "Preparing",
      items: [{ id: "i1", quantityBase: 2000 }],
    });
    expect(mapped.status).toBe("Preparing");
    expect(mapped.items[0].quantityBase).toBe(2000);
  });

  it("creates a shipment with the delivery-note row version", async () => {
    vi.mocked(apiRequest).mockReset();
    vi.mocked(apiRequest).mockResolvedValue({ id: "s1", status: "Preparing", items: [] });
    await createShipment({ deliveryNoteId: "d1", expectedDeliveryNoteRowVersion: 3 });
    expect(apiRequest).toHaveBeenCalledWith({
      path: "/shipments",
      method: "POST",
      body: { deliveryNoteId: "d1", expectedDeliveryNoteRowVersion: 3 },
      idempotent: true,
    });
  });
});
