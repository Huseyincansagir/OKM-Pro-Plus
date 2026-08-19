import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";

describe("form controls", () => {
  it("associates input errors and hides the hint while invalid", () => {
    render(
      <Input label="Kod" name="kod" hint="Yardım metni" error="Bu alan zorunludur" />,
    );

    const field = screen.getByLabelText(/Kod/);
    expect(field).toHaveAttribute("aria-invalid", "true");
    expect(field).toHaveAttribute("aria-describedby", "kod-error");
    expect(screen.getByText("Bu alan zorunludur")).toHaveAttribute("id", "kod-error");
    expect(screen.queryByText("Yardım metni")).not.toBeInTheDocument();
  });

  it("shows the hint when there is no error", () => {
    render(<Input label="Kod" name="kod" hint="Yardım metni" />);

    const field = screen.getByLabelText("Kod");
    expect(field).not.toHaveAttribute("aria-invalid");
    expect(field).toHaveAttribute("aria-describedby", "kod-hint");
    expect(screen.getByText("Yardım metni")).toBeInTheDocument();
  });

  it("associates select errors with the field", () => {
    render(
      <Select
        label="Depo"
        name="depo"
        error="Depo seçin"
        options={[{ value: "", label: "Seçin" }]}
      />,
    );

    const field = screen.getByLabelText(/Depo/);
    expect(field).toHaveAttribute("aria-invalid", "true");
    expect(field).toHaveAttribute("aria-describedby", "depo-error");
    expect(screen.getByText("Depo seçin")).toBeInTheDocument();
  });
});
