import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it } from "vitest";
import { TransferCreate } from "@/components/warehouse/transfer-create";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import { setWindowWidth } from "@/test/viewport";

describe("TransferCreate", () => {
  beforeEach(() => {
    setWindowWidth(1280);
    resetSessionStore();
  });

  afterEach(() => {
    resetSessionStore();
  });

  it("does not open the form without stock-transfer.create", async () => {
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Yusuf Kaya",
      roles: ["admin"],
      permissions: ["stock-transfer.read"],
    });
    render(<TransferCreate />);
    expect(await screen.findByText("Transfer bu oturumda açılamaz")).toBeInTheDocument();
  });
});
