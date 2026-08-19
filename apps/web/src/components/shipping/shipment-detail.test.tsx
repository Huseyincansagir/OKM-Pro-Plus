import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ShipmentDetailBoard } from "@/components/shipping/shipment-detail";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import { getShipment } from "@/lib/shipping/shipments";
import { setWindowWidth } from "@/test/viewport";

vi.mock("@/lib/shipping/shipments", async () => {
  const actual = await vi.importActual<typeof import("@/lib/shipping/shipments")>(
    "@/lib/shipping/shipments",
  );
  return { ...actual, getShipment: vi.fn() };
});

describe("ShipmentDetailBoard", () => {
  beforeEach(() => {
    setWindowWidth(1280);
    resetSessionStore();
    vi.mocked(getShipment).mockReset();
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
  });
});
