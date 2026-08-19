import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { StockMovementList } from "@/components/warehouse/stock-movement-list";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import { listStockMovements } from "@/lib/warehouse/stock-movements";
import { setWindowWidth } from "@/test/viewport";

vi.mock("@/lib/warehouse/stock-movements", async () => {
  const actual = await vi.importActual<typeof import("@/lib/warehouse/stock-movements")>(
    "@/lib/warehouse/stock-movements",
  );
  return { ...actual, listStockMovements: vi.fn() };
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

describe("StockMovementList", () => {
  beforeEach(() => {
    setWindowWidth(1280);
    resetSessionStore();
    vi.mocked(listStockMovements).mockReset();
  });

  afterEach(() => {
    resetSessionStore();
  });

  it("skips APIs without stock.read", async () => {
    authenticate([]);
    render(<StockMovementList />);
    expect(await screen.findByText("Hareketler bu oturumda görünmez")).toBeInTheDocument();
    expect(listStockMovements).not.toHaveBeenCalled();
  });

  it("renders server quantityBase and does not invent a signed total", async () => {
    authenticate(["stock.read"]);
    vi.mocked(listStockMovements).mockResolvedValue([
      {
        id: "m1",
        productId: "p1",
        productCode: "NAP-001",
        productName: "Peçete",
        warehouseId: "w1",
        warehouseCode: "MAIN",
        warehouseName: "Ana",
        locationCode: "A1",
        movementType: "ProductionIn",
        effect: "In",
        quantityBase: 40,
        sourceEntityType: "ProductionOrderRecord",
        sourceEntityId: "po1",
        reversedFromId: null,
        packagingSnapshot: null,
        createdAt: "2026-08-19T10:00:00Z",
      },
      {
        id: "m2",
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
        packagingSnapshot: null,
        createdAt: "2026-08-19T11:00:00Z",
      },
    ]);

    render(<StockMovementList />);

    expect(await screen.findByText("Üretim girişi")).toBeInTheDocument();
    expect(screen.getByText("40")).toBeInTheDocument();
    expect(screen.getByText("8")).toBeInTheDocument();
    expect(screen.getByLabelText("Giriş: 1")).toBeInTheDocument();
    expect(screen.getByLabelText("Çıkış: 1")).toBeInTheDocument();
    expect(screen.queryByText("-8")).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: /üretim girişi/i })).toHaveAttribute(
      "href",
      "/depo/hareketler/m1",
    );
  });
});
