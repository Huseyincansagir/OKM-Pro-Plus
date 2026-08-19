import { afterEach, describe, expect, it } from "vitest";
import {
  resetQuoteBasketStore,
  toQuoteRequestItems,
  useQuoteBasketStore,
} from "@/lib/catalog/quote-basket-store";
import type { QuoteBasketLine } from "@/lib/catalog/types";

const line = (overrides: Partial<QuoteBasketLine> = {}): QuoteBasketLine => ({
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
  ...overrides,
});

describe("quote basket store", () => {
  afterEach(() => {
    resetQuoteBasketStore();
  });

  it("merges the same product and packaging instead of adding a second row", () => {
    useQuoteBasketStore.getState().addLine(line());
    useQuoteBasketStore.getState().addLine(line({ enteredQuantity: 2, viewMode: "BaseUnit" }));
    expect(useQuoteBasketStore.getState().lines).toHaveLength(1);
    expect(useQuoteBasketStore.getState().lines[0].enteredQuantity).toBe(7);
    expect(useQuoteBasketStore.getState().lines[0].viewMode).toBe("BaseUnit");
  });

  it("keeps a different packaging as a separate line", () => {
    useQuoteBasketStore.getState().addLine(line());
    useQuoteBasketStore.getState().addLine(line({ enteredPackagingId: "pkg-pack", packagingName: "Paket" }));
    expect(useQuoteBasketStore.getState().lines).toHaveLength(2);
  });

  it("builds a submit payload without quantityBase", () => {
    useQuoteBasketStore.getState().addLine(line());
    const items = toQuoteRequestItems(useQuoteBasketStore.getState().lines);
    expect(items).toEqual([
      {
        productId: "p1",
        enteredQuantity: 5,
        enteredPackagingId: "pkg-case",
        viewMode: "Packaging",
      },
    ]);
    expect(JSON.stringify(items)).not.toContain("quantityBase");
    expect(JSON.stringify(items)).not.toContain("2000");
  });
});
