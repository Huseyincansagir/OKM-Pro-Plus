import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { LoadPlanWizard } from "@/components/shipping/load-plan-wizard";
import {
  assignLoadPlanVehicle,
  createLoadPlan,
  evaluateVehicleFit,
  getLoadPlan,
  lockLoadPlan,
  validateLoadPlan,
  type ShipmentPackageRow,
} from "@/lib/shipping/dispatch";

vi.mock("@/lib/shipping/dispatch", async () => {
  const actual = await vi.importActual<typeof import("@/lib/shipping/dispatch")>(
    "@/lib/shipping/dispatch",
  );
  return {
    ...actual,
    createLoadPlan: vi.fn(),
    evaluateVehicleFit: vi.fn(),
    getLoadPlan: vi.fn(),
    assignLoadPlanVehicle: vi.fn(),
    validateLoadPlan: vi.fn(),
    lockLoadPlan: vi.fn(),
  };
});

const physical = JSON.stringify({
  lengthMm: 1200,
  widthMm: 800,
  heightMm: 150,
  tareWeightKg: 30,
  grossWeightKg: 230,
  volumeM3: 1.44,
  maxStackCount: 1,
});

const pkg: ShipmentPackageRow = {
  id: "pkg1",
  shipmentItemId: "si1",
  routeStopId: "st1",
  quantityBase: 2000,
  status: "Created",
  physicalSnapshot: physical,
};

const route = {
  id: "rp1",
  status: "Locked",
  version: 2,
  rowVersion: 5,
  vehicleId: "v1",
  driverId: "d1",
  stops: [{ id: "st1", sequenceNo: 1, customerId: "c1", addressId: "a1", status: "Pending" }],
};

describe("LoadPlanWizard", () => {
  beforeEach(() => {
    vi.mocked(createLoadPlan).mockReset();
    vi.mocked(evaluateVehicleFit).mockReset();
    vi.mocked(getLoadPlan).mockReset();
    vi.mocked(assignLoadPlanVehicle).mockReset();
    vi.mocked(validateLoadPlan).mockReset();
    vi.mocked(lockLoadPlan).mockReset();
  });

  it("hides without shipment.load-plan", () => {
    const { container } = render(
      <LoadPlanWizard
        shipmentId="s1"
        shipmentRowVersion={3}
        packages={[pkg]}
        routePlan={route}
        vehicles={[]}
        canCreate={false}
        canFit={false}
        canLock={false}
        canOverride={false}
        onChanged={() => undefined}
      />,
    );
    expect(container).toBeEmptyDOMElement();
  });

  it("creates a draft from server package quantityBase and physical snapshot", async () => {
    const user = userEvent.setup();
    vi.mocked(createLoadPlan).mockResolvedValue({
      id: "lp1",
      status: "Draft",
      feasibilityStatus: "Feasible",
      routePlanId: "rp1",
      vehicleId: null,
      vehicleCapacityId: null,
      rowVersion: 1,
      inputSnapshotHash: null,
    });
    render(
      <LoadPlanWizard
        shipmentId="s1"
        shipmentRowVersion={3}
        packages={[pkg]}
        routePlan={route}
        vehicles={[{ id: "v1", plateNumber: "34ABC", status: "Available", rowVersion: 1 }]}
        canCreate
        canFit
        canLock
        canOverride={false}
        onChanged={() => undefined}
      />,
    );
    await user.click(screen.getByRole("button", { name: "Yük planı" }));
    await user.click(await screen.findByRole("button", { name: "Taslak oluştur" }));
    expect(createLoadPlan).toHaveBeenCalledWith("s1", {
      routePlanId: "rp1",
      expectedRoutePlanVersion: 2,
      expectedShipmentRowVersion: 3,
      packages: [pkg],
      fallbackStopId: "st1",
    });
    expect(await screen.findByText(/Plan lp1/)).toBeInTheDocument();
  });

  it("does not invent package dimensions when snapshot is empty", async () => {
    const user = userEvent.setup();
    render(
      <LoadPlanWizard
        shipmentId="s1"
        shipmentRowVersion={3}
        packages={[{ ...pkg, physicalSnapshot: "{}" }]}
        routePlan={route}
        vehicles={[]}
        canCreate
        canFit={false}
        canLock={false}
        canOverride={false}
        onChanged={() => undefined}
      />,
    );
    await user.click(screen.getByRole("button", { name: "Yük planı" }));
    expect(await screen.findByText(/Ölçü uydurulmaz/)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Taslak oluştur" })).toBeDisabled();
    expect(createLoadPlan).not.toHaveBeenCalled();
  });
});
