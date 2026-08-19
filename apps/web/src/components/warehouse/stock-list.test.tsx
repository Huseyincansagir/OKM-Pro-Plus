import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { StockList } from "@/components/warehouse/stock-list";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import { listStocks, listWarehouses } from "@/lib/warehouse/stocks";
import { setWindowWidth } from "@/test/viewport";

vi.mock("@/lib/warehouse/stocks", async () => {
  const actual = await vi.importActual<typeof import("@/lib/warehouse/stocks")>(
    "@/lib/warehouse/stocks",
  );
  return { ...actual, listStocks: vi.fn(), listWarehouses: vi.fn() };
});

describe("StockList", () => {
  beforeEach(() => {
    setWindowWidth(1280);
    resetSessionStore();
    vi.mocked(listStocks).mockReset();
    vi.mocked(listWarehouses).mockReset();
  });

  afterEach(() => {
    resetSessionStore();
  });

  it("skips APIs without stock.read", async () => {
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Yusuf Kaya",
      roles: ["admin"],
      permissions: [],
    });
    render(<StockList />);
    expect(await screen.findByText("Stok bu oturumda görünmez")).toBeInTheDocument();
    expect(listStocks).not.toHaveBeenCalled();
  });

  it("renders server availableQtyBase and does not invent a zero", async () => {
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Yusuf Kaya",
      roles: ["admin"],
      permissions: ["stock.read"],
    });
    vi.mocked(listWarehouses).mockResolvedValue([
      { id: "w1", code: "MAIN", name: "Ana", isActive: true },
    ]);
    vi.mocked(listStocks).mockResolvedValue([
      {
        id: "s1",
        productId: "p1",
        productCode: "NAP-001",
        productName: "Peçete",
        warehouseId: "w1",
        warehouseCode: "MAIN",
        warehouseName: "Ana",
        locationCode: "A1",
        onHandQtyBase: 100,
        reservedQtyBase: 20,
        availableQtyBase: 80,
      },
    ]);

    render(<StockList />);

    expect(await screen.findByText("NAP-001")).toBeInTheDocument();
    expect(screen.getByText("80")).toBeInTheDocument();
    expect(screen.getByLabelText("Depo: 1")).toBeInTheDocument();
  });
});
