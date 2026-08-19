"use client";

import { useEffect, useState } from "react";
import { FileText, Layers, Receipt, Wallet } from "lucide-react";
import { AppShell } from "@/components/shell/app-shell";
import { KpiMetric } from "@/components/dashboard/kpi-metric";
import { EmptyState } from "@/components/states/empty-state";
import { ErrorState } from "@/components/states/error-state";
import { PermissionDenied } from "@/components/states/permission-denied";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader, CardTitle } from "@/components/ui/card";
import { DataTable } from "@/components/ui/data-table";
import { StatusBadge } from "@/components/ui/status-badge";
import { ApiError } from "@/lib/api/types";
import { userFacingMessage } from "@/lib/api/auth-client";
import { useSessionStore } from "@/lib/auth/session-store";
import {
  listCurrentAccounts,
  listDeliveryNotes,
  listInvoices,
  type AccountRow,
  type DeliveryNoteRow,
  type InvoiceRow,
} from "@/lib/finance/ledgers";

function formatMoney(value: number | null, currency: string): string {
  if (value === null || !currency) {
    return "—";
  }
  return new Intl.NumberFormat("tr-TR", {
    style: "currency",
    currency,
    maximumFractionDigits: 2,
  }).format(value);
}

export function FinanceBoard() {
  const user = useSessionStore((state) => state.user);
  const permissions = user?.permissions ?? [];
  const canReadInvoices = permissions.includes("invoice.read");
  const canReadNotes = permissions.includes("delivery-note.read");
  const canReadAccounts = permissions.includes("current-account.read");
  const canRead = canReadInvoices || canReadNotes || canReadAccounts;
  const [invoices, setInvoices] = useState<InvoiceRow[] | null>(null);
  const [notes, setNotes] = useState<DeliveryNoteRow[] | null>(null);
  const [accounts, setAccounts] = useState<AccountRow[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [denied, setDenied] = useState(false);
  const [loading, setLoading] = useState(canRead);
  const [reload, setReload] = useState(0);

  useEffect(() => {
    if (!canRead) {
      setLoading(false);
      return;
    }
    let cancelled = false;
    setLoading(true);
    setError(null);
    setDenied(false);
    Promise.all([
      canReadInvoices ? listInvoices() : Promise.resolve([]),
      canReadNotes ? listDeliveryNotes() : Promise.resolve([]),
      canReadAccounts ? listCurrentAccounts() : Promise.resolve([]),
    ])
      .then(([invoiceRows, noteRows, accountRows]) => {
        if (cancelled) return;
        setInvoices(invoiceRows);
        setNotes(noteRows);
        setAccounts(accountRows);
      })
      .catch((caught) => {
        if (cancelled) return;
        if (caught instanceof ApiError && caught.kind === "permission_denied") {
          setDenied(true);
          return;
        }
        setError(userFacingMessage(caught));
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [canRead, canReadAccounts, canReadInvoices, canReadNotes, reload]);

  const ready = !loading && !error && !denied;
  const invoiceTotal = invoices?.length ?? 0;
  const noteTotal = notes?.length ?? 0;
  const accountTotal = accounts?.length ?? 0;

  return (
    <AppShell
      currentHref="/cari"
      breadcrumbs={[
        { label: "Çalışma alanı", href: "/dashboard" },
        { label: "Cari ve muhasebe" },
      ]}
      pageTitle="Cari ve muhasebe"
      pageDescription="Faturalar, irsaliyeler ve cari bakiyeler sunucudan gelir. Eksik tutar ₺0 yazılmaz."
      pageActions={
        canRead ? (
          <Button variant="secondary" loading={loading} onClick={() => setReload((value) => value + 1)}>
            Yenile
          </Button>
        ) : null
      }
    >
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <KpiMetric
          label="Fatura"
          value={ready && canReadInvoices ? String(invoiceTotal) : "—"}
          unit="belge"
          icon={Receipt}
          tone="teal"
          unavailable={!ready || !canReadInvoices}
          caption="GET /invoices"
        />
        <KpiMetric
          label="İrsaliye"
          value={ready && canReadNotes ? String(noteTotal) : "—"}
          unit="belge"
          icon={FileText}
          tone="amber"
          unavailable={!ready || !canReadNotes}
          caption="GET /delivery-notes"
        />
        <KpiMetric
          label="Cari kart"
          value={ready && canReadAccounts ? String(accountTotal) : "—"}
          unit="hesap"
          icon={Wallet}
          tone="navy"
          unavailable={!ready || !canReadAccounts}
          caption="GET /current-accounts"
        />
        <KpiMetric
          label="Pencere"
          value={ready ? "100" : "—"}
          icon={Layers}
          tone="navy"
          unavailable={!ready}
          caption="Her liste en fazla 100"
        />
      </div>

      {!canRead ? (
        <div className="mt-4">
          <PermissionDenied
            title="Cari bu oturumda görünmez"
            description="invoice.read / delivery-note.read / current-account.read yok."
          />
        </div>
      ) : denied ? (
        <div className="mt-4">
          <PermissionDenied />
        </div>
      ) : error ? (
        <div className="mt-4">
          <ErrorState title="Cari yüklenemedi" description={error} onRetry={() => setReload((value) => value + 1)} />
        </div>
      ) : (
        <div className="mt-4 space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Faturalar</CardTitle>
            </CardHeader>
            <CardBody>
              {!canReadInvoices ? (
                <p className="text-sm text-slate-600">invoice.read yok.</p>
              ) : loading || !invoices ? (
                <p className="text-sm text-slate-600">Yükleniyor…</p>
              ) : invoices.length === 0 ? (
                <EmptyState title="Fatura yok" description="GET /invoices boş." />
              ) : (
                <DataTable
                  columns={[
                    { id: "no", header: "Belge", accessor: (row) => row.invoiceNumber || row.id.slice(0, 8) },
                    {
                      id: "status",
                      header: "Durum",
                      accessor: (row) => (
                        <StatusBadge
                          status={row.status === "Issued" ? "success" : "pending"}
                          label={row.status}
                        />
                      ),
                    },
                    {
                      id: "total",
                      header: "Tutar",
                      accessor: (row) => formatMoney(row.grandTotal, row.currencyCode || "TRY"),
                    },
                  ]}
                  rows={invoices}
                  getRowId={(row) => row.id}
                />
              )}
            </CardBody>
          </Card>
          <Card>
            <CardHeader>
              <CardTitle>Cari bakiyeler</CardTitle>
            </CardHeader>
            <CardBody>
              {!canReadAccounts ? (
                <p className="text-sm text-slate-600">current-account.read yok.</p>
              ) : loading || !accounts ? (
                <p className="text-sm text-slate-600">Yükleniyor…</p>
              ) : accounts.length === 0 ? (
                <EmptyState title="Cari hesap yok" description="Hesap yokken ₺0 yazılmaz." />
              ) : (
                <DataTable
                  columns={[
                    { id: "customer", header: "Müşteri", accessor: (row) => row.customerId.slice(0, 8) },
                    {
                      id: "debit",
                      header: "Borç",
                      accessor: (row) => formatMoney(row.debitTotal, row.currencyCode || "TRY"),
                    },
                    {
                      id: "credit",
                      header: "Alacak",
                      accessor: (row) => formatMoney(row.creditTotal, row.currencyCode || "TRY"),
                    },
                    {
                      id: "balance",
                      header: "Bakiye",
                      accessor: (row) => formatMoney(row.balance, row.currencyCode || "TRY"),
                    },
                  ]}
                  rows={accounts}
                  getRowId={(row) => row.customerId}
                />
              )}
            </CardBody>
          </Card>
        </div>
      )}
    </AppShell>
  );
}
