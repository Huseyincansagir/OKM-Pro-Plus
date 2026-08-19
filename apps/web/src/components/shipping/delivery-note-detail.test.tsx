import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { DeliveryNoteDetailBoard } from "@/components/shipping/delivery-note-detail";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import { getDeliveryNote, issueDeliveryNote } from "@/lib/shipping/delivery-notes";
import { setWindowWidth } from "@/test/viewport";

vi.mock("@/lib/shipping/delivery-notes", async () => {
  const actual = await vi.importActual<typeof import("@/lib/shipping/delivery-notes")>(
    "@/lib/shipping/delivery-notes",
  );
  return { ...actual, getDeliveryNote: vi.fn(), issueDeliveryNote: vi.fn() };
});

const draft = {
  id: "d1",
  documentNumber: "DN-1",
  salesOrderId: "o1",
  customerId: "c1",
  status: "Draft",
  issuedAt: null,
  itemCount: 1,
  rowVersion: 1,
  items: [
    {
      id: "i1",
      salesOrderItemId: "soi1",
      productId: "p1",
      quantityBase: 2000,
      enteredQuantity: 1,
      shippedQty: 0,
      remainingToInvoice: 0,
    },
  ],
};

describe("DeliveryNoteDetailBoard", () => {
  beforeEach(() => {
    setWindowWidth(1280);
    resetSessionStore();
    vi.mocked(getDeliveryNote).mockReset();
    vi.mocked(issueDeliveryNote).mockReset();
  });

  afterEach(() => {
    resetSessionStore();
  });

  it("issues a draft after confirmation", async () => {
    const user = userEvent.setup();
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Yusuf Kaya",
      roles: ["admin"],
      permissions: ["delivery-note.read", "delivery-note.issue"],
    });
    vi.mocked(getDeliveryNote).mockResolvedValue(draft);
    vi.mocked(issueDeliveryNote).mockResolvedValue({ ...draft, status: "Issued" });

    render(<DeliveryNoteDetailBoard id="d1" />);
    expect(await screen.findAllByText("DN-1")).not.toHaveLength(0);
    expect(screen.getByText("2000")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Kesinleştir" }));
    const dialog = await screen.findByRole("dialog");
    await user.click(within(dialog).getByRole("button", { name: "Kesinleştir" }));
    expect(issueDeliveryNote).toHaveBeenCalledWith("d1");
  });
});
