import { apiRequest } from "@/lib/api/client";
import { ApiError } from "@/lib/api/types";

export type InvoiceItemDetail = {
  id: string;
  deliveryNoteItemId: string;
  productId: string;
  quantityBase: number;
  enteredQuantity: number | null;
  enteredPackagingId: string | null;
  unitPrice: number;
  lineTotal: number;
  rowVersion: number | null;
};

export type InvoiceDetail = {
  id: string;
  invoiceNumber: string;
  customerId: string;
  status: string;
  currencyCode: string;
  subtotal: number;
  taxTotal: number;
  grandTotal: number;
  items: InvoiceItemDetail[];
  issuedAt: string | null;
  rowVersion: number | null;
};

export type CreateInvoiceItemInput = {
  deliveryNoteItemId: string;
  enteredQuantity: number;
  enteredPackagingId?: string | null;
  viewMode?: string | null;
  unitPrice: number;
  taxCodeId?: string | null;
};

export type CreateInvoiceInput = {
  customerId: string;
  currencyCode?: string;
  items: CreateInvoiceItemInput[];
};

function asRecord(value: unknown): Record<string, unknown> {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : {};
}

function asFiniteNumber(value: unknown, defaultValue = 0): number {
  return typeof value === "number" && Number.isFinite(value) ? value : defaultValue;
}

export function mapInvoiceDetail(raw: unknown): InvoiceDetail {
  const record = asRecord(raw);
  const rawItems = Array.isArray(record.items) ? record.items : [];
  const items: InvoiceItemDetail[] = rawItems.map((item) => {
    const itemRecord = asRecord(item);
    return {
      id: String(itemRecord.id ?? ""),
      deliveryNoteItemId: String(itemRecord.deliveryNoteItemId ?? ""),
      productId: String(itemRecord.productId ?? ""),
      quantityBase: asFiniteNumber(itemRecord.quantityBase),
      enteredQuantity:
        typeof itemRecord.enteredQuantity === "number" && Number.isFinite(itemRecord.enteredQuantity)
          ? itemRecord.enteredQuantity
          : null,
      enteredPackagingId:
        typeof itemRecord.enteredPackagingId === "string" ? itemRecord.enteredPackagingId : null,
      unitPrice: asFiniteNumber(itemRecord.unitPrice),
      lineTotal: asFiniteNumber(itemRecord.lineTotal),
      rowVersion:
        typeof itemRecord.rowVersion === "number" && Number.isFinite(itemRecord.rowVersion)
          ? itemRecord.rowVersion
          : null,
    };
  });

  return {
    id: String(record.id ?? ""),
    invoiceNumber: String(record.invoiceNumber ?? ""),
    customerId: String(record.customerId ?? ""),
    status: String(record.status ?? ""),
    currencyCode: String(record.currencyCode ?? "TRY"),
    subtotal: asFiniteNumber(record.subtotal),
    taxTotal: asFiniteNumber(record.taxTotal),
    grandTotal: asFiniteNumber(record.grandTotal),
    items,
    issuedAt: typeof record.issuedAt === "string" ? record.issuedAt : null,
    rowVersion:
      typeof record.rowVersion === "number" && Number.isFinite(record.rowVersion)
        ? record.rowVersion
        : null,
  };
}

export async function getInvoice(id: string): Promise<InvoiceDetail> {
  const raw = await apiRequest<unknown>({ path: `/invoices/${id}`, method: "GET" });
  if (!raw || typeof raw !== "object") {
    throw new ApiError({
      kind: "unexpected",
      status: 200,
      title: "Beklenmeyen yanıt",
      detail: "Fatura verisi beklenen biçimde değil.",
    });
  }
  return mapInvoiceDetail(raw);
}

export async function createInvoice(input: CreateInvoiceInput): Promise<InvoiceDetail> {
  const raw = await apiRequest<unknown>({
    path: "/invoices",
    method: "POST",
    body: {
      customerId: input.customerId,
      currencyCode: input.currencyCode || "TRY",
      items: input.items.map((item) => ({
        deliveryNoteItemId: item.deliveryNoteItemId,
        enteredQuantity: item.enteredQuantity,
        enteredPackagingId: item.enteredPackagingId ?? null,
        viewMode: item.viewMode ?? null,
        unitPrice: item.unitPrice,
        taxCodeId: item.taxCodeId ?? null,
      })),
    },
    idempotent: true,
  });
  return mapInvoiceDetail(raw);
}

export async function issueInvoice(id: string): Promise<InvoiceDetail> {
  const raw = await apiRequest<unknown>({
    path: `/invoices/${id}/issue`,
    method: "POST",
    idempotent: true,
  });
  return mapInvoiceDetail(raw);
}

import type { StatusKind } from "@/components/ui/status-badge";

export function invoiceStatusKind(status: string): StatusKind {
  switch (status) {
    case "Issued":
    case "Paid":
      return "success";
    case "Draft":
    case "ReadyToIssue":
      return "pending";
    case "PartiallyPaid":
      return "info";
    case "Cancelled":
    case "Reversed":
      return "critical";
    default:
      return "pending";
  }
}
