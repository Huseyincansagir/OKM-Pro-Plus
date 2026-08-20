import { describe, expect, it, vi } from "vitest";
import { apiRequest } from "@/lib/api/client";
import { createRoutePlan, listRoutePlans, mapRoutePlan } from "@/lib/shipping/dispatch";

vi.mock("@/lib/api/client", () => ({
  apiRequest: vi.fn(),
}));

describe("route plans", () => {
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
