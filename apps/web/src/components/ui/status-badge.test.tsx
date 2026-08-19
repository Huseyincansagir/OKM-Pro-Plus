import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { StatusBadge, type StatusKind } from "@/components/ui/status-badge";

const statuses: Array<{ status: StatusKind; label: string }> = [
  { status: "pending", label: "Onay bekliyor" },
  { status: "active", label: "Hazırlanıyor" },
  { status: "success", label: "Tamamlandı" },
  { status: "critical", label: "Kritik" },
  { status: "info", label: "İzleniyor" },
  { status: "inactive", label: "Pasif" },
];

describe("StatusBadge", () => {
  it.each(statuses)(
    "renders $label with an icon so status is not color-only",
    ({ status, label }) => {
      const { container } = render(<StatusBadge status={status} />);

      expect(screen.getByText(label)).toBeInTheDocument();
      expect(container.querySelector("svg")).not.toBeNull();
    },
  );

  it("accepts an override label", () => {
    render(<StatusBadge status="pending" label="Onaya gönderildi" />);
    expect(screen.getByText("Onaya gönderildi")).toBeInTheDocument();
  });
});
