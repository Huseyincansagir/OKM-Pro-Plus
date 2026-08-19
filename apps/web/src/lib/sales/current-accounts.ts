import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";

export type CurrentAccountSummary = {
  customerId: string;
  currencyCode: string;
  debitTotal: number;
  creditTotal: number;
  balance: number;
};

function asRecord(value: unknown): Record<string, unknown> {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : {};
}

function asNumber(value: unknown): number | null {
  return typeof value === "number" && Number.isFinite(value) ? value : null;
}

export function mapCurrentAccount(raw: unknown): CurrentAccountSummary {
  const record = asRecord(raw);
  const debitTotal = asNumber(record.debitTotal);
  const creditTotal = asNumber(record.creditTotal);
  const balance = asNumber(record.balance);
  if (debitTotal === null || creditTotal === null || balance === null) {
    throw new ApiError({
      kind: "unexpected",
      status: 200,
      title: "Beklenmeyen yanıt",
      detail: "Cari hesap tutarları eksik; sayı uydurulmaz.",
    });
  }
  return {
    customerId: String(record.customerId ?? ""),
    currencyCode: String(record.currencyCode || "TRY"),
    debitTotal,
    creditTotal,
    balance,
  };
}

export async function getCurrentAccount(customerId: string): Promise<CurrentAccountSummary | null> {
  try {
    const raw = await apiRequest<unknown>({
      path: `/current-accounts/${customerId}`,
      method: "GET",
    });
    return mapCurrentAccount(raw);
  } catch (error) {
    if (error instanceof ApiError && (error.kind === "not_found" || error.status === 404)) {
      return null;
    }
    throw error;
  }
}
