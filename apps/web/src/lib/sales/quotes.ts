import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";
import type { StatusKind } from "@/components/ui/status-badge";

export type QuoteSummary = {
  id: string;
  quoteNumber: string;
  status: string;
  customerId: string;
  customerCode: string;
  customerLegalName: string;
  quoteRequestId: string;
  currencyCode: string;
  totalNet: number | null;
  totalTax: number | null;
  totalGross: number | null;
  validUntil: string | null;
  issuedAt: string | null;
  itemCount: number;
  createdAt: string;
};

export type QuoteLine = {
  id: string;
  productId: string;
  quoteRequestItemId: string;
  enteredQuantity: number;
  enteredPackagingId: string | null;
  quantityBase: number | null;
  packagingName: string;
  unitPrice: number | null;
  listUnitPrice: number | null;
  priceListId: string | null;
  taxCode: string | null;
  lineNet: number | null;
};

export type QuoteDetail = QuoteSummary & {
  issuedBy: string | null;
  items: QuoteLine[];
};

export type CreateQuoteItemInput = {
  quoteRequestItemId: string;
  unitPrice: number;
  taxCode?: string;
};

export type CreateQuoteInput = {
  quoteRequestId: string;
  currencyCode: string;
  validUntil?: string;
  items: CreateQuoteItemInput[];
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

export function quoteStatusKind(status: string): StatusKind {
  if (status === "Draft") return "pending";
  if (status === "Issued") return "success";
  return "info";
}

export function quoteStatusLabel(status: string): string {
  if (status === "Draft") return "Taslak";
  if (status === "Issued") return "Kesinleşti";
  return status || "Bilinmiyor";
}

export function canCreateQuoteFromRequest(status: string, customerId: string | null): boolean {
  return status === "InReview" && Boolean(customerId);
}

export function canIssueQuote(status: string): boolean {
  return status === "Draft";
}

export function mapQuoteLine(raw: unknown): QuoteLine {
  const record = asRecord(raw);
  return {
    id: String(record.id ?? ""),
    productId: String(record.productId ?? ""),
    quoteRequestItemId: String(record.quoteRequestItemId ?? ""),
    enteredQuantity:
      typeof record.enteredQuantity === "number" && Number.isFinite(record.enteredQuantity)
        ? record.enteredQuantity
        : 0,
    enteredPackagingId:
      typeof record.enteredPackagingId === "string" ? record.enteredPackagingId : null,
    quantityBase: asFiniteNumber(record.quantityBase),
    packagingName: packagingNameFromSnapshot(record.packagingSnapshot),
    unitPrice: asFiniteNumber(record.unitPrice),
    listUnitPrice: asFiniteNumber(record.listUnitPrice),
    priceListId: record.priceListId ? String(record.priceListId) : null,
    taxCode: record.taxCode ? String(record.taxCode) : null,
    lineNet: asFiniteNumber(record.lineNet),
  };
}

export function mapQuoteSummary(raw: unknown): QuoteSummary {
  const record = asRecord(raw);
  const items = Array.isArray(record.items) ? record.items : [];
  return {
    id: String(record.id ?? ""),
    quoteNumber: String(record.quoteNumber ?? ""),
    status: String(record.status ?? ""),
    customerId: String(record.customerId ?? ""),
    customerCode: String(record.customerCode ?? ""),
    customerLegalName: String(record.customerLegalName ?? ""),
    quoteRequestId: String(record.quoteRequestId ?? ""),
    currencyCode: String(record.currencyCode ?? ""),
    totalNet: asFiniteNumber(record.totalNet),
    totalTax: asFiniteNumber(record.totalTax),
    totalGross: asFiniteNumber(record.totalGross),
    validUntil: record.validUntil ? String(record.validUntil) : null,
    issuedAt: record.issuedAt ? String(record.issuedAt) : null,
    itemCount: items.length,
    createdAt: String(record.createdAt ?? ""),
  };
}

export function mapQuoteDetail(raw: unknown): QuoteDetail {
  const record = asRecord(raw);
  const summary = mapQuoteSummary(record);
  const items = Array.isArray(record.items) ? record.items.map(mapQuoteLine) : [];
  return {
    ...summary,
    itemCount: items.length,
    issuedBy: record.issuedBy ? String(record.issuedBy) : null,
    items,
  };
}

export async function listQuotes(): Promise<QuoteSummary[]> {
  const raw = await apiRequest<unknown>({
    path: "/quotes",
    method: "GET",
  });
  if (!Array.isArray(raw)) {
    throw new ApiError({
      kind: "unexpected",
      status: 200,
      title: "Beklenmeyen yanıt",
      detail: "Teklif listesi beklenen biçimde değil.",
    });
  }
  return raw.map(mapQuoteSummary);
}

export async function getQuote(id: string): Promise<QuoteDetail> {
  const raw = await apiRequest<unknown>({
    path: `/quotes/${id}`,
    method: "GET",
  });
  const detail = mapQuoteDetail(raw);
  if (!detail.id) {
    throw new ApiError({
      kind: "not_found",
      status: 404,
      title: "Bulunamadı",
      detail: "Teklif bulunamadı.",
    });
  }
  return detail;
}

export async function createQuote(input: CreateQuoteInput): Promise<QuoteDetail> {
  const raw = await apiRequest<unknown>({
    path: "/quotes",
    method: "POST",
    body: {
      quoteRequestId: input.quoteRequestId,
      currencyCode: input.currencyCode,
      validUntil: input.validUntil || null,
      items: input.items.map((item) => ({
        quoteRequestItemId: item.quoteRequestItemId,
        unitPrice: item.unitPrice,
        taxCode: item.taxCode || null,
      })),
    },
    idempotent: true,
  });
  const mapped = mapQuoteDetail(raw);
  if (!mapped.id) {
    throw new ApiError({
      kind: "unexpected",
      status: 201,
      title: "Beklenmeyen yanıt",
      detail: "Teklif oluşturuldu ama yanıt geçersiz.",
    });
  }
  return mapped;
}

export async function issueQuote(id: string): Promise<QuoteDetail> {
  const raw = await apiRequest<unknown>({
    path: `/quotes/${id}/issue`,
    method: "POST",
    idempotent: true,
  });
  return mapQuoteDetail(raw);
}
