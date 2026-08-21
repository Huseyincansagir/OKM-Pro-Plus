import { beforeEach, describe, expect, it, vi } from "vitest";
import { apiRequest } from "@/lib/api/client";
import { createInvoice, getInvoice, invoiceStatusKind, issueInvoice, mapInvoiceDetail } from "@/lib/finance/invoices";

vi.mock("@/lib/api/client", () => ({
  apiRequest: vi.fn(),
}));

describe("invoices client", () => {
  beforeEach(() => {
    vi.mocked(apiRequest).mockReset();
  });

  it("maps invoice detail correctly", () => {
    const raw = {
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
          deliveryNoteItemId: "dn-item-1",
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
    };

    const mapped = mapInvoiceDetail(raw);
    expect(mapped.id).toBe("inv-1");
    expect(mapped.invoiceNumber).toBe("INV-2026-000001");
    expect(mapped.grandTotal).toBe(1200);
    expect(mapped.items).toHaveLength(1);
    expect(mapped.items[0].unitPrice).toBe(10);
  });

  it("calls getInvoice and returns mapped detail", async () => {
    vi.mocked(apiRequest).mockResolvedValue({
      id: "inv-1",
      invoiceNumber: "INV-2026-000001",
      customerId: "cust-1",
      status: "Draft",
      currencyCode: "TRY",
      subtotal: 500,
      taxTotal: 100,
      grandTotal: 600,
      items: [],
      issuedAt: null,
      rowVersion: 1,
    });

    const result = await getInvoice("inv-1");
    expect(apiRequest).toHaveBeenCalledWith({ path: "/invoices/inv-1", method: "GET" });
    expect(result.id).toBe("inv-1");
    expect(result.grandTotal).toBe(600);
  });

  it("calls createInvoice with correct body and idempotency", async () => {
    vi.mocked(apiRequest).mockResolvedValue({
      id: "inv-created",
      invoiceNumber: "INV-2026-000002",
      customerId: "cust-1",
      status: "Draft",
      currencyCode: "TRY",
      subtotal: 2000,
      taxTotal: 400,
      grandTotal: 2400,
      items: [],
      issuedAt: null,
      rowVersion: 1,
    });

    const input = {
      customerId: "cust-1",
      currencyCode: "TRY",
      items: [
        {
          deliveryNoteItemId: "dni-1",
          enteredQuantity: 50,
          enteredPackagingId: null,
          viewMode: "Piece",
          unitPrice: 40,
          taxCodeId: null,
        },
      ],
    };

    const result = await createInvoice(input);
    expect(apiRequest).toHaveBeenCalledWith({
      path: "/invoices",
      method: "POST",
      body: {
        customerId: "cust-1",
        currencyCode: "TRY",
        items: [
          {
            deliveryNoteItemId: "dni-1",
            enteredQuantity: 50,
            enteredPackagingId: null,
            viewMode: "Piece",
            unitPrice: 40,
            taxCodeId: null,
          },
        ],
      },
      idempotent: true,
    });
    expect(result.id).toBe("inv-created");
  });

  it("calls issueInvoice with idempotent POST", async () => {
    vi.mocked(apiRequest).mockResolvedValue({
      id: "inv-1",
      invoiceNumber: "INV-2026-000001",
      customerId: "cust-1",
      status: "Issued",
      currencyCode: "TRY",
      subtotal: 1000,
      taxTotal: 200,
      grandTotal: 1200,
      items: [],
      issuedAt: "2026-08-21T10:00:00Z",
      rowVersion: 2,
    });

    const result = await issueInvoice("inv-1");
    expect(apiRequest).toHaveBeenCalledWith({
      path: "/invoices/inv-1/issue",
      method: "POST",
      idempotent: true,
    });
    expect(result.status).toBe("Issued");
    expect(result.issuedAt).toBe("2026-08-21T10:00:00Z");
  });

  it("maps status kinds correctly", () => {
    expect(invoiceStatusKind("Draft")).toBe("pending");
    expect(invoiceStatusKind("ReadyToIssue")).toBe("pending");
    expect(invoiceStatusKind("Issued")).toBe("success");
    expect(invoiceStatusKind("Paid")).toBe("success");
    expect(invoiceStatusKind("PartiallyPaid")).toBe("info");
    expect(invoiceStatusKind("Cancelled")).toBe("critical");
  });
});
