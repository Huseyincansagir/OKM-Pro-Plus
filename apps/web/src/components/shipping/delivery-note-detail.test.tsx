import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { DeliveryNoteDetailBoard } from "@/components/shipping/delivery-note-detail";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import { getDeliveryNote, issueDeliveryNote } from "@/lib/shipping/delivery-notes";
import { createInvoice } from "@/lib/finance/invoices";
import { setWindowWidth } from "@/test/viewport";

vi.mock("@/lib/shipping/delivery-notes", async () => {
  const actual = await vi.importActual<typeof import("@/lib/shipping/delivery-notes")>(
    "@/lib/shipping/delivery-notes",
  );
  return { ...actual, getDeliveryNote: vi.fn(), issueDeliveryNote: vi.fn() };
});

vi.mock("@/lib/finance/invoices", async () => {
  const actual = await vi.importActual<typeof import("@/lib/finance/invoices")>(
    "@/lib/finance/invoices",
  );
  return { ...actual, createInvoice: vi.fn() };
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
    vi.mocked(createInvoice).mockReset();
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

  it("creates an invoice from issued delivery note with invoice.create permission", async () => {
    const user = userEvent.setup();
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Yusuf Kaya",
      roles: ["admin"],
      permissions: ["delivery-note.read", "invoice.create"],
    });
    vi.mocked(getDeliveryNote).mockResolvedValue({
      ...draft,
      status: "Issued",
      items: [
        {
          id: "i1",
          salesOrderItemId: "soi1",
          productId: "p1",
          quantityBase: 2000,
          enteredQuantity: 1,
          shippedQty: 2000,
          remainingToInvoice: 2000,
        },
      ],
    });
    vi.mocked(createInvoice).mockResolvedValue({
      id: "inv-created",
      invoiceNumber: "INV-2026-000001",
      customerId: "c1",
      status: "Draft",
      currencyCode: "TRY",
      subtotal: 20000,
      taxTotal: 4000,
      grandTotal: 24000,
      items: [],
      issuedAt: null,
      rowVersion: 1,
    });

    render(<DeliveryNoteDetailBoard id="d1" />);
    const invoiceBtn = await screen.findByRole("button", { name: "Fatura oluştur" });
    await user.click(invoiceBtn);

    expect(await screen.findByRole("heading", { name: "Fatura oluştur" })).toBeInTheDocument();
    const createBtn = screen.getByRole("button", { name: "Faturayı oluştur" });
    await user.click(createBtn);

    expect(createInvoice).toHaveBeenCalledWith({
      customerId: "c1",
      currencyCode: "TRY",
      items: [
        {
          deliveryNoteItemId: "i1",
          enteredQuantity: 2000,
          enteredPackagingId: null,
          viewMode: "Piece",
          unitPrice: 0,
          taxCodeId: null,
        },
      ],
    });
  });
});

