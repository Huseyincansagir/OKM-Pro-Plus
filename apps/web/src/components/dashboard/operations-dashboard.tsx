"use client";

import { useEffect, useMemo, useState } from "react";
import {
  CalendarDays,
  ClipboardList,
  Factory,
  LineChart,
  ShoppingCart,
  Wallet,
} from "lucide-react";
import { AppShell } from "@/components/shell/app-shell";
import { KpiMetric } from "@/components/dashboard/kpi-metric";
import { Alert } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader, CardTitle } from "@/components/ui/card";
import { DataTable } from "@/components/ui/data-table";
import { StatusBadge } from "@/components/ui/status-badge";
import { EmptyState } from "@/components/states/empty-state";
import { ErrorState } from "@/components/states/error-state";
import { PermissionDenied } from "@/components/states/permission-denied";
import { ApiError } from "@/lib/api/types";
import { userFacingMessage } from "@/lib/api/auth-client";
import { useSessionStore } from "@/lib/auth/session-store";
import {
  listQuoteRequests,
  quoteRequestStatusKind,
  quoteRequestStatusLabel,
  readSystemHealth,
  systemHealthLabel,
  type QuoteRequestSummary,
} from "@/lib/dashboard/quote-requests";

const QUOTE_COLUMNS = [
  { id: "createdAt", header: "Tarih/Saat" },
  { id: "kind", header: "Tür" },
  { id: "candidate", header: "Açıklama" },
  { id: "requestNumber", header: "İlgili kayıt" },
  { id: "status", header: "Durum" },
  { id: "items", header: "Kalem" },
] as const;

const UNAVAILABLE_RAIL = [
  {
    title: "Riskli müşteriler",
    reason: "Cari risk listesi yok (GET /current-accounts yalnızca müşteri id ile).",
  },
  {
    title: "Geciken ödemeler",
    reason: "Ödeme/vade özet listesi yok (GET /payments listesi yok).",
  },
  {
    title: "Faturalaşmamış irsaliyeler",
    reason: "İrsaliye listesi yok (GET /delivery-notes yalnızca id ile).",
  },
] as const;

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

function todayLabel(): string {
  return new Intl.DateTimeFormat("tr-TR", {
    weekday: "long",
    day: "numeric",
    month: "long",
    year: "numeric",
  }).format(new Date());
}

