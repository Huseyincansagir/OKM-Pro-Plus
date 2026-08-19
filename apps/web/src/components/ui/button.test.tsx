import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { Button } from "@/components/ui/button";

describe("Button", () => {
  it("disables the control and marks it busy while loading", () => {
    render(<Button loading>Kaydet</Button>);

    const button = screen.getByRole("button", { name: /Kaydet/ });
    expect(button).toBeDisabled();
    expect(button).toHaveAttribute("aria-busy", "true");
    expect(screen.getByLabelText("Yükleniyor")).toBeInTheDocument();
  });
});
