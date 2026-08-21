import { apiRequest } from "@/lib/api/client";
import { ApiError } from "@/lib/api/types";
import type { StatusKind } from "@/components/ui/status-badge";

export type PaymentRow = {
  id: string;
  customerId: string;
  amount: number;
  paymentMethodId: string;
  status: string;
  invoiceId: string | null;
  appliedAt: string | null;
};

export type PaymentMethodOption = {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
};

export type CurrentTransactionRow = {
  id: string;
  currentAccountId: string;
  transactionType: string;
  debitAmount: number;
  creditAmount: number;
  currencyCode: string;
  sourceEntityType: string;
  sourceEntityId: string;
  createdAt: string;
};

export type ApplyPaymentInput = {
  customerId: string;
  amount: number;
  paymentMethodId: string;
  invoiceId?: string | null;
  reference?: string | null;
};

function asRecord(value: unknown): Record<string, unknown> {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : {};
}

function asFiniteNumber(value: unknown, defaultValue = 0): number {
  return typeof value === "number" && Number.isFinite(value) ? value : defaultValue;
}

export function mapPaymentRow(raw: unknown): PaymentRow {
  const record = asRecord(raw);
  return {
    id: String(record.id ?? ""),
    customerId: String(record.customerId ?? ""),
    amount: asFiniteNumber(record.amount),
    paymentMethodId: String(record.paymentMethodId ?? ""),
    status: String(record.status ?? "Applied"),
    invoiceId: typeof record.invoiceId === "string" ? record.invoiceId : null,
    appliedAt: typeof record.appliedAt === "string" ? record.appliedAt : null,
  };
}

export function mapPaymentMethod(raw: unknown): PaymentMethodOption {
  const record = asRecord(raw);
  return {
    id: String(record.id ?? ""),
    code: String(record.code ?? ""),
    name: String(record.name ?? record.code ?? ""),
    isActive: Boolean(record.isActive),
  };
}

export function mapCurrentTransaction(raw: unknown): CurrentTransactionRow {
  const record = asRecord(raw);
  return {
    id: String(record.id ?? ""),
    currentAccountId: String(record.currentAccountId ?? ""),
    transactionType: String(record.transactionType ?? ""),
    debitAmount: asFiniteNumber(record.debitAmount),
    creditAmount: asFiniteNumber(record.creditAmount),
    currencyCode: String(record.currencyCode ?? "TRY"),
    sourceEntityType: String(record.sourceEntityType ?? ""),
    sourceEntityId: String(record.sourceEntityId ?? ""),
    createdAt: String(record.createdAt ?? ""),
  };
}

export async function listPayments(): Promise<PaymentRow[]> {
  const raw = await apiRequest<unknown>({ path: "/payments", method: "GET" });
  if (!Array.isArray(raw)) {
    throw new ApiError({
      kind: "unexpected",
      status: 200,
      title: "Beklenmeyen yanıt",
      detail: "Ödeme listesi beklenen biçimde değil.",
    });
  }
  return raw.map(mapPaymentRow);
}

export async function listPaymentMethods(): Promise<PaymentMethodOption[]> {
  const raw = await apiRequest<unknown>({ path: "/payments/methods", method: "GET" });
  if (!Array.isArray(raw)) {
    throw new ApiError({
      kind: "unexpected",
      status: 200,
      title: "Beklenmeyen yanıt",
      detail: "Ödeme yöntemi listesi beklenen biçimde değil.",
    });
  }
  return raw.map(mapPaymentMethod);
}

export async function listCustomerTransactions(customerId: string): Promise<CurrentTransactionRow[]> {
  const raw = await apiRequest<unknown>({
    path: `/current-accounts/${customerId}/transactions`,
    method: "GET",
  });
  if (!Array.isArray(raw)) {
    throw new ApiError({
      kind: "unexpected",
      status: 200,
      title: "Beklenmeyen yanıt",
      detail: "Cari hareket listesi beklenen biçimde değil.",
    });
  }
  return raw.map(mapCurrentTransaction);
}

export async function applyPayment(input: ApplyPaymentInput): Promise<PaymentRow> {
  const raw = await apiRequest<unknown>({
    path: "/payments",
    method: "POST",
    body: {
      customerId: input.customerId,
      amount: input.amount,
      paymentMethodId: input.paymentMethodId,
      invoiceId: input.invoiceId ?? null,
      reference: input.reference ?? null,
    },
    idempotent: true,
  });
  return mapPaymentRow(raw);
}

export function paymentStatusKind(status: string): StatusKind {
  switch (status) {
    case "Applied":
    case "Completed":
      return "success";
    case "Draft":
    case "Pending":
      return "pending";
    case "Reversed":
    case "Cancelled":
      return "critical";
    default:
      return "info";
  }
}
