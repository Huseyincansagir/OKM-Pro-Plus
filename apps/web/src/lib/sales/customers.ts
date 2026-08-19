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
  primaryContactName: string;
  priceGroupCode: string;
  priceGroupName: string;
};

export type CustomerContact = {
  id: string;
  fullName: string;
  email: string;
  phone: string;
  roleTitle: string;
  isPrimary: boolean;
  isActive: boolean;
};

export type CustomerAddress = {
  id: string;
  addressType: string;
  title: string;
  line1: string;
  city: string;
  isDefault: boolean;
  isActive: boolean;
};

export type CustomerCard = CustomerSummary & {
  taxNumber: string;
  taxOffice: string;
  priceListId: string;
  priceListCode: string;
  contacts: CustomerContact[];
  addresses: CustomerAddress[];
};

export type CustomerPriceContext = {
  customerId: string;
  boundToCurrentAccount: boolean;
  customerPriceGroupCode: string;
  priceListCode: string;
  currencyCode: string;
  prices: Array<{
    productId: string;
    packagingId: string | null;
    unitPrice: number | null;
  }>;
};

export type CustomerOutboundEmail = {
  id: string;
  to: string;
  subject: string;
  status: string;
  lastError: string;
  createdAt: string;
  sentAt: string;
};

export type PriceGroupOption = {
  id: string;
  code: string;
  name: string;
  priceListCode: string;
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
    primaryContactName: String(record.primaryContactName ?? ""),
    priceGroupCode: String(record.priceGroupCode ?? ""),
    priceGroupName: String(record.priceGroupName ?? ""),
  };
}

function mapContact(raw: unknown): CustomerContact {
  const record = asRecord(raw);
  return {
    id: String(record.id ?? ""),
    fullName: String(record.fullName ?? ""),
    email: String(record.email ?? ""),
    phone: String(record.phone ?? ""),
    roleTitle: String(record.roleTitle ?? ""),
    isPrimary: Boolean(record.isPrimary),
    isActive: record.isActive !== false,
  };
}

