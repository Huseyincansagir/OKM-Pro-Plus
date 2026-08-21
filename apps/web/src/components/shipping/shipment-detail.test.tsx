import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ShipmentDetailBoard } from "@/components/shipping/shipment-detail";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import { getShipment } from "@/lib/shipping/shipments";
import {
  completeLoadVerification,
  deliverStop,
  listDispatchRuns,
  listDrivers,
  listLoadPlans,
  listRoutePlans,
  listShipmentPackages,
  listVehicles,
  prepareDispatchRun,
  scanLoadVerificationPackage,
  startLoadVerification,
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
    prepareDispatchRun: vi.fn(),
    startLoadVerification: vi.fn(),
    scanLoadVerificationPackage: vi.fn(),
    completeLoadVerification: vi.fn(),
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
    vi.mocked(prepareDispatchRun).mockReset();
    vi.mocked(startLoadVerification).mockReset();
    vi.mocked(scanLoadVerificationPackage).mockReset();
    vi.mocked(completeLoadVerification).mockReset();
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

  it("opens dispatch preparation dialog and executes prepareDispatchRun when loaded and plans are locked", async () => {
    const user = userEvent.setup();
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Yusuf Kaya",
      roles: ["admin"],
      permissions: ["shipment.read", "shipment.dispatch"],
    });
    vi.mocked(getShipment).mockResolvedValue({
      id: "s1",
      deliveryNoteId: "d1",
      customerId: "c1",
      status: "Loaded",
      itemCount: 1,
      rowVersion: 3,
      createdAt: "2026-08-19T10:00:00Z",
      items: [{ id: "i1", deliveryNoteItemId: "di1", productId: "p1", quantityBase: 2000 }],
    });
    vi.mocked(listRoutePlans).mockResolvedValue([
      {
        id: "rp1",
        status: "Locked",
        version: 1,
        rowVersion: 4,
        vehicleId: "v1",
        driverId: "dr1",
        stops: [{ id: "st1", sequenceNo: 1, customerId: "c1", addressId: "a1", status: "Pending" }],
      },
    ]);
    vi.mocked(listLoadPlans).mockResolvedValue([
      {
        id: "lp1",
        status: "Locked",
        feasibilityStatus: "Feasible",
        routePlanId: "rp1",
        vehicleId: "v1",
        vehicleCapacityId: "vc1",
        rowVersion: 5,
        inputSnapshotHash: "hash123",
      },
    ]);
    vi.mocked(listVehicles).mockResolvedValue([
      { id: "v1", plateNumber: "34 ABC 123", status: "Available", rowVersion: 1 },
    ]);
    vi.mocked(listDrivers).mockResolvedValue([
      { id: "dr1", fullName: "Ahmet Yılmaz", status: "Active", isActive: true },
    ]);
    vi.mocked(listShipmentPackages).mockResolvedValue([
      {
        id: "pkg1",
        shipmentItemId: "i1",
        routeStopId: "st1",
        quantityBase: 2000,
        status: "Loaded",
        physicalSnapshot: "{}",
        packageCode: "PKG-001",
      },
    ]);
    vi.mocked(prepareDispatchRun).mockResolvedValue({
      id: "run1",
      shipmentId: "s1",
      loadPlanId: "lp1",
      routePlanId: "rp1",
      status: "Prepared",
      rowVersion: 1,
      stops: [{ routeStopId: "st1", sequenceNo: 1, status: "Pending", proofRecipient: null }],
    });

    render(<ShipmentDetailBoard id="s1" />);
    expect(await screen.findByText(/Sefer hazırlama işlemini başlatın/)).toBeInTheDocument();
    const prepareBtn = screen.getByRole("button", { name: "Sefer hazırla" });
    expect(prepareBtn).toBeEnabled();

    await user.click(prepareBtn);
    expect(await screen.findByRole("heading", { name: "Sefer hazırlama" })).toBeInTheDocument();
    expect(screen.getByText("34 ABC 123")).toBeInTheDocument();
    expect(screen.getByText("Ahmet Yılmaz")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Seferi oluştur" }));
    expect(prepareDispatchRun).toHaveBeenCalledWith("rp1", {
      shipmentId: "s1",
      loadPlanId: "lp1",
      vehicleId: "v1",
      driverId: "dr1",
      stops: [{ routeStopId: "st1", sequenceNo: 1 }],
      expectedLoadPlanRowVersion: 5,
      expectedShipmentRowVersion: 3,
      expectedRoutePlanRowVersion: 4,
    });
  });

  it("hides yüklemeyi tamamla when user lacks shipment.load-verify", async () => {
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Yusuf Kaya",
      roles: ["admin"],
      permissions: ["shipment.read", "shipment.load-plan", "shipment.dispatch"],
    });
    vi.mocked(getShipment).mockResolvedValue({
      id: "s1",
      deliveryNoteId: "d1",
      customerId: "c1",
      status: "Preparing",
      itemCount: 1,
      rowVersion: 2,
      createdAt: "2026-08-19T10:00:00Z",
      items: [{ id: "i1", deliveryNoteItemId: "di1", productId: "p1", quantityBase: 2000 }],
    });
    vi.mocked(listRoutePlans).mockResolvedValue([
      {
        id: "rp1",
        status: "Locked",
        version: 1,
        rowVersion: 2,
        vehicleId: "v1",
        driverId: "dr1",
        stops: [{ id: "st1", sequenceNo: 1, customerId: "c1", addressId: "a1", status: "Pending" }],
      },
    ]);
    vi.mocked(listLoadPlans).mockResolvedValue([
      {
        id: "lp1",
        status: "Locked",
        feasibilityStatus: "Feasible",
        routePlanId: "rp1",
        vehicleId: "v1",
        vehicleCapacityId: "vc1",
        rowVersion: 3,
        inputSnapshotHash: "hash123",
      },
    ]);
    vi.mocked(listShipmentPackages).mockResolvedValue([
      {
        id: "pkg1",
        shipmentItemId: "i1",
        routeStopId: "st1",
        quantityBase: 2000,
        status: "Available",
        physicalSnapshot: "{}",
        packageCode: "PKG-001",
      },
    ]);

    render(<ShipmentDetailBoard id="s1" />);
    expect(await screen.findAllByText("Preparing")).not.toHaveLength(0);
    expect(screen.queryByRole("button", { name: "Yüklemeyi tamamla (Loaded)" })).not.toBeInTheDocument();
  });

  it("offers yüklemeyi tamamla when shipment is Preparing, load plan is Locked, and user has shipment.load-verify", async () => {
    const user = userEvent.setup();
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Yusuf Kaya",
      roles: ["admin"],
      permissions: ["shipment.read", "shipment.load-verify"],
    });
    vi.mocked(getShipment).mockResolvedValue({
      id: "s1",
      deliveryNoteId: "d1",
      customerId: "c1",
      status: "Preparing",
      itemCount: 1,
      rowVersion: 2,
      createdAt: "2026-08-19T10:00:00Z",
      items: [{ id: "i1", deliveryNoteItemId: "di1", productId: "p1", quantityBase: 2000 }],
    });
    vi.mocked(listRoutePlans).mockResolvedValue([
      {
        id: "rp1",
        status: "Locked",
        version: 1,
        rowVersion: 2,
        vehicleId: "v1",
        driverId: "dr1",
        stops: [{ id: "st1", sequenceNo: 1, customerId: "c1", addressId: "a1", status: "Pending" }],
      },
    ]);
    vi.mocked(listLoadPlans).mockResolvedValue([
      {
        id: "lp1",
        status: "Locked",
        feasibilityStatus: "Feasible",
        routePlanId: "rp1",
        vehicleId: "v1",
        vehicleCapacityId: "vc1",
        rowVersion: 3,
        inputSnapshotHash: "hash123",
      },
    ]);
    vi.mocked(listShipmentPackages).mockResolvedValue([
      {
        id: "pkg1",
        shipmentItemId: "i1",
        routeStopId: "st1",
        quantityBase: 2000,
        status: "Available",
        physicalSnapshot: "{}",
        packageCode: "PKG-001",
      },
    ]);
    vi.mocked(startLoadVerification).mockResolvedValue({
      id: "ses1",
      loadPlanId: "lp1",
      shipmentId: "s1",
      status: "InProgress",
      rowVersion: 1,
    });
    vi.mocked(scanLoadVerificationPackage).mockResolvedValue({});
    vi.mocked(completeLoadVerification).mockResolvedValue({
      id: "ses1",
      loadPlanId: "lp1",
      shipmentId: "s1",
      status: "Completed",
      rowVersion: 2,
    });

    render(<ShipmentDetailBoard id="s1" />);
    const verifyBtn = await screen.findByRole("button", { name: "Yüklemeyi tamamla (Loaded)" });
    await user.click(verifyBtn);

    expect(await screen.findByRole("heading", { name: "Yükleme doğrulaması" })).toBeInTheDocument();
    
    // Click Tümünü Doğrula to scan all packages
    const scanAllBtn = await screen.findByRole("button", { name: "Tümünü Doğrula" });
    await user.click(scanAllBtn);

    // Confirm verification
    const confirmBtn = await screen.findByRole("button", { name: "Yüklemeyi onayla (Loaded)" });
    await user.click(confirmBtn);

    expect(startLoadVerification).toHaveBeenCalledWith("lp1", 3);
    expect(scanLoadVerificationPackage).toHaveBeenCalledWith("ses1", 1, "PKG-001");
    expect(completeLoadVerification).toHaveBeenCalledWith("ses1", 2);
  });
});

