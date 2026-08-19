import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";
import { customerStatusLabel, listCustomers, mapCustomerSummary } from "@/lib/sales/customers";

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
});
