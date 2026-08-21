import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { FinanceBoard } from "@/components/finance/finance-board";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import { listCurrentAccounts, listDeliveryNotes, listInvoices } from "@/lib/finance/ledgers";
import { listPayments, listPaymentMethods } from "@/lib/finance/payments";
import { setWindowWidth } from "@/test/viewport";

vi.mock("@/lib/finance/ledgers", async () => {
  const actual = await vi.importActual<typeof import("@/lib/finance/ledgers")>(
    "@/lib/finance/ledgers",
  );
  return {
    ...actual,
    listInvoices: vi.fn(),
    listDeliveryNotes: vi.fn(),
    listCurrentAccounts: vi.fn(),
  };
});

vi.mock("@/lib/finance/payments", async () => {
  const actual = await vi.importActual<typeof import("@/lib/finance/payments")>(
    "@/lib/finance/payments",
  );
  return {
    ...actual,
    listPayments: vi.fn(),
    listPaymentMethods: vi.fn(),
  };
});

describe("FinanceBoard", () => {
  beforeEach(() => {
    setWindowWidth(1280);
    resetSessionStore();
    vi.mocked(listInvoices).mockReset();
    vi.mocked(listDeliveryNotes).mockReset();
    vi.mocked(listCurrentAccounts).mockReset();
    vi.mocked(listPayments).mockReset();
    vi.mocked(listPaymentMethods).mockReset();
  });

  afterEach(() => {
    resetSessionStore();
  });

  it("skips ledger APIs without finance read permissions", async () => {
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Yusuf Kaya",
      roles: ["admin"],
      permissions: [],
    });
    render(<FinanceBoard />);
    expect(await screen.findByText("Cari bu oturumda görünmez")).toBeInTheDocument();
    expect(listInvoices).not.toHaveBeenCalled();
  });

  it("links irsaliye rows without inventing a zero total", async () => {
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Yusuf Kaya",
      roles: ["admin"],
      permissions: ["delivery-note.read"],
    });
    vi.mocked(listDeliveryNotes).mockResolvedValue([
      { id: "dn1", documentNumber: "DN-2026-000001", customerId: "c1", status: "Draft", itemCount: 1 },
    ]);
    vi.mocked(listInvoices).mockResolvedValue([]);
    vi.mocked(listCurrentAccounts).mockResolvedValue([]);
    vi.mocked(listPayments).mockResolvedValue([]);

    render(<FinanceBoard />);
    const link = await screen.findByRole("link", { name: "DN-2026-000001" });
    expect(link).toHaveAttribute("href", "/sevkiyat/irsaliyeler/dn1");
    expect(screen.queryByText("₺0")).not.toBeInTheDocument();
  });

  it("renders payments and opens payment modal on click", async () => {
    const user = userEvent.setup();
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Yusuf Kaya",
      roles: ["admin"],
      permissions: ["current-account.read", "payment.read", "payment.apply"],
    });
    vi.mocked(listDeliveryNotes).mockResolvedValue([]);
    vi.mocked(listInvoices).mockResolvedValue([]);
    vi.mocked(listCurrentAccounts).mockResolvedValue([]);
    vi.mocked(listPaymentMethods).mockResolvedValue([
      { id: "pm1", code: "BANK", name: "Banka Havalesi", isActive: true },
    ]);
    vi.mocked(listPayments).mockResolvedValue([
      {
        id: "pay-1",
        customerId: "c1",
        amount: 2500,
        currencyCode: "TRY",
        paymentMethodId: "pm1",
        status: "Applied",
        invoiceId: null,
        appliedAt: "2026-08-21T12:00:00Z",
      },
    ]);

    render(<FinanceBoard />);
    expect(await screen.findByText("Tahsilatlar ve Ödemeler")).toBeInTheDocument();
    expect(screen.getByText(/2\.500/)).toBeInTheDocument();
    expect(screen.getByText("Serbest Tahsilat")).toBeInTheDocument();

    const paymentBtn = screen.getByRole("button", { name: "Tahsilat / Ödeme Girişi" });
    await user.click(paymentBtn);
    expect(await screen.findByRole("heading", { name: "Tahsilat / Ödeme Girişi" })).toBeInTheDocument();
  });
});
