"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { ClipboardCheck, FileText, Inbox, Package } from "lucide-react";
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
  listSalesOrders,
  salesOrderStatusKind,
  salesOrderStatusLabel,
  type SalesOrderSummary,
} from "@/lib/sales/orders";

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

export function OrderList() {
  const router = useRouter();
  const user = useSessionStore((state) => state.user);
  const permissions = user?.permissions ?? [];
  const canRead = permissions.includes("order.read");
  const [rows, setRows] = useState<SalesOrderSummary[] | null>(null);
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
    listSalesOrders()
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
  const pending = rows?.filter((row) => row.status === "PendingApproval").length ?? 0;
  const approved = rows?.filter((row) => row.status === "Approved").length ?? 0;
  const total = rows?.length ?? 0;

  return (
    <AppShell
      currentHref="/satis/siparisler"
      breadcrumbs={[
        { label: "Çalışma alanı", href: "/dashboard" },
        { label: "Satış", href: "/satis/teklif-talepleri" },
        { label: "Siparişler" },
      ]}
      pageTitle="Siparişler"
      pageDescription="GET /orders penceresi. Ürün listesinden serbest sipariş yok. Liste en fazla 100 kayıttır."
      pageActions={
        <div className="flex flex-wrap gap-2">
          <Button variant="secondary" onClick={() => router.push("/satis/teklifler")}>
            Teklifler
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
          caption="GET /orders · Draft"
        />
        <KpiMetric
          label="Onay bekliyor"
          value={ready ? String(pending) : "—"}
          unit="belge"
          icon={ClipboardCheck}
          tone="teal"
          unavailable={!ready}
          caption="GET /orders · PendingApproval"
        />
        <KpiMetric
          label="Onaylandı"
          value={ready ? String(approved) : "—"}
          unit="belge"
          icon={Package}
          tone="teal"
          unavailable={!ready}
          caption="GET /orders · Approved"
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
              title="Siparişler bu oturumda görünmez"
              description="order.read yok. Bu kontrol yalnızca görünürlük içindir; gerçek yetki backend’dedir."
            />
          ) : denied ? (
            <PermissionDenied />
          ) : error ? (
            <ErrorState
              title="Siparişler yüklenemedi"
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
              title="Sipariş yok"
              description="Bu pencerede sipariş belgesi yok. Serbest ürün seçicisi bu dilimde yoktur."
            />
          ) : (
            <DataTable
              columns={[
                {
                  id: "number",
                  header: "Belge",
                  accessor: (row) => (
                    <Link
                      href={`/satis/siparisler/${row.id}`}
                      className="inline-flex items-center gap-2 font-semibold text-teal-600"
                    >
                      <Glyph icon={FileText} />
                      {row.orderNumber}
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
                    <StatusBadge
                      status={salesOrderStatusKind(row.status)}
                      label={salesOrderStatusLabel(row.status)}
                    />
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
