import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";

export type QuantityPreview = {
  productId: string;
  enteredQuantity: number;
  quantityBase: number | null;
  displayText: string;
  availableBaseQuantity: number | null;
  warnings: string[];
  viewMode: string;
  packagingName: string;
};

function asRecord(value: unknown): Record<string, unknown> {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : {};
}

function asFiniteNumber(value: unknown): number | null {
  return typeof value === "number" && Number.isFinite(value) ? value : null;
}

export function mapQuantityPreview(raw: unknown): QuantityPreview {
  const record = asRecord(raw);
  const packaging = asRecord(record.enteredPackaging);
  const warnings = Array.isArray(record.warnings)
    ? record.warnings.filter((item): item is string => typeof item === "string")
    : [];
  return {
    productId: String(record.productId ?? ""),
    enteredQuantity:
      typeof record.enteredQuantity === "number" && Number.isFinite(record.enteredQuantity)
        ? record.enteredQuantity
        : 0,
    quantityBase: asFiniteNumber(record.quantityBase),
    displayText: String(record.displayText ?? ""),
    availableBaseQuantity: asFiniteNumber(record.availableBaseQuantity),
    warnings,
    viewMode: String(record.viewMode ?? ""),
    packagingName: String(packaging.name ?? ""),
  };
}

export async function previewQuantity(input: {
  productId: string;
  enteredQuantity: number;
  enteredPackagingId: string | null;
  viewMode: string;
  operationType: string;
  warehouseId?: string | null;
}): Promise<QuantityPreview> {
  const raw = await apiRequest<unknown>({
    path: "/mobile/quantity-previews",
    method: "POST",
    body: {
      productId: input.productId,
      enteredQuantity: input.enteredQuantity,
      enteredPackagingId: input.enteredPackagingId,
      viewMode: input.viewMode,
      operationType: input.operationType,
      warehouseId: input.warehouseId || null,
    },
  });
  const mapped = mapQuantityPreview(raw);
  if (!mapped.productId) {
    throw new ApiError({
      kind: "not_found",
      status: 404,
      title: "Bulunamadı",
      detail: "Miktar önizlemesi alınamadı.",
    });
  }
  return mapped;
}
