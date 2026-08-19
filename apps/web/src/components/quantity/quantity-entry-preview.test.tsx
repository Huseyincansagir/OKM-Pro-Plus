import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { QuantityEntryPreview } from "@/components/quantity/quantity-entry-preview";

describe("QuantityEntryPreview", () => {
  it("renders the supplied canonical values without converting", () => {
    render(
      <QuantityEntryPreview
        displayQuantity={5}
        displayUnit="Koli"
        baseQuantity={1}
        baseUnit="adet"
        conversionLabel="sunucu sonucu"
      />,
    );

    expect(screen.getByText("Giriş: 5 Koli")).toBeInTheDocument();
    expect(screen.getByText("1 adet")).toBeInTheDocument();
    expect(screen.getByText("sunucu sonucu")).toBeInTheDocument();
    expect(screen.queryByText("10.000")).not.toBeInTheDocument();
  });

  it("shows loading and error states", () => {
    const { rerender } = render(<QuantityEntryPreview isLoading />);
    expect(screen.getByLabelText("Yükleniyor")).toBeInTheDocument();

    rerender(<QuantityEntryPreview error="Önizleme alınamadı" />);
    expect(screen.getByText("Miktar önizlemesi alınamadı")).toBeInTheDocument();
    expect(screen.getByText("Önizleme alınamadı")).toBeInTheDocument();
  });
});
