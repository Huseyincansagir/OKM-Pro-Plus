"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Building2, FileText, Package, Stamp, Wallet } from "lucide-react";
import { AppShell } from "@/components/shell/app-shell";
import { KpiMetric } from "@/components/dashboard/kpi-metric";
import { EmptyState } from "@/components/states/empty-state";
import { ErrorState } from "@/components/states/error-state";
import { PermissionDenied } from "@/components/states/permission-denied";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader, CardTitle } from "@/components/ui/card";
import { DataTable } from "@/components/ui/data-table";
import { Dialog } from "@/components/ui/dialog";
import { Glyph } from "@/components/ui/glyph";
import { StatusBadge } from "@/components/ui/status-badge";
import { ApiError } from "@/lib/api/types";
import { userFacingMessage } from "@/lib/api/auth-client";
import { useSessionStore } from "@/lib/auth/session-store";
import {
  canIssueQuote,
  getQuote,
  issueQuote,
  quoteStatusKind,
  quoteStatusLabel,
  type QuoteDetail as QuoteDetailModel,
} from "@/lib/sales/quotes";

function formatDateTime(iso: string | null): string {
  if (!iso) {
    return "—";
  }
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return iso;
  }
  return new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "short",
    timeStyle: "short",
  }).format(date);
}

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

