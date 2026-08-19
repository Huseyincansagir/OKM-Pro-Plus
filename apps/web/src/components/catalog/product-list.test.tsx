import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ProductList } from "@/components/catalog/product-list";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import { listStaffProducts } from "@/lib/catalog/staff-products";
import { setWindowWidth } from "@/test/viewport";

vi.mock("@/lib/catalog/staff-products", async () => {
  const actual = await vi.importActual<typeof import("@/lib/catalog/staff-products")>(
    "@/lib/catalog/staff-products",
  );
  return { ...actual, listStaffProducts: vi.fn() };
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

describe("ProductList", () => {
  beforeEach(() => {
    setWindowWidth(1280);
    resetSessionStore();
    vi.mocked(listStaffProducts).mockReset();
  });

  afterEach(() => {
    resetSessionStore();
  });

  it("skips the list API without product.read", async () => {
    authenticate([]);
    render(<ProductList />);
    expect(await screen.findByText("Ürünler bu oturumda görünmez")).toBeInTheDocument();
    expect(listStaffProducts).not.toHaveBeenCalled();
  });

  it("renders rows without inventing stock", async () => {
    authenticate(["product.read"]);
    vi.mocked(listStaffProducts).mockResolvedValue([
      {
        id: "p1",
        code: "NAP-001",
        slug: "premium-pecete",
        name: "Premium Peçete",
        categoryName: "Peçeteler",
        isActive: true,
        isPublic: true,
        baseUomName: "Adet",
        packagingCount: 3,
        createdAt: "2026-08-19T00:00:00Z",
      },
    ]);

    render(<ProductList />);

    expect(await screen.findByText("NAP-001")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Ürünler" })).toBeInTheDocument();
    expect(screen.getByLabelText("Aktif: 1")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /NAP-001/ })).toHaveAttribute("href", "/urunler/p1");
    expect(screen.queryByText(/9000/)).not.toBeInTheDocument();
  });
});
