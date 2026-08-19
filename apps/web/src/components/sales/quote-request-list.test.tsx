import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { QuoteRequestList } from "@/components/sales/quote-request-list";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import { listQuoteRequests } from "@/lib/dashboard/quote-requests";
import { setWindowWidth } from "@/test/viewport";

vi.mock("@/lib/dashboard/quote-requests", async () => {
  const actual = await vi.importActual<typeof import("@/lib/dashboard/quote-requests")>(
    "@/lib/dashboard/quote-requests",
  );
  return {
    ...actual,
    listQuoteRequests: vi.fn(),
  };
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

describe("QuoteRequestList", () => {
  beforeEach(() => {
    setWindowWidth(1280);
    resetSessionStore();
    vi.mocked(listQuoteRequests).mockReset();
  });

  afterEach(() => {
    resetSessionStore();
  });

  it("skips the list API without quote-request.read", async () => {
    authenticate([]);
    render(<QuoteRequestList />);
    expect(
      await screen.findByText("Teklif talepleri bu oturumda görünmez"),
    ).toBeInTheDocument();
    expect(listQuoteRequests).not.toHaveBeenCalled();
  });

  it("renders rows and counts from the real list window", async () => {
    authenticate(["quote-request.read"]);
    vi.mocked(listQuoteRequests).mockResolvedValue([
      {
        id: "qr-1",
        requestNumber: "TLT-2026-0001",
        status: "Received",
        source: "Public",
        candidateName: "Acme / Ali Veli",
        itemCount: 2,
        createdAt: "2026-08-19T10:00:00Z",
      },
    ]);

    render(<QuoteRequestList />);

    expect(await screen.findByText("TLT-2026-0001")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Teklif Talepleri" })).toBeInTheDocument();
    expect(screen.getByLabelText("Alındı: 1")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /TLT-2026-0001/ })).toHaveAttribute(
      "href",
      "/satis/teklif-talepleri/qr-1",
    );
  });
});
