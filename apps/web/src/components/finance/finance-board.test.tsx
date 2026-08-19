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
});
