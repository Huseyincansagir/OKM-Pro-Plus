import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { WarehouseTransferList } from "@/components/operations/warehouse-transfer-list";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import { listTransfers } from "@/lib/warehouse/transfers";
import { setWindowWidth } from "@/test/viewport";

vi.mock("@/lib/warehouse/transfers", async () => {
  const actual = await vi.importActual<typeof import("@/lib/warehouse/transfers")>(
    "@/lib/warehouse/transfers",
  );
  return { ...actual, listTransfers: vi.fn() };
});

describe("WarehouseTransferList", () => {
  beforeEach(() => {
    setWindowWidth(1280);
    resetSessionStore();
    vi.mocked(listTransfers).mockReset();
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
    render(<WarehouseTransferList />);
    expect(await screen.findByText("Depo transferleri bu oturumda görünmez")).toBeInTheDocument();
    expect(listTransfers).not.toHaveBeenCalled();
  });

  it("renders server quantityBase and the create action with permission", async () => {
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Yusuf Kaya",
      roles: ["admin"],
      permissions: ["stock-transfer.read", "stock-transfer.create"],
    });
    vi.mocked(listTransfers).mockResolvedValue([
      {
        id: "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
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
      },
    ]);
    render(<WarehouseTransferList />);
    expect(await screen.findByText("NAP-001")).toBeInTheDocument();
    expect(screen.getByText("2000")).toBeInTheDocument();
    expect(screen.queryByText("-2000")).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Yeni transfer" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /aaaaaaaa/i })).toHaveAttribute(
      "href",
      "/depo/transferler/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
    );
  });
});
