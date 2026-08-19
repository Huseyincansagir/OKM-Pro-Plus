"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Building2, Shield, UserRound, Users } from "lucide-react";
import { AppShell } from "@/components/shell/app-shell";
import { KpiMetric } from "@/components/dashboard/kpi-metric";
import { EmptyState } from "@/components/states/empty-state";
import { ErrorState } from "@/components/states/error-state";
import { PermissionDenied } from "@/components/states/permission-denied";
import { Button } from "@/components/ui/button";
import { Card, CardBody } from "@/components/ui/card";
import { DataTable } from "@/components/ui/data-table";
import { Glyph } from "@/components/ui/glyph";
import { StatusBadge, type StatusKind } from "@/components/ui/status-badge";
import { ApiError } from "@/lib/api/types";
import { userFacingMessage } from "@/lib/api/auth-client";
import { useSessionStore } from "@/lib/auth/session-store";
import {
  customerStatusLabel,
  listCustomers,
  type CustomerSummary,
} from "@/lib/sales/customers";

function statusKind(status: string): StatusKind {
  if (status === "Active") return "success";
  if (status === "Candidate") return "pending";
  if (status === "Blocked") return "critical";
  return "inactive";
}

function formatDate(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return iso || "—";
  }
  return new Intl.DateTimeFormat("tr-TR", { dateStyle: "short" }).format(date);
}

export function CustomerList() {
  const router = useRouter();
  const user = useSessionStore((state) => state.user);
  const permissions = user?.permissions ?? [];
  const canRead = permissions.includes("customer.read");
  const [rows, setRows] = useState<CustomerSummary[] | null>(null);
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
    listCustomers()
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
  const active = rows?.filter((row) => row.status === "Active").length ?? 0;
  const candidate = rows?.filter((row) => row.status === "Candidate").length ?? 0;
  const blocked = rows?.filter((row) => row.status === "Blocked").length ?? 0;
  const total = rows?.length ?? 0;

  return (
    <AppShell
      currentHref="/satis/musteriler"
      breadcrumbs={[
        { label: "Çalışma alanı", href: "/dashboard" },
        { label: "Satış", href: "/satis/teklif-talepleri" },
        { label: "Müşteriler" },
      ]}
      pageTitle="Müşteriler"
      pageDescription="Aktif kartlar teklif talebine bağlanabilir. Liste en fazla 100 kayıttır."
      pageActions={
        <div className="flex flex-wrap gap-2">
          <Button variant="secondary" onClick={() => router.push("/satis/teklif-talepleri")}>
            Teklif talepleri
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
          label="Aktif"
          value={ready ? String(active) : "—"}
          unit="kart"
          icon={Building2}
          tone="teal"
          unavailable={!ready}
          caption="GET /customers · Active"
        />
        <KpiMetric
          label="Aday"
          value={ready ? String(candidate) : "—"}
          unit="kart"
          icon={UserRound}
          tone="amber"
          unavailable={!ready}
          caption="GET /customers · Candidate"
        />
        <KpiMetric
          label="Engelli"
          value={ready ? String(blocked) : "—"}
          unit="kart"
          icon={Shield}
          tone="navy"
          unavailable={!ready}
          caption="GET /customers · Blocked"
        />
        <KpiMetric
          label="Toplam"
          value={ready ? String(total) : "—"}
          unit="kayıt"
          icon={Users}
          tone="navy"
          unavailable={!ready}
          caption="Liste penceresi · en fazla 100"
        />
      </div>

      <Card className="mt-4">
        <CardBody>
          {!canRead ? (
            <PermissionDenied
              title="Müşteriler bu oturumda görünmez"
              description="customer.read yok. Bu kontrol yalnızca görünürlük içindir; gerçek yetki backend’dedir."
            />
          ) : denied ? (
            <PermissionDenied />
          ) : error ? (
            <ErrorState
              title="Müşteriler yüklenemedi"
              description={error}
              onRetry={() => setReload((value) => value + 1)}
            />
          ) : loading ? (
            <DataTable
              columns={[
                { id: "code", header: "Kod", accessor: () => null },
                { id: "name", header: "Unvan", accessor: () => null },
                { id: "status", header: "Durum", accessor: () => null },
              ]}
              rows={[]}
              getRowId={() => ""}
              loading
            />
          ) : !rows || rows.length === 0 ? (
            <EmptyState
              title="Müşteri yok"
              description="Henüz müşteri kartı yok veya liste boş."
            />
          ) : (
            <DataTable
              columns={[
                {
                  id: "code",
                  header: "Kod",
                  accessor: (row) => (
                    <span className="inline-flex items-center gap-2 font-semibold text-navy-950">
                      <Glyph icon={Building2} />
                      {row.customerCode}
                    </span>
                  ),
                },
                {
                  id: "name",
                  header: "Unvan",
                  accessor: (row) => row.legalName,
                },
                {
                  id: "contact",
                  header: "İletişim",
                  accessor: (row) => row.email || row.phone || "—",
                },
                {
                  id: "status",
                  header: "Durum",
                  accessor: (row) => (
                    <StatusBadge status={statusKind(row.status)} label={customerStatusLabel(row.status)} />
                  ),
                },
                {
                  id: "createdAt",
                  header: "Kayıt",
                  accessor: (row) => formatDate(row.createdAt),
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
