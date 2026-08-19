import type { PublicPackaging, PublicProduct, PublicProductPage } from "@/lib/catalog/types";

function asRecord(value: unknown): Record<string, unknown> {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : {};
}

function asString(value: unknown, fallback = ""): string {
  return typeof value === "string" ? value : fallback;
}

function asNumber(value: unknown, fallback = 0): number {
  return typeof value === "number" && Number.isFinite(value) ? value : fallback;
}

function asBool(value: unknown, fallback = false): boolean {
  return typeof value === "boolean" ? value : fallback;
}

export function mapPublicPackaging(raw: unknown): PublicPackaging {
  const record = asRecord(raw);
  return {
    id: asString(record.id),
    level: asString(record.level),
    name: asString(record.name),
    quantityInBaseUom: asNumber(record.quantityInBaseUom),
    isSellable: asBool(record.isSellable, true),
    allowPartial: asBool(record.allowPartial),
    effectiveVersion: asString(record.effectiveVersion),
  };
}

export function mapPublicProduct(raw: unknown): PublicProduct {
  const record = asRecord(raw);
  const uom = asRecord(record.baseUom);
  const packagings = Array.isArray(record.packagings)
    ? record.packagings.map(mapPublicPackaging)
    : [];

  return {
    id: asString(record.id),
    code: asString(record.code),
    slug: asString(record.slug),
    name: asString(record.name),
    description: typeof record.description === "string" ? record.description : null,
    sizeLabel: typeof record.sizeLabel === "string" ? record.sizeLabel : null,
    categoryCode: asString(record.categoryCode),
    categoryName: asString(record.categoryName),
    baseUomCode: asString(uom.code),
    baseUomName: asString(uom.displayName, asString(uom.code)),
    packagings,
    primaryImageUrl: typeof record.primaryImageUrl === "string" ? record.primaryImageUrl : null,
  };
}

export function mapPublicProductPage(raw: unknown): PublicProductPage {
  const record = asRecord(raw);
  const items = Array.isArray(record.items) ? record.items.map(mapPublicProduct) : [];
  return {
    items,
    page: asNumber(record.page, 1),
    pageSize: asNumber(record.pageSize, 24),
    totalCount: asNumber(record.totalCount),
    hasNextPage: asBool(record.hasNextPage),
  };
}

export function packagingDefinitionLabel(
  packaging: Pick<PublicPackaging, "name" | "quantityInBaseUom">,
  baseUomCode: string,
): string {
  return `1 ${packaging.name} = ${packaging.quantityInBaseUom} ${baseUomCode}`;
}

export function sellablePackagings(product: PublicProduct): PublicPackaging[] {
  const sellable = product.packagings.filter((item) => item.isSellable);
  return sellable.length > 0 ? sellable : product.packagings;
}
