import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { DesignSystemPreview } from "@/components/preview/design-system-preview";
import { ToastProvider } from "@/components/ui/toast";
import { setWindowWidth } from "@/test/viewport";

describe("DesignSystemPreview quantity wiring", () => {
  it("switches display strings without mutating operation quantity fields", async () => {
    const user = userEvent.setup();
    setWindowWidth(1280);
    render(
      <ToastProvider>
        <DesignSystemPreview />
      </ToastProvider>,
    );

    expect(screen.getByText("viewMode")).toBeInTheDocument();
    expect(screen.getByText("Packaging")).toBeInTheDocument();
    expect(screen.getByText("pkg-case-demo")).toBeInTheDocument();
    expect(screen.getByText("Giriş: 5 Koli")).toBeInTheDocument();
    expect(screen.getByText("10.000 adet")).toBeInTheDocument();

    await user.click(screen.getByRole("radio", { name: "Temel Birim" }));

    expect(screen.getByText("BaseUnit")).toBeInTheDocument();
    expect(screen.getByText("Giriş: 10.000 adet")).toBeInTheDocument();
    expect(screen.getByText("pkg-case-demo")).toBeInTheDocument();
    expect(screen.getByText("Girilen miktar").closest("div")).toHaveTextContent("5");
    expect(screen.getByText("10.000 adet / 50.000 adet")).toBeInTheDocument();
  });
});
