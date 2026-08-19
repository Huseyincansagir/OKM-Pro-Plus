import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { QuoteCreate } from "@/components/sales/quote-create";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import { getQuoteRequest } from "@/lib/dashboard/quote-requests";
import { createQuote } from "@/lib/sales/quotes";
import { setWindowWidth } from "@/test/viewport";

vi.mock("@/lib/dashboard/quote-requests", async () => {
  const actual = await vi.importActual<typeof import("@/lib/dashboard/quote-requests")>(
    "@/lib/dashboard/quote-requests",
  );
  return { ...actual, getQuoteRequest: vi.fn() };
});

vi.mock("@/lib/sales/quotes", async () => {
  const actual = await vi.importActual<typeof import("@/lib/sales/quotes")>(
    "@/lib/sales/quotes",
  );
  return { ...actual, createQuote: vi.fn() };
});

const request = {
  id: "qr-1",
  requestNumber: "TLT-2026-0001",
  status: "InReview",
  source: "Public",
  candidateName: "Acme / Ali Veli",
  candidateEmail: "a@b.com",
  candidatePhone: "555",
  customerId: "c1",
  itemCount: 1,
  createdAt: "2026-08-19T10:00:00Z",
  items: [
    {
      id: "l1",
      productId: "p1",
      enteredQuantity: 5,
      enteredPackagingId: "pkg",
      quantityBase: 10000,
      packagingName: "Koli",
    },
  ],
};

function authenticate(permissions: string[]) {
  useSessionStore.getState().setAuthenticated({
    id: "u1",
    userName: "admin",
    displayName: "Yusuf Kaya",
    roles: ["admin"],
    permissions,
  });
}

describe("QuoteCreate", () => {
  beforeEach(() => {
    setWindowWidth(1280);
    resetSessionStore();
    vi.mocked(getQuoteRequest).mockReset();
    vi.mocked(createQuote).mockReset();
  });

  afterEach(() => {
    resetSessionStore();
  });

  it("hides the form without quote.create", async () => {
    authenticate(["quote.read"]);
    render(<QuoteCreate quoteRequestId="qr-1" />);
    expect(await screen.findByText("Teklif belgesi oluşturulamaz")).toBeInTheDocument();
    expect(getQuoteRequest).not.toHaveBeenCalled();
  });

  it("does not invent a product picker when quoteRequestId is missing", async () => {
    authenticate(["quote.create", "quote-request.read"]);
    render(<QuoteCreate quoteRequestId={null} />);
    expect(await screen.findByText("Talep seçilmedi")).toBeInTheDocument();
    expect(getQuoteRequest).not.toHaveBeenCalled();
  });

  it("requires a unit price and submits staff prices only", async () => {
    const user = userEvent.setup();
    authenticate(["quote.create", "quote-request.read"]);
    vi.mocked(getQuoteRequest).mockResolvedValue(request);
    vi.mocked(createQuote).mockResolvedValue({
      id: "q-new",
      quoteNumber: "TEK-2026-000001",
      status: "Draft",
      customerId: "c1",
      customerCode: "MUS-1",
      customerLegalName: "Acme",
      quoteRequestId: "qr-1",
      currencyCode: "TRY",
      totalNet: 10,
      totalTax: 0,
      totalGross: 10,
      validUntil: null,
      issuedAt: null,
      issuedBy: null,
      itemCount: 1,
      createdAt: "2026-08-19T10:00:00Z",
      items: [],
    });

    render(<QuoteCreate quoteRequestId="qr-1" />);
    expect(await screen.findByText("TLT-2026-0001")).toBeInTheDocument();
    expect(screen.getByText(/Temel karşılık: 10000/)).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Taslak teklif oluştur" }));
    expect(await screen.findByText("Her kalem için sıfır veya pozitif birim fiyat girin.")).toBeInTheDocument();
    expect(createQuote).not.toHaveBeenCalled();

    await user.type(screen.getByLabelText(/Birim fiyat/), "12.5");
    await user.click(screen.getByRole("button", { name: "Taslak teklif oluştur" }));

    expect(createQuote).toHaveBeenCalledWith({
      quoteRequestId: "qr-1",
      currencyCode: "TRY",
      validUntil: undefined,
      items: [{ quoteRequestItemId: "l1", unitPrice: 12.5 }],
    });
  });
});
