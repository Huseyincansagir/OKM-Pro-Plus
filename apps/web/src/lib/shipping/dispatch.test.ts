import { beforeEach, describe, expect, it, vi } from "vitest";
import { apiRequest } from "@/lib/api/client";
import {
  completeLoadVerification,
  confirmDispatch,
  createLoadPlan,
  createRoutePlan,
  deliverStop,
  listRoutePlans,
  mapRoutePlan,
  physicalFromSnapshot,
  prepareDispatchRun,
  startLoadVerification,
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

  it("reads physical snapshot without inventing stock", () => {
    expect(physicalFromSnapshot("{}")).toBeNull();
    expect(
      physicalFromSnapshot(
        JSON.stringify({ lengthMm: 1200, widthMm: 800, heightMm: 150, grossWeightKg: 20, volumeM3: 1, onHand: 9 }),
      ),
    ).toMatchObject({ lengthMm: 1200, volumeM3: 1 });
  });

  it("creates a load plan with server quantityBase and no client conversion", async () => {
    vi.mocked(apiRequest).mockResolvedValue({ id: "lp1", status: "Draft", rowVersion: 1, loadUnits: [] });
    await createLoadPlan("sh1", {
      routePlanId: "rp1",
      expectedRoutePlanVersion: 2,
      expectedShipmentRowVersion: 4,
      fallbackStopId: "st1",
      packages: [
        {
          id: "pkg1",
          shipmentItemId: "si1",
          routeStopId: "st1",
          quantityBase: 2000,
          status: "Created",
          physicalSnapshot: JSON.stringify({
            lengthMm: 1200,
            widthMm: 800,
            heightMm: 150,
            tareWeightKg: 10,
            grossWeightKg: 40,
            volumeM3: 1.2,
          }),
        },
      ],
    });
    const call = vi.mocked(apiRequest).mock.calls[0][0];
    expect(call.path).toBe("/shipments/sh1/load-plans");
    const unit = (call.body as { loadUnits: Array<{ items: Array<{ quantityBase: number }> }> }).loadUnits[0];
    expect(unit.items[0].quantityBase).toBe(2000);
    expect(call.body as Record<string, unknown>).not.toHaveProperty("quantityBase");
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

  it("prepares a dispatch run with expected versions and route stops", async () => {
    vi.mocked(apiRequest).mockResolvedValue({
      id: "run1",
      shipmentId: "sh1",
      loadPlanId: "lp1",
      routePlanId: "rp1",
      status: "Prepared",
      rowVersion: 1,
      stops: [{ routeStopId: "st1", sequenceNo: 1, status: "Pending", proofRecipient: null }],
    });
    const run = await prepareDispatchRun("rp1", {
      shipmentId: "sh1",
      loadPlanId: "lp1",
      vehicleId: "v1",
      driverId: "d1",
      stops: [{ routeStopId: "st1", sequenceNo: 1 }],
      expectedLoadPlanRowVersion: 2,
      expectedShipmentRowVersion: 3,
      expectedRoutePlanRowVersion: 4,
    });
    expect(apiRequest).toHaveBeenCalledWith({
      path: "/route-plans/rp1/dispatch",
      method: "POST",
      body: {
        shipmentId: "sh1",
        loadPlanId: "lp1",
        vehicleId: "v1",
        driverId: "d1",
        plannedDepartureAt: null,
        stops: [{ routeStopId: "st1", sequenceNo: 1 }],
        expectedLoadPlanRowVersion: 2,
        expectedShipmentRowVersion: 3,
        expectedRoutePlanRowVersion: 4,
      },
      idempotent: true,
    });
    expect(run.status).toBe("Prepared");
  });

  it("starts and completes load verification sessions", async () => {
    vi.mocked(apiRequest).mockResolvedValue({
      id: "ses1",
      loadPlanId: "lp1",
      shipmentId: "sh1",
      status: "InProgress",
      rowVersion: 1,
    });
    const session = await startLoadVerification("lp1", 2);
    expect(apiRequest).toHaveBeenCalledWith({
      path: "/load-plans/lp1/load-verification/sessions",
      method: "POST",
      body: {},
      ifMatch: "2",
      idempotent: true,
    });
    expect(session.status).toBe("InProgress");

    vi.mocked(apiRequest).mockResolvedValue({
      id: "ses1",
      loadPlanId: "lp1",
      shipmentId: "sh1",
      status: "Completed",
      rowVersion: 2,
    });
    const completed = await completeLoadVerification("ses1", 1);
    expect(apiRequest).toHaveBeenCalledWith({
      path: "/load-verification/sessions/ses1/complete",
      method: "POST",
      body: {},
      ifMatch: "1",
      idempotent: true,
    });
    expect(completed.status).toBe("Completed");
  });
});

