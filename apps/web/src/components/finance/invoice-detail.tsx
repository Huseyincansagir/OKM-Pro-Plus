"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Building2, Calendar, Layers, Receipt, Wallet } from "lucide-react";
import { AppShell } from "@/components/shell/app-shell";
import { KpiMetric } from "@/components/dashboard/kpi-metric";
import { ErrorState } from "@/components/states/error-state";
import { PermissionDenied } from "@/components/states/permission-denied";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader, CardTitle } from "@/components/ui/card";
import { DataTable } from "@/components/ui/data-table";
import { Dialog } from "@/components/ui/dialog";
import { StatusBadge } from "@/components/ui/status-badge";
import { ApiError } from "@/lib/api/types";
import { userFacingMessage } from "@/lib/api/auth-client";
import { useSessionStore } from "@/lib/auth/session-store";
import { getInvoice, invoiceStatusKind, issueInvoice, type InvoiceDetail } from "@/lib/finance/invoices";

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

export function InvoiceDetailBoard({ id }: { id: string }) {
  const router = useRouter();
  const user = useSessionStore((state) => state.user);
  const permissions = user?.permissions ?? [];
  const canRead = permissions.includes("invoice.read");
  const canIssue = permissions.includes("invoice.issue");
  const canReadAccounts = permissions.includes("current-account.read");
  const [invoice, setInvoice] = useState<InvoiceDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [denied, setDenied] = useState(false);
  const [loading, setLoading] = useState(canRead);
  const [reload, setReload] = useState(0);
  const [acting, setActing] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const [issueConfirmOpen, setIssueConfirmOpen] = useState(false);

  useEffect(() => {
    if (!canRead) {
      setLoading(false);
      return;
    }
    let cancelled = false;
    setLoading(true);
    setError(null);
    setDenied(false);
    getInvoice(id)
      .then((result) => {
        if (!cancelled) setInvoice(result);
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
  }, [canRead, id, reload]);

  async function handleIssue() {
    if (!invoice) return;
    setActing(true);
    setActionError(null);
    try {
      const updated = await issueInvoice(invoice.id);
      setInvoice(updated);
      setIssueConfirmOpen(false);
      setReload((v) => v + 1);
    } catch (caught) {
      setActionError(userFacingMessage(caught));
    } finally {
      setActing(false);
    }
  }

  const isDraft = invoice?.status === "Draft" || invoice?.status === "ReadyToIssue";
  const isIssued = invoice?.status === "Issued" || invoice?.status === "Paid";

  return (
    <AppShell
      currentHref="/cari"
      breadcrumbs={[
        { label: "Çalışma alanı", href: "/dashboard" },
        { label: "Cari ve muhasebe", href: "/cari" },
        { label: invoice?.invoiceNumber || "Fatura" },
      ]}
      pageTitle={invoice?.invoiceNumber || "Fatura"}
      pageDescription="GET /invoices/{id}. Fatura kesinleştirildiğinde cari hesaba otomatik borç kaydı (CurrentTransaction) açılır."
      pageActions={
        <div className="flex flex-wrap gap-2">
          <Button variant="secondary" onClick={() => router.push("/cari")}>
            Cari panosu
          </Button>
          {canRead ? (
            <Button variant="secondary" loading={loading} onClick={() => setReload((value) => value + 1)}>
              Yenile
            </Button>
          ) : null}
          {invoice && canIssue && isDraft ? (
            <Button onClick={() => setIssueConfirmOpen(true)}>Faturayı kesinleştir (Issue)</Button>
          ) : null}
        </div>
      }
    >
      {!canRead ? (
        <PermissionDenied
          title="Fatura bu oturumda görünmez"
          description="invoice.read yok. Bu kontrol yalnızca görünürlük içindir; gerçek yetki backend’dedir."
        />
      ) : denied ? (
        <PermissionDenied />
      ) : error ? (
        <ErrorState title="Fatura yüklenemedi" description={error} onRetry={() => setReload((value) => value + 1)} />
      ) : loading || !invoice ? (
        <p className="text-sm text-slate-600">Yükleniyor…</p>
      ) : (
        <div className="space-y-4">
          {isIssued ? (
            <Alert tone="success" title="Fatura Kesinleştirildi (Issued)">
              Fatura başarıyla kesinleştirildi. İlgili tutar müşteri cari hesabına borç olarak işlendi.
              {canReadAccounts ? (
                <span className="ml-1">
                  Cari ekstresini görmek için{" "}
                  <Link className="font-semibold underline text-teal-800" href="/cari">
                    Cari Yönetimi
                  </Link>{" "}
                  sayfasına gidebilirsiniz.
                </span>
              ) : null}
            </Alert>
          ) : (
            <Alert tone="info" title="Taslak Fatura (Draft)">
              Bu fatura taslak durumundadır. Kesinleştirildiğinde (Issue) stok hareketine dokunulmadan müşterinin cari
              hesabına borç kaydı atılacaktır.
            </Alert>
          )}

          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <KpiMetric
              label="Durum"
              value={invoice.status}
              icon={Receipt}
              tone={isIssued ? "teal" : "amber"}
              caption="Sunucu status"
            />
            <KpiMetric
              label="Genel Toplam"
              value={formatMoney(invoice.grandTotal, invoice.currencyCode)}
              icon={Wallet}
              tone="navy"
              caption="KDV dahil genel toplam"
            />
            <KpiMetric
              label="Kalem"
              value={String(invoice.items.length)}
              unit="satır"
              icon={Layers}
              tone="navy"
              caption="Fatura satır sayısı"
            />
            <KpiMetric
              label="Müşteri"
              value={invoice.customerId.slice(0, 8) || "—"}
              icon={Building2}
              tone="teal"
              caption="customerId"
            />
          </div>

          <div className="grid gap-4 lg:grid-cols-3">
            <div className="lg:col-span-2">
              <Card>
                <CardHeader>
                  <CardTitle>Fatura Kalemleri</CardTitle>
                </CardHeader>
                <CardBody>
                  <div className="mb-3 flex items-center justify-between">
                    <StatusBadge status={invoiceStatusKind(invoice.status)} label={invoice.status} />
                    <span className="text-xs text-slate-500">
                      Para Birimi: <strong>{invoice.currencyCode}</strong>
                    </span>
                  </div>
                  <DataTable
                    columns={[
                      { id: "product", header: "Ürün", accessor: (item) => item.productId.slice(0, 8) },
                      {
                        id: "qty",
                        header: "Temel miktar",
                        accessor: (item) => String(item.quantityBase),
                      },
                      {
                        id: "price",
                        header: "Birim fiyat",
                        accessor: (item) => formatMoney(item.unitPrice, invoice.currencyCode),
                      },
                      {
                        id: "total",
                        header: "Satır toplamı",
                        accessor: (item) => formatMoney(item.lineTotal, invoice.currencyCode),
                      },
                    ]}
                    rows={invoice.items}
                    getRowId={(item) => item.id}
                  />
                </CardBody>
              </Card>
            </div>

            <div>
              <Card>
                <CardHeader>
                  <CardTitle>Tutar Özeti</CardTitle>
                </CardHeader>
                <CardBody className="space-y-3 text-sm">
                  <div className="flex justify-between border-b border-slate-100 pb-2">
                    <span className="text-slate-600">Ara Toplam:</span>
                    <span className="font-semibold text-slate-800">
                      {formatMoney(invoice.subtotal, invoice.currencyCode)}
                    </span>
                  </div>
                  <div className="flex justify-between border-b border-slate-100 pb-2">
                    <span className="text-slate-600">KDV Toplamı:</span>
                    <span className="font-semibold text-slate-800">
                      {formatMoney(invoice.taxTotal, invoice.currencyCode)}
                    </span>
                  </div>
                  <div className="flex justify-between pt-1 text-base">
                    <span className="font-bold text-slate-900">Genel Toplam:</span>
                    <span className="font-bold text-teal-700">
                      {formatMoney(invoice.grandTotal, invoice.currencyCode)}
                    </span>
                  </div>
                  {invoice.issuedAt ? (
                    <div className="mt-4 rounded-lg bg-slate-50 p-2.5 text-xs text-slate-600 flex items-center gap-1.5">
                      <Calendar className="h-4 w-4 text-slate-400 shrink-0" />
                      <span>
                        Düzenleme tarihi: {new Date(invoice.issuedAt).toLocaleDateString("tr-TR")}
                      </span>
                    </div>
                  ) : null}
                </CardBody>
              </Card>
            </div>
          </div>
        </div>
      )}

      <Dialog
        open={issueConfirmOpen}
        onOpenChange={(open) => {
          if (!open && !acting) setIssueConfirmOpen(false);
        }}
        title="Faturayı kesinleştir (Issue)"
        description="Fatura kesinleştiğinde alokasyonlar sabitlenir ve müşterinin cari hesabına borç kaydı işlenir. Bu işlem geri alınamaz."
        footer={
          <div className="flex justify-end gap-2">
            <Button variant="secondary" disabled={acting} onClick={() => setIssueConfirmOpen(false)}>
              Vazgeç
            </Button>
            <Button loading={acting} onClick={() => void handleIssue()}>
              Kesinleştir (Issue)
            </Button>
          </div>
        }
      >
        {actionError ? <Alert tone="danger" title="Kesinleştirme başarısız">{actionError}</Alert> : null}
        <div className="space-y-2 text-sm">
          <p>
            Fatura No: <strong>{invoice?.invoiceNumber}</strong>
          </p>
          <p>
            Genel Toplam: <strong>{invoice ? formatMoney(invoice.grandTotal, invoice.currencyCode) : "—"}</strong>
          </p>
        </div>
      </Dialog>
    </AppShell>
  );
}
