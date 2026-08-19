import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";
import {
  getQuoteRequest,
  listQuoteRequests,
  mapQuoteRequestDetail,
  mapQuoteRequestSummary,
  quoteRequestSourceLabel,
  quoteRequestStatusKind,
  quoteRequestStatusLabel,
  readSystemHealth,
  reviewQuoteRequest,
} from "@/lib/dashboard/quote-requests";

vi.mock("@/lib/api/client", () => ({
  apiRequest: vi.fn(),
}));

describe("mapQuoteRequestSummary", () => {
  it("keeps list fields and drops quantityBase item extras", () => {
    const mapped = mapQuoteRequestSummary({
      id: "qr-1",
      requestNumber: "TLT-2026-0001",
      status: "Received",
      source: "Public",
      candidateName: "Acme / Ali Veli",
      candidateEmail: "a@b.com",
      candidatePhone: "555",
      createdAt: "2026-08-19T10:00:00Z",
      items: [
        {
          id: "line-1",
          productId: "p1",
          enteredQuantity: 5,
          enteredPackagingId: "pkg",
          quantityBase: 10000,
          packagingSnapshot: "{}",
        },
      ],
      unitPrice: 12.5,
    });

    expect(mapped).toEqual({
      id: "qr-1",
      requestNumber: "TLT-2026-0001",
      status: "Received",
      source: "Public",
      candidateName: "Acme / Ali Veli",
      itemCount: 1,
      createdAt: "2026-08-19T10:00:00Z",
    });
    expect(mapped).not.toHaveProperty("quantityBase");
    expect(mapped).not.toHaveProperty("items");
    expect(mapped).not.toHaveProperty("unitPrice");
    expect(mapped).not.toHaveProperty("candidateEmail");
  });

  it("counts items only from the items array", () => {
    const mapped = mapQuoteRequestSummary({
      id: "qr-2",
      requestNumber: "TLT-2",
      itemCount: 99,
      items: [{ id: "a" }, { id: "b" }],
    });
    expect(mapped.itemCount).toBe(2);
  });
});

describe("quote request status labels", () => {
  it("maps backend statuses without inventing Reviewed", () => {
    expect(quoteRequestStatusLabel("Received")).toBe("Alındı");
    expect(quoteRequestStatusLabel("InReview")).toBe("İncelemede");
    expect(quoteRequestStatusLabel("Converted")).toBe("Dönüştürüldü");
    expect(quoteRequestStatusLabel("Rejected")).toBe("Reddedildi");
    expect(quoteRequestStatusLabel("Closed")).toBe("Kapatıldı");
    expect(quoteRequestStatusKind("Received")).toBe("pending");
    expect(quoteRequestStatusKind("InReview")).toBe("active");
    expect(quoteRequestSourceLabel("Public")).toBe("Public katalog");
  });
});

describe("quote-requests client", () => {
  beforeEach(() => {
    vi.mocked(apiRequest).mockReset();
  });

  it("lists quote requests from GET /quote-requests", async () => {
    vi.mocked(apiRequest).mockResolvedValue([
      {
        id: "qr-1",
        requestNumber: "TLT-1",
        status: "Received",
        source: "Public",
        candidateName: "Acme",
        items: [],
        createdAt: "2026-08-19T00:00:00Z",
      },
    ]);

    const rows = await listQuoteRequests();

    expect(apiRequest).toHaveBeenCalledWith({
      path: "/quote-requests",
      method: "GET",
    });
    expect(rows).toHaveLength(1);
    expect(rows[0].requestNumber).toBe("TLT-1");
  });

  it("rejects a non-array list payload instead of fabricating rows", async () => {
    vi.mocked(apiRequest).mockResolvedValue({ totalSales: 1285750, items: [] });

    await expect(listQuoteRequests()).rejects.toBeInstanceOf(ApiError);
  });

  it("reads system health without inventing operational status", async () => {
    vi.mocked(apiRequest).mockResolvedValue({
      status: "operational",
      service: "FactoryErp.Api",
    });

    const health = await readSystemHealth();

    expect(apiRequest).toHaveBeenCalledWith({
      path: "/system/health",
      method: "GET",
    });
    expect(health).toEqual({ status: "operational" });
  });

  it("reads a quote request detail without client-side conversion", async () => {
    vi.mocked(apiRequest).mockResolvedValue({
      id: "qr-1",
      requestNumber: "TLT-1",
      status: "Received",
      source: "Public",
      candidateName: "Acme / Ali",
      candidateEmail: "a@b.com",
      candidatePhone: "555",
      createdAt: "2026-08-19T00:00:00Z",
      items: [
        {
          id: "l1",
          productId: "p1",
          enteredQuantity: 5,
          enteredPackagingId: "pkg",
          quantityBase: 10000,
          packagingSnapshot: JSON.stringify({ name: "Koli" }),
        },
      ],
    });

    const detail = await getQuoteRequest("qr-1");
    expect(apiRequest).toHaveBeenCalledWith({
      path: "/quote-requests/qr-1",
      method: "GET",
    });
    expect(detail.items[0]).toMatchObject({
      enteredQuantity: 5,
      quantityBase: 10000,
      packagingName: "Koli",
    });
  });

  it("reviews with null customerId and no quantityBase", async () => {
    vi.mocked(apiRequest).mockResolvedValue({
      id: "qr-1",
      requestNumber: "TLT-1",
      status: "InReview",
      source: "Public",
      candidateName: "Acme",
      items: [],
      createdAt: "2026-08-19T00:00:00Z",
    });

    await reviewQuoteRequest("qr-1", null);

    const argument = vi.mocked(apiRequest).mock.calls[0][0];
    expect(argument.path).toBe("/quote-requests/qr-1/review");
    expect(argument.method).toBe("POST");
    expect(argument.idempotent).toBe(true);
    expect(argument.body).toEqual({ customerId: null });
    expect(JSON.stringify(argument.body)).not.toContain("quantityBase");
  });
});

describe("mapQuoteRequestDetail", () => {
  it("keeps server quantityBase on lines and does not invent packaging math", () => {
    const detail = mapQuoteRequestDetail({
      id: "qr-1",
      requestNumber: "TLT-1",
      status: "Received",
      source: "Public",
      candidateName: "Acme",
      candidateEmail: "a@b.com",
      items: [
        {
          id: "l1",
          enteredQuantity: 2,
          quantityBase: 4000,
          packagingSnapshot: '{"name":"Koli","quantityInBaseUom":2000}',
        },
      ],
    });
    expect(detail.items[0].quantityBase).toBe(4000);
    expect(detail.items[0].packagingName).toBe("Koli");
    expect(detail.candidateEmail).toBe("a@b.com");
  });
});
