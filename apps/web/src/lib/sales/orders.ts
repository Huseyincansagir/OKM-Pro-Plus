import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";
import type { StatusKind } from "@/components/ui/status-badge";

export type SalesOrderSummary = {
  id: string;
  orderNumber: string;
  status: string;
  customerId: string;
  customerCode: string;
  customerLegalName: string;
  sourceQuoteId: string | null;
  sourceQuoteNumber: string | null;
  currencyCode: string;
  totalNet: number | null;
  totalTax: number | null;
  totalGross: number | null;
  itemCount: number;
  createdAt: string;
};

export type SalesOrderLine = {
  id: string;
  productId: string;
  enteredQuantity: number;
  enteredPackagingId: string | null;
  orderedQty: number | null;
  reservedQty: number | null;
  shippedQty: number | null;
  remainingQty: number | null;
  packagingName: string;
  unitPrice: number | null;
};

export type SalesOrderDetail = SalesOrderSummary & {
  rowVersion: number | null;
  items: SalesOrderLine[];
};

function asRecord(value: unknown): Record<string, unknown> {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : {};
}

function asFiniteNumber(value: unknown): number | null {
  return typeof value === "number" && Number.isFinite(value) ? value : null;
}

function packagingNameFromSnapshot(raw: unknown): string {
  if (typeof raw !== "string" || !raw) {
    return "—";
  }
  try {
    const parsed = JSON.parse(raw) as { name?: unknown };
    return typeof parsed.name === "string" && parsed.name ? parsed.name : "—";
  } catch {
    return "—";
  }
}

export function salesOrderStatusKind(status: string): StatusKind {
  if (status === "Draft" || status === "PendingApproval") return "pending";
  if (status === "Approved" || status === "Preparing") return "active";
  if (status === "PartiallyShipped") return "info";
  if (status === "Fulfilled" || status === "Completed") return "success";
  if (status === "Cancelled") return "critical";
  return "inactive";
}

export function salesOrderStatusLabel(status: string): string {
  if (status === "Draft") return "Taslak";
  if (status === "PendingApproval") return "Onay bekliyor";
  if (status === "Approved") return "Onaylandı";
  if (status === "Preparing") return "Hazırlanıyor";
  if (status === "PartiallyShipped") return "Kısmi sevk";
  if (status === "Fulfilled") return "Karşılandı";
  if (status === "Completed") return "Tamamlandı";
  if (status === "Cancelled") return "İptal";
  return status || "Bilinmiyor";
}

export function canSubmitSalesOrder(status: string): boolean {
  return status === "Draft";
}

export function canDecideSalesOrder(status: string): boolean {
  return status === "PendingApproval";
}

export function mapSalesOrderLine(raw: unknown): SalesOrderLine {
  const record = asRecord(raw);
  return {
    id: String(record.id ?? ""),
    productId: String(record.productId ?? ""),
    enteredQuantity:
      typeof record.enteredQuantity === "number" && Number.isFinite(record.enteredQuantity)
        ? record.enteredQuantity
        : 0,
    enteredPackagingId:
      typeof record.enteredPackagingId === "string" ? record.enteredPackagingId : null,
    orderedQty: asFiniteNumber(record.orderedQty),
    reservedQty: asFiniteNumber(record.reservedQty),
    shippedQty: asFiniteNumber(record.shippedQty),
    remainingQty: asFiniteNumber(record.remainingQty),
    packagingName: packagingNameFromSnapshot(record.packagingSnapshot),
    unitPrice: asFiniteNumber(record.unitPrice),
  };
}

export function mapSalesOrderSummary(raw: unknown): SalesOrderSummary {
  const record = asRecord(raw);
  const items = Array.isArray(record.items) ? record.items : [];
  return {
    id: String(record.id ?? ""),
    orderNumber: String(record.orderNumber ?? ""),
    status: String(record.status ?? ""),
    customerId: String(record.customerId ?? ""),
    customerCode: String(record.customerCode ?? ""),
    customerLegalName: String(record.customerLegalName ?? ""),
    sourceQuoteId: typeof record.sourceQuoteId === "string" ? record.sourceQuoteId : null,
    sourceQuoteNumber: typeof record.sourceQuoteNumber === "string" ? record.sourceQuoteNumber : null,
    currencyCode: String(record.currencyCode ?? ""),
    totalNet: asFiniteNumber(record.totalNet),
    totalTax: asFiniteNumber(record.totalTax),
    totalGross: asFiniteNumber(record.totalGross),
    itemCount: items.length,
    createdAt: String(record.createdAt ?? ""),
  };
}

export function mapSalesOrderDetail(raw: unknown): SalesOrderDetail {
  const record = asRecord(raw);
  const summary = mapSalesOrderSummary(record);
  const items = Array.isArray(record.items) ? record.items.map(mapSalesOrderLine) : [];
  return {
    ...summary,
    itemCount: items.length,
    rowVersion: asFiniteNumber(record.rowVersion),
    items,
  };
}

export async function listSalesOrders(): Promise<SalesOrderSummary[]> {
  const raw = await apiRequest<unknown>({
    path: "/orders",
    method: "GET",
  });
  if (!Array.isArray(raw)) {
    throw new ApiError({
      kind: "unexpected",
      status: 200,
      title: "Beklenmeyen yanıt",
      detail: "Sipariş listesi beklenen biçimde değil.",
    });
  }
  return raw.map(mapSalesOrderSummary);
}

export async function getSalesOrder(id: string): Promise<SalesOrderDetail> {
  const raw = await apiRequest<unknown>({
    path: `/orders/${id}`,
    method: "GET",
  });
  const detail = mapSalesOrderDetail(raw);
  if (!detail.id) {
    throw new ApiError({
      kind: "not_found",
      status: 404,
      title: "Bulunamadı",
      detail: "Sipariş bulunamadı.",
    });
  }
  return detail;
}

export async function submitSalesOrder(id: string): Promise<SalesOrderDetail> {
  const raw = await apiRequest<unknown>({
    path: `/orders/${id}/submit`,
    method: "POST",
    idempotent: true,
  });
  return mapSalesOrderDetail(raw);
}

export async function approveSalesOrder(id: string, comment?: string): Promise<SalesOrderDetail> {
  const raw = await apiRequest<unknown>({
    path: `/orders/${id}/approve`,
    method: "POST",
    body: { comment: comment || null },
    idempotent: true,
  });
  return mapSalesOrderDetail(raw);
}

export async function rejectSalesOrder(id: string, comment: string): Promise<SalesOrderDetail> {
  const raw = await apiRequest<unknown>({
    path: `/orders/${id}/reject`,
    method: "POST",
    body: { comment },
    idempotent: true,
  });
  return mapSalesOrderDetail(raw);
}
