import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { StockCountDetail } from "@/components/warehouse/stock-count-detail";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import { listStaffProducts } from "@/lib/catalog/staff-products";
import { addStockCountItem, getStockCount } from "@/lib/warehouse/counts";
import { setWindowWidth } from "@/test/viewport";

vi.mock("@/lib/warehouse/counts", async () => {
  const actual = await vi.importActual<typeof import("@/lib/warehouse/counts")>("@/lib/warehouse/counts");
  return { ...actual, getStockCount: vi.fn(), addStockCountItem: vi.fn(), completeStockCount: vi.fn() };
});
vi.mock("@/lib/catalog/staff-products", async () => {
  const actual = await vi.importActual<typeof import("@/lib/catalog/staff-products")>(
    "@/lib/catalog/staff-products",
  );
  return { ...actual, listStaffProducts: vi.fn() };
});

const draft = {
  id: "c1",
  documentNumber: "CNT-2026-000001",
  warehouseCode: "MAIN",
  locationCode: "A1",
  status: "Draft",
  itemCount: 1,
  createdAt: "2026-08-19T00:00:00Z",
  items: [
    {
      id: "i1",
      productId: "p1",
      productCode: "NAP-001",
      countedQtyBase: 12,
      systemOnHandQtyBase: 10,
      varianceQtyBase: 2,
    },
  ],
};

describe("StockCountDetail", () => {
  beforeEach(() => {
    setWindowWidth(1280);
    resetSessionStore();
    vi.mocked(getStockCount).mockReset();
    vi.mocked(addStockCountItem).mockReset();
    vi.mocked(listStaffProducts).mockReset();
    vi.mocked(listStaffProducts).mockResolvedValue([]);
  });

  afterEach(() => {
    resetSessionStore();
  });

  it("skips the API without stock-count.read", async () => {
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Yusuf Kaya",
      roles: ["admin"],
      permissions: ["stock.read"],
    });
    render(<StockCountDetail id="c1" />);
    expect(await screen.findByText("Sayım bu oturumda görünmez")).toBeInTheDocument();
    expect(getStockCount).not.toHaveBeenCalled();
  });

  it("shows server variance and hides complete without permission", async () => {
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Yusuf Kaya",
      roles: ["admin"],
      permissions: ["stock-count.read"],
    });
    vi.mocked(getStockCount).mockResolvedValue(draft);

    render(<StockCountDetail id="c1" />);
    expect(await screen.findByRole("heading", { name: "CNT-2026-000001" })).toBeInTheDocument();
    expect(screen.getByText("12")).toBeInTheDocument();
    expect(screen.getByText("10")).toBeInTheDocument();
    expect(screen.getByText("2")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Tamamla" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Ekle" })).not.toBeInTheDocument();
  });

  it("adds an item with countedQtyBase only", async () => {
    const user = userEvent.setup();
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Yusuf Kaya",
      roles: ["admin"],
      permissions: ["stock-count.read", "stock-count.manage"],
    });
    vi.mocked(getStockCount).mockResolvedValue({ ...draft, items: [], itemCount: 0 });
    vi.mocked(listStaffProducts).mockResolvedValue([
      {
        id: "p1",
        code: "NAP-001",
        slug: "pecete",
        name: "Peçete",
        categoryName: "Kağıt",
        isActive: true,
        isPublic: true,
        baseUomName: "adet",
        packagingCount: 1,
        createdAt: "2026-08-19T00:00:00Z",
      },
    ]);
    vi.mocked(addStockCountItem).mockResolvedValue(draft);

    render(<StockCountDetail id="c1" />);
    await user.selectOptions(await screen.findByLabelText(/Ürün/), "p1");
    const counted = screen.getByLabelText(/Sayılan \(temel\)/);
    await user.clear(counted);
    await user.type(counted, "12");
    await user.click(screen.getByRole("button", { name: "Ekle" }));
    expect(addStockCountItem).toHaveBeenCalledWith("c1", { productId: "p1", countedQtyBase: 12 });
    const body = vi.mocked(addStockCountItem).mock.calls[0][1];
    expect(body).not.toHaveProperty("onHandQtyBase");
    expect(body).not.toHaveProperty("varianceQtyBase");
  });
});
