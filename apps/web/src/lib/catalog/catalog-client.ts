"use client";

import { apiRequest } from "@/lib/api/client";
import { mapPublicProduct, mapPublicProductPage } from "@/lib/catalog/map-product";
import type { PublicProduct, PublicProductPage, QuoteRequestResult } from "@/lib/catalog/types";

export async function listPublicProducts(query: {
  search?: string;
  category?: string;
  page?: number;
  pageSize?: number;
}): Promise<PublicProductPage> {
  const params = new URLSearchParams();
  if (query.search) params.set("search", query.search);
  if (query.category) params.set("category", query.category);
  if (query.page) params.set("page", String(query.page));
  if (query.pageSize) params.set("pageSize", String(query.pageSize));
  const suffix = params.toString() ? `?${params.toString()}` : "";
  const raw = await apiRequest<unknown>({
    path: `/public/catalog/products${suffix}`,
    method: "GET",
    auth: false,
  });
  return mapPublicProductPage(raw);
}

export async function getPublicProduct(slug: string): Promise<PublicProduct> {
  const raw = await apiRequest<unknown>({
    path: `/public/catalog/products/${encodeURIComponent(slug)}`,
    method: "GET",
    auth: false,
  });
  return mapPublicProduct(raw);
}

export type SubmitQuoteRequestInput = {
  companyName: string;
  contactName: string;
  phone: string;
  email: string;
  items: Array<{
    productId: string;
    enteredQuantity: number;
    enteredPackagingId: string | null;
    viewMode: string;
  }>;
  note?: string;
  consentAccepted: boolean;
};

export async function submitPublicQuoteRequest(
  input: SubmitQuoteRequestInput,
): Promise<QuoteRequestResult> {
  const raw = await apiRequest<{
    id: string;
    requestNumber: string;
    status: string;
    createdAt: string;
  }>({
    path: "/public/quote-requests",
    method: "POST",
    auth: false,
    idempotent: false,
    body: {
      companyName: input.companyName,
      contactName: input.contactName,
      phone: input.phone,
      email: input.email,
      items: input.items,
      note: input.note,
      consentAccepted: input.consentAccepted,
    },
  });

  return {
    id: raw.id,
    requestNumber: raw.requestNumber,
    status: raw.status,
    createdAt: raw.createdAt,
  };
}
