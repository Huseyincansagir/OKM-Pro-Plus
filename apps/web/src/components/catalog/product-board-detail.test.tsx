import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ProductBoardDetail } from "@/components/catalog/product-board-detail";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import { getStaffProduct } from "@/lib/catalog/staff-products";
import { setWindowWidth } from "@/test/viewport";

vi.mock("@/lib/catalog/staff-products", async () => {
  const actual = await vi.importActual<typeof import("@/lib/catalog/staff-products")>(
    "@/lib/catalog/staff-products",
  );
  return { ...actual, getStaffProduct: vi.fn() };
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

describe("ProductBoardDetail", () => {
  beforeEach(() => {
    setWindowWidth(1280);
    resetSessionStore();
    vi.mocked(getStaffProduct).mockReset();
  });

  afterEach(() => {
    resetSessionStore();
  });

  it("shows server packaging quantity and does not invent stock", async () => {
    authenticate(["product.read"]);
    vi.mocked(getStaffProduct).mockResolvedValue({
      id: "p1",
      code: "NAP-001",
      slug: "premium-pecete",
      name: "Premium Peçete",
      categoryName: "Peçeteler",
      isActive: true,
      isPublic: true,
      baseUomName: "Adet",
      packagingCount: 1,
      createdAt: "2026-08-19T00:00:00Z",
      description: "Koli",
      sizeLabel: "33x33",
      categoryCode: "NAPKIN",
      packagings: [
        {
          id: "pkg",
          level: "Case",
          name: "Koli",
          quantityInBaseUom: 2000,
          isSellable: true,
          allowPartial: false,
        },
      ],
    });

    render(<ProductBoardDetail id="p1" />);

    expect(await screen.findByRole("heading", { name: "Premium Peçete" })).toBeInTheDocument();
    expect(screen.getByText("2000")).toBeInTheDocument();
    expect(screen.getByText("Stok bu kartta yok")).toBeInTheDocument();
  });
});
