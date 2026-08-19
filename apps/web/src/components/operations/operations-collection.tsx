"use client";

import { useEffect, useState } from "react";
import type { LucideIcon } from "lucide-react";
import { AppShell } from "@/components/shell/app-shell";
import { KpiMetric } from "@/components/dashboard/kpi-metric";
import { EmptyState } from "@/components/states/empty-state";
import { ErrorState } from "@/components/states/error-state";
import { PermissionDenied } from "@/components/states/permission-denied";
import { Button } from "@/components/ui/button";
import { Card, CardBody } from "@/components/ui/card";
import { DataTable, type DataTableColumn } from "@/components/ui/data-table";
import { ApiError } from "@/lib/api/types";
import { userFacingMessage } from "@/lib/api/auth-client";
import { useSessionStore } from "@/lib/auth/session-store";

export function OperationsCollection<T extends { id: string; status: string }>({
  currentHref,
  title,
  description,
  permission,
  load,
  columns,
  kpis,
  emptyTitle,
}: {
  currentHref: string;
  title: string;
  description: string;
  permission: string;
  load: () => Promise<T[]>;
  columns: DataTableColumn<T>[];
  kpis: Array<{
    status?: string;
    label: string;
    icon: LucideIcon;
    caption: string;
    tone?: "teal" | "amber" | "navy";
  }>;
  emptyTitle: string;
}) {
  const user = useSessionStore((state) => state.user);
  const canRead = (user?.permissions ?? []).includes(permission);
  const [rows, setRows] = useState<T[] | null>(null);
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
    load()
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
  }, [canRead, load, reload]);

  const ready = Boolean(rows) && !loading && !error && !denied;

  return (
    <AppShell
      currentHref={currentHref}
      breadcrumbs={[
        { label: "Çalışma alanı", href: "/dashboard" },
        { label: title },
      ]}
      pageTitle={title}
      pageDescription={description}
      pageActions={
        canRead ? (
          <Button variant="secondary" loading={loading} onClick={() => setReload((value) => value + 1)}>
            Yenile
          </Button>
        ) : null
      }
    >
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        {kpis.map((kpi) => {
          const value = kpi.status
            ? (rows?.filter((row) => row.status === kpi.status).length ?? 0)
            : (rows?.length ?? 0);
          return (
            <KpiMetric
              key={kpi.label}
              label={kpi.label}
              value={ready ? String(value) : "—"}
              icon={kpi.icon}
              tone={kpi.tone ?? "teal"}
              unavailable={!ready}
              caption={kpi.caption}
            />
          );
        })}
      </div>
      <Card className="mt-4">
        <CardBody>
          {!canRead ? (
            <PermissionDenied
              title={`${title} bu oturumda görünmez`}
              description={`${permission} yok. Bu kontrol yalnızca görünürlük içindir; gerçek yetki backend’dedir.`}
            />
          ) : denied ? (
            <PermissionDenied />
          ) : error ? (
            <ErrorState
              title={`${title} yüklenemedi`}
              description={error}
              onRetry={() => setReload((value) => value + 1)}
            />
          ) : loading ? (
            <DataTable
              columns={columns.map((column) => ({ ...column, accessor: () => null }))}
              rows={[]}
              getRowId={() => ""}
              loading
            />
          ) : !rows || rows.length === 0 ? (
            <EmptyState title={emptyTitle} description="Liste penceresi boş." />
          ) : (
            <DataTable columns={columns} rows={rows} getRowId={(row) => row.id} />
          )}
        </CardBody>
      </Card>
    </AppShell>
  );
}
