import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { OrderList } from "@/components/sales/order-list";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import { listSalesOrders } from "@/lib/sales/orders";
import { setWindowWidth } from "@/test/viewport";

vi.mock("@/lib/sales/orders", async () => {
  const actual = await vi.importActual<typeof import("@/lib/sales/orders")>(
    "@/lib/sales/orders",
  );
  return { ...actual, listSalesOrders: vi.fn() };
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

describe("OrderList", () => {
  beforeEach(() => {
    setWindowWidth(1280);
    resetSessionStore();
    vi.mocked(listSalesOrders).mockReset();
  });

  afterEach(() => {
    resetSessionStore();
  });

  it("skips the list API without order.read", async () => {
    authenticate([]);
    render(<OrderList />);
    expect(await screen.findByText("Siparişler bu oturumda görünmez")).toBeInTheDocument();
    expect(listSalesOrders).not.toHaveBeenCalled();
  });

  it("renders rows and counts from the real list window", async () => {
    authenticate(["order.read"]);
    vi.mocked(listSalesOrders).mockResolvedValue([
      {
        id: "o1",
        orderNumber: "SO-2026-000001",
        status: "Draft",
        customerId: "c1",
        customerCode: "MUS-1",
        customerLegalName: "Acme",
        sourceQuoteId: null,
        sourceQuoteNumber: null,
        currencyCode: "TRY",
        totalNet: 80,
        totalTax: 0,
        totalGross: 80,
        itemCount: 1,
        createdAt: "2026-08-19T10:00:00Z",
      },
    ]);

    render(<OrderList />);

    expect(await screen.findByText("SO-2026-000001")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Siparişler" })).toBeInTheDocument();
    expect(screen.getByLabelText("Taslak: 1")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /SO-2026-000001/ })).toHaveAttribute(
      "href",
      "/satis/siparisler/o1",
    );
  });
});
