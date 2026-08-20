import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

const routerPush = vi.hoisted(() => vi.fn());
vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: routerPush, replace: vi.fn(), prefetch: vi.fn(), back: vi.fn() }),
  usePathname: vi.fn(() => "/"),
  useSearchParams: () => new URLSearchParams(),
}));
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { QuoteDetail } from "@/components/sales/quote-detail";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import {
  convertQuoteToOrder,
  getQuote,
  issueQuote,
} from "@/lib/sales/quotes";
import { setWindowWidth } from "@/test/viewport";

vi.mock("@/lib/sales/quotes", async () => {
  const actual = await vi.importActual<typeof import("@/lib/sales/quotes")>(
    "@/lib/sales/quotes",
  );
  return { ...actual, getQuote: vi.fn(), issueQuote: vi.fn(), convertQuoteToOrder: vi.fn() };
});

const detail = {
  id: "q1",
  quoteNumber: "TEK-2026-000001",
  status: "Draft",
  customerId: "c1",
  customerCode: "MUS-2026-000001",
  customerLegalName: "Acme",
  quoteRequestId: "qr1",
  currencyCode: "TRY",
  totalNet: 20000,
  totalTax: 0,
  totalGross: 20000,
  validUntil: null,
  issuedAt: null,
  issuedBy: null,
  itemCount: 1,
  createdAt: "2026-08-19T10:00:00Z",
  items: [
    {
      id: "i1",
      productId: "p1",
      quoteRequestItemId: "l1",
      enteredQuantity: 5,
      enteredPackagingId: "pkg",
      quantityBase: 10000,
      packagingName: "Koli",
      unitPrice: 2,
      listUnitPrice: 2,
      priceListId: "pl1",
      taxCode: null,
      lineNet: 20000,
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

describe("QuoteDetail", () => {
  beforeEach(() => {
    setWindowWidth(1280);
    resetSessionStore();
    routerPush.mockReset();
    vi.mocked(getQuote).mockReset();
    vi.mocked(issueQuote).mockReset();
    vi.mocked(convertQuoteToOrder).mockReset();
  });

  afterEach(() => {
    resetSessionStore();
  });

  it("shows server quantityBase and lineNet and hides issue without permission", async () => {
    authenticate(["quote.read"]);
    vi.mocked(getQuote).mockResolvedValue(detail);

    render(<QuoteDetail id="q1" />);

    expect(await screen.findByRole("heading", { name: "TEK-2026-000001" })).toBeInTheDocument();
    expect(screen.getByText("10000")).toBeInTheDocument();
    expect(screen.getByText("Koli")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Kesinleştir" })).not.toBeInTheDocument();
    expect(issueQuote).not.toHaveBeenCalled();
  });

  it("hides conversion without quote.convert permission", async () => {
    authenticate(["quote.read"]);
    vi.mocked(getQuote).mockResolvedValue({
      ...detail,
      status: "Issued",
      issuedAt: "2026-08-19T12:00:00Z",
    });

    render(<QuoteDetail id="q1" />);

    expect(await screen.findByRole("heading", { name: "TEK-2026-000001" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Teklifi Siparişe Dönüştür" })).not.toBeInTheDocument();
  });

  it("converts an issued quote and redirects to the created order", async () => {
    const user = userEvent.setup();
    authenticate(["quote.read", "quote.convert"]);
    vi.mocked(getQuote).mockResolvedValue({
      ...detail,
      status: "Issued",
      issuedAt: "2026-08-19T12:00:00Z",
    });
    vi.mocked(convertQuoteToOrder).mockResolvedValue({
      id: "o1",
      orderNumber: "SO-2026-000001",
      status: "Draft",
      customerId: "c1",
      customerCode: "MUS-2026-000001",
      customerLegalName: "Acme",
      sourceQuoteId: "q1",
      sourceQuoteNumber: "TEK-2026-000001",
      currencyCode: "TRY",
      totalNet: 20000,
      totalTax: 0,
      totalGross: 20000,
      itemCount: 1,
      createdAt: "2026-08-19T12:01:00Z",
      rowVersion: 1,
      items: [],
    });

    render(<QuoteDetail id="q1" />);

    await user.click(await screen.findByRole("button", { name: "Teklifi Siparişe Dönüştür" }));
    const dialog = await screen.findByRole("dialog");
    expect(within(dialog).getByText("TEK-2026-000001")).toBeInTheDocument();
    expect(within(dialog).getByText("Acme")).toBeInTheDocument();
    await user.click(within(dialog).getByRole("button", { name: "Sipariş oluştur" }));

    expect(convertQuoteToOrder).toHaveBeenCalledWith("q1");
    expect(routerPush).toHaveBeenCalledWith("/satis/siparisler/o1");
  });

  it("issues a draft after confirmation", async () => {
    const user = userEvent.setup();
    authenticate(["quote.read", "quote.issue"]);
    vi.mocked(getQuote).mockResolvedValue(detail);
    vi.mocked(issueQuote).mockResolvedValue({
      ...detail,
      status: "Issued",
      issuedAt: "2026-08-19T12:00:00Z",
    });

    render(<QuoteDetail id="q1" />);

    const open = await screen.findByRole("button", { name: "Kesinleştir" });
    await user.click(open);
    const dialog = await screen.findByRole("dialog");
    await user.click(within(dialog).getByRole("button", { name: "Kesinleştir" }));
    expect(issueQuote).toHaveBeenCalledWith("q1");
  });
});
