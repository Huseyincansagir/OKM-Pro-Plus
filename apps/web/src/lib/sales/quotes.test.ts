import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";
import {
  canCreateQuoteFromRequest,
  canIssueQuote,
  createQuote,
  issueQuote,
  listQuotes,
  mapQuoteDetail,
  mapQuoteSummary,
  quoteStatusLabel,
} from "@/lib/sales/quotes";

vi.mock("@/lib/api/client", () => ({
  apiRequest: vi.fn(),
}));

describe("mapQuoteSummary", () => {
  it("keeps server totals and drops extras", () => {
    const mapped = mapQuoteSummary({
      id: "q1",
      quoteNumber: "TEK-2026-000001",
      status: "Draft",
      customerId: "c1",
      customerCode: "MUS-2026-000001",
      customerLegalName: "Acme",
      quoteRequestId: "qr1",
      currencyCode: "TRY",
      totalNet: 120,
      totalTax: 0,
      totalGross: 120,
      createdAt: "2026-08-19T00:00:00Z",
      items: [{ id: "i1" }],
      suggestedPrice: 99,
      riskScore: 1,
    });
    expect(mapped).toEqual({
      id: "q1",
      quoteNumber: "TEK-2026-000001",
      status: "Draft",
      customerId: "c1",
      customerCode: "MUS-2026-000001",
      customerLegalName: "Acme",
      quoteRequestId: "qr1",
      currencyCode: "TRY",
      totalNet: 120,
      totalTax: 0,
      totalGross: 120,
      validUntil: null,
      issuedAt: null,
      itemCount: 1,
      createdAt: "2026-08-19T00:00:00Z",
    });
    expect(mapped).not.toHaveProperty("suggestedPrice");
    expect(quoteStatusLabel("Draft")).toBe("Taslak");
    expect(canCreateQuoteFromRequest("InReview", "c1")).toBe(true);
    expect(canCreateQuoteFromRequest("InReview", null)).toBe(false);
    expect(canIssueQuote("Draft")).toBe(true);
    expect(canIssueQuote("Issued")).toBe(false);
  });

  it("does not invent totals when the server omits them", () => {
    const mapped = mapQuoteSummary({
      id: "q1",
      quoteNumber: "TEK-2026-000002",
      status: "Draft",
      items: [],
    });
    expect(mapped.totalNet).toBeNull();
    expect(mapped.totalGross).toBeNull();
  });
});

describe("mapQuoteDetail", () => {
  it("maps lineNet from the server and does not invent quantityBase", () => {
    const mapped = mapQuoteDetail({
      id: "q1",
      quoteNumber: "TEK-2026-000001",
      status: "Draft",
      customerId: "c1",
      items: [
        {
          id: "i1",
          productId: "p1",
          quoteRequestItemId: "l1",
          enteredQuantity: 5,
          quantityBase: 10000,
          unitPrice: 2,
          lineNet: 20000,
          packagingSnapshot: JSON.stringify({ name: "Koli" }),
        },
      ],
    });
    expect(mapped.items[0]).toMatchObject({
      packagingName: "Koli",
      quantityBase: 10000,
      unitPrice: 2,
      lineNet: 20000,
    });
  });
});

describe("quote API", () => {
  beforeEach(() => {
    vi.mocked(apiRequest).mockReset();
  });

  it("calls GET /quotes and rejects a non-array payload", async () => {
    vi.mocked(apiRequest).mockResolvedValue([{ id: "q1", quoteNumber: "TEK-1" }]);
    const rows = await listQuotes();
    expect(apiRequest).toHaveBeenCalledWith({ path: "/quotes", method: "GET" });
    expect(rows[0].quoteNumber).toBe("TEK-1");

    vi.mocked(apiRequest).mockResolvedValue({ items: [], total: 0 });
    await expect(listQuotes()).rejects.toBeInstanceOf(ApiError);
  });

  it("creates a quote with staff prices and no client quantityBase", async () => {
    vi.mocked(apiRequest).mockResolvedValue({
      id: "q-new",
      quoteNumber: "TEK-2026-000001",
      status: "Draft",
      items: [],
    });

    const created = await createQuote({
      quoteRequestId: "qr1",
      currencyCode: "TRY",
      items: [{ quoteRequestItemId: "l1", unitPrice: 12.5 }],
    });

    const argument = vi.mocked(apiRequest).mock.calls[0][0];
    expect(argument.path).toBe("/quotes");
    expect(argument.method).toBe("POST");
    expect(argument.idempotent).toBe(true);
    expect(argument.body).toEqual({
      quoteRequestId: "qr1",
      currencyCode: "TRY",
      validUntil: null,
      items: [{ quoteRequestItemId: "l1", unitPrice: 12.5, taxCode: null }],
    });
    expect(JSON.stringify(argument.body)).not.toContain("quantityBase");
    expect(created.quoteNumber).toBe("TEK-2026-000001");
  });

  it("issues a draft via POST /quotes/{id}/issue", async () => {
    vi.mocked(apiRequest).mockResolvedValue({
      id: "q1",
      quoteNumber: "TEK-2026-000001",
      status: "Issued",
      items: [],
    });
    const issued = await issueQuote("q1");
    expect(apiRequest).toHaveBeenCalledWith({
      path: "/quotes/q1/issue",
      method: "POST",
      idempotent: true,
    });
    expect(issued.status).toBe("Issued");
  });
});
