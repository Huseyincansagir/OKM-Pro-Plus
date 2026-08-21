import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";
import type { StatusKind } from "@/components/ui/status-badge";

export type DeliveryNoteLine = {
  id: string;
  salesOrderItemId: string;
  productId: string;
  quantityBase: number | null;
  enteredQuantity: number;
  enteredPackagingId: string | null;
  viewMode: string | null;
  shippedQty: number | null;
  remainingToInvoice: number | null;
};

export type DeliveryNoteDetail = {
  id: string;
  documentNumber: string;
  salesOrderId: string;
  customerId: string;
  status: string;
  issuedAt: string | null;
  itemCount: number;
  rowVersion: number | null;
  items: DeliveryNoteLine[];
};

function asRecord(value: unknown): Record<string, unknown> {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : {};
}

function asFiniteNumber(value: unknown): number | null {
  return typeof value === "number" && Number.isFinite(value) ? value : null;
}

export function deliveryNoteStatusKind(status: string): StatusKind {
  if (status === "Draft" || status === "Prepared" || status === "ReadyToIssue") return "pending";
  if (status === "Issued") return "success";
  if (status === "Cancelled") return "critical";
  return "info";
}

export function mapDeliveryNoteLine(raw: unknown): DeliveryNoteLine {
  const record = asRecord(raw);
  return {
    id: String(record.id ?? ""),
    salesOrderItemId: String(record.salesOrderItemId ?? ""),
    productId: String(record.productId ?? ""),
    quantityBase: asFiniteNumber(record.quantityBase),
    enteredQuantity:
      typeof record.enteredQuantity === "number" && Number.isFinite(record.enteredQuantity)
        ? record.enteredQuantity
        : 0,
    enteredPackagingId:
      typeof record.enteredPackagingId === "string" ? record.enteredPackagingId : null,
    viewMode: typeof record.viewMode === "string" ? record.viewMode : null,
    shippedQty: asFiniteNumber(record.shippedQty),
    remainingToInvoice: asFiniteNumber(record.remainingToInvoice),
  };
}

export function mapDeliveryNoteDetail(raw: unknown): DeliveryNoteDetail {
  const record = asRecord(raw);
  const items = Array.isArray(record.items) ? record.items.map(mapDeliveryNoteLine) : [];
  return {
    id: String(record.id ?? ""),
    documentNumber: String(record.documentNumber ?? ""),
    salesOrderId: String(record.salesOrderId ?? ""),
    customerId: String(record.customerId ?? ""),
    status: String(record.status ?? ""),
    issuedAt: typeof record.issuedAt === "string" ? record.issuedAt : null,
    itemCount: items.length,
    rowVersion: asFiniteNumber(record.rowVersion),
    items,
  };
}

export async function getDeliveryNote(id: string): Promise<DeliveryNoteDetail> {
  const detail = mapDeliveryNoteDetail(
    await apiRequest<unknown>({ path: `/delivery-notes/${id}`, method: "GET" }),
  );
  if (!detail.id) {
    throw new ApiError({
      kind: "not_found",
      status: 404,
      title: "Bulunamadı",
      detail: "İrsaliye bulunamadı.",
    });
  }
  return detail;
}

export async function issueDeliveryNote(id: string): Promise<DeliveryNoteDetail> {
  return mapDeliveryNoteDetail(
    await apiRequest<unknown>({
      path: `/delivery-notes/${id}/issue`,
      method: "POST",
      idempotent: true,
    }),
  );
}

export async function createDeliveryNote(input: {
  salesOrderId: string;
  items: Array<{
    salesOrderItemId: string;
    enteredQuantity: number;
    enteredPackagingId: string | null;
    viewMode: string;
  }>;
}): Promise<DeliveryNoteDetail> {
  return mapDeliveryNoteDetail(
    await apiRequest<unknown>({
      path: "/delivery-notes",
      method: "POST",
      body: input,
      idempotent: true,
    }),
  );
}

export function canIssueDeliveryNote(status: string): boolean {
  return status === "Draft" || status === "Prepared" || status === "ReadyToIssue";
}
