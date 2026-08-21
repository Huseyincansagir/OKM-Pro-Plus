"use client";

import { useEffect, useState } from "react";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Dialog } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { userFacingMessage } from "@/lib/api/auth-client";
import {
  applyPayment,
  listPaymentMethods,
  type PaymentMethodOption,
  type PaymentRow,
} from "@/lib/finance/payments";
import { listCustomers, type CustomerSummary } from "@/lib/sales/customers";
import { listInvoices, type InvoiceRow } from "@/lib/finance/ledgers";

export type PaymentModalProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  initialCustomerId?: string;
  initialInvoiceId?: string;
  initialAmount?: number;
  onSuccess?: (payment: PaymentRow) => void;
};

export function PaymentModal({
  open,
  onOpenChange,
  initialCustomerId,
  initialInvoiceId,
  initialAmount,
  onSuccess,
}: PaymentModalProps) {
  const [customers, setCustomers] = useState<CustomerSummary[]>([]);
  const [methods, setMethods] = useState<PaymentMethodOption[]>([]);
  const [invoices, setInvoices] = useState<InvoiceRow[]>([]);
  const [customerId, setCustomerId] = useState(initialCustomerId || "");
  const [invoiceId, setInvoiceId] = useState(initialInvoiceId || "");
  const [paymentMethodId, setPaymentMethodId] = useState("");
  const [amount, setAmount] = useState(initialAmount !== undefined ? String(initialAmount) : "");
  const [reference, setReference] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) return;
    setError(null);
    setCustomerId(initialCustomerId || "");
    setInvoiceId(initialInvoiceId || "");
    setAmount(initialAmount !== undefined ? String(initialAmount) : "");
    setReference("");

    listPaymentMethods()
      .then((res) => {
        setMethods(res);
        setPaymentMethodId((current) => current || (res.length > 0 ? res[0].id : ""));
      })
      .catch(() => {});

    listCustomers()
      .then((res) => setCustomers(res.filter((c) => c.status === "Active")))
      .catch(() => {});

    listInvoices()
      .then((res) => setInvoices(res.filter((inv) => inv.status === "Issued" || inv.status === "PartiallyPaid")))
      .catch(() => {});
  }, [open, initialCustomerId, initialInvoiceId, initialAmount]);

  // When customer changes, if current invoice doesn't belong to customer, reset invoiceId
  const availableInvoices = invoices.filter((inv) => (customerId ? inv.customerId === customerId : true));

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const parsedAmount = parseFloat(amount);
    if (isNaN(parsedAmount) || parsedAmount <= 0) {
      setError("Geçerli bir ödeme tutarı giriniz.");
      return;
    }
    if (!customerId) {
      setError("Müşteri seçimi zorunludur.");
      return;
    }
    if (!paymentMethodId) {
      setError("Ödeme yöntemi seçimi zorunludur.");
      return;
    }

    setSubmitting(true);
    setError(null);
    try {
      const payment = await applyPayment({
        customerId,
        amount: parsedAmount,
        paymentMethodId,
        invoiceId: invoiceId || null,
        reference: reference.trim() || null,
      });
      onOpenChange(false);
      onSuccess?.(payment);
    } catch (caught) {
      setError(userFacingMessage(caught));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(nextOpen) => {
        if (!submitting) onOpenChange(nextOpen);
      }}
      title="Tahsilat / Ödeme Girişi"
      description="Tahsilat uygulandığında müşterinin cari hesabına alacak kaydı (Credit) işlenir ve bakiye güncellenir."
      footer={
        <div className="flex justify-end gap-2">
          <Button variant="secondary" disabled={submitting} onClick={() => onOpenChange(false)}>
            Vazgeç
          </Button>
          <Button loading={submitting} onClick={(e) => void handleSubmit(e)}>
            Tahsilatı Kaydet
          </Button>
        </div>
      }
    >
      <form onSubmit={(e) => void handleSubmit(e)} className="space-y-4 text-sm">
        {error ? <Alert tone="danger" title="İşlem Başarısız">{error}</Alert> : null}

        <div>
          <Select
            label="Müşteri"
            value={customerId}
            onChange={(e) => {
              setCustomerId(e.target.value);
              setInvoiceId("");
            }}
            disabled={Boolean(initialCustomerId) || submitting}
            options={[
              { value: "", label: "Müşteri seçin..." },
              ...customers.map((c) => ({
                value: c.id,
                label: `${c.legalName} (${c.customerCode || c.id.slice(0, 8)})`,
              })),
            ]}
          />
        </div>

        <div>
          <Select
            label="Ödeme Yöntemi"
            value={paymentMethodId}
            onChange={(e) => setPaymentMethodId(e.target.value)}
            disabled={submitting}
            options={[
              { value: "", label: "Ödeme yöntemi seçin..." },
              ...methods.map((m) => ({
                value: m.id,
                label: m.name || m.code,
              })),
            ]}
          />
        </div>

        <div>
          <Input
            label="Tutar (TRY)"
            type="number"
            step="0.01"
            min="0.01"
            placeholder="0.00"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            disabled={submitting}
            required
          />
        </div>

        <div>
          <Select
            label="İlişkili Fatura (İsteğe Bağlı)"
            value={invoiceId}
            onChange={(e) => {
              const selected = e.target.value;
              setInvoiceId(selected);
              if (selected) {
                const targetInvoice = invoices.find((inv) => inv.id === selected);
                if (targetInvoice && targetInvoice.grandTotal !== null && !amount) {
                  setAmount(String(targetInvoice.grandTotal));
                }
              }
            }}
            disabled={Boolean(initialInvoiceId) || submitting}
            options={[
              { value: "", label: "Serbest Tahsilat (Faturasız)" },
              ...availableInvoices.map((inv) => ({
                value: inv.id,
                label: `${inv.invoiceNumber || inv.id.slice(0, 8)} — ${inv.grandTotal ? `₺${inv.grandTotal}` : "—"} (${inv.status})`,
              })),
            ]}
          />
        </div>

        <div>
          <Input
            label="Açıklama / Dekont Referansı"
            type="text"
            placeholder="Örn: Dekont #987654"
            value={reference}
            onChange={(e) => setReference(e.target.value)}
            disabled={submitting}
          />
        </div>
      </form>
    </Dialog>
  );
}
