import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";
import {
  createCustomer,
  customerStatusLabel,
  getCustomerPriceContext,
  listCustomers,
  mapCustomerSummary,
} from "@/lib/sales/customers";

vi.mock("@/lib/api/client", () => ({
  apiRequest: vi.fn(),
}));

describe("mapCustomerSummary", () => {
  it("keeps master-data fields and drops extras", () => {
    const mapped = mapCustomerSummary({
      id: "c1",
      customerCode: "DEMO-001",
      legalName: "Demo Horeca Tedarik",
      status: "Active",
      email: "demo@example.local",
      phone: "555",
      createdAt: "2026-08-19T00:00:00Z",
      balance: 1285750,
      riskScore: 92,
    });
    expect(mapped).toEqual({
      id: "c1",
      customerCode: "DEMO-001",
      legalName: "Demo Horeca Tedarik",
      status: "Active",
      email: "demo@example.local",
      phone: "555",
      createdAt: "2026-08-19T00:00:00Z",
      primaryContactName: "",
      priceGroupCode: "",
      priceGroupName: "",
    });
    expect(mapped).not.toHaveProperty("balance");
    expect(mapped).not.toHaveProperty("riskScore");
    expect(customerStatusLabel("Active")).toBe("Aktif");
  });
});

describe("listCustomers", () => {
  beforeEach(() => {
    vi.mocked(apiRequest).mockReset();
  });

  it("calls GET /customers", async () => {
    vi.mocked(apiRequest).mockResolvedValue([
      { id: "c1", customerCode: "DEMO-001", legalName: "Demo", status: "Active" },
    ]);
    const rows = await listCustomers();
    expect(apiRequest).toHaveBeenCalledWith({ path: "/customers", method: "GET" });
    expect(rows[0].customerCode).toBe("DEMO-001");
  });

  it("rejects a non-array payload instead of fabricating rows", async () => {
    vi.mocked(apiRequest).mockResolvedValue({ items: [], totalDebt: 1 });
    await expect(listCustomers()).rejects.toBeInstanceOf(ApiError);
  });

  it("creates a customer without client-generated codes or balances", async () => {
    vi.mocked(apiRequest).mockResolvedValue({
      id: "c-new",
      customerCode: "MUS-2026-000001",
      legalName: "Acme",
      status: "Active",
    });

    const created = await createCustomer({ legalName: "Acme", email: "a@b.com" });

    const argument = vi.mocked(apiRequest).mock.calls[0][0];
    expect(argument.path).toBe("/customers");
    expect(argument.method).toBe("POST");
    expect(argument.idempotent).toBe(true);
    expect(argument.body).toEqual({
      legalName: "Acme",
      email: "a@b.com",
      phone: null,
      taxNumber: null,
      taxOffice: null,
    });
    expect(JSON.stringify(argument.body)).not.toContain("customerCode");
    expect(JSON.stringify(argument.body)).not.toContain("balance");
    expect(created.customerCode).toBe("MUS-2026-000001");
  });

  it("maps price context without inventing a list price or cari link", async () => {
    vi.mocked(apiRequest).mockResolvedValue({
      customerId: "c1",
      boundToCurrentAccount: false,
      customerPriceGroupCode: "VADELI",
      priceListCode: "VADELI",
      currencyCode: "TRY",
      prices: [{ productId: "p1", packagingId: null, unitPrice: 14.5 }],
    });
    const context = await getCustomerPriceContext("c1");
    expect(apiRequest).toHaveBeenCalledWith({
      path: "/customers/c1/price-context",
      method: "GET",
    });
    expect(context.boundToCurrentAccount).toBe(false);
    expect(context.prices[0].unitPrice).toBe(14.5);
  });
});
