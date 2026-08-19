export type PublicPackaging = {
  id: string;
  level: string;
  name: string;
  quantityInBaseUom: number;
  isSellable: boolean;
  allowPartial: boolean;
  effectiveVersion: string;
};

export type PublicProduct = {
  id: string;
  code: string;
  slug: string;
  name: string;
  description: string | null;
  sizeLabel: string | null;
  categoryCode: string;
  categoryName: string;
  baseUomCode: string;
  baseUomName: string;
  packagings: PublicPackaging[];
  primaryImageUrl: string | null;
};

export type PublicProductPage = {
  items: PublicProduct[];
  page: number;
  pageSize: number;
  totalCount: number;
  hasNextPage: boolean;
};

export type QuoteBasketLine = {
  productId: string;
  slug: string;
  name: string;
  code: string;
  primaryImageUrl: string | null;
  enteredQuantity: number;
  enteredPackagingId: string | null;
  packagingName: string;
  catalogQuantityInBaseUom: number;
  baseUomCode: string;
  viewMode: "BaseUnit" | "Packaging" | "Breakdown";
  note: string;
};

export type QuoteRequestResult = {
  id: string;
  requestNumber: string;
  status: string;
  createdAt: string;
};
