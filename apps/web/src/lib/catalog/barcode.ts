import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";

export type BarcodeResolution = {
  barcode: string;
  productId: string;
  productCode: string;
  productName: string;
  packagingId: string | null;
  packagingName: string | null;
  quantityInBaseUom: number | null;
};

function asRecord(value: unknown): Record<string, unknown> {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : {};
}

function asFiniteNumber(value: unknown): number | null {
  return typeof value === "number" && Number.isFinite(value) ? value : null;
}

export function mapBarcodeResolution(raw: unknown): BarcodeResolution {
  const record = asRecord(raw);
  return {
    barcode: String(record.barcode ?? ""),
    productId: String(record.productId ?? ""),
    productCode: String(record.productCode ?? ""),
    productName: String(record.productName ?? ""),
    packagingId: typeof record.packagingId === "string" ? record.packagingId : null,
    packagingName: typeof record.packagingName === "string" ? record.packagingName : null,
    quantityInBaseUom: asFiniteNumber(record.quantityInBaseUom),
  };
}

export async function resolveBarcode(barcode: string): Promise<BarcodeResolution> {
  const mapped = mapBarcodeResolution(
    await apiRequest<unknown>({
      path: "/mobile/barcodes/resolve",
      method: "POST",
      body: { barcode },
    }),
  );
  if (!mapped.productId) {
    throw new ApiError({
      kind: "not_found",
      status: 404,
      title: "Barkod bulunamadı",
      detail: "Aktif barkod eşleşmesi yok.",
    });
  }
  return mapped;
}
