import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { OperationsDashboard } from "@/components/dashboard/operations-dashboard";
import { ApiError } from "@/lib/api/types";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import {
  listQuoteRequests,
  readSystemHealth,
} from "@/lib/dashboard/quote-requests";
import { setWindowWidth } from "@/test/viewport";

vi.mock("@/lib/dashboard/quote-requests", async () => {
  const actual = await vi.importActual<typeof import("@/lib/dashboard/quote-requests")>(
    "@/lib/dashboard/quote-requests",
  );
  return {
    ...actual,
    listQuoteRequests: vi.fn(),
    readSystemHealth: vi.fn(),
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

describe("OperationsDashboard", () => {
  beforeEach(() => {
    setWindowWidth(1280);
    resetSessionStore();
    vi.mocked(listQuoteRequests).mockReset();
    vi.mocked(readSystemHealth).mockReset();
  });

  afterEach(() => {
    resetSessionStore();
  });

  it("renders mockup chrome without fabricating sales or production numbers", async () => {
    authenticate(["quote-request.read", "system.read"]);
    vi.mocked(listQuoteRequests).mockResolvedValue([]);
    vi.mocked(readSystemHealth).mockResolvedValue({ status: "operational" });

    render(<OperationsDashboard />);

    expect(await screen.findByRole("heading", { name: "Genel Bakış" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Satış Trendi" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Üretim Performansı" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Son Aktiviteler" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Riskli Müşteriler" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Geciken Ödemeler" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Faturalaşmamış İrsaliyeler" })).toBeInTheDocument();
    expect(screen.getByLabelText("Bugünkü Satış: bağlı değil")).toBeInTheDocument();
    expect(screen.getByLabelText("Bugünkü Üretim: bağlı değil")).toBeInTheDocument();
    expect(screen.getByLabelText("Bekleyen Sipariş: bağlı değil")).toBeInTheDocument();
    expect(screen.getByLabelText("Tahsilat: bağlı değil")).toBeInTheDocument();
    expect(screen.queryByText(/1[.\s]?285[.\s]?750/)).not.toBeInTheDocument();
    expect(screen.queryByText(/%18[,.]6/)).not.toBeInTheDocument();
    expect(screen.queryByText(/965[.\s]?430/)).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Satış" })).not.toBeInTheDocument();
  });

  it("skips the quote list API when quote-request.read is missing", async () => {
    authenticate(["system.read"]);
    vi.mocked(readSystemHealth).mockResolvedValue({ status: "operational" });

    render(<OperationsDashboard />);

    expect(
      await screen.findByText("Teklif talepleri bu oturumda görünmez"),
    ).toBeInTheDocument();
    expect(listQuoteRequests).not.toHaveBeenCalled();
    expect(readSystemHealth).toHaveBeenCalledTimes(1);
    expect(screen.getByText(/gerçek yetki backend’dedir/)).toBeInTheDocument();
  });

  it("skips health when system.read is missing", async () => {
    authenticate(["quote-request.read"]);
    vi.mocked(listQuoteRequests).mockResolvedValue([]);

    render(<OperationsDashboard />);

    expect(await screen.findByText("Teklif talebi yok")).toBeInTheDocument();
    expect(readSystemHealth).not.toHaveBeenCalled();
    expect(screen.getByText("API bağlı değil")).toBeInTheDocument();
  });

  it("renders real quote rows in the activity slot without quantityBase", async () => {
    authenticate(["quote-request.read", "system.read"]);
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
    vi.mocked(readSystemHealth).mockResolvedValue({ status: "operational" });

    render(<OperationsDashboard />);

    expect(await screen.findByText("TLT-2026-0001")).toBeInTheDocument();
    expect(screen.getByText("Acme / Ali Veli · Alındı")).toBeInTheDocument();
    expect(screen.getByText("1 alındı")).toBeInTheDocument();
    expect(screen.getByText("API · Çalışıyor")).toBeInTheDocument();
    expect(screen.getByText("Teklif talebi")).toBeInTheDocument();
    expect(document.body.textContent).not.toContain("quantityBase");
    expect(screen.getByLabelText("Bekleyen Sipariş: bağlı değil")).toBeInTheDocument();
  });

  it("does not show a zero quote KPI when the list fails", async () => {
    authenticate(["quote-request.read"]);
    vi.mocked(listQuoteRequests).mockRejectedValue(new Error("Bağlantı koptu"));

    render(<OperationsDashboard />);

    expect(await screen.findByText("Bağlantı koptu")).toBeInTheDocument();
    expect(screen.queryByText("0 alındı")).not.toBeInTheDocument();
  });

  it("shows permission denied when the quote API returns 403", async () => {
    authenticate(["quote-request.read"]);
    vi.mocked(listQuoteRequests).mockRejectedValue(
      new ApiError({
        kind: "permission_denied",
        status: 403,
        detail: "Forbidden",
      }),
    );

    render(<OperationsDashboard />);

    expect(await screen.findByText("Bu işlem için yetkiniz yok")).toBeInTheDocument();
    expect(screen.queryByText("0 alındı")).not.toBeInTheDocument();
  });

  it("retries the quote list after an error", async () => {
    const user = userEvent.setup();
    authenticate(["quote-request.read"]);
    vi.mocked(listQuoteRequests)
      .mockRejectedValueOnce(new Error("Bağlantı koptu"))
      .mockResolvedValueOnce([]);

    render(<OperationsDashboard />);

    expect(await screen.findByText("Bağlantı koptu")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Tekrar dene" }));
    expect(await screen.findByText("Teklif talebi yok")).toBeInTheDocument();
    expect(listQuoteRequests).toHaveBeenCalledTimes(2);
  });

  it("does not treat a health failure as still loading", async () => {
    authenticate(["system.read"]);
    vi.mocked(readSystemHealth).mockRejectedValue(new Error("Health kapalı"));

    render(<OperationsDashboard />);

    expect(await screen.findByText("Health kapalı")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.queryByText("sorgulanıyor")).not.toBeInTheDocument();
    });
  });
});
