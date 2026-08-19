import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";

export type WarehouseSummary = {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
};

export type StockRow = {
  id: string;
  productId: string;
  productCode: string;
  productName: string;
  warehouseId: string;
  warehouseCode: string;
  warehouseName: string;
  locationCode: string;
  onHandQtyBase: number | null;
  reservedQtyBase: number | null;
  availableQtyBase: number | null;
};

function asRecord(value: unknown): Record<string, unknown> {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : {};
}

function asFiniteNumber(value: unknown): number | null {
  return typeof value === "number" && Number.isFinite(value) ? value : null;
}

async function listArray(path: string, detail: string): Promise<unknown[]> {
  const raw = await apiRequest<unknown>({ path, method: "GET" });
  if (!Array.isArray(raw)) {
    throw new ApiError({
      kind: "unexpected",
      status: 200,
      title: "Beklenmeyen yanıt",
      detail,
    });
  }
  return raw;
}

export function mapWarehouse(raw: unknown): WarehouseSummary {
  const record = asRecord(raw);
  return {
    id: String(record.id ?? ""),
    code: String(record.code ?? ""),
    name: String(record.name ?? ""),
    isActive: record.isActive === true,
  };
}

export function mapStockRow(raw: unknown): StockRow {
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
    onHandQtyBase: asFiniteNumber(record.onHandQtyBase),
    reservedQtyBase: asFiniteNumber(record.reservedQtyBase),
    availableQtyBase: asFiniteNumber(record.availableQtyBase),
  };
}

export type WarehouseLocation = {
  id: string;
  warehouseId: string;
  code: string;
  name: string;
  isActive: boolean;
};

export function mapWarehouseLocation(raw: unknown): WarehouseLocation {
  const record = asRecord(raw);
  return {
    id: String(record.id ?? ""),
    warehouseId: String(record.warehouseId ?? ""),
    code: String(record.code ?? ""),
    name: String(record.name ?? ""),
    isActive: record.isActive === true,
  };
}

export async function listWarehouses(): Promise<WarehouseSummary[]> {
  return (await listArray("/warehouses", "Depo listesi beklenen biçimde değil.")).map(mapWarehouse);
}

export async function listWarehouseLocations(warehouseId: string): Promise<WarehouseLocation[]> {
  return (
    await listArray(
      `/warehouses/${warehouseId}/locations`,
      "Lokasyon listesi beklenen biçimde değil.",
    )
  ).map(mapWarehouseLocation);
}

export async function listStocks(): Promise<StockRow[]> {
  return (await listArray("/stocks", "Stok listesi beklenen biçimde değil.")).map(mapStockRow);
}
