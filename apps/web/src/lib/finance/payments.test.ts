import { beforeEach, describe, expect, it, vi } from "vitest";
import { apiRequest } from "@/lib/api/client";
import {
  applyPayment,
  listCustomerTransactions,
  listPaymentMethods,
  listPayments,
  mapCurrentTransaction,
  mapPaymentMethod,
  mapPaymentRow,
  paymentStatusKind,
} from "@/lib/finance/payments";

vi.mock("@/lib/api/client", () => ({
  apiRequest: vi.fn(),
}));

describe("payments API client", () => {
  beforeEach(() => {
    vi.mocked(apiRequest).mockReset();
  });

  it("maps payment row correctly", () => {
    const raw = {
      id: "pay-1",
      customerId: "cust-1",
      amount: 1500,
      paymentMethodId: "pm-1",
      status: "Applied",
      invoiceId: "inv-1",
      appliedAt: "2026-08-21T11:00:00Z",
    };
    const mapped = mapPaymentRow(raw);
    expect(mapped.id).toBe("pay-1");
    expect(mapped.amount).toBe(1500);
    expect(mapped.invoiceId).toBe("inv-1");
    expect(mapped.status).toBe("Applied");
  });

  it("maps payment method correctly", () => {
    const raw = {
      id: "pm-1",
      code: "BANK",
      name: "Banka Havalesi",
      isActive: true,
    };
    const mapped = mapPaymentMethod(raw);
    expect(mapped.id).toBe("pm-1");
    expect(mapped.code).toBe("BANK");
    expect(mapped.name).toBe("Banka Havalesi");
    expect(mapped.isActive).toBe(true);
  });

  it("maps current transaction correctly", () => {
    const raw = {
      id: "tx-1",
      currentAccountId: "acc-1",
      transactionType: "PaymentApplied",
      debitAmount: 0,
      creditAmount: 2500,
      currencyCode: "TRY",
      sourceEntityType: "PaymentRecord",
      sourceEntityId: "pay-1",
      createdAt: "2026-08-21T11:00:00Z",
    };
    const mapped = mapCurrentTransaction(raw);
    expect(mapped.id).toBe("tx-1");
    expect(mapped.creditAmount).toBe(2500);
    expect(mapped.debitAmount).toBe(0);
    expect(mapped.transactionType).toBe("PaymentApplied");
  });

  it("calls listPayments and returns mapped list", async () => {
    vi.mocked(apiRequest).mockResolvedValue([
      {
        id: "pay-1",
        customerId: "cust-1",
        amount: 500,
        paymentMethodId: "pm-1",
        status: "Applied",
        invoiceId: null,
        appliedAt: null,
      },
    ]);

    const result = await listPayments();
    expect(apiRequest).toHaveBeenCalledWith({ path: "/payments", method: "GET" });
    expect(result).toHaveLength(1);
    expect(result[0].amount).toBe(500);
  });

  it("calls listPaymentMethods and returns mapped list", async () => {
    vi.mocked(apiRequest).mockResolvedValue([
      {
        id: "pm-1",
        code: "CASH",
        name: "Nakit",
        isActive: true,
      },
    ]);

    const result = await listPaymentMethods();
    expect(apiRequest).toHaveBeenCalledWith({ path: "/payments/methods", method: "GET" });
    expect(result).toHaveLength(1);
    expect(result[0].code).toBe("CASH");
  });

  it("calls listCustomerTransactions and returns mapped list", async () => {
    vi.mocked(apiRequest).mockResolvedValue([
      {
        id: "tx-1",
        currentAccountId: "acc-1",
        transactionType: "InvoiceIssued",
        debitAmount: 1000,
        creditAmount: 0,
        currencyCode: "TRY",
        sourceEntityType: "InvoiceRecord",
        sourceEntityId: "inv-1",
        createdAt: "2026-08-21T10:00:00Z",
      },
    ]);

    const result = await listCustomerTransactions("cust-1");
    expect(apiRequest).toHaveBeenCalledWith({
      path: "/current-accounts/cust-1/transactions",
      method: "GET",
    });
    expect(result).toHaveLength(1);
    expect(result[0].debitAmount).toBe(1000);
  });

  it("calls applyPayment with idempotent POST", async () => {
    vi.mocked(apiRequest).mockResolvedValue({
      id: "pay-new",
      customerId: "cust-1",
      amount: 750,
      paymentMethodId: "pm-bank",
      status: "Applied",
      invoiceId: "inv-1",
      appliedAt: "2026-08-21T12:00:00Z",
    });

    const result = await applyPayment({
      customerId: "cust-1",
      amount: 750,
      paymentMethodId: "pm-bank",
      invoiceId: "inv-1",
      reference: "Dekont No 12345",
    });

    expect(apiRequest).toHaveBeenCalledWith({
      path: "/payments",
      method: "POST",
      body: {
        customerId: "cust-1",
        amount: 750,
        paymentMethodId: "pm-bank",
        invoiceId: "inv-1",
        reference: "Dekont No 12345",
      },
      idempotent: true,
    });
    expect(result.id).toBe("pay-new");
    expect(result.amount).toBe(750);
  });

  it("maps paymentStatusKind properly", () => {
    expect(paymentStatusKind("Applied")).toBe("success");
    expect(paymentStatusKind("Completed")).toBe("success");
    expect(paymentStatusKind("Pending")).toBe("pending");
    expect(paymentStatusKind("Reversed")).toBe("critical");
  });
});
