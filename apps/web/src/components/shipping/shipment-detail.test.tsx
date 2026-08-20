import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ShipmentDetailBoard } from "@/components/shipping/shipment-detail";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import { getShipment } from "@/lib/shipping/shipments";
import {
  deliverStop,
  listDispatchRuns,
  listDrivers,
  listLoadPlans,
  listRoutePlans,
  listShipmentPackages,
  listVehicles,
} from "@/lib/shipping/dispatch";
import { setWindowWidth } from "@/test/viewport";

vi.mock("@/lib/shipping/shipments", async () => {
  const actual = await vi.importActual<typeof import("@/lib/shipping/shipments")>(
    "@/lib/shipping/shipments",
  );
  return { ...actual, getShipment: vi.fn() };
});
vi.mock("@/lib/shipping/dispatch", async () => {
  const actual = await vi.importActual<typeof import("@/lib/shipping/dispatch")>(
    "@/lib/shipping/dispatch",
  );
  return {
    ...actual,
    listDispatchRuns: vi.fn(),
    listLoadPlans: vi.fn(),
    listRoutePlans: vi.fn(),
    listShipmentPackages: vi.fn(),
    listVehicles: vi.fn(),
    listDrivers: vi.fn(),
    deliverStop: vi.fn(),
    confirmDispatch: vi.fn(),
  };
});

