import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { QuoteRequestDetail } from "@/components/sales/quote-request-detail";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import {
  getQuoteRequest,
  reviewQuoteRequest,
} from "@/lib/dashboard/quote-requests";
import { setWindowWidth } from "@/test/viewport";

vi.mock("@/lib/dashboard/quote-requests", async () => {
  const actual = await vi.importActual<typeof import("@/lib/dashboard/quote-requests")>(
    "@/lib/dashboard/quote-requests",
  );
  return {
    ...actual,
    getQuoteRequest: vi.fn(),
    reviewQuoteRequest: vi.fn(),
  };
});

const detail = {
  id: "qr-1",
  requestNumber: "TLT-2026-0001",
  status: "Received",
  source: "Public",
  candidateName: "Acme / Ali Veli",
  candidateEmail: "a@b.com",
  candidatePhone: "555",
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

describe("QuoteRequestDetail", () => {
  beforeEach(() => {
    setWindowWidth(1280);
    resetSessionStore();
    vi.mocked(getQuoteRequest).mockReset();
    vi.mocked(reviewQuoteRequest).mockReset();
  });

  afterEach(() => {
    resetSessionStore();
  });

  it("shows server quantityBase and does not offer review without permission", async () => {
    authenticate(["quote-request.read"]);
    vi.mocked(getQuoteRequest).mockResolvedValue(detail);

    render(<QuoteRequestDetail id="qr-1" />);

    expect(await screen.findByRole("heading", { name: "TLT-2026-0001" })).toBeInTheDocument();
    expect(screen.getByText("10000")).toBeInTheDocument();
    expect(screen.getByText("Koli")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "İncelemeye al" })).not.toBeInTheDocument();
    expect(reviewQuoteRequest).not.toHaveBeenCalled();
  });

  it("reviews with null customerId after confirmation", async () => {
    const user = userEvent.setup();
    authenticate(["quote-request.read", "quote-request.review"]);
    vi.mocked(getQuoteRequest).mockResolvedValue(detail);
    vi.mocked(reviewQuoteRequest).mockResolvedValue({
      ...detail,
      status: "InReview",
    });

    render(<QuoteRequestDetail id="qr-1" />);

    const openReview = await screen.findByRole("button", { name: "İncelemeye al" });
    await user.click(openReview);
    const dialog = await screen.findByRole("dialog");
    expect(dialog).toHaveTextContent("customerId: null");
    await user.click(within(dialog).getByRole("button", { name: "İncelemeye al" }));
    expect(reviewQuoteRequest).toHaveBeenCalledWith("qr-1", null);
  });
});
