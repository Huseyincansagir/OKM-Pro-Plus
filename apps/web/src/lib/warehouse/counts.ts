import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";
import type { StatusKind } from "@/components/ui/status-badge";

export type StockCountItem = {
  id: string;
  productId: string;
  productCode: string;
  countedQtyBase: number | null;
  systemOnHandQtyBase: number | null;
  varianceQtyBase: number | null;
};

export type StockCountRow = {
  id: string;
  documentNumber: string;
  warehouseCode: string;
  locationCode: string;
  status: string;
  itemCount: number;
  createdAt: string;
  items: StockCountItem[];
};

function asRecord(value: unknown): Record<string, unknown> {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : {};
}

function asFiniteNumber(value: unknown): number | null {
  return typeof value === "number" && Number.isFinite(value) ? value : null;
}

export function countStatusKind(status: string): StatusKind {
  if (status === "Draft") return "pending";
  if (status === "Completed") return "success";
  return "info";
}

export function mapCountItem(raw: unknown): StockCountItem {
  const record = asRecord(raw);
  return {
    id: String(record.id ?? ""),
    productId: String(record.productId ?? ""),
    productCode: String(record.productCode ?? ""),
    countedQtyBase: asFiniteNumber(record.countedQtyBase),
    systemOnHandQtyBase: asFiniteNumber(record.systemOnHandQtyBase),
    varianceQtyBase: asFiniteNumber(record.varianceQtyBase),
  };
}

export function mapStockCount(raw: unknown): StockCountRow {
  const record = asRecord(raw);
  const items = Array.isArray(record.items) ? record.items.map(mapCountItem) : [];
  return {
    id: String(record.id ?? ""),
    documentNumber: String(record.documentNumber ?? ""),
    warehouseCode: String(record.warehouseCode ?? ""),
    locationCode: String(record.locationCode ?? ""),
    status: String(record.status ?? ""),
    itemCount: items.length,
    createdAt: String(record.createdAt ?? ""),
    items,
  };
}

export async function listStockCounts(): Promise<StockCountRow[]> {
  const raw = await apiRequest<unknown>({ path: "/stock-counts", method: "GET" });
  if (!Array.isArray(raw)) {
    throw new ApiError({
      kind: "unexpected",
      status: 200,
      title: "Beklenmeyen yanıt",
      detail: "Sayım listesi beklenen biçimde değil.",
    });
  }
  return raw.map(mapStockCount);
}

export async function getStockCount(id: string): Promise<StockCountRow> {
  return mapStockCount(await apiRequest<unknown>({ path: `/stock-counts/${id}`, method: "GET" }));
}

export async function createStockCount(input: {
  warehouseId: string;
  locationId: string;
}): Promise<StockCountRow> {
  return mapStockCount(
    await apiRequest<unknown>({ path: "/stock-counts", method: "POST", body: input, idempotent: true }),
  );
}

export async function addStockCountItem(
  id: string,
  input: { productId: string; countedQtyBase: number },
): Promise<StockCountRow> {
  return mapStockCount(
    await apiRequest<unknown>({
      path: `/stock-counts/${id}/items`,
      method: "POST",
      body: input,
      idempotent: true,
    }),
  );
}

export async function completeStockCount(id: string): Promise<StockCountRow> {
  return mapStockCount(
    await apiRequest<unknown>({
      path: `/stock-counts/${id}/complete`,
      method: "POST",
      idempotent: true,
    }),
  );
}
