import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";
import type { StatusKind } from "@/components/ui/status-badge";

export type ShipmentLine = {
  id: string;
  deliveryNoteItemId: string;
  productId: string;
  quantityBase: number | null;
};

export type ShipmentDetail = {
  id: string;
  deliveryNoteId: string;
  customerId: string;
  status: string;
  itemCount: number;
  rowVersion: number | null;
  createdAt: string;
  items: ShipmentLine[];
};

function asRecord(value: unknown): Record<string, unknown> {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : {};
}

function asFiniteNumber(value: unknown): number | null {
  return typeof value === "number" && Number.isFinite(value) ? value : null;
}

export function shipmentStatusKind(status: string): StatusKind {
  if (status === "Draft" || status === "Preparing" || status === "Planned") return "pending";
  if (status === "Loaded" || status === "Dispatched" || status === "InTransit") return "active";
  if (status === "Delivered" || status === "Completed") return "success";
  if (status === "Cancelled") return "critical";
  return "info";
}

export function mapShipmentLine(raw: unknown): ShipmentLine {
  const record = asRecord(raw);
  return {
    id: String(record.id ?? ""),
    deliveryNoteItemId: String(record.deliveryNoteItemId ?? ""),
    productId: String(record.productId ?? ""),
    quantityBase: asFiniteNumber(record.quantityBase),
  };
}

export function mapShipmentDetail(raw: unknown): ShipmentDetail {
  const record = asRecord(raw);
  const items = Array.isArray(record.items) ? record.items.map(mapShipmentLine) : [];
  return {
    id: String(record.id ?? ""),
    deliveryNoteId: String(record.deliveryNoteId ?? ""),
    customerId: String(record.customerId ?? ""),
    status: String(record.status ?? ""),
    itemCount: items.length,
    rowVersion: asFiniteNumber(record.rowVersion),
    createdAt: String(record.createdAt ?? ""),
    items,
  };
}

export async function getShipment(id: string): Promise<ShipmentDetail> {
  const detail = mapShipmentDetail(
    await apiRequest<unknown>({ path: `/shipments/${id}`, method: "GET" }),
  );
  if (!detail.id) {
    throw new ApiError({
      kind: "not_found",
      status: 404,
      title: "Bulunamadı",
      detail: "Sevkiyat bulunamadı.",
    });
  }
  return detail;
}

export async function createShipment(input: {
  deliveryNoteId: string;
  expectedDeliveryNoteRowVersion: number;
}): Promise<ShipmentDetail> {
  return mapShipmentDetail(
    await apiRequest<unknown>({
      path: "/shipments",
      method: "POST",
      body: input,
      idempotent: true,
    }),
  );
}
