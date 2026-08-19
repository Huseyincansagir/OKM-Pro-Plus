import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";
import type { StatusKind } from "@/components/ui/status-badge";

function asRecord(value: unknown): Record<string, unknown> {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : {};
}

function asFiniteNumber(value: unknown): number | null {
  return typeof value === "number" && Number.isFinite(value) ? value : null;
}

async function listArray(path: string, title: string): Promise<unknown[]> {
  const raw = await apiRequest<unknown>({ path, method: "GET" });
  if (!Array.isArray(raw)) {
    throw new ApiError({
      kind: "unexpected",
      status: 200,
      title: "Beklenmeyen yanıt",
      detail: `${title} beklenen biçimde değil.`,
    });
  }
  return raw;
}

export type TransferRow = {
  id: string;
  productId: string;
  status: string;
  quantityBase: number | null;
  enteredQuantity: number;
  createdAt: string;
};

export type ProductionRow = {
  id: string;
  productId: string;
  status: string;
  plannedQuantityBase: number | null;
  completedQuantityBase: number | null;
  remainingQuantityBase: number | null;
};

export type ShipmentRow = {
  id: string;
  deliveryNoteId: string;
  customerId: string;
  status: string;
  itemCount: number;
  createdAt: string;
};

export function transferStatusKind(status: string): StatusKind {
  if (status === "Draft") return "pending";
  if (status === "Completed") return "success";
  if (status === "Cancelled") return "critical";
  return "info";
}

export function productionStatusKind(status: string): StatusKind {
  if (status === "Planned") return "pending";
  if (status === "Released" || status === "InProgress") return "active";
  if (status === "Completed") return "success";
  if (status === "Cancelled") return "critical";
  return "info";
}

export function shipmentStatusKind(status: string): StatusKind {
  if (status === "Draft" || status === "Planned") return "pending";
  if (status === "Dispatched" || status === "InTransit") return "active";
  if (status === "Delivered" || status === "Completed") return "success";
  if (status === "Cancelled") return "critical";
  return "info";
}

export function mapTransfer(raw: unknown): TransferRow {
  const record = asRecord(raw);
  return {
    id: String(record.id ?? ""),
    productId: String(record.productId ?? ""),
    status: String(record.status ?? ""),
    quantityBase: asFiniteNumber(record.quantityBase),
    enteredQuantity:
      typeof record.enteredQuantity === "number" && Number.isFinite(record.enteredQuantity)
        ? record.enteredQuantity
        : 0,
    createdAt: String(record.createdAt ?? ""),
  };
}

export function mapProduction(raw: unknown): ProductionRow {
  const record = asRecord(raw);
  return {
    id: String(record.id ?? ""),
    productId: String(record.productId ?? ""),
    status: String(record.status ?? ""),
    plannedQuantityBase: asFiniteNumber(record.plannedQuantityBase),
    completedQuantityBase: asFiniteNumber(record.completedQuantityBase),
    remainingQuantityBase: asFiniteNumber(record.remainingQuantityBase),
  };
}

export function mapShipment(raw: unknown): ShipmentRow {
  const record = asRecord(raw);
  const items = Array.isArray(record.items) ? record.items : [];
  return {
    id: String(record.id ?? ""),
    deliveryNoteId: String(record.deliveryNoteId ?? ""),
    customerId: String(record.customerId ?? ""),
    status: String(record.status ?? ""),
    itemCount: items.length,
    createdAt: String(record.createdAt ?? ""),
  };
}

export async function listTransfers(): Promise<TransferRow[]> {
  return (await listArray("/warehouse-transfers", "Transfer listesi")).map(mapTransfer);
}

export async function listProductionOrders(): Promise<ProductionRow[]> {
  return (await listArray("/production/orders", "Üretim listesi")).map(mapProduction);
}

export async function listShipments(): Promise<ShipmentRow[]> {
  return (await listArray("/shipments", "Sevkiyat listesi")).map(mapShipment);
}
