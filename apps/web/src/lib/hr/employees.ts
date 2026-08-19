import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";

export type EmployeeRow = {
  id: string;
  code: string;
  fullName: string;
  title: string;
  department: string;
  status: string;
  createdAt: string;
};

function asRecord(value: unknown): Record<string, unknown> {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : {};
}

export function mapEmployee(raw: unknown): EmployeeRow {
  const record = asRecord(raw);
  return {
    id: String(record.id ?? ""),
    code: String(record.code ?? ""),
    fullName: String(record.fullName ?? ""),
    title: String(record.title ?? ""),
    department: String(record.department ?? ""),
    status: String(record.status ?? ""),
    createdAt: String(record.createdAt ?? ""),
  };
}

export async function listEmployees(): Promise<EmployeeRow[]> {
  const raw = await apiRequest<unknown>({ path: "/employees", method: "GET" });
  if (!Array.isArray(raw)) {
    throw new ApiError({
      kind: "unexpected",
      status: 200,
      title: "Beklenmeyen yanıt",
      detail: "Personel listesi beklenen biçimde değil.",
    });
  }
  return raw.map(mapEmployee);
}

export async function createEmployee(input: {
  fullName: string;
  title?: string;
  department?: string;
}): Promise<EmployeeRow> {
  return mapEmployee(
    await apiRequest<unknown>({
      path: "/employees",
      method: "POST",
      body: input,
      idempotent: true,
    }),
  );
}
