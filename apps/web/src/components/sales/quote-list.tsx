"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { FileText, Inbox, Stamp, Wallet } from "lucide-react";
import { AppShell } from "@/components/shell/app-shell";
import { KpiMetric } from "@/components/dashboard/kpi-metric";
import { EmptyState } from "@/components/states/empty-state";
import { ErrorState } from "@/components/states/error-state";
import { PermissionDenied } from "@/components/states/permission-denied";
import { Button } from "@/components/ui/button";
import { Card, CardBody } from "@/components/ui/card";
import { DataTable } from "@/components/ui/data-table";
import { Glyph } from "@/components/ui/glyph";
import { StatusBadge } from "@/components/ui/status-badge";
import { ApiError } from "@/lib/api/types";
import { userFacingMessage } from "@/lib/api/auth-client";
import { useSessionStore } from "@/lib/auth/session-store";
import {
  listQuotes,
  quoteStatusKind,
  quoteStatusLabel,
  type QuoteSummary,
} from "@/lib/sales/quotes";

function formatDateTime(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return iso || "—";
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

export function QuoteList() {
  const router = useRouter();
  const user = useSessionStore((state) => state.user);
  const permissions = user?.permissions ?? [];
  const canRead = permissions.includes("quote.read");
  const [rows, setRows] = useState<QuoteSummary[] | null>(null);
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
    listQuotes()
      .then((result) => {
        if (!cancelled) setRows(result);
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
  }, [canRead, reload]);

  const ready = Boolean(rows) && !loading && !error && !denied;
  const draft = rows?.filter((row) => row.status === "Draft").length ?? 0;
  const issued = rows?.filter((row) => row.status === "Issued").length ?? 0;
  const total = rows?.length ?? 0;
  const issuedGross = (rows ?? [])
    .filter((row) => row.status === "Issued")
    .reduce<number | null>((sum, row) => {
      if (row.totalGross === null) {
        return sum;
      }
      return (sum ?? 0) + row.totalGross;
    }, null);

  return (
    <AppShell
      currentHref="/satis/teklifler"
      breadcrumbs={[
        { label: "Çalışma alanı", href: "/dashboard" },
        { label: "Satış", href: "/satis/teklif-talepleri" },
        { label: "Teklifler" },
      ]}
      pageTitle="Teklifler"
      pageDescription="Talepten oluşan belgeler. Fiyat personel girer; tutarlar sunucudan gelir. Liste en fazla 100 kayıttır."
      pageActions={
        <div className="flex flex-wrap gap-2">
          <Button variant="secondary" onClick={() => router.push("/satis/teklif-talepleri")}>
            Teklif talepleri
          </Button>
          <Button variant="secondary" onClick={() => router.push("/satis/musteriler")}>
            Müşteriler
          </Button>
          {canRead ? (
            <Button variant="secondary" loading={loading} onClick={() => setReload((value) => value + 1)}>
              Yenile
            </Button>
          ) : null}
        </div>
      }
    >
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <KpiMetric
          label="Taslak"
          value={ready ? String(draft) : "—"}
          unit="belge"
          icon={Inbox}
          tone="amber"
          unavailable={!ready}
          caption="GET /quotes · Draft"
        />
        <KpiMetric
          label="Kesinleşti"
          value={ready ? String(issued) : "—"}
          unit="belge"
          icon={Stamp}
          tone="teal"
          unavailable={!ready}
          caption="GET /quotes · Issued"
        />
        <KpiMetric
          label="Kesinleşen tutar"
          value={ready ? formatMoney(issuedGross, "TRY") : "—"}
          icon={Wallet}
          tone="teal"
          unavailable={!ready}
          caption="Liste penceresi · Issued TotalGross"
        />
        <KpiMetric
          label="Toplam"
          value={ready ? String(total) : "—"}
          unit="kayıt"
          icon={FileText}
          tone="navy"
          unavailable={!ready}
          caption="Liste penceresi · en fazla 100"
        />
      </div>

      <Card className="mt-4">
        <CardBody>
          {!canRead ? (
            <PermissionDenied
              title="Teklifler bu oturumda görünmez"
              description="quote.read yok. Bu kontrol yalnızca görünürlük içindir; gerçek yetki backend’dedir."
            />
          ) : denied ? (
            <PermissionDenied />
          ) : error ? (
            <ErrorState
              title="Teklifler yüklenemedi"
              description={error}
              onRetry={() => setReload((value) => value + 1)}
            />
          ) : loading ? (
            <DataTable
              columns={[
                { id: "number", header: "Belge", accessor: () => null },
                { id: "customer", header: "Müşteri", accessor: () => null },
                { id: "status", header: "Durum", accessor: () => null },
              ]}
              rows={[]}
              getRowId={() => ""}
              loading
            />
          ) : !rows || rows.length === 0 ? (
            <EmptyState
              title="Teklif yok"
              description="Henüz teklif belgesi yok. İncelemedeki talepten oluşturulur; ürün listesinden serbest teklif yok."
            />
          ) : (
            <DataTable
              columns={[
                {
                  id: "number",
                  header: "Belge",
                  accessor: (row) => (
                    <Link
                      href={`/satis/teklifler/${row.id}`}
                      className="inline-flex items-center gap-2 font-semibold text-teal-600"
                    >
                      <Glyph icon={FileText} />
                      {row.quoteNumber}
                    </Link>
                  ),
                },
                {
                  id: "customer",
                  header: "Müşteri",
                  accessor: (row) =>
                    row.customerCode
                      ? `${row.customerCode} · ${row.customerLegalName}`
                      : row.customerLegalName || "—",
                },
                {
                  id: "status",
                  header: "Durum",
                  accessor: (row) => (
                    <StatusBadge status={quoteStatusKind(row.status)} label={quoteStatusLabel(row.status)} />
                  ),
                },
                {
                  id: "total",
                  header: "Tutar",
                  accessor: (row) => formatMoney(row.totalGross, row.currencyCode || "TRY"),
                },
                {
                  id: "items",
                  header: "Kalem",
                  accessor: (row) => String(row.itemCount),
                },
                {
                  id: "createdAt",
                  header: "Kayıt",
                  accessor: (row) => formatDateTime(row.createdAt),
                },
              ]}
              rows={rows}
              getRowId={(row) => row.id}
            />
          )}
        </CardBody>
      </Card>
    </AppShell>
  );
}