export function QuoteDetail({ id }: { id: string }) {
  const router = useRouter();
  const user = useSessionStore((state) => state.user);
  const permissions = user?.permissions ?? [];
  const canRead = permissions.includes("quote.read");
  const canIssue = permissions.includes("quote.issue");
  const [detail, setDetail] = useState<QuoteDetailModel | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [denied, setDenied] = useState(false);
  const [loading, setLoading] = useState(canRead);
  const [reload, setReload] = useState(0);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [issuing, setIssuing] = useState(false);
  const [issueError, setIssueError] = useState<string | null>(null);

  useEffect(() => {
    if (!canRead) {
      setLoading(false);
      return;
    }
    let cancelled = false;
    setLoading(true);
    setError(null);
    setDenied(false);
    getQuote(id)
      .then((result) => {
        if (!cancelled) setDetail(result);
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

  async function confirmIssue() {
    setIssuing(true);
    setIssueError(null);
    try {
      const next = await issueQuote(id);
      setDetail(next);
      setConfirmOpen(false);
    } catch (caught) {
      setIssueError(userFacingMessage(caught));
    } finally {
      setIssuing(false);
    }
  }

  const issuable = Boolean(detail && canIssue && canIssueQuote(detail.status));

  return (
    <AppShell
      currentHref="/satis/teklifler"
      breadcrumbs={[
        { label: "Çalışma alanı", href: "/dashboard" },
        { label: "Satış", href: "/satis/teklif-talepleri" },
        { label: "Teklifler", href: "/satis/teklifler" },
        { label: detail?.quoteNumber || "Belge" },
      ]}
      pageTitle={detail?.quoteNumber || "Teklif"}
      pageDescription="Satır tutarı ve temel karşılık sunucudan gelir. İstemci çarpma yapmaz."
      pageActions={
        <div className="flex flex-wrap gap-2">
          <Button variant="secondary" onClick={() => router.push("/satis/teklifler")}>
            Listeye dön
          </Button>
          <Button variant="secondary" onClick={() => setReload((value) => value + 1)} loading={loading}>
            Yenile
          </Button>
          {issuable ? (
            <Button onClick={() => setConfirmOpen(true)}>Kesinleştir</Button>
          ) : null}
        </div>
      }
    >
      {!canRead ? (
        <PermissionDenied
          title="Teklif bu oturumda görünmez"
          description="quote.read yok. Bu kontrol yalnızca görünürlük içindir; gerçek yetki backend’dedir."
        />
      ) : denied ? (
        <PermissionDenied />
      ) : error ? (
        <ErrorState
          title="Teklif yüklenemedi"
          description={error}
          onRetry={() => setReload((value) => value + 1)}
        />
      ) : loading || !detail ? (
        <DataTable
          columns={[
            { id: "packaging", header: "Ambalaj", accessor: () => null },
            { id: "qty", header: "Girilen miktar", accessor: () => null },
            { id: "base", header: "Temel karşılık", accessor: () => null },
          ]}
          rows={[]}
          getRowId={() => ""}
          loading
        />
      ) : (
        <div className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <KpiMetric
              label="Durum"
              value={quoteStatusLabel(detail.status)}
              icon={Stamp}
              tone="amber"
              caption={detail.status}
            />
            <KpiMetric
              label="Tutar"
              value={formatMoney(detail.totalGross, detail.currencyCode || "TRY")}
              icon={Wallet}
              tone="teal"
              caption="Sunucu totalGross"
            />
            <KpiMetric
              label="Kalem"
              value={String(detail.itemCount)}
              unit="satır"
              icon={Package}
              tone="navy"
              caption="Satır sayısı · miktar değil"
            />
            <KpiMetric
              label={detail.status === "Issued" ? "Kesinleşme" : "Geçerlilik"}
              value={
                detail.status === "Issued"
                  ? formatDateTime(detail.issuedAt)
                  : formatDateTime(detail.validUntil)
              }
              icon={FileText}
              tone="teal"
              caption={detail.status === "Issued" ? "Sunucu issuedAt" : "Sunucu validUntil"}
            />
          </div>

          <div className="grid gap-4 lg:grid-cols-2">
            <Card>
              <CardHeader>
                <div className="flex items-center gap-2">
                  <Glyph icon={Building2} />
                  <CardTitle>Müşteri</CardTitle>
                </div>
              </CardHeader>
              <CardBody className="space-y-3 text-sm text-slate-600">
                <p className="text-navy-950">
                  {detail.customerCode
                    ? `${detail.customerCode} · ${detail.customerLegalName}`
                    : detail.customerLegalName || "—"}
                </p>
                <p>
                  Talep:{" "}
                  <Link
                    href={`/satis/teklif-talepleri/${detail.quoteRequestId}`}
                    className="font-semibold text-teal-600"
                  >
                    belgeye bağlı talep
                  </Link>
                </p>
              </CardBody>
            </Card>
            <Card>
              <CardHeader>
                <CardTitle>Sonraki adım</CardTitle>
                <StatusBadge
                  status={quoteStatusKind(detail.status)}
                  label={quoteStatusLabel(detail.status)}
                />
              </CardHeader>
              <CardBody className="space-y-3">
                {detail.status === "Draft" ? (
                  <Alert tone="info" title="Taslak">
                    Kesinleştirme stok rezervasyonu veya sipariş oluşturmaz. PDF yok.
                  </Alert>
                ) : (
                  <Alert tone="success" title="Kesinleşti">
                    Belge yayınlandı. Sipariş bu dilimde oluşturulmaz.
                  </Alert>
                )}
                {!canIssue ? (
                  <p className="text-sm text-slate-600">
                    quote.issue yok. Buton gizlidir; yetki backend’dedir.
                  </p>
                ) : null}
              </CardBody>
            </Card>
          </div>

          {detail.items.length === 0 ? (
            <EmptyState title="Kalem yok" description="Bu teklifte ürün satırı bulunmuyor." />
          ) : (
            <Card>
              <CardHeader>
                <div className="flex items-center gap-2">
                  <Glyph icon={Package} />
                  <CardTitle>Kalemler</CardTitle>
                </div>
              </CardHeader>
              <CardBody className="pt-3">
                <DataTable
                  columns={[
                    {
                      id: "packaging",
                      header: "Ambalaj",
                      accessor: (row) => (
                        <span className="inline-flex items-center gap-2">
                          <Glyph icon={Package} />
                          {row.packagingName}
                        </span>
                      ),
                    },
                    {
                      id: "qty",
                      header: "Girilen miktar",
                      accessor: (row) => String(row.enteredQuantity),
                    },
                    {
                      id: "base",
                      header: "Temel karşılık",
                      accessor: (row) =>
                        row.quantityBase === null ? "—" : String(row.quantityBase),
                    },
                    {
                      id: "price",
                      header: "Birim fiyat",
                      accessor: (row) => formatMoney(row.unitPrice, detail.currencyCode || "TRY"),
                    },
                    {
                      id: "net",
                      header: "Satır tutarı",
                      accessor: (row) => formatMoney(row.lineNet, detail.currencyCode || "TRY"),
                    },
                  ]}
                  rows={detail.items}
                  getRowId={(row) => row.id}
                />
                <p className="mt-3 text-xs text-slate-500">
                  Temel karşılık ve satır tutarı sunucu alanlarıdır. İstemci quantityBase veya lineNet üretmez.
                </p>
              </CardBody>
            </Card>
          )}
        </div>
      )}

      <Dialog
        open={confirmOpen}
        onOpenChange={setConfirmOpen}
        title="Teklifi kesinleştir"
        description="Durum Issued olur. Stok rezervasyonu ve sipariş oluşmaz."
        footer={
          <>
            <Button variant="secondary" onClick={() => setConfirmOpen(false)}>
              Vazgeç
            </Button>
            <Button loading={issuing} onClick={() => void confirmIssue()}>
              Kesinleştir
            </Button>
          </>
        }
      >
        {issueError ? (
          <Alert tone="danger" title="Kesinleştirilemedi">
            {issueError}
          </Alert>
        ) : (
          <p className="text-sm text-slate-600">
            Bu işlem geri alınamaz. Belge numarası değişmez.
          </p>
        )}
      </Dialog>
    </AppShell>
  );
}