describe("ShipmentDetailBoard", () => {
  beforeEach(() => {
    setWindowWidth(1280);
    resetSessionStore();
    vi.mocked(getShipment).mockReset();
    vi.mocked(listDispatchRuns).mockReset();
    vi.mocked(listLoadPlans).mockReset();
    vi.mocked(listRoutePlans).mockReset();
    vi.mocked(listShipmentPackages).mockReset();
    vi.mocked(listDispatchRuns).mockResolvedValue([]);
    vi.mocked(listLoadPlans).mockResolvedValue([]);
    vi.mocked(listRoutePlans).mockResolvedValue([]);
    vi.mocked(listShipmentPackages).mockResolvedValue([]);
    vi.mocked(listVehicles).mockResolvedValue([]);
    vi.mocked(listDrivers).mockResolvedValue([]);
    vi.mocked(deliverStop).mockReset();
  });

  afterEach(() => {
    resetSessionStore();
  });

  it("renders Preparing and does not invent Delivered", async () => {
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Yusuf Kaya",
      roles: ["admin"],
      permissions: ["shipment.read"],
    });
    vi.mocked(getShipment).mockResolvedValue({
      id: "s1",
      deliveryNoteId: "d1",
      customerId: "c1",
      status: "Preparing",
      itemCount: 1,
      rowVersion: 1,
      createdAt: "2026-08-19T10:00:00Z",
      items: [{ id: "i1", deliveryNoteItemId: "di1", productId: "p1", quantityBase: 2000 }],
    });

    render(<ShipmentDetailBoard id="s1" />);
    expect(await screen.findAllByText("Preparing")).not.toHaveLength(0);
    expect(screen.getByText("2000")).toBeInTheDocument();
    expect(screen.getByText("Teslim POD ile yazılır")).toBeInTheDocument();
    expect(screen.queryByText("Delivered")).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Rota + durak" })).not.toBeInTheDocument();
  });

  it("offers route creation when preparing and hides deliver without an arrived stop", async () => {
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Yusuf Kaya",
      roles: ["admin"],
      permissions: ["shipment.read", "shipment.route-manage"],
    });
    vi.mocked(getShipment).mockResolvedValue({
      id: "s1",
      deliveryNoteId: "d1",
      customerId: "c1",
      status: "Preparing",
      itemCount: 1,
      rowVersion: 1,
      createdAt: "2026-08-19T10:00:00Z",
      items: [{ id: "i1", deliveryNoteItemId: "di1", productId: "p1", quantityBase: 2000 }],
    });

    render(<ShipmentDetailBoard id="s1" />);
    expect(await screen.findByRole("button", { name: "Rota + durak" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Teslim yaz" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Yük planı" })).not.toBeInTheDocument();
  });

  it("offers load plan wizard with shipment.load-plan", async () => {
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Yusuf Kaya",
      roles: ["admin"],
      permissions: ["shipment.read", "shipment.load-plan"],
    });
    vi.mocked(getShipment).mockResolvedValue({
      id: "s1",
      deliveryNoteId: "d1",
      customerId: "c1",
      status: "Preparing",
      itemCount: 1,
      rowVersion: 1,
      createdAt: "2026-08-19T10:00:00Z",
      items: [{ id: "i1", deliveryNoteItemId: "di1", productId: "p1", quantityBase: 2000 }],
    });

    render(<ShipmentDetailBoard id="s1" />);
    expect(await screen.findByRole("button", { name: "Yük planı" })).toBeInTheDocument();
  });

  it("shows teslim yaz only for an arrived stop", async () => {
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Yusuf Kaya",
      roles: ["admin"],
      permissions: ["shipment.read", "shipment.route-execute"],
    });
    vi.mocked(getShipment).mockResolvedValue({
      id: "s1",
      deliveryNoteId: "d1",
      customerId: "c1",
      status: "InTransit",
      itemCount: 1,
      rowVersion: 2,
      createdAt: "2026-08-19T10:00:00Z",
      items: [{ id: "i1", deliveryNoteItemId: "di1", productId: "p1", quantityBase: 2000 }],
    });
    vi.mocked(listDispatchRuns).mockResolvedValue([
      {
        id: "run1",
        shipmentId: "s1",
        loadPlanId: "lp1",
        routePlanId: "rp1",
        status: "InTransit",
        rowVersion: 4,
        stops: [
          { routeStopId: "st1", sequenceNo: 1, status: "Arrived", proofRecipient: null },
        ],
      },
    ]);

    render(<ShipmentDetailBoard id="s1" />);
    expect(await screen.findByRole("button", { name: "Teslim yaz" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Rota + durak" })).not.toBeInTheDocument();
  });

  it("posts recipient on teslim yaz without quantityBase", async () => {
    const user = userEvent.setup();
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Yusuf Kaya",
      roles: ["admin"],
      permissions: ["shipment.read", "shipment.route-execute"],
    });
    vi.mocked(getShipment).mockResolvedValue({
      id: "s1",
      deliveryNoteId: "d1",
      customerId: "c1",
      status: "InTransit",
      itemCount: 1,
      rowVersion: 2,
      createdAt: "2026-08-19T10:00:00Z",
      items: [{ id: "i1", deliveryNoteItemId: "di1", productId: "p1", quantityBase: 2000 }],
    });
    vi.mocked(listDispatchRuns).mockResolvedValue([
      {
        id: "run1",
        shipmentId: "s1",
        loadPlanId: "lp1",
        routePlanId: "rp1",
        status: "InTransit",
        rowVersion: 4,
        stops: [{ routeStopId: "st1", sequenceNo: 1, status: "Arrived", proofRecipient: null }],
      },
    ]);
    vi.mocked(deliverStop).mockResolvedValue({
      id: "run1",
      shipmentId: "s1",
      loadPlanId: "lp1",
      routePlanId: "rp1",
      status: "InTransit",
      rowVersion: 5,
      stops: [{ routeStopId: "st1", sequenceNo: 1, status: "Delivered", proofRecipient: "Ali Kaya" }],
    });

    render(<ShipmentDetailBoard id="s1" />);
    await user.type(await screen.findByLabelText("Teslim alan #1"), "Ali Kaya");
    await user.click(screen.getByRole("button", { name: "Teslim yaz" }));
    expect(deliverStop).toHaveBeenCalledWith("run1", "st1", {
      recipientName: "Ali Kaya",
      rowVersion: 4,
    });
    const args = vi.mocked(deliverStop).mock.calls[0][2] as Record<string, unknown>;
    expect(args).not.toHaveProperty("quantityBase");
  });
});
