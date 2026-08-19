"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { ClipboardList, Globe2, Inbox, Search } from "lucide-react";
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
  listQuoteRequests,
  quoteRequestSourceLabel,
  quoteRequestStatusKind,
  quoteRequestStatusLabel,
  type QuoteRequestSummary,
} from "@/lib/dashboard/quote-requests";

const COLUMNS = [
  { id: "requestNumber", header: "Talep" },
  { id: "candidate", header: "Aday" },
  { id: "source", header: "Kaynak" },
  { id: "status", header: "Durum" },
  { id: "items", header: "Kalem" },
  { id: "createdAt", header: "Tarih" },
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

export function QuoteRequestList() {
  const user = useSessionStore((state) => state.user);
  const permissions = user?.permissions ?? [];
  const canRead = permissions.includes("quote-request.read");
  const [rows, setRows] = useState<QuoteRequestSummary[] | null>(null);
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
    listQuoteRequests()
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
  const received = rows?.filter((row) => row.status === "Received").length ?? 0;
  const inReview = rows?.filter((row) => row.status === "InReview").length ?? 0;
  const converted = rows?.filter((row) => row.status === "Converted").length ?? 0;
  const total = rows?.length ?? 0;

  return (
    <AppShell
      currentHref="/satis/teklif-talepleri"
      breadcrumbs={[
        { label: "Çalışma alanı", href: "/dashboard" },
        { label: "Satış" },
        { label: "Teklif talepleri" },
      ]}
      pageTitle="Teklif Talepleri"
      pageDescription="Public katalogdan gelen talepler. Son 100 kayıt; sipariş listesi değildir."
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
          label="Alındı"
          value={ready ? String(received) : "—"}
          unit="talep"
          icon={Inbox}
          tone="amber"
          unavailable={!ready}
          caption="GET /quote-requests · Received"
        />
        <KpiMetric
          label="İncelemede"
          value={ready ? String(inReview) : "—"}
          unit="talep"
          icon={Search}
          tone="teal"
          unavailable={!ready}
          caption="GET /quote-requests · InReview"
        />
        <KpiMetric
          label="Dönüştürüldü"
          value={ready ? String(converted) : "—"}
          unit="talep"
          icon={ClipboardList}
          tone="teal"
          unavailable={!ready}
          caption="GET /quote-requests · Converted"
        />
        <KpiMetric
          label="Toplam"
          value={ready ? String(total) : "—"}
          unit="kayıt"
          icon={Globe2}
          tone="navy"
          unavailable={!ready}
          caption="Liste penceresi · en fazla 100"
        />
      </div>

      <Card className="mt-4">
        <CardBody>
          {!canRead ? (
            <PermissionDenied
              title="Teklif talepleri bu oturumda görünmez"
              description="quote-request.read yok. Bu kontrol yalnızca görünürlük içindir; gerçek yetki backend’dedir."
            />
          ) : denied ? (
            <PermissionDenied />
          ) : error ? (
            <ErrorState
              title="Teklif talepleri yüklenemedi"
              description={error}
              onRetry={() => setReload((value) => value + 1)}
            />
          ) : loading ? (
            <DataTable
              columns={COLUMNS.map((column) => ({
                id: column.id,
                header: column.header,
                accessor: () => null,
              }))}
              rows={[]}
              getRowId={() => ""}
              loading
            />
          ) : !rows || rows.length === 0 ? (
            <EmptyState
              title="Teklif talebi yok"
              description="Public katalogdan henüz talep gelmemiş veya liste boş."
            />
          ) : (
            <DataTable
              columns={[
                {
                  id: "requestNumber",
                  header: "Talep",
                  accessor: (row) => (
                    <Link
                      href={`/satis/teklif-talepleri/${row.id}`}
                      className="inline-flex items-center gap-2 font-semibold text-teal-600"
                    >
                      <Glyph icon={ClipboardList} />
                      {row.requestNumber}
                    </Link>
                  ),
                },
                {
                  id: "candidate",
                  header: "Aday",
                  accessor: (row) => row.candidateName,
                },
                {
                  id: "source",
                  header: "Kaynak",
                  accessor: (row) => (
                    <span className="inline-flex items-center gap-2">
                      <Glyph icon={Globe2} tone="navy" />
                      {quoteRequestSourceLabel(row.source)}
                    </span>
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
                {
                  id: "createdAt",
                  header: "Tarih",
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
