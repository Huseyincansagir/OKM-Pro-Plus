import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { TransferCreate } from "@/components/warehouse/transfer-create";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import { resolveBarcode } from "@/lib/catalog/barcode";
import { getStaffProduct, listStaffProducts } from "@/lib/catalog/staff-products";
import { listWarehouseLocations, listWarehouses } from "@/lib/warehouse/stocks";
import { setWindowWidth } from "@/test/viewport";

vi.mock("@/lib/catalog/barcode", async () => {
  const actual = await vi.importActual<typeof import("@/lib/catalog/barcode")>("@/lib/catalog/barcode");
  return { ...actual, resolveBarcode: vi.fn() };
});
vi.mock("@/lib/catalog/staff-products", async () => {
  const actual = await vi.importActual<typeof import("@/lib/catalog/staff-products")>(
    "@/lib/catalog/staff-products",
  );
  return { ...actual, listStaffProducts: vi.fn(), getStaffProduct: vi.fn() };
});
vi.mock("@/lib/warehouse/stocks", async () => {
  const actual = await vi.importActual<typeof import("@/lib/warehouse/stocks")>("@/lib/warehouse/stocks");
  return { ...actual, listWarehouses: vi.fn(), listWarehouseLocations: vi.fn() };
});

describe("TransferCreate", () => {
  beforeEach(() => {
    setWindowWidth(1280);
    resetSessionStore();
    vi.mocked(resolveBarcode).mockReset();
    vi.mocked(listStaffProducts).mockReset();
    vi.mocked(getStaffProduct).mockReset();
    vi.mocked(listWarehouses).mockReset();
    vi.mocked(listWarehouseLocations).mockReset();
  });

  afterEach(() => {
    resetSessionStore();
  });

  it("does not open the form without stock-transfer.create", async () => {
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Yusuf Kaya",
      roles: ["admin"],
      permissions: ["stock-transfer.read"],
    });
    render(<TransferCreate />);
    expect(await screen.findByText("Transfer bu oturumda açılamaz")).toBeInTheDocument();
  });

  it("resolves a USB barcode on Enter without sending quantityBase", async () => {
    const user = userEvent.setup();
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Yusuf Kaya",
      roles: ["admin"],
      permissions: ["stock-transfer.create", "stock.read", "product.read", "barcode.resolve"],
    });
    vi.mocked(listStaffProducts).mockResolvedValue([]);
    vi.mocked(listWarehouses).mockResolvedValue([]);
    vi.mocked(resolveBarcode).mockResolvedValue({
      barcode: "869000000001",
      productId: "p1",
      productCode: "NAP-001",
      productName: "Peçete",
      packagingId: "pkg",
      packagingName: "Paket",
      quantityInBaseUom: 100,
    });
    vi.mocked(getStaffProduct).mockResolvedValue({
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
      description: "",
      sizeLabel: "",
      categoryCode: "KAG",
      packagings: [
        { id: "pkg", level: "Pack", name: "Paket", quantityInBaseUom: 100, isSellable: true, allowPartial: false },
      ],
    });

    render(<TransferCreate />);
    const barcode = await screen.findByLabelText("Barkod");
    await user.type(barcode, "869000000001{Enter}");
    expect(resolveBarcode).toHaveBeenCalledWith("869000000001");
    expect(await screen.findByText("USB okuyucu Enter ile çözer. Kamera yok.")).toBeInTheDocument();
  });

  it("hides the barcode field without barcode.resolve", async () => {
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Yusuf Kaya",
      roles: ["admin"],
      permissions: ["stock-transfer.create", "stock.read", "product.read"],
    });
    vi.mocked(listStaffProducts).mockResolvedValue([]);
    vi.mocked(listWarehouses).mockResolvedValue([]);
    render(<TransferCreate />);
    expect(await screen.findByText("Kaynak ve hedef")).toBeInTheDocument();
    expect(screen.queryByLabelText("Barkod")).not.toBeInTheDocument();
    expect(resolveBarcode).not.toHaveBeenCalled();
  });
});
