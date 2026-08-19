import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";
import {
  listStaffProducts,
  mapStaffProductDetail,
  mapStaffProductSummary,
  staffProductStatusLabel,
} from "@/lib/catalog/staff-products";

vi.mock("@/lib/api/client", () => ({
  apiRequest: vi.fn(),
}));

describe("mapStaffProductSummary", () => {
  it("keeps master-data flags and drops stock extras", () => {
    const mapped = mapStaffProductSummary({
      id: "p1",
      code: "NAP-001",
      slug: "premium-pecete",
      name: "Premium Peçete",
      categoryName: "Peçeteler",
      isActive: true,
      isPublic: true,
      baseUom: { code: "Piece", displayName: "Adet" },
      packagings: [{ id: "pkg" }],
      createdAt: "2026-08-19T00:00:00Z",
      onHand: 9000,
      listPrice: 12.5,
    });
    expect(mapped).toEqual({
      id: "p1",
      code: "NAP-001",
      slug: "premium-pecete",
      name: "Premium Peçete",
      categoryName: "Peçeteler",
      isActive: true,
      isPublic: true,
      baseUomName: "Adet",
      packagingCount: 1,
      createdAt: "2026-08-19T00:00:00Z",
    });
    expect(mapped).not.toHaveProperty("onHand");
    expect(staffProductStatusLabel(mapped)).toBe("Public");
  });
});

describe("mapStaffProductDetail", () => {
  it("maps packaging quantityInBaseUom from the server", () => {
    const mapped = mapStaffProductDetail({
      id: "p1",
      code: "NAP-001",
      name: "Premium Peçete",
      isActive: true,
      packagings: [
        {
          id: "pkg",
          level: "Case",
          name: "Koli",
          quantityInBaseUom: 2000,
          isSellable: true,
        },
      ],
    });
    expect(mapped.packagings[0]).toMatchObject({
      name: "Koli",
      quantityInBaseUom: 2000,
      isSellable: true,
    });
  });
});

describe("staff product API", () => {
  beforeEach(() => {
    vi.mocked(apiRequest).mockReset();
  });

  it("calls GET /products and rejects a public-style page wrapper", async () => {
    vi.mocked(apiRequest).mockResolvedValue([{ id: "p1", code: "NAP-001", name: "Peçete" }]);
    const rows = await listStaffProducts();
    expect(apiRequest).toHaveBeenCalledWith({ path: "/products", method: "GET" });
    expect(rows[0].code).toBe("NAP-001");

    vi.mocked(apiRequest).mockResolvedValue({ items: [], totalCount: 1 });
    await expect(listStaffProducts()).rejects.toBeInstanceOf(ApiError);
  });
});
