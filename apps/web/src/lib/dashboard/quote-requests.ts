import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";
import type { StatusKind } from "@/components/ui/status-badge";

export type QuoteRequestSummary = {
  id: string;
  requestNumber: string;
  status: string;
  source: string;
  candidateName: string;
  itemCount: number;
  createdAt: string;
};

export type SystemHealth = {
  status: string;
};

function asRecord(value: unknown): Record<string, unknown> {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : {};
}

export function mapQuoteRequestSummary(raw: unknown): QuoteRequestSummary {
  const record = asRecord(raw);
  const items = Array.isArray(record.items) ? record.items : [];
  return {
    id: String(record.id ?? ""),
    requestNumber: String(record.requestNumber ?? ""),
    status: String(record.status ?? ""),
    source: String(record.source ?? ""),
    candidateName: String(record.candidateName ?? ""),
    itemCount: items.length,
    createdAt: String(record.createdAt ?? ""),
  };
}

export function quoteRequestStatusKind(status: string): StatusKind {
  if (status === "Received") return "pending";
  if (status === "InReview") return "active";
  if (status === "Converted") return "success";
  if (status === "Rejected") return "critical";
  if (status === "Closed") return "inactive";
  return "info";
}

export function quoteRequestStatusLabel(status: string): string {
  if (status === "Received") return "Alındı";
  if (status === "InReview") return "İncelemede";
  if (status === "Converted") return "Dönüştürüldü";
  if (status === "Rejected") return "Reddedildi";
  if (status === "Closed") return "Kapatıldı";
  return status || "Bilinmiyor";
}

export function quoteRequestSourceLabel(source: string): string {
  if (source === "Public") return "Public katalog";
  return source || "—";
}

export async function listQuoteRequests(): Promise<QuoteRequestSummary[]> {
  const raw = await apiRequest<unknown>({
    path: "/quote-requests",
    method: "GET",
  });
  if (!Array.isArray(raw)) {
    throw new ApiError({
      kind: "unexpected",
      status: 200,
      title: "Beklenmeyen yanıt",
      detail: "Teklif talebi listesi beklenen biçimde değil.",
    });
  }
  return raw.map(mapQuoteRequestSummary);
}

export async function readSystemHealth(): Promise<SystemHealth> {
  const raw = await apiRequest<Record<string, unknown>>({
    path: "/system/health",
    method: "GET",
  });
  return { status: String(raw.status ?? "unknown") };
}

export function systemHealthLabel(status: string | null): string {
  if (!status) return "sorgulanıyor";
  if (status === "operational") return "Çalışıyor";
  return status;
}
