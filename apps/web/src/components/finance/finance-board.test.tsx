import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { FinanceBoard } from "@/components/finance/finance-board";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import { listCurrentAccounts, listDeliveryNotes, listInvoices } from "@/lib/finance/ledgers";
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

describe("FinanceBoard", () => {
  beforeEach(() => {
    setWindowWidth(1280);
    resetSessionStore();
    vi.mocked(listInvoices).mockReset();
    vi.mocked(listDeliveryNotes).mockReset();
    vi.mocked(listCurrentAccounts).mockReset();
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

    render(<FinanceBoard />);
    const link = await screen.findByRole("link", { name: "DN-2026-000001" });
    expect(link).toHaveAttribute("href", "/sevkiyat/irsaliyeler/dn1");
    expect(screen.queryByText("₺0")).not.toBeInTheDocument();
  });
});