export function mapCustomerCard(raw: unknown): CustomerCard {
  const record = asRecord(raw);
  const summary = mapCustomerSummary(record);
  const contacts = Array.isArray(record.contacts) ? record.contacts.map(mapContact) : [];
  const addresses = Array.isArray(record.addresses)
    ? record.addresses.map((item) => {
        const address = asRecord(item);
        return {
          id: String(address.id ?? ""),
          addressType: String(address.addressType ?? ""),
          title: String(address.title ?? ""),
          line1: String(address.line1 ?? ""),
          city: String(address.city ?? ""),
          isDefault: Boolean(address.isDefault),
          isActive: address.isActive !== false,
        };
      })
    : [];
  return {
    ...summary,
    taxNumber: String(record.taxNumber ?? ""),
    taxOffice: String(record.taxOffice ?? ""),
    priceListId: String(record.priceListId ?? ""),
    priceListCode: String(record.priceListCode ?? ""),
    contacts,
    addresses,
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

export type CreateCustomerInput = {
  legalName: string;
  email?: string;
  phone?: string;
  taxNumber?: string;
  taxOffice?: string;
};

export async function createCustomer(input: CreateCustomerInput): Promise<CustomerSummary> {
  const raw = await apiRequest<unknown>({
    path: "/customers",
    method: "POST",
    body: {
      legalName: input.legalName,
      email: input.email || null,
      phone: input.phone || null,
      taxNumber: input.taxNumber || null,
      taxOffice: input.taxOffice || null,
    },
    idempotent: true,
  });
  const mapped = mapCustomerSummary(raw);
  if (!mapped.id) {
    throw new ApiError({
      kind: "unexpected",
      status: 201,
      title: "Beklenmeyen yanıt",
      detail: "Müşteri oluşturuldu ama yanıt geçersiz.",
    });
  }
  return mapped;
}

export async function getCustomer(id: string): Promise<CustomerCard> {
  const raw = await apiRequest<unknown>({
    path: `/customers/${id}`,
    method: "GET",
  });
  const mapped = mapCustomerCard(raw);
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

export async function createCustomerContact(
  customerId: string,
  input: { fullName: string; email?: string; phone?: string; roleTitle?: string; isPrimary?: boolean },
): Promise<CustomerContact> {
  const raw = await apiRequest<unknown>({
    path: `/customers/${customerId}/contacts`,
    method: "POST",
    body: {
      fullName: input.fullName,
      email: input.email || null,
      phone: input.phone || null,
      roleTitle: input.roleTitle || null,
      isPrimary: Boolean(input.isPrimary),
    },
    idempotent: true,
  });
  return mapContact(raw);
}

export async function listCustomerPriceGroups(): Promise<PriceGroupOption[]> {
  const raw = await apiRequest<unknown>({
    path: "/customer-price-groups",
    method: "GET",
  });
  if (!Array.isArray(raw)) {
    throw new ApiError({
      kind: "unexpected",
      status: 200,
      title: "Beklenmeyen yanıt",
      detail: "Fiyat grubu listesi beklenen biçimde değil.",
    });
  }
  return raw.map((item) => {
    const record = asRecord(item);
    return {
      id: String(record.id ?? ""),
      code: String(record.code ?? ""),
      name: String(record.name ?? ""),
      priceListCode: String(record.priceListCode ?? ""),
    };
  });
}

export async function assignCustomerPriceGroup(customerId: string, customerPriceGroupId: string): Promise<void> {
  await apiRequest<unknown>({
    path: `/customers/${customerId}/price-group`,
    method: "POST",
    body: { customerPriceGroupId },
    idempotent: true,
  });
}

export async function getCustomerPriceContext(customerId: string): Promise<CustomerPriceContext> {
  const raw = await apiRequest<unknown>({
    path: `/customers/${customerId}/price-context`,
    method: "GET",
  });
  const record = asRecord(raw);
  const prices = Array.isArray(record.prices) ? record.prices : [];
  return {
    customerId: String(record.customerId ?? customerId),
    boundToCurrentAccount: record.boundToCurrentAccount === true,
    customerPriceGroupCode: String(record.customerPriceGroupCode ?? ""),
    priceListCode: String(record.priceListCode ?? ""),
    currencyCode: String(record.currencyCode ?? ""),
    prices: prices.map((item) => {
      const price = asRecord(item);
      return {
        productId: String(price.productId ?? ""),
        packagingId: typeof price.packagingId === "string" ? price.packagingId : null,
        unitPrice:
          typeof price.unitPrice === "number" && Number.isFinite(price.unitPrice) ? price.unitPrice : null,
      };
    }),
  };
}

export async function sendCustomerEmail(
  customerId: string,
  input: { contactId?: string; to?: string; subject: string; body: string },
): Promise<CustomerOutboundEmail> {
  const raw = await apiRequest<unknown>({
    path: `/customers/${customerId}/outbound-emails`,
    method: "POST",
    body: {
      contactId: input.contactId || null,
      to: input.to || null,
      subject: input.subject,
      body: input.body,
    },
    idempotent: true,
  });
  const record = asRecord(raw);
  return {
    id: String(record.id ?? ""),
    to: String(record.to ?? ""),
    subject: String(record.subject ?? ""),
    status: String(record.status ?? ""),
    lastError: String(record.lastError ?? ""),
    createdAt: String(record.createdAt ?? ""),
    sentAt: String(record.sentAt ?? ""),
  };
}

export async function listCustomerEmails(customerId: string): Promise<CustomerOutboundEmail[]> {
  const raw = await apiRequest<unknown>({
    path: `/customers/${customerId}/outbound-emails`,
    method: "GET",
  });
  if (!Array.isArray(raw)) {
    throw new ApiError({
      kind: "unexpected",
      status: 200,
      title: "Beklenmeyen yanıt",
      detail: "E-posta listesi beklenen biçimde değil.",
    });
  }
  return raw.map((item) => {
    const record = asRecord(item);
    return {
      id: String(record.id ?? ""),
      to: String(record.to ?? ""),
      subject: String(record.subject ?? ""),
      status: String(record.status ?? ""),
      lastError: String(record.lastError ?? ""),
      createdAt: String(record.createdAt ?? ""),
      sentAt: String(record.sentAt ?? ""),
    };
  });
}
