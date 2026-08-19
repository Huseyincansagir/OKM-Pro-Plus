"use client";

import { useEffect, useMemo, useState, type ReactNode } from "react";
import Link from "next/link";
import {
  Building2,
  CalendarDays,
  ChevronDown,
  ClipboardList,
  Clock3,
  Factory,
  FileText,
  LineChart,
  List,
  ShoppingCart,
  UserRound,
  Wallet,
} from "lucide-react";
import { AppShell } from "@/components/shell/app-shell";
import { KpiMetric } from "@/components/dashboard/kpi-metric";
import { RailListCard } from "@/components/dashboard/rail-list-card";
import { UnavailableChart } from "@/components/dashboard/unavailable-chart";
import { Badge } from "@/components/ui/badge";
import { Glyph } from "@/components/ui/glyph";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader, CardTitle } from "@/components/ui/card";
import { DataTable } from "@/components/ui/data-table";
import { EmptyState } from "@/components/states/empty-state";
import { ErrorState } from "@/components/states/error-state";
import { PermissionDenied } from "@/components/states/permission-denied";
import { ApiError } from "@/lib/api/types";
import { userFacingMessage } from "@/lib/api/auth-client";
import { useSessionStore } from "@/lib/auth/session-store";
import {
  listQuoteRequests,
  quoteRequestStatusLabel,
  readSystemHealth,
  systemHealthLabel,
  type QuoteRequestSummary,
} from "@/lib/dashboard/quote-requests";

const ACTIVITY_COLUMNS = [
  { id: "createdAt", header: "Tarih/Saat" },
  { id: "kind", header: "Tür" },
  { id: "detail", header: "Açıklama" },
  { id: "record", header: "İlgili Kayıt" },
  { id: "user", header: "Kullanıcı" },
] as const;

const ACTIVITY_PAGE = 5;

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

