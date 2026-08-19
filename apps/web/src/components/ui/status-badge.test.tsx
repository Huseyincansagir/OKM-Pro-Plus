import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { StatusBadge } from "@/components/ui/status-badge";

describe("StatusBadge", () => {
  it("renders Turkish text with an icon and does not rely on color alone", () => {
    const { container } = render(<StatusBadge status="pending" />);

    expect(screen.getByText("Onay bekliyor")).toBeInTheDocument();
    expect(container.querySelector("svg")).not.toBeNull();
  });
});
