"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { ArrowLeftRight, CheckCircle2, Inbox, Layers } from "lucide-react";
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
  listTransfers,
  transferStatusKind,
  type TransferRow,
} from "@/lib/warehouse/transfers";

export function WarehouseTransferList() {
  const router = useRouter();
  const user = useSessionStore((state) => state.user);
  const permissions = user?.permissions ?? [];
  const canRead = permissions.includes("stock-transfer.read");
  const canCreate = permissions.includes("stock-transfer.create");
  const [rows, setRows] = useState<TransferRow[] | null>(null);
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
    listTransfers()
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
  const drafts = rows?.filter((row) => row.status === "Draft").length ?? 0;
  const completed = rows?.filter((row) => row.status === "Completed").length ?? 0;
  const cancelled = rows?.filter((row) => row.status === "Cancelled").length ?? 0;

  return (
    <AppShell
      currentHref="/depo"
      breadcrumbs={[
        { label: "Çalışma alanı", href: "/dashboard" },
        { label: "Depo", href: "/depo" },
        { label: "Transferler" },
      ]}
      pageTitle="Depo transferleri"
      pageDescription="GET /warehouse-transfers. quantityBase sunucudan gelir. Oluşturma ve tamamlama komutları vardır; stok yalnız complete’te hareket eder."
      pageActions={
        <div className="flex flex-wrap gap-2">
          <Button variant="secondary" onClick={() => router.push("/depo")}>
            Stok
          </Button>
          {canCreate ? (
            <Button onClick={() => router.push("/depo/transferler/yeni")}>Yeni transfer</Button>
          ) : null}
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
          value={ready ? String(drafts) : "—"}
          unit="satır"
          icon={Inbox}
          tone="amber"
          unavailable={!ready}
          caption="Draft"
        />
        <KpiMetric
          label="Tamam"
          value={ready ? String(completed) : "—"}
          unit="satır"
          icon={CheckCircle2}
          unavailable={!ready}
          caption="Completed"
        />
        <KpiMetric
          label="İptal"
          value={ready ? String(cancelled) : "—"}
          unit="satır"
          icon={ArrowLeftRight}
          tone="navy"
          unavailable={!ready}
          caption="Cancelled"
        />
        <KpiMetric
          label="Toplam"
          value={ready ? String(rows?.length ?? 0) : "—"}
          unit="kayıt"
          icon={Layers}
          tone="navy"
          unavailable={!ready}
          caption="Pencere · 100"
        />
      </div>
      <Card className="mt-4">
        <CardBody>
          {!canRead ? (
            <PermissionDenied
              title="Depo transferleri bu oturumda görünmez"
              description="stock-transfer.read yok. Bu kontrol yalnızca görünürlük içindir; gerçek yetki backend’dedir."
            />
          ) : denied ? (
            <PermissionDenied />
          ) : error ? (
            <ErrorState
              title="Transferler yüklenemedi"
              description={error}
              onRetry={() => setReload((value) => value + 1)}
            />
          ) : loading ? (
            <DataTable
              columns={[
                { id: "id", header: "Id", accessor: () => null },
                { id: "qty", header: "Temel miktar", accessor: () => null },
              ]}
              rows={[]}
              getRowId={() => ""}
              loading
            />
          ) : !rows || rows.length === 0 ? (
            <EmptyState title="Transfer yok" description="Bu pencerede transfer belgesi yok." />
          ) : (
            <DataTable
              columns={[
                {
                  id: "id",
                  header: "Belge",
                  accessor: (row) => (
                    <Link
                      href={`/depo/transferler/${row.id}`}
                      className="inline-flex items-center gap-2 font-semibold text-teal-600"
                    >
                      <Glyph icon={ArrowLeftRight} />
                      {row.id.slice(0, 8)}
                    </Link>
                  ),
                },
                {
                  id: "status",
                  header: "Durum",
                  accessor: (row) => (
                    <StatusBadge status={transferStatusKind(row.status)} label={row.status} />
                  ),
                },
                { id: "product", header: "Ürün", accessor: (row) => row.productCode || row.productId.slice(0, 8) },
                {
                  id: "from",
                  header: "Kaynak",
                  accessor: (row) => `${row.sourceWarehouseCode || "—"} / ${row.sourceLocationCode || "—"}`,
                },
                {
                  id: "to",
                  header: "Hedef",
                  accessor: (row) => `${row.targetWarehouseCode || "—"} / ${row.targetLocationCode || "—"}`,
                },
                {
                  id: "qty",
                  header: "Temel miktar",
                  accessor: (row) => (row.quantityBase === null ? "—" : String(row.quantityBase)),
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
