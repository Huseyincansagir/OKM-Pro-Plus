import { beforeEach, describe, expect, it, vi } from "vitest";
import { apiRequest } from "@/lib/api/client";
import { listPublicProducts, submitPublicQuoteRequest } from "@/lib/catalog/catalog-client";

vi.mock("@/lib/api/client", () => ({
  apiRequest: vi.fn(),
}));

describe("catalog-client", () => {
  beforeEach(() => {
    vi.mocked(apiRequest).mockReset();
  });

  it("lists public products without triggering auth refresh", async () => {
    vi.mocked(apiRequest).mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 24,
      totalCount: 0,
      hasNextPage: false,
    });

    await listPublicProducts({ search: "pecete", page: 1 });

    expect(apiRequest).toHaveBeenCalledWith({
      path: "/public/catalog/products?search=pecete&page=1",
      method: "GET",
      auth: false,
    });
  });

  it("submits quote requests without quantityBase or idempotency", async () => {
    vi.mocked(apiRequest).mockResolvedValue({
      id: "q1",
      requestNumber: "TLT-1",
      status: "Received",
      createdAt: "2026-08-19T00:00:00Z",
    });

    await submitPublicQuoteRequest({
      companyName: "Acme",
      contactName: "Ali Veli",
      phone: "555",
      email: "a@b.com",
      consentAccepted: true,
      items: [
        {
          productId: "p1",
          enteredQuantity: 5,
          enteredPackagingId: "pkg",
          viewMode: "Packaging",
        },
      ],
    });

    const argument = vi.mocked(apiRequest).mock.calls[0][0];
    expect(argument.auth).toBe(false);
    expect(argument.idempotent).toBe(false);
    expect(argument.path).toBe("/public/quote-requests");
    expect(JSON.stringify(argument.body)).not.toContain("quantityBase");
  });
});
