import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ProductDetail } from "@/components/public/product-detail";
import { getPublicProduct } from "@/lib/catalog/catalog-client";
import { resetQuoteBasketStore, useQuoteBasketStore } from "@/lib/catalog/quote-basket-store";

vi.mock("@/lib/catalog/catalog-client", () => ({
  getPublicProduct: vi.fn(),
}));

const product = {
  id: "p1",
  code: "PS-033",
  slug: "ultra-soft",
  name: "Ultra Soft",
  description: "2 katlı",
  sizeLabel: "33x33",
  categoryCode: "napkin",
  categoryName: "Peçete",
  baseUomCode: "ADT",
  baseUomName: "Adet",
  packagings: [
    {
      id: "pkg-case",
      level: "Case",
      name: "Koli",
      quantityInBaseUom: 2000,
      isSellable: true,
      allowPartial: false,
      effectiveVersion: "v1",
    },
  ],
  primaryImageUrl: null,
};

describe("ProductDetail", () => {
  beforeEach(() => {
    resetQuoteBasketStore();
    vi.mocked(getPublicProduct).mockResolvedValue(product);
  });

  it("adds entered quantity and packaging without inventing quantityBase", async () => {
    const user = userEvent.setup();
    render(<ProductDetail slug="ultra-soft" />);

    expect(await screen.findByRole("heading", { name: "Ultra Soft" })).toBeInTheDocument();
    await user.click(screen.getByRole("radio", { name: "Temel Birim" }));
    await user.click(screen.getByRole("button", { name: "Teklife ekle" }));

    const line = useQuoteBasketStore.getState().lines[0];
    expect(line.enteredQuantity).toBe(1);
    expect(line.enteredPackagingId).toBe("pkg-case");
    expect(line.viewMode).toBe("BaseUnit");
    expect(line).not.toHaveProperty("quantityBase");
    expect(screen.getByText(/temel karşılık gönderimde sunucu tarafından hesaplanır/i)).toBeInTheDocument();
    expect(screen.queryByText("Temel karşılık:")).not.toBeInTheDocument();
  });

  it("shows a quantity error instead of adding a silent line", async () => {
    const user = userEvent.setup();
    render(<ProductDetail slug="ultra-soft" />);
    await screen.findByRole("heading", { name: "Ultra Soft" });
    const quantity = screen.getByRole("spinbutton", { name: /Miktar/ });
    await user.clear(quantity);
    await user.type(quantity, "0");
    await user.click(screen.getByRole("button", { name: "Teklife ekle" }));
    expect(await screen.findByText("Geçerli bir miktar girin.")).toBeInTheDocument();
    expect(useQuoteBasketStore.getState().lines).toHaveLength(0);
  });

  it("does not change packaging when viewMode changes", async () => {
    const user = userEvent.setup();
    render(<ProductDetail slug="ultra-soft" />);
    await screen.findByRole("heading", { name: "Ultra Soft" });
    const select = screen.getByLabelText("İşlem ambalajı") as HTMLSelectElement;
    expect(select.value).toBe("pkg-case");
    await user.click(screen.getByRole("radio", { name: "Kırılım" }));
    expect(select.value).toBe("pkg-case");
  });
});
