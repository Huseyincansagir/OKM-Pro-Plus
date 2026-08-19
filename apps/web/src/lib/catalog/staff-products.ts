import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";
import type { StatusKind } from "@/components/ui/status-badge";

export type StaffPackaging = {
  id: string;
  level: string;
  name: string;
  quantityInBaseUom: number | null;
  isSellable: boolean;
  allowPartial: boolean;
};

export type StaffProductSummary = {
  id: string;
  code: string;
  slug: string;
  name: string;
  categoryName: string;
  isActive: boolean;
  isPublic: boolean;
  baseUomName: string;
  packagingCount: number;
  createdAt: string;
};

export type StaffProductDetail = StaffProductSummary & {
  description: string;
  sizeLabel: string;
  categoryCode: string;
  packagings: StaffPackaging[];
};

function asRecord(value: unknown): Record<string, unknown> {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : {};
}

function asFiniteNumber(value: unknown): number | null {
  return typeof value === "number" && Number.isFinite(value) ? value : null;
}

export function staffProductStatusKind(product: { isActive: boolean; isPublic: boolean }): StatusKind {
  if (!product.isActive) return "inactive";
  if (product.isPublic) return "success";
  return "info";
}

export function staffProductStatusLabel(product: { isActive: boolean; isPublic: boolean }): string {
  if (!product.isActive) return "Pasif";
  if (product.isPublic) return "Public";
  return "İç kayıt";
}

export function mapStaffPackaging(raw: unknown): StaffPackaging {
  const record = asRecord(raw);
  return {
    id: String(record.id ?? ""),
    level: String(record.level ?? ""),
    name: String(record.name ?? ""),
    quantityInBaseUom: asFiniteNumber(record.quantityInBaseUom),
    isSellable: record.isSellable === true,
    allowPartial: record.allowPartial === true,
  };
}

export function mapStaffProductSummary(raw: unknown): StaffProductSummary {
  const record = asRecord(raw);
  const packagings = Array.isArray(record.packagings) ? record.packagings : [];
  const baseUom = asRecord(record.baseUom);
  return {
    id: String(record.id ?? ""),
    code: String(record.code ?? ""),
    slug: String(record.slug ?? ""),
    name: String(record.name ?? ""),
    categoryName: String(record.categoryName ?? ""),
    isActive: record.isActive === true,
    isPublic: record.isPublic === true,
    baseUomName: String(baseUom.displayName ?? baseUom.code ?? ""),
    packagingCount: packagings.length,
    createdAt: String(record.createdAt ?? ""),
  };
}

export function mapStaffProductDetail(raw: unknown): StaffProductDetail {
  const record = asRecord(raw);
  const summary = mapStaffProductSummary(record);
  const packagings = Array.isArray(record.packagings) ? record.packagings.map(mapStaffPackaging) : [];
  return {
    ...summary,
    packagingCount: packagings.length,
    description: String(record.description ?? ""),
    sizeLabel: String(record.sizeLabel ?? ""),
    categoryCode: String(record.categoryCode ?? ""),
    packagings,
  };
}

export async function listStaffProducts(): Promise<StaffProductSummary[]> {
  const raw = await apiRequest<unknown>({
    path: "/products",
    method: "GET",
  });
  if (!Array.isArray(raw)) {
    throw new ApiError({
      kind: "unexpected",
      status: 200,
      title: "Beklenmeyen yanıt",
      detail: "Ürün listesi beklenen biçimde değil.",
    });
  }
  return raw.map(mapStaffProductSummary);
}

export async function getStaffProduct(id: string): Promise<StaffProductDetail> {
  const raw = await apiRequest<unknown>({
    path: `/products/${id}`,
    method: "GET",
  });
  const detail = mapStaffProductDetail(raw);
  if (!detail.id) {
    throw new ApiError({
      kind: "not_found",
      status: 404,
      title: "Bulunamadı",
      detail: "Ürün bulunamadı.",
    });
  }
  return detail;
}
