import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { PaymentModal } from "@/components/finance/payment-modal";
import { applyPayment, listPaymentMethods } from "@/lib/finance/payments";
import { listCustomers } from "@/lib/sales/customers";
import { listInvoices } from "@/lib/finance/ledgers";

vi.mock("@/lib/finance/payments", () => ({
  applyPayment: vi.fn(),
  listPaymentMethods: vi.fn(),
}));

vi.mock("@/lib/sales/customers", () => ({
  listCustomers: vi.fn(),
}));

vi.mock("@/lib/finance/ledgers", () => ({
  listInvoices: vi.fn(),
}));

describe("PaymentModal", () => {
  beforeEach(() => {
    vi.mocked(applyPayment).mockReset();
    vi.mocked(listPaymentMethods).mockResolvedValue([
      { id: "pm-1", code: "BANK", name: "Banka Havalesi", isActive: true },
      { id: "pm-2", code: "CASH", name: "Nakit", isActive: true },
    ]);
    vi.mocked(listCustomers).mockResolvedValue([
      {
        id: "cust-1",
        customerCode: "MUS-2026-000001",
        legalName: "Acme Sanayi A.S.",
        status: "Active",
        email: "info@acme.com",
        phone: "555-1234",
        createdAt: "2026-08-21T10:00:00Z",
        primaryContactName: "Ali Veli",
        priceGroupCode: "STD",
        priceGroupName: "Standart",
      },
    ]);
    vi.mocked(listInvoices).mockResolvedValue([
      {
        id: "inv-1",
        invoiceNumber: "INV-2026-000001",
        customerId: "cust-1",
        status: "Issued",
        currencyCode: "TRY",
        grandTotal: 1500,
        itemCount: 1,
      },
    ]);
  });

  it("renders modal and loads customers, methods, invoices", async () => {
    render(<PaymentModal open={true} onOpenChange={vi.fn()} />);
    expect(await screen.findByText("Tahsilat / Ödeme Girişi")).toBeInTheDocument();
    expect(await screen.findByText("Acme Sanayi A.S. (MUS-2026-000001)")).toBeInTheDocument();
    expect(screen.getByText("Banka Havalesi")).toBeInTheDocument();
  });

  it("submits payment form successfully and calls onSuccess", async () => {
    const user = userEvent.setup();
    const onOpenChange = vi.fn();
    const onSuccess = vi.fn();

    vi.mocked(applyPayment).mockResolvedValue({
      id: "pay-1",
      customerId: "cust-1",
      amount: 1500,
      paymentMethodId: "pm-1",
      status: "Applied",
      invoiceId: "inv-1",
      appliedAt: "2026-08-21T12:00:00Z",
    });

    render(
      <PaymentModal
        open={true}
        onOpenChange={onOpenChange}
        initialCustomerId="cust-1"
        initialInvoiceId="inv-1"
        initialAmount={1500}
        onSuccess={onSuccess}
      />,
    );

    expect(await screen.findByText("Tahsilat / Ödeme Girişi")).toBeInTheDocument();

    const saveBtn = screen.getByRole("button", { name: "Tahsilatı Kaydet" });
    await user.click(saveBtn);

    expect(applyPayment).toHaveBeenCalledWith({
      customerId: "cust-1",
      amount: 1500,
      paymentMethodId: "pm-1",
      invoiceId: "inv-1",
      reference: null,
    });
    expect(onOpenChange).toHaveBeenCalledWith(false);
    expect(onSuccess).toHaveBeenCalled();
  });
});
