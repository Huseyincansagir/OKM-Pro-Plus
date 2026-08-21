import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { InvoiceDetailBoard } from "@/components/finance/invoice-detail";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import { getInvoice, issueInvoice } from "@/lib/finance/invoices";
import { setWindowWidth } from "@/test/viewport";

vi.mock("@/lib/finance/invoices", async () => {
  const actual = await vi.importActual<typeof import("@/lib/finance/invoices")>(
    "@/lib/finance/invoices",
  );
  return {
    ...actual,
    getInvoice: vi.fn(),
    issueInvoice: vi.fn(),
  };
});

describe("InvoiceDetailBoard", () => {
  beforeEach(() => {
    setWindowWidth(1280);
    resetSessionStore();
    vi.mocked(getInvoice).mockReset();
    vi.mocked(issueInvoice).mockReset();
  });

  afterEach(() => {
    resetSessionStore();
  });

  it("shows permission denied when invoice.read is missing", async () => {
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "staff",
      displayName: "Staff User",
      roles: ["staff"],
      permissions: [],
    });

    render(<InvoiceDetailBoard id="inv-1" />);
    expect(await screen.findByText("Fatura bu oturumda görünmez")).toBeInTheDocument();
    expect(getInvoice).not.toHaveBeenCalled();
  });

  it("renders draft invoice and shows issue action with permission", async () => {
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Admin User",
      roles: ["admin"],
      permissions: ["invoice.read", "invoice.issue"],
    });

    vi.mocked(getInvoice).mockResolvedValue({
      id: "inv-1",
      invoiceNumber: "INV-2026-000001",
      customerId: "cust-1",
      status: "Draft",
      currencyCode: "TRY",
      subtotal: 1000,
      taxTotal: 200,
      grandTotal: 1200,
      items: [
        {
          id: "item-1",
          deliveryNoteItemId: "dni-1",
          productId: "prod-1",
          quantityBase: 100,
          enteredQuantity: 100,
          enteredPackagingId: null,
          unitPrice: 10,
          lineTotal: 1000,
          rowVersion: 1,
        },
      ],
      issuedAt: null,
      rowVersion: 1,
    });

    render(<InvoiceDetailBoard id="inv-1" />);
    expect(await screen.findAllByText("INV-2026-000001")).not.toHaveLength(0);
    expect(screen.getAllByText("Draft")).not.toHaveLength(0);
    expect(screen.getByRole("button", { name: "Faturayı kesinleştir (Issue)" })).toBeInTheDocument();
  });

  it("issues invoice on confirmation and transitions to issued state", async () => {
    const user = userEvent.setup();
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Admin User",
      roles: ["admin"],
      permissions: ["invoice.read", "invoice.issue", "current-account.read"],
    });

    vi.mocked(getInvoice).mockResolvedValue({
      id: "inv-1",
      invoiceNumber: "INV-2026-000001",
      customerId: "cust-1",
      status: "Draft",
      currencyCode: "TRY",
      subtotal: 1000,
      taxTotal: 200,
      grandTotal: 1200,
      items: [
        {
          id: "item-1",
          deliveryNoteItemId: "dni-1",
          productId: "prod-1",
          quantityBase: 100,
          enteredQuantity: 100,
          enteredPackagingId: null,
          unitPrice: 10,
          lineTotal: 1000,
          rowVersion: 1,
        },
      ],
      issuedAt: null,
      rowVersion: 1,
    });

    vi.mocked(issueInvoice).mockResolvedValue({
      id: "inv-1",
      invoiceNumber: "INV-2026-000001",
      customerId: "cust-1",
      status: "Issued",
      currencyCode: "TRY",
      subtotal: 1000,
      taxTotal: 200,
      grandTotal: 1200,
      items: [
        {
          id: "item-1",
          deliveryNoteItemId: "dni-1",
          productId: "prod-1",
          quantityBase: 100,
          enteredQuantity: 100,
          enteredPackagingId: null,
          unitPrice: 10,
          lineTotal: 1000,
          rowVersion: 1,
        },
      ],
      issuedAt: "2026-08-21T10:00:00Z",
      rowVersion: 2,
    });

    render(<InvoiceDetailBoard id="inv-1" />);
    const issueBtn = await screen.findByRole("button", { name: "Faturayı kesinleştir (Issue)" });
    await user.click(issueBtn);

    expect(await screen.findByRole("heading", { name: "Faturayı kesinleştir (Issue)" })).toBeInTheDocument();
    const confirmBtn = screen.getByRole("button", { name: "Kesinleştir (Issue)" });
    await user.click(confirmBtn);

    expect(issueInvoice).toHaveBeenCalledWith("inv-1");
  });
});
