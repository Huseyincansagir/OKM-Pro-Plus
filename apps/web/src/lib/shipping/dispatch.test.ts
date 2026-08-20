import { beforeEach, describe, expect, it, vi } from "vitest";
import { apiRequest } from "@/lib/api/client";
import {
  confirmDispatch,
  createRoutePlan,
  deliverStop,
  listRoutePlans,
  mapRoutePlan,
} from "@/lib/shipping/dispatch";

vi.mock("@/lib/api/client", () => ({
  apiRequest: vi.fn(),
}));

describe("route plans", () => {
  beforeEach(() => {
    vi.mocked(apiRequest).mockReset();
  });

  it("maps stops without inventing delivery", () => {
    const mapped = mapRoutePlan({
      id: "r1",
      status: "Draft",
      version: 1,
      rowVersion: 2,
      stops: [{ id: "s1", sequenceNo: 1, status: "Pending" }],
      delivered: true,
    });
    expect(mapped.status).toBe("Draft");
    expect(mapped.stops[0].status).toBe("Pending");
    expect(mapped).not.toHaveProperty("delivered");
  });

  it("lists GET /shipments/{id}/route-plans", async () => {
    vi.mocked(apiRequest).mockResolvedValue([{ id: "r1", status: "Draft", stops: [] }]);
    const rows = await listRoutePlans("sh1");
    expect(apiRequest).toHaveBeenCalledWith({ path: "/shipments/sh1/route-plans", method: "GET" });
    expect(rows[0].id).toBe("r1");
  });

  it("sends If-Match on confirm and recipient on deliver", async () => {
    vi.mocked(apiRequest).mockResolvedValue({
      id: "d1",
      status: "Dispatched",
      rowVersion: 2,
      stops: [],
    });
    await confirmDispatch("d1", 7);
    expect(apiRequest).toHaveBeenCalledWith({
      path: "/dispatch-runs/d1/confirm",
      method: "POST",
      ifMatch: "7",
      idempotent: true,
    });
    vi.mocked(apiRequest).mockResolvedValue({
      id: "d1",
      status: "InTransit",
      rowVersion: 3,
      stops: [{ routeStopId: "st1", sequenceNo: 1, status: "Delivered", proofRecipient: "Ali" }],
    });
    await deliverStop("d1", "st1", { recipientName: "Ali", rowVersion: 3 });
    const call = vi.mocked(apiRequest).mock.calls.at(-1)?.[0];
    expect(call?.path).toBe("/dispatch-runs/d1/stops/st1/deliver");
    expect(call?.ifMatch).toBe("3");
    expect(call?.body).toMatchObject({ recipientName: "Ali" });
    expect(call?.body as Record<string, unknown>).not.toHaveProperty("quantityBase");
  });

  it("creates a route plan with shipment row version", async () => {
    vi.mocked(apiRequest).mockResolvedValue({ id: "r1", status: "Draft", rowVersion: 1, stops: [] });
    await createRoutePlan("sh1", 4);
    expect(apiRequest).toHaveBeenCalledWith({
      path: "/shipments/sh1/route-plans",
      method: "POST",
      body: { expectedShipmentRowVersion: 4, plannedStartAt: null, plannedEndAt: null },
      idempotent: true,
    });
  });
});
