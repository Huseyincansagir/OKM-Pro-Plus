import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { EmptyState } from "@/components/states/empty-state";
import { ErrorState } from "@/components/states/error-state";
import { PermissionDenied } from "@/components/states/permission-denied";

describe("state components", () => {
  it("renders empty copy with an optional action", () => {
    render(
      <EmptyState
        title="Kayıt yok"
        description="Henüz sipariş yok."
        action={<button type="button">Oluştur</button>}
      />,
    );

    expect(screen.getByText("Kayıt yok")).toBeInTheDocument();
    expect(screen.getByText("Henüz sipariş yok.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Oluştur" })).toBeInTheDocument();
  });

  it("exposes error copy as an alert and retries", async () => {
    const user = userEvent.setup();
    const onRetry = vi.fn();
    render(
      <ErrorState title="Yüklenemedi" description="Bağlantı kesildi." onRetry={onRetry} />,
    );

    expect(screen.getByRole("alert")).toHaveTextContent("Yüklenemedi");
    await user.click(screen.getByRole("button", { name: "Tekrar dene" }));
    expect(onRetry).toHaveBeenCalledTimes(1);
  });

  it("explains permission denial without implying UI is authorization", () => {
    render(<PermissionDenied />);

    expect(screen.getByRole("alert")).toHaveTextContent("Bu işlem için yetkiniz yok");
    expect(
      screen.getByText(/Gerekli yetki backend tarafından doğrulanır/),
    ).toBeInTheDocument();
  });
});
