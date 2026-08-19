import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";
import type { StatusKind } from "@/components/ui/status-badge";

export type StockMovementRow = {
  id: string;
  productId: string;
  productCode: string;
  productName: string;
  warehouseId: string;
  warehouseCode: string;
  warehouseName: string;
  locationCode: string;
  movementType: string;
  effect: string;
  quantityBase: number | null;
  sourceEntityType: string;
  sourceEntityId: string | null;
  reversedFromId: string | null;
  packagingSnapshot: string | null;
  createdAt: string;
};

function asRecord(value: unknown): Record<string, unknown> {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : {};
}

function asFiniteNumber(value: unknown): number | null {
  return typeof value === "number" && Number.isFinite(value) ? value : null;
}

function asOptionalString(value: unknown): string | null {
  return typeof value === "string" && value.length > 0 ? value : null;
}

export function mapStockMovement(raw: unknown): StockMovementRow {
  const record = asRecord(raw);
  return {
    id: String(record.id ?? ""),
    productId: String(record.productId ?? ""),
    productCode: String(record.productCode ?? ""),
    productName: String(record.productName ?? ""),
    warehouseId: String(record.warehouseId ?? ""),
    warehouseCode: String(record.warehouseCode ?? ""),
    warehouseName: String(record.warehouseName ?? ""),
    locationCode: String(record.locationCode ?? ""),
    movementType: String(record.movementType ?? ""),
    effect: String(record.effect ?? ""),
    quantityBase: asFiniteNumber(record.quantityBase),
    sourceEntityType: String(record.sourceEntityType ?? ""),
    sourceEntityId: asOptionalString(record.sourceEntityId),
    reversedFromId: asOptionalString(record.reversedFromId),
    packagingSnapshot: asOptionalString(record.packagingSnapshot),
    createdAt: String(record.createdAt ?? ""),
  };
}

export function movementEffectKind(effect: string): StatusKind {
  if (effect === "In") return "success";
  if (effect === "Out") return "pending";
  return "info";
}

export function movementEffectLabel(effect: string): string {
  if (effect === "In") return "Giriş";
  if (effect === "Out") return "Çıkış";
  if (effect === "Unknown") return "Yön yok";
  return effect || "—";
}

export function movementTypeLabel(movementType: string): string {
  if (movementType === "WarehouseTransferOut") return "Transfer çıkışı";
  if (movementType === "WarehouseTransferIn") return "Transfer girişi";
  if (movementType === "ProductionIn") return "Üretim girişi";
  if (movementType === "DeliveryIssue") return "İrsaliye çıkışı";
  return movementType || "—";
}

export function formatMovementInstant(value: string): string {
  if (!value) {
    return "—";
  }
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString("tr-TR");
}

export async function listStockMovements(): Promise<StockMovementRow[]> {
  const raw = await apiRequest<unknown>({ path: "/stock-movements", method: "GET" });
  if (!Array.isArray(raw)) {
    throw new ApiError({
      kind: "unexpected",
      status: 200,
      title: "Beklenmeyen yanıt",
      detail: "Stok hareketi listesi beklenen biçimde değil.",
    });
  }
  return raw.map(mapStockMovement);
}

export async function getStockMovement(id: string): Promise<StockMovementRow> {
  return mapStockMovement(
    await apiRequest<unknown>({ path: `/stock-movements/${id}`, method: "GET" }),
  );
}
