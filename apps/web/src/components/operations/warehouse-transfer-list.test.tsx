import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { WarehouseTransferList } from "@/components/operations/warehouse-transfer-list";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import { listTransfers } from "@/lib/operations/boards";
import { setWindowWidth } from "@/test/viewport";

vi.mock("@/lib/operations/boards", async () => {
  const actual = await vi.importActual<typeof import("@/lib/operations/boards")>(
    "@/lib/operations/boards",
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
});
