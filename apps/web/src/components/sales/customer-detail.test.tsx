import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { CustomerDetail } from "@/components/sales/customer-detail";
import { ApiError } from "@/lib/api/types";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import { getCustomer } from "@/lib/sales/customers";
import { getCurrentAccount } from "@/lib/sales/current-accounts";
import { setWindowWidth } from "@/test/viewport";

vi.mock("@/lib/sales/customers", async () => {
  const actual = await vi.importActual<typeof import("@/lib/sales/customers")>(
    "@/lib/sales/customers",
  );
  return { ...actual, getCustomer: vi.fn() };
});

vi.mock("@/lib/sales/current-accounts", async () => {
  const actual = await vi.importActual<typeof import("@/lib/sales/current-accounts")>(
    "@/lib/sales/current-accounts",
  );
  return { ...actual, getCurrentAccount: vi.fn() };
});

const customer = {
  id: "c1",
  customerCode: "DEMO-001",
  legalName: "Demo Horeca Tedarik",
  status: "Active",
  email: "demo@example.local",
  phone: "555",
  createdAt: "2026-08-19T00:00:00Z",
};

function authenticate(permissions: string[]) {
  useSessionStore.getState().setAuthenticated({
    id: "u1",
    userName: "admin",
    displayName: "Yusuf Kaya",
    roles: ["admin"],
    permissions,
  });
}

describe("CustomerDetail", () => {
  beforeEach(() => {
    setWindowWidth(1280);
    resetSessionStore();
    vi.mocked(getCustomer).mockReset();
    vi.mocked(getCurrentAccount).mockReset();
  });

  afterEach(() => {
    resetSessionStore();
  });

  it("does not invent ₺0 when the current account is missing", async () => {
    authenticate(["customer.read", "current-account.read"]);
    vi.mocked(getCustomer).mockResolvedValue(customer);
    vi.mocked(getCurrentAccount).mockResolvedValue(null);

    render(<CustomerDetail id="c1" />);

    expect(await screen.findByRole("heading", { name: "Demo Horeca Tedarik" })).toBeInTheDocument();
    expect(await screen.findByText("Cari hesap yok")).toBeInTheDocument();
    expect(screen.getByLabelText("Bakiye: bağlı değil")).toBeInTheDocument();
    expect(screen.queryByLabelText(/Bakiye: ₺/)).not.toBeInTheDocument();
  });

  it("renders server balances and hides cari without permission", async () => {
    authenticate(["customer.read"]);
    vi.mocked(getCustomer).mockResolvedValue(customer);

    render(<CustomerDetail id="c1" />);

    expect(await screen.findByLabelText("Kod: DEMO-001")).toBeInTheDocument();
    expect(getCurrentAccount).not.toHaveBeenCalled();
    expect(screen.getByText(/current-account.read yok/)).toBeInTheDocument();
  });

  it("shows a real balance from GET /current-accounts", async () => {
    authenticate(["customer.read", "current-account.read"]);
    vi.mocked(getCustomer).mockResolvedValue(customer);
    vi.mocked(getCurrentAccount).mockResolvedValue({
      customerId: "c1",
      currencyCode: "TRY",
      debitTotal: 100,
      creditTotal: 40,
      balance: 60,
    });

    render(<CustomerDetail id="c1" />);

    expect(await screen.findByText(/60,00/)).toBeInTheDocument();
    expect(getCurrentAccount).toHaveBeenCalledWith("c1");
  });

  it("does not treat a 403 on cari as a zero balance", async () => {
    authenticate(["customer.read", "current-account.read"]);
    vi.mocked(getCustomer).mockResolvedValue(customer);
    vi.mocked(getCurrentAccount).mockRejectedValue(
      new ApiError({ kind: "permission_denied", status: 403, detail: "Yasak" }),
    );

    render(<CustomerDetail id="c1" />);

    expect(await screen.findByText("Cari hesap görülemez")).toBeInTheDocument();
    expect(screen.getByLabelText("Bakiye: bağlı değil")).toBeInTheDocument();
  });
});
