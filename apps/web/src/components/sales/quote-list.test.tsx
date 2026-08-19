import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { QuoteList } from "@/components/sales/quote-list";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import { listQuotes } from "@/lib/sales/quotes";
import { setWindowWidth } from "@/test/viewport";

vi.mock("@/lib/sales/quotes", async () => {
  const actual = await vi.importActual<typeof import("@/lib/sales/quotes")>(
    "@/lib/sales/quotes",
  );
  return { ...actual, listQuotes: vi.fn() };
});

function authenticate(permissions: string[]) {
  useSessionStore.getState().setAuthenticated({
    id: "u1",
    userName: "admin",
    displayName: "Yusuf Kaya",
    roles: ["admin"],
    permissions,
  });
}

describe("QuoteList", () => {
  beforeEach(() => {
    setWindowWidth(1280);
    resetSessionStore();
    vi.mocked(listQuotes).mockReset();
  });

  afterEach(() => {
    resetSessionStore();
  });

  it("skips the list API without quote.read", async () => {
    authenticate([]);
    render(<QuoteList />);
    expect(await screen.findByText("Teklifler bu oturumda görünmez")).toBeInTheDocument();
    expect(listQuotes).not.toHaveBeenCalled();
  });

  it("renders rows and counts from the real list window", async () => {
    authenticate(["quote.read"]);
    vi.mocked(listQuotes).mockResolvedValue([
      {
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
        itemCount: 2,
        createdAt: "2026-08-19T10:00:00Z",
      },
    ]);

    render(<QuoteList />);

    expect(await screen.findByText("TEK-2026-000001")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Teklifler" })).toBeInTheDocument();
    expect(screen.getByLabelText("Taslak: 1")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /TEK-2026-000001/ })).toHaveAttribute(
      "href",
      "/satis/teklifler/q1",
    );
  });
});
