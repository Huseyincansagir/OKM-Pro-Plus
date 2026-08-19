import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { QuoteCheckout } from "@/components/public/quote-checkout";
import { submitPublicQuoteRequest } from "@/lib/catalog/catalog-client";
import { resetQuoteBasketStore, useQuoteBasketStore } from "@/lib/catalog/quote-basket-store";

vi.mock("@/lib/catalog/catalog-client", () => ({
  submitPublicQuoteRequest: vi.fn(),
}));

describe("QuoteCheckout", () => {
  beforeEach(() => {
    resetQuoteBasketStore();
    vi.mocked(submitPublicQuoteRequest).mockReset();
  });

  it("does not show the submit form when the basket is empty", () => {
    render(<QuoteCheckout />);
    expect(screen.getByText("Henüz ürün eklemediniz")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Teklif talebini gönder" })).not.toBeInTheDocument();
  });

  it("submits entered quantity only and keeps form data after a failure", async () => {
    const user = userEvent.setup();
    useQuoteBasketStore.getState().addLine({
      productId: "p1",
      slug: "ultra-soft",
      name: "Ultra Soft",
      code: "PS-033",
      primaryImageUrl: null,
      enteredQuantity: 5,
      enteredPackagingId: "pkg-case",
      packagingName: "Koli",
      catalogQuantityInBaseUom: 2000,
      baseUomCode: "ADT",
      viewMode: "Packaging",
      note: "",
    });
    vi.mocked(submitPublicQuoteRequest).mockRejectedValue(new Error("Sunucu reddetti"));

    render(<QuoteCheckout />);
    await user.click(screen.getByRole("button", { name: "Bilgilerimi gir ve teklif iste" }));
    await user.type(screen.getByLabelText(/Firma adı/), "Acme");
    await user.type(screen.getByLabelText(/Yetkili adı soyadı/), "Ali Veli");
    await user.type(screen.getByLabelText(/Telefon/), "555");
    await user.type(screen.getByLabelText(/E-posta/), "a@b.com");
    await user.click(screen.getByLabelText(/İletişim bilgilerimin/));
    await user.click(screen.getByRole("button", { name: "Teklif talebini gönder" }));

    expect(await screen.findByText(/Sunucu reddetti/)).toBeInTheDocument();
    expect(screen.getByLabelText(/Firma adı/)).toHaveValue("Acme");
    expect(submitPublicQuoteRequest).toHaveBeenCalledTimes(1);
    const payload = vi.mocked(submitPublicQuoteRequest).mock.calls[0][0];
    expect(payload.items[0]).toEqual({
      productId: "p1",
      enteredQuantity: 5,
      enteredPackagingId: "pkg-case",
      viewMode: "Packaging",
    });
    expect(JSON.stringify(payload)).not.toContain("quantityBase");
  }, 15000);

  it("does not submit an empty contact form", async () => {
    const user = userEvent.setup();
    useQuoteBasketStore.getState().addLine({
      productId: "p1",
      slug: "ultra-soft",
      name: "Ultra Soft",
      code: "PS-033",
      primaryImageUrl: null,
      enteredQuantity: 1,
      enteredPackagingId: "pkg-case",
      packagingName: "Koli",
      catalogQuantityInBaseUom: 2000,
      baseUomCode: "ADT",
      viewMode: "Packaging",
      note: "",
    });

    render(<QuoteCheckout />);
    await user.click(screen.getByRole("button", { name: "Bilgilerimi gir ve teklif iste" }));
    await user.click(screen.getByRole("button", { name: "Teklif talebini gönder" }));

    expect(await screen.findByText("Firma adı zorunludur.")).toBeInTheDocument();
    expect(submitPublicQuoteRequest).not.toHaveBeenCalled();
  });
});
