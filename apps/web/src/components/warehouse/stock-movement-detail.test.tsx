import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { StockMovementDetail } from "@/components/warehouse/stock-movement-detail";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import { getStockMovement } from "@/lib/warehouse/stock-movements";
import { setWindowWidth } from "@/test/viewport";

vi.mock("@/lib/warehouse/stock-movements", async () => {
  const actual = await vi.importActual<typeof import("@/lib/warehouse/stock-movements")>(
    "@/lib/warehouse/stock-movements",
  );
  return { ...actual, getStockMovement: vi.fn() };
});

function authenticate(permissions: string[]) {
  useSessionStore.getState().setAuthenticated({
    id: "u1",
    userName: "admin",
    displayName: "Yusuf Kaya",
    roles: ["admin"],
    permissions,
  });
}

describe("StockMovementDetail", () => {
  beforeEach(() => {
    setWindowWidth(1280);
    resetSessionStore();
    vi.mocked(getStockMovement).mockReset();
  });

  afterEach(() => {
    resetSessionStore();
  });

  it("skips the API without stock.read", async () => {
    authenticate([]);
    render(<StockMovementDetail id="m1" />);
    expect(await screen.findByText("Hareket bu oturumda görünmez")).toBeInTheDocument();
    expect(getStockMovement).not.toHaveBeenCalled();
  });

  it("renders the server snapshot without converting quantity", async () => {
    authenticate(["stock.read"]);
    vi.mocked(getStockMovement).mockResolvedValue({
      id: "m1",
      productId: "p1",
      productCode: "NAP-001",
      productName: "Peçete",
      warehouseId: "w1",
      warehouseCode: "MAIN",
      warehouseName: "Ana",
      locationCode: "A1",
      movementType: "DeliveryIssue",
      effect: "Out",
      quantityBase: 8,
      sourceEntityType: "DeliveryNoteRecord",
      sourceEntityId: "dn1",
      reversedFromId: null,
      packagingSnapshot: '{"name":"Koli"}',
      createdAt: "2026-08-19T11:00:00Z",
    });

    render(<StockMovementDetail id="m1" />);

    expect(await screen.findAllByText("İrsaliye çıkışı")).not.toHaveLength(0);
    expect(screen.getByText("8")).toBeInTheDocument();
    expect(screen.getByText('{"name":"Koli"}')).toBeInTheDocument();
    expect(screen.queryByText("-8")).not.toBeInTheDocument();
  });
});