function HeaderChip({
  icon: Icon,
  children,
  title,
}: {
  icon: typeof CalendarDays;
  children: ReactNode;
  title: string;
}) {
  return (
    <span
      title={title}
      className="inline-flex min-h-[34px] items-center gap-2 rounded-[9px] border border-surface-200 bg-white px-2.5 text-xs text-slate-600"
    >
      <Icon className="h-3.5 w-3.5" aria-hidden="true" />
      {children}
      <ChevronDown className="h-3 w-3 text-slate-400" aria-hidden="true" />
    </span>
  );
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
  const [showAllQuotes, setShowAllQuotes] = useState(false);

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
  const visibleQuotes = quotesReady && quotes
    ? showAllQuotes
      ? quotes
      : quotes.slice(0, ACTIVITY_PAGE)
    : [];

  const apiChip = !canReadSystem
    ? "API bağlı değil"
    : healthDenied
      ? "API yetkisiz"
      : healthError
        ? "API alınamadı"
        : healthLoading
          ? "API · sorgulanıyor"
          : `API · ${systemHealthLabel(health)}`;

  return (
    <AppShell
      currentHref="/dashboard"
      pageTitle="Genel Bakış"
      pageActions={
        <>
          <HeaderChip icon={CalendarDays} title="Tarih seçici bağlı değil">
            <time dateTime={new Date().toISOString().slice(0, 10)}>{today}</time>
          </HeaderChip>
          <HeaderChip icon={Building2} title="Fabrika seçici bağlı değil">
            Fabrika bağlı değil
          </HeaderChip>
          <span
            title={!canReadSystem ? "system.read yok; çağrı yapılmaz" : "GET /system/health"}
            className="inline-flex min-h-[34px] items-center rounded-[9px] border border-surface-200 bg-white px-2.5 text-xs text-slate-600"
          >
            {apiChip}
          </span>
        </>
      }
    >
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <KpiMetric
          label="Bugünkü Satış"
          value="—"
          icon={ShoppingCart}
          tone="teal"
          unavailable
          caption="Bu ay: — · satış özeti yok"
        />
        <KpiMetric
          label="Bugünkü Üretim"
          value="—"
          unit="adet"
          icon={Factory}
          tone="teal"
          unavailable
          caption="Bu ay: — · iş emri listesi yok"
          showEmptyTrack
        />
        <KpiMetric
          label="Bekleyen Sipariş"
          value="—"
          unit="sipariş"
          icon={ClipboardList}
          tone="amber"
          unavailable
          secondary="— tutarında"
          caption="En eski: — · GET /orders yok"
        />
        <KpiMetric
          label="Tahsilat"
          value="—"
          icon={Wallet}
          tone="teal"
          unavailable
          caption="Bu ay: — · tahsilat özeti yok"
        />
      </div>

      {healthError ? (
        <div className="mt-4">
          <ErrorState
            title="Sistem durumu alınamadı"
            description={healthError}
            onRetry={() => setReload((value) => value + 1)}
          />
        </div>
      ) : null}

      <div className="mt-4 grid gap-4 xl:grid-cols-3">
        <div className="space-y-4 xl:col-span-2">
          <div className="grid gap-4 lg:grid-cols-2">
            <UnavailableChart
              title="Satış Trendi"
              icon={LineChart}
              legend={["Tutar (₺)", "7 Günlük Ortalama"]}
              stats={[
                { label: "Toplam Satış" },
                { label: "Günlük Ortalama" },
                { label: "En Yüksek Gün" },
                { label: "Karşılaştırma" },
              ]}
              reason="Satış zaman serisi yok. Sahte grafik çizilmez."
            />
            <UnavailableChart
              title="Üretim Performansı"
              icon={Factory}
              legend={["Üretilen (adet)", "Kapasite (adet)", "% Verimlilik"]}
              stats={[
                { label: "Toplam Üretim", unit: "adet" },
                { label: "Ortalama Günlük", unit: "adet" },
                { label: "Ortalama Verimlilik" },
                { label: "Kapasite Kullanımı" },
              ]}
              reason="Kapasite/verimlilik özeti yok. Sahte seri üretilmez."
            />
          </div>

          <Card>
            <CardHeader>
              <div className="flex min-w-0 items-center gap-2">
                <List className="h-4 w-4 text-slate-500" aria-hidden="true" />
                <div>
                  <CardTitle>Son Aktiviteler</CardTitle>
                  <p className="mt-0.5 text-[11px] text-slate-500">
                    Teklif talepleri · GET /quote-requests · son 100 kayıt
                  </p>
                </div>
              </div>
              <div className="flex items-center gap-2">
                {quotesReady ? <Badge tone="amber">{receivedCount} alındı</Badge> : null}
                {canReadQuotes ? (
                  <Button
                    variant="secondary"
                    size="sm"
                    loading={quotesLoading}
                    onClick={() => setReload((value) => value + 1)}
                  >
                    Yenile
                  </Button>
                ) : null}
                <Link href="/satis/teklif-talepleri" className="text-xs font-semibold text-teal-600">
                  Tüm Aktiviteler
                </Link>
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
                  columns={ACTIVITY_COLUMNS.map((column) => ({
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
                <>
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
                        accessor: () => (
                          <span className="inline-flex items-center gap-2">
                            <Glyph icon={ClipboardList} />
                            Teklif talebi
                          </span>
                        ),
                      },
                      {
                        id: "detail",
                        header: "Açıklama",
                        accessor: (row) =>
                          `${row.candidateName} · ${quoteRequestStatusLabel(row.status)}`,
                      },
                      {
                        id: "record",
                        header: "İlgili Kayıt",
                        accessor: (row) => (
                          <Link
                            href={`/satis/teklif-talepleri/${row.id}`}
                            className="font-semibold text-teal-600"
                          >
                            {row.requestNumber}
                          </Link>
                        ),
                      },
                      {
                        id: "user",
                        header: "Kullanıcı",
                        accessor: () => (
                          <span className="text-slate-500" title="Kayıtta aktör yok">
                            —
                          </span>
                        ),
                      },
                    ]}
                    rows={visibleQuotes}
                    getRowId={(row) => row.id}
                  />
                  {quotes.length > ACTIVITY_PAGE ? (
                    <button
                      type="button"
                      className="mt-3 w-full text-center text-xs font-semibold text-teal-600"
                      onClick={() => setShowAllQuotes((value) => !value)}
                    >
                      {showAllQuotes ? "Daha az göster" : "Daha Fazla Göster"}
                    </button>
                  ) : (
                    <p className="mt-3 text-center text-xs text-slate-400">Daha Fazla Göster</p>
                  )}
                </>
              )}
            </CardBody>
          </Card>
        </div>

        <div className="space-y-4">
          <RailListCard
            title="Riskli Müşteriler"
            icon={UserRound}
            columns={["Müşteri", "Risk Skoru", "Son İşlem"]}
            reason="Cari risk listesi yok."
          />
          <RailListCard
            title="Geciken Ödemeler"
            icon={Clock3}
            columns={["Müşteri", "Vadesi Geçen", "Tutar (₺)"]}
            reason="Ödeme/vade özet listesi yok."
          />
          <RailListCard
            title="Faturalaşmamış İrsaliyeler"
            icon={FileText}
            columns={["İrsaliye No", "Tarih", "Tutar (₺)"]}
            reason="İrsaliye listesi yok."
          />
        </div>
      </div>
    </AppShell>
  );
}
