import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { TransferDetail } from "@/components/warehouse/transfer-detail";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import { completeTransfer, getTransfer } from "@/lib/warehouse/transfers";
import { setWindowWidth } from "@/test/viewport";

vi.mock("@/lib/warehouse/transfers", async () => {
  const actual = await vi.importActual<typeof import("@/lib/warehouse/transfers")>(
    "@/lib/warehouse/transfers",
  );
  return { ...actual, getTransfer: vi.fn(), completeTransfer: vi.fn(), cancelTransfer: vi.fn() };
});

const draft = {
  id: "t1",
  productId: "p1",
  productCode: "NAP-001",
  sourceWarehouseCode: "MAIN",
  sourceLocationCode: "A1",
  targetWarehouseCode: "MAIN",
  targetLocationCode: "B1",
  status: "Draft",
  quantityBase: 2000,
  enteredQuantity: 1,
  enteredPackagingId: "pkg",
  viewMode: "Packaging",
  createdAt: "2026-08-19T10:00:00Z",
};

describe("TransferDetail", () => {
  beforeEach(() => {
    setWindowWidth(1280);
    resetSessionStore();
    vi.mocked(getTransfer).mockReset();
    vi.mocked(completeTransfer).mockReset();
  });

  afterEach(() => {
    resetSessionStore();
  });

  it("skips the API without stock-transfer.read", async () => {
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Yusuf Kaya",
      roles: ["admin"],
      permissions: [],
    });
    render(<TransferDetail id="t1" />);
    expect(await screen.findByText("Transfer bu oturumda görünmez")).toBeInTheDocument();
    expect(getTransfer).not.toHaveBeenCalled();
  });

  it("completes a draft after confirmation", async () => {
    const user = userEvent.setup();
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Yusuf Kaya",
      roles: ["admin"],
      permissions: ["stock-transfer.read", "stock-transfer.complete"],
    });
    vi.mocked(getTransfer).mockResolvedValue(draft);
    vi.mocked(completeTransfer).mockResolvedValue({ ...draft, status: "Completed" });

    render(<TransferDetail id="t1" />);
    expect(await screen.findByText("2000")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Tamamla" }));
    const dialog = await screen.findByRole("dialog");
    await user.click(within(dialog).getByRole("button", { name: "Tamamla" }));
    expect(completeTransfer).toHaveBeenCalledWith("t1");
  });
});
