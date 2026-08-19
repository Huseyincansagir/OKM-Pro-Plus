import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";

export type InvoiceRow = {
  id: string;
  invoiceNumber: string;
  customerId: string;
  status: string;
  currencyCode: string;
  grandTotal: number | null;
  itemCount: number;
};

export type DeliveryNoteRow = {
  id: string;
  documentNumber: string;
  customerId: string;
  status: string;
  itemCount: number;
};

export type AccountRow = {
  customerId: string;
  currencyCode: string;
  debitTotal: number | null;
  creditTotal: number | null;
  balance: number | null;
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

export function mapInvoiceRow(raw: unknown): InvoiceRow {
  const record = asRecord(raw);
  const items = Array.isArray(record.items) ? record.items : [];
  return {
    id: String(record.id ?? ""),
    invoiceNumber: String(record.invoiceNumber ?? ""),
    customerId: String(record.customerId ?? ""),
    status: String(record.status ?? ""),
    currencyCode: String(record.currencyCode ?? ""),
    grandTotal: asFiniteNumber(record.grandTotal),
    itemCount: items.length,
  };
}

export function mapDeliveryNoteRow(raw: unknown): DeliveryNoteRow {
  const record = asRecord(raw);
  const items = Array.isArray(record.items) ? record.items : [];
  return {
    id: String(record.id ?? ""),
    documentNumber: String(record.documentNumber ?? ""),
    customerId: String(record.customerId ?? ""),
    status: String(record.status ?? ""),
    itemCount: items.length,
  };
}

export function mapAccountRow(raw: unknown): AccountRow {
  const record = asRecord(raw);
  return {
    customerId: String(record.customerId ?? ""),
    currencyCode: String(record.currencyCode ?? ""),
    debitTotal: asFiniteNumber(record.debitTotal),
    creditTotal: asFiniteNumber(record.creditTotal),
    balance: asFiniteNumber(record.balance),
  };
}

export async function listInvoices(): Promise<InvoiceRow[]> {
  return (await listArray("/invoices", "Fatura listesi beklenen biçimde değil.")).map(mapInvoiceRow);
}

export async function listDeliveryNotes(): Promise<DeliveryNoteRow[]> {
  return (await listArray("/delivery-notes", "İrsaliye listesi beklenen biçimde değil.")).map(
    mapDeliveryNoteRow,
  );
}

export async function listCurrentAccounts(): Promise<AccountRow[]> {
  return (await listArray("/current-accounts", "Cari listesi beklenen biçimde değil.")).map(mapAccountRow);
}