export function OperationsDashboard() {
  const user = useSessionStore((state) => state.user);
  const permissions = user?.permissions ?? [];
  const canReadQuotes = permissions.includes("quote-request.read");
  const canReadSystem = permissions.includes("system.read");

  const [quotes, setQuotes] = useState<QuoteRequestSummary[] | null>(null);
  const [quotesError, setQuotesError] = useState<string | null>(null);
  const [quotesDenied, setQuotesDenied] = useState(false);
  const [quotesLoading, setQuotesLoading] = useState(canReadQuotes);
  const [reload, setReload] = useState(0);
  const [health, setHealth] = useState<string | null>(null);
  const [healthError, setHealthError] = useState<string | null>(null);
  const [healthDenied, setHealthDenied] = useState(false);
  const [healthLoading, setHealthLoading] = useState(canReadSystem);

  useEffect(() => {
    if (!canReadQuotes) {
      setQuotesLoading(false);
      return;
    }
    let cancelled = false;
    setQuotesLoading(true);
    setQuotesError(null);
    setQuotesDenied(false);
    listQuoteRequests()
      .then((rows) => {
        if (!cancelled) setQuotes(rows);
      })
      .catch((error) => {
        if (cancelled) return;
        if (error instanceof ApiError && error.kind === "permission_denied") {
          setQuotesDenied(true);
          return;
        }
        setQuotesError(userFacingMessage(error));
      })
      .finally(() => {
        if (!cancelled) setQuotesLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [canReadQuotes, reload]);

  useEffect(() => {
    if (!canReadSystem) {
      setHealthLoading(false);
      return;
    }
    let cancelled = false;
    setHealthLoading(true);
    setHealthError(null);
    setHealthDenied(false);
    readSystemHealth()
      .then((result) => {
        if (!cancelled) setHealth(result.status);
      })
      .catch((error) => {
        if (cancelled) return;
        if (error instanceof ApiError && error.kind === "permission_denied") {
          setHealthDenied(true);
          return;
        }
        setHealth(null);
        setHealthError(userFacingMessage(error));
      })
      .finally(() => {
        if (!cancelled) setHealthLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [canReadSystem, reload]);

  const receivedCount = quotes?.filter((row) => row.status === "Received").length ?? 0;
  const today = useMemo(() => todayLabel(), []);
  const quotesReady = Boolean(quotes) && !quotesDenied && !quotesError && !quotesLoading;

  return (
    <AppShell
      currentHref="/dashboard"
      pageTitle="Genel Bakış"
      pageActions={
        <span className="inline-flex min-h-[37px] items-center gap-2 rounded-[10px] border border-surface-200 bg-white px-3 text-xs font-semibold text-navy-800">
          <CalendarDays className="h-3.5 w-3.5" aria-hidden="true" />
          <time dateTime={new Date().toISOString().slice(0, 10)}>{today}</time>
        </span>
      }
    >
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <KpiMetric
          label="Bugünkü satış"
          value="—"
          icon={ShoppingCart}
          tone="teal"
          unavailable
          caption="Satış özeti yok · GET /orders listesi yok"
        />
        <KpiMetric
          label="Bugünkü üretim"
          value="—"
          unit="adet"
          icon={Factory}
          tone="teal"
          unavailable
          caption="Üretim özeti yok · GET /production/orders listesi yok"
        />
        <KpiMetric
          label="Bekleyen sipariş"
          value="—"
          unit="sipariş"
          icon={ClipboardList}
          tone="amber"
          unavailable
          caption="GET /orders yok · teklif talebi sipariş değildir"
        />
        <KpiMetric
          label="Tahsilat"
          value="—"
          icon={Wallet}
          tone="teal"
          unavailable
          caption="Tahsilat özeti yok · GET /payments listesi yok"
        />
      </div>

      <div className="mt-4 grid gap-4 xl:grid-cols-3">
        <div className="space-y-4 xl:col-span-2">
          <div className="grid gap-4 lg:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle>Satış trendi</CardTitle>
                <Badge tone="neutral">Bağlı değil</Badge>
              </CardHeader>
              <CardBody>
                <div className="flex min-h-[140px] items-center gap-3 rounded-xl border border-dashed border-surface-200 bg-surface-50 px-4 py-6">
                  <LineChart className="h-5 w-5 text-slate-400" aria-hidden="true" />
                  <p className="text-sm text-slate-600">
                    Satış tutarı zaman serisi yok. Sahte grafik çizilmez.
                  </p>
                </div>
              </CardBody>
            </Card>
            <Card>
              <CardHeader>
                <CardTitle>Üretim performansı</CardTitle>
                <Badge tone="neutral">Bağlı değil</Badge>
              </CardHeader>
              <CardBody>
                <div className="flex min-h-[140px] items-center gap-3 rounded-xl border border-dashed border-surface-200 bg-surface-50 px-4 py-6">
                  <Factory className="h-5 w-5 text-slate-400" aria-hidden="true" />
                  <p className="text-sm text-slate-600">
                    Kapasite/verimlilik özeti yok. Sahte seri üretilmez.
                  </p>
                </div>
              </CardBody>
            </Card>
          </div>

          <Card>
            <CardHeader>
              <div>
                <CardTitle>Son teklif talepleri</CardTitle>
                <p className="mt-1 text-xs text-slate-500">GET /quote-requests · son 100 kayıt</p>
              </div>
              <div className="flex items-center gap-2">
                {quotesReady ? (
                  <Badge tone="amber">{receivedCount} alındı</Badge>
                ) : null}
                {canReadQuotes ? (
                  <Button
                    variant="secondary"
                    size="sm"
                    loading={quotesLoading}
                    onClick={() => setReload((value) => value + 1)}
                  >
                    Talepleri yenile
                  </Button>
                ) : null}
              </div>
            </CardHeader>
            <CardBody className="pt-3">
              {!canReadQuotes ? (
                <PermissionDenied
                  title="Teklif talepleri bu oturumda görünmez"
                  description="quote-request.read yok. Bu kontrol yalnızca görünürlük içindir; gerçek yetki backend’dedir."
                />
              ) : quotesDenied ? (
                <PermissionDenied />
              ) : quotesError ? (
                <ErrorState
                  title="Teklif talepleri yüklenemedi"
                  description={quotesError}
                  onRetry={() => setReload((value) => value + 1)}
                />
              ) : quotesLoading ? (
                <DataTable
                  columns={QUOTE_COLUMNS.map((column) => ({
                    id: column.id,
                    header: column.header,
                    accessor: () => null,
                  }))}
                  rows={[]}
                  getRowId={() => ""}
                  loading
                />
              ) : !quotes || quotes.length === 0 ? (
                <EmptyState
                  title="Teklif talebi yok"
                  description="Public katalogdan henüz talep gelmemiş veya liste boş."
                />
              ) : (
                <DataTable
                  columns={[
                    {
                      id: "createdAt",
                      header: "Tarih/Saat",
                      accessor: (row) => formatDateTime(row.createdAt),
                    },
                    {
                      id: "kind",
                      header: "Tür",
                      accessor: () => "Teklif talebi",
                    },
                    {
                      id: "candidate",
                      header: "Açıklama",
                      accessor: (row) => row.candidateName,
                    },
                    {
                      id: "requestNumber",
                      header: "İlgili kayıt",
                      accessor: (row) => (
                        <span className="font-semibold text-teal-600">{row.requestNumber}</span>
                      ),
                    },
                    {
                      id: "status",
                      header: "Durum",
                      accessor: (row) => (
                        <StatusBadge
                          status={quoteRequestStatusKind(row.status)}
                          label={quoteRequestStatusLabel(row.status)}
                        />
                      ),
                    },
                    {
                      id: "items",
                      header: "Kalem",
                      accessor: (row) => String(row.itemCount),
                    },
                  ]}
                  rows={quotes}
                  getRowId={(row) => row.id}
                />
              )}
            </CardBody>
          </Card>
        </div>

        <div className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>API durumu</CardTitle>
            </CardHeader>
            <CardBody>
              {!canReadSystem ? (
                <p className="text-sm text-slate-600">
                  system.read yok. Health çağrısı yapılmaz; bu kontrol yalnızca görünürlük içindir.
                </p>
              ) : healthDenied ? (
                <PermissionDenied title="Sistem durumu görülemez" />
              ) : healthError ? (
                <ErrorState
                  title="Sistem durumu alınamadı"
                  description={healthError}
                  onRetry={() => setReload((value) => value + 1)}
                />
              ) : (
                <>
                  <p className="text-[25px] font-extrabold tracking-tight text-navy-950">
                    {healthLoading ? "sorgulanıyor" : systemHealthLabel(health)}
                  </p>
                  <p className="mt-1 text-xs text-slate-500">GET /system/health</p>
                  <p className="mt-2 text-xs text-slate-500">
                    Oturum yetkisi: {permissions.length} (UX; yetki backend’dedir)
                  </p>
                </>
              )}
            </CardBody>
          </Card>

          {UNAVAILABLE_RAIL.map((widget) => (
            <Card key={widget.title}>
              <CardHeader>
                <CardTitle>{widget.title}</CardTitle>
                <Badge tone="neutral">Bağlı değil</Badge>
              </CardHeader>
              <CardBody className="pt-3">
                <p className="text-sm text-slate-600">{widget.reason}</p>
              </CardBody>
            </Card>
          ))}

          <Alert tone="info" title="Sahte KPI yok">
            Mockup’taki satış, üretim, sipariş ve tahsilat kartları görsel referanstır; özet
            endpoint’i gelene kadar sayı uydurulmaz.
          </Alert>
        </div>
      </div>
    </AppShell>
  );
}
