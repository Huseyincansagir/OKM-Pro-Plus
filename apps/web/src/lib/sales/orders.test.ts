import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";
import {
  canDecideSalesOrder,
  canSubmitSalesOrder,
  listSalesOrders,
  mapSalesOrderDetail,
  mapSalesOrderSummary,
  rejectSalesOrder,
  salesOrderStatusLabel,
  submitSalesOrder,
} from "@/lib/sales/orders";

vi.mock("@/lib/api/client", () => ({
  apiRequest: vi.fn(),
}));

describe("mapSalesOrderSummary", () => {
  it("keeps server totals and drops extras", () => {
    const mapped = mapSalesOrderSummary({
      id: "o1",
      orderNumber: "SO-2026-000001",
      status: "Draft",
      customerId: "c1",
      customerCode: "MUS-1",
      customerLegalName: "Acme",
      sourceQuoteId: null,
      sourceQuoteNumber: null,
      currencyCode: "TRY",
      totalNet: 80,
      totalTax: 0,
      totalGross: 80,
      createdAt: "2026-08-19T00:00:00Z",
      items: [{ id: "i1" }],
      riskScore: 9,
    });
    expect(mapped).toEqual({
      id: "o1",
      orderNumber: "SO-2026-000001",
      status: "Draft",
      customerId: "c1",
      customerCode: "MUS-1",
      customerLegalName: "Acme",
      sourceQuoteId: null,
      sourceQuoteNumber: null,
      currencyCode: "TRY",
      totalNet: 80,
      totalTax: 0,
      totalGross: 80,
      itemCount: 1,
      createdAt: "2026-08-19T00:00:00Z",
    });
    expect(mapped).not.toHaveProperty("riskScore");
    expect(salesOrderStatusLabel("PendingApproval")).toBe("Onay bekliyor");
    expect(canSubmitSalesOrder("Draft")).toBe(true);
    expect(canDecideSalesOrder("PendingApproval")).toBe(true);
  });

  it("does not invent totals when the server omits them", () => {
    const mapped = mapSalesOrderSummary({ id: "o1", orderNumber: "SO-1", items: [] });
    expect(mapped.totalGross).toBeNull();
  });
});

describe("mapSalesOrderDetail", () => {
  it("maps server remainingQty and does not invent quantityBase", () => {
    const mapped = mapSalesOrderDetail({
      id: "o1",
      orderNumber: "SO-1",
      status: "Approved",
      items: [
        {
          id: "i1",
          productId: "p1",
          enteredQuantity: 5,
          orderedQty: 10000,
          remainingQty: 10000,
          reservedQty: 10000,
          shippedQty: 0,
          unitPrice: 2,
          packagingSnapshot: JSON.stringify({ name: "Koli" }),
        },
      ],
    });
    expect(mapped.items[0]).toMatchObject({
      packagingName: "Koli",
      orderedQty: 10000,
      remainingQty: 10000,
    });
  });
});

describe("order API", () => {
  beforeEach(() => {
    vi.mocked(apiRequest).mockReset();
  });

  it("calls GET /orders and rejects a non-array payload", async () => {
    vi.mocked(apiRequest).mockResolvedValue([{ id: "o1", orderNumber: "SO-1" }]);
    const rows = await listSalesOrders();
    expect(apiRequest).toHaveBeenCalledWith({ path: "/orders", method: "GET" });
    expect(rows[0].orderNumber).toBe("SO-1");

    vi.mocked(apiRequest).mockResolvedValue({ items: [] });
    await expect(listSalesOrders()).rejects.toBeInstanceOf(ApiError);
  });

  it("submits and rejects via existing command routes", async () => {
    vi.mocked(apiRequest).mockResolvedValue({ id: "o1", status: "PendingApproval", items: [] });
    await submitSalesOrder("o1");
    expect(apiRequest).toHaveBeenCalledWith({
      path: "/orders/o1/submit",
      method: "POST",
      idempotent: true,
    });

    vi.mocked(apiRequest).mockResolvedValue({ id: "o1", status: "Cancelled", items: [] });
    await rejectSalesOrder("o1", "Stok yok");
    expect(apiRequest).toHaveBeenCalledWith({
      path: "/orders/o1/reject",
      method: "POST",
      body: { comment: "Stok yok" },
      idempotent: true,
    });
  });
});
