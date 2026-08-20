import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { StockCountBoard } from "@/components/warehouse/stock-count-board";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import { listStockCounts } from "@/lib/warehouse/counts";
import { listWarehouses } from "@/lib/warehouse/stocks";
import { setWindowWidth } from "@/test/viewport";

vi.mock("@/lib/warehouse/counts", async () => {
  const actual = await vi.importActual<typeof import("@/lib/warehouse/counts")>("@/lib/warehouse/counts");
  return { ...actual, listStockCounts: vi.fn(), createStockCount: vi.fn() };
});
vi.mock("@/lib/warehouse/stocks", async () => {
  const actual = await vi.importActual<typeof import("@/lib/warehouse/stocks")>("@/lib/warehouse/stocks");
  return { ...actual, listWarehouses: vi.fn(), listWarehouseLocations: vi.fn() };
});

describe("StockCountBoard", () => {
  beforeEach(() => {
    setWindowWidth(1280);
    resetSessionStore();
    vi.mocked(listStockCounts).mockReset();
    vi.mocked(listWarehouses).mockReset();
  });

  afterEach(() => {
    resetSessionStore();
  });

  it("skips APIs without stock-count.read", async () => {
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Yusuf Kaya",
      roles: ["admin"],
      permissions: ["stock.read"],
    });
    render(<StockCountBoard />);
    expect(await screen.findByText("Sayım bu oturumda görünmez")).toBeInTheDocument();
    expect(listStockCounts).not.toHaveBeenCalled();
  });

  it("renders server document numbers without inventing variance totals", async () => {
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Yusuf Kaya",
      roles: ["admin"],
      permissions: ["stock-count.read"],
    });
    vi.mocked(listWarehouses).mockResolvedValue([]);
    vi.mocked(listStockCounts).mockResolvedValue([
      {
        id: "c1",
        documentNumber: "CNT-2026-000001",
        warehouseCode: "MAIN",
        locationCode: "A1",
        status: "Draft",
        itemCount: 2,
        createdAt: "2026-08-19T00:00:00Z",
        items: [],
      },
    ]);
    render(<StockCountBoard />);
    expect(await screen.findByText("CNT-2026-000001")).toBeInTheDocument();
    expect(screen.getByText("2")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Taslak aç" })).not.toBeInTheDocument();
  });
});
