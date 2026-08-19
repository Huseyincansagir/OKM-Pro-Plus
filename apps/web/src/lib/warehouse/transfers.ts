import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";
import type { StatusKind } from "@/components/ui/status-badge";

export type TransferRow = {
  id: string;
  productId: string;
  productCode: string;
  sourceWarehouseCode: string;
  sourceLocationCode: string;
  targetWarehouseCode: string;
  targetLocationCode: string;
  status: string;
  quantityBase: number | null;
  enteredQuantity: number;
  enteredPackagingId: string | null;
  viewMode: string;
  createdAt: string;
};

function asRecord(value: unknown): Record<string, unknown> {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : {};
}

function asFiniteNumber(value: unknown): number | null {
  return typeof value === "number" && Number.isFinite(value) ? value : null;
}

export function transferStatusKind(status: string): StatusKind {
  if (status === "Draft") return "pending";
  if (status === "Completed") return "success";
  if (status === "Cancelled") return "critical";
  return "info";
}

export function mapTransfer(raw: unknown): TransferRow {
  const record = asRecord(raw);
  return {
    id: String(record.id ?? ""),
    productId: String(record.productId ?? ""),
    productCode: String(record.productCode ?? ""),
    sourceWarehouseCode: String(record.sourceWarehouseCode ?? ""),
    sourceLocationCode: String(record.sourceLocationCode ?? ""),
    targetWarehouseCode: String(record.targetWarehouseCode ?? ""),
    targetLocationCode: String(record.targetLocationCode ?? ""),
    status: String(record.status ?? ""),
    quantityBase: asFiniteNumber(record.quantityBase),
    enteredQuantity:
      typeof record.enteredQuantity === "number" && Number.isFinite(record.enteredQuantity)
        ? record.enteredQuantity
        : 0,
    enteredPackagingId:
      typeof record.enteredPackagingId === "string" ? record.enteredPackagingId : null,
    viewMode: String(record.viewMode ?? ""),
    createdAt: String(record.createdAt ?? ""),
  };
}

export async function listTransfers(): Promise<TransferRow[]> {
  const raw = await apiRequest<unknown>({ path: "/warehouse-transfers", method: "GET" });
  if (!Array.isArray(raw)) {
    throw new ApiError({
      kind: "unexpected",
      status: 200,
      title: "Beklenmeyen yanıt",
      detail: "Transfer listesi beklenen biçimde değil.",
    });
  }
  return raw.map(mapTransfer);
}

export async function getTransfer(id: string): Promise<TransferRow> {
  return mapTransfer(await apiRequest<unknown>({ path: `/warehouse-transfers/${id}`, method: "GET" }));
}

export async function createTransfer(input: {
  productId: string;
  sourceWarehouseId: string;
  sourceLocationId: string;
  targetWarehouseId: string;
  targetLocationId: string;
  enteredQuantity: number;
  enteredPackagingId: string | null;
  viewMode: string;
}): Promise<TransferRow> {
  return mapTransfer(
    await apiRequest<unknown>({
      path: "/warehouse-transfers",
      method: "POST",
      body: input,
      idempotent: true,
    }),
  );
}

export async function completeTransfer(id: string): Promise<TransferRow> {
  return mapTransfer(
    await apiRequest<unknown>({
      path: `/warehouse-transfers/${id}/complete`,
      method: "POST",
      idempotent: true,
    }),
  );
}

export async function cancelTransfer(id: string): Promise<TransferRow> {
  return mapTransfer(
    await apiRequest<unknown>({
      path: `/warehouse-transfers/${id}/cancel`,
      method: "POST",
      idempotent: true,
    }),
  );
}
