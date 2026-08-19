import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";
import { listCurrentAccounts, listInvoices, mapAccountRow } from "@/lib/finance/ledgers";

vi.mock("@/lib/api/client", () => ({
  apiRequest: vi.fn(),
}));

describe("finance ledgers", () => {
  beforeEach(() => {
    vi.mocked(apiRequest).mockReset();
  });

  it("does not invent a zero balance when totals are missing", () => {
    const mapped = mapAccountRow({ customerId: "c1", currencyCode: "TRY" });
    expect(mapped.balance).toBeNull();
    expect(mapped.debitTotal).toBeNull();
  });

  it("lists invoices from GET /invoices", async () => {
    vi.mocked(apiRequest).mockResolvedValue([
      { id: "i1", invoiceNumber: "INV-1", status: "Draft", grandTotal: 10, items: [{}] },
    ]);
    const rows = await listInvoices();
    expect(apiRequest).toHaveBeenCalledWith({ path: "/invoices", method: "GET" });
    expect(rows[0].grandTotal).toBe(10);
    expect(rows[0].itemCount).toBe(1);
  });

  it("rejects a non-array current-account payload", async () => {
    vi.mocked(apiRequest).mockResolvedValue({ items: [], totalDebt: 0 });
    await expect(listCurrentAccounts()).rejects.toBeInstanceOf(ApiError);
  });
});
