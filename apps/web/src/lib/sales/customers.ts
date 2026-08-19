import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";

export type CustomerSummary = {
  id: string;
  customerCode: string;
  legalName: string;
  status: string;
  email: string;
  phone: string;
  createdAt: string;
};

function asRecord(value: unknown): Record<string, unknown> {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : {};
}

export function customerStatusLabel(status: string): string {
  if (status === "Active") return "Aktif";
  if (status === "Candidate") return "Aday";
  if (status === "Inactive") return "Pasif";
  if (status === "Blocked") return "Engelli";
  return status || "Bilinmiyor";
}

export function mapCustomerSummary(raw: unknown): CustomerSummary {
  const record = asRecord(raw);
  return {
    id: String(record.id ?? ""),
    customerCode: String(record.customerCode ?? ""),
    legalName: String(record.legalName ?? ""),
    status: String(record.status ?? ""),
    email: String(record.email ?? ""),
    phone: String(record.phone ?? ""),
    createdAt: String(record.createdAt ?? ""),
  };
}

export async function listCustomers(): Promise<CustomerSummary[]> {
  const raw = await apiRequest<unknown>({
    path: "/customers",
    method: "GET",
  });
  if (!Array.isArray(raw)) {
    throw new ApiError({
      kind: "unexpected",
      status: 200,
      title: "Beklenmeyen yanıt",
      detail: "Müşteri listesi beklenen biçimde değil.",
    });
  }
  return raw.map(mapCustomerSummary);
}

export async function getCustomer(id: string): Promise<CustomerSummary> {
  const raw = await apiRequest<unknown>({
    path: `/customers/${id}`,
    method: "GET",
  });
  const mapped = mapCustomerSummary(raw);
  if (!mapped.id) {
    throw new ApiError({
      kind: "not_found",
      status: 404,
      title: "Bulunamadı",
      detail: "Müşteri bulunamadı.",
    });
  }
  return mapped;
}
