import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { CustomerList } from "@/components/sales/customer-list";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import { listCustomers } from "@/lib/sales/customers";
import { setWindowWidth } from "@/test/viewport";

vi.mock("@/lib/sales/customers", async () => {
  const actual = await vi.importActual<typeof import("@/lib/sales/customers")>(
    "@/lib/sales/customers",
  );
  return {
    ...actual,
    listCustomers: vi.fn(),
  };
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

describe("CustomerList", () => {
  beforeEach(() => {
    setWindowWidth(1280);
    resetSessionStore();
    vi.mocked(listCustomers).mockReset();
  });

  afterEach(() => {
    resetSessionStore();
  });

  it("skips the list API without customer.read", async () => {
    authenticate(["quote-request.read"]);
    render(<CustomerList />);
    expect(await screen.findByText("Müşteriler bu oturumda görünmez")).toBeInTheDocument();
    expect(listCustomers).not.toHaveBeenCalled();
  });

  it("renders rows without inventing balances", async () => {
    authenticate(["customer.read"]);
    vi.mocked(listCustomers).mockResolvedValue([
      {
        id: "c1",
        customerCode: "DEMO-001",
        legalName: "Demo Horeca Tedarik",
        status: "Active",
        email: "demo@example.local",
        phone: "555",
        createdAt: "2026-08-19T00:00:00Z",
      },
    ]);

    render(<CustomerList />);

    expect(await screen.findByText("DEMO-001")).toBeInTheDocument();
    expect(screen.getByText("Demo Horeca Tedarik")).toBeInTheDocument();
    expect(screen.getByLabelText("Aktif: 1")).toBeInTheDocument();
    expect(screen.queryByText(/1[.\s]?285[.\s]?750/)).not.toBeInTheDocument();
  });
});
