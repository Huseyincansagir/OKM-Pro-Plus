import { describe, expect, it } from "vitest";
import { mapPublicProduct, packagingDefinitionLabel } from "@/lib/catalog/map-product";

describe("mapPublicProduct", () => {
  it("keeps only public catalog fields and drops stock or price extras", () => {
    const product = mapPublicProduct({
      id: "p1",
      code: "PS-033",
      slug: "ultra-soft",
      name: "Ultra Soft",
      description: "2 katlı",
      sizeLabel: "33x33",
      categoryCode: "napkin",
      categoryName: "Peçete",
      baseUom: { code: "ADT", displayName: "Adet", dimension: "count", decimalScale: 0 },
      packagings: [
        {
          id: "pkg-1",
          level: "Case",
          name: "Koli",
          quantityInBaseUom: 2000,
          isSellable: true,
          allowPartial: false,
          effectiveVersion: "v1",
        },
      ],
      primaryImageUrl: null,
      stockOnHand: 9000,
      unitPrice: 12.5,
      cost: 4,
    });

    expect(product).toEqual({
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
          id: "pkg-1",
          level: "Case",
          name: "Koli",
          quantityInBaseUom: 2000,
          isSellable: true,
          allowPartial: false,
          effectiveVersion: "v1",
        },
      ],
      primaryImageUrl: null,
    });
    expect(product).not.toHaveProperty("stockOnHand");
    expect(product).not.toHaveProperty("unitPrice");
    expect(packagingDefinitionLabel(product.packagings[0], product.baseUomCode)).toBe(
      "1 Koli = 2000 ADT",
    );
  });
});
