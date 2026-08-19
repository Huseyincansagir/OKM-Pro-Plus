"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { ClipboardList, Layers } from "lucide-react";
import { AppShell } from "@/components/shell/app-shell";
import { KpiMetric } from "@/components/dashboard/kpi-metric";
import { EmptyState } from "@/components/states/empty-state";
import { ErrorState } from "@/components/states/error-state";
import { PermissionDenied } from "@/components/states/permission-denied";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader, CardTitle } from "@/components/ui/card";
import { DataTable } from "@/components/ui/data-table";
import { Select } from "@/components/ui/select";
import { StatusBadge } from "@/components/ui/status-badge";
import { Glyph } from "@/components/ui/glyph";
import { ApiError } from "@/lib/api/types";
import { userFacingMessage } from "@/lib/api/auth-client";
import { useSessionStore } from "@/lib/auth/session-store";
import { countStatusKind, createStockCount, listStockCounts, type StockCountRow } from "@/lib/warehouse/counts";
import { listWarehouseLocations, listWarehouses, type WarehouseLocation, type WarehouseSummary } from "@/lib/warehouse/stocks";

export function StockCountBoard() {
  const router = useRouter();
  const user = useSessionStore((state) => state.user);
  const permissions = user?.permissions ?? [];
  const canRead = permissions.includes("stock-count.read");
  const canManage = permissions.includes("stock-count.manage");
  const [rows, setRows] = useState<StockCountRow[] | null>(null);
  const [warehouses, setWarehouses] = useState<WarehouseSummary[]>([]);
  const [locations, setLocations] = useState<WarehouseLocation[]>([]);
  const [warehouseId, setWarehouseId] = useState("");
  const [locationId, setLocationId] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [denied, setDenied] = useState(false);
  const [loading, setLoading] = useState(canRead);
  const [reload, setReload] = useState(0);
  const [acting, setActing] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  useEffect(() => {
    if (!canRead) {
      setLoading(false);
      return;
    }
    let cancelled = false;
    setLoading(true);
    Promise.all([listStockCounts(), listWarehouses()])
      .then(([counts, warehouseRows]) => {
        if (cancelled) return;
        setRows(counts);
        setWarehouses(warehouseRows.filter((row) => row.isActive));
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

  useEffect(() => {
    if (!warehouseId) {
      setLocations([]);
      setLocationId("");
      return;
    }
    let cancelled = false;
    listWarehouseLocations(warehouseId)
      .then((result) => {
        if (!cancelled) {
          setLocations(result.filter((row) => row.isActive));
          setLocationId("");
        }
      })
      .catch((caught) => {
        if (!cancelled) setActionError(userFacingMessage(caught));
      });
    return () => {
      cancelled = true;
    };
  }, [warehouseId]);

  async function submit() {
    if (!warehouseId || !locationId) {
      setActionError("Depo ve lokasyon zorunludur.");
      return;
    }
    setActing(true);
    setActionError(null);
    try {
      const created = await createStockCount({ warehouseId, locationId });
      router.push(`/depo/sayimlar/${created.id}`);
    } catch (caught) {
      setActionError(userFacingMessage(caught));
    } finally {
      setActing(false);
    }
  }

  const ready = Boolean(rows) && !loading && !error && !denied;

  return (
    <AppShell
      currentHref="/depo"
      breadcrumbs={[
        { label: "Çalışma alanı", href: "/dashboard" },
        { label: "Depo", href: "/depo" },
        { label: "Sayımlar" },
      ]}
      pageTitle="Stok sayımı"
      pageDescription="GET /stock-counts. Complete anında canlı on-hand ile fark hesaplanır; istemci stok yazmaz."
      pageActions={
        <Button variant="secondary" onClick={() => router.push("/depo")}>
          Stok
        </Button>
      }
    >
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <KpiMetric
          label="Taslak"
          value={ready ? String(rows?.filter((row) => row.status === "Draft").length ?? 0) : "—"}
          icon={ClipboardList}
          tone="amber"
          unavailable={!ready}
          caption="Draft"
        />
        <KpiMetric
          label="Tamam"
          value={ready ? String(rows?.filter((row) => row.status === "Completed").length ?? 0) : "—"}
          icon={Layers}
          unavailable={!ready}
          caption="Completed"
        />
      </div>
      {canManage ? (
        <Card className="mt-4">
          <CardHeader>
            <CardTitle>Yeni sayım</CardTitle>
          </CardHeader>
          <CardBody className="grid gap-3 md:grid-cols-2">
            <Select
              label="Depo"
              required
              value={warehouseId}
              onChange={(event) => setWarehouseId(event.target.value)}
              options={[
                { value: "", label: "Seçin" },
                ...warehouses.map((row) => ({ value: row.id, label: `${row.code} — ${row.name}` })),
              ]}
            />
            <Select
              label="Lokasyon"
              required
              value={locationId}
              onChange={(event) => setLocationId(event.target.value)}
              options={[
                { value: "", label: warehouseId ? "Seçin" : "Önce depo" },
                ...locations.map((row) => ({ value: row.id, label: `${row.code} — ${row.name}` })),
              ]}
            />
            {actionError ? <div className="md:col-span-2"><Alert tone="danger" title="Sayım açılmadı">{actionError}</Alert></div> : null}
            <Button loading={acting} onClick={() => void submit()}>
              Taslak aç
            </Button>
          </CardBody>
        </Card>
      ) : null}
      <Card className="mt-4">
        <CardBody>
          {!canRead ? (
            <PermissionDenied
              title="Sayım bu oturumda görünmez"
              description="stock-count.read yok. Bu kontrol yalnızca görünürlük içindir; gerçek yetki backend’dedir."
            />
          ) : denied ? (
            <PermissionDenied />
          ) : error ? (
            <ErrorState title="Sayımlar yüklenemedi" description={error} onRetry={() => setReload((value) => value + 1)} />
          ) : loading || !rows ? (
            <p className="text-sm text-slate-600">Yükleniyor…</p>
          ) : rows.length === 0 ? (
            <EmptyState title="Sayım yok" description="Bu pencerede sayım belgesi yok." />
          ) : (
            <DataTable
              columns={[
                {
                  id: "no",
                  header: "Belge",
                  accessor: (row) => (
                    <Link className="inline-flex items-center gap-2 font-semibold text-teal-600" href={`/depo/sayimlar/${row.id}`}>
                      <Glyph icon={ClipboardList} />
                      {row.documentNumber}
                    </Link>
                  ),
                },
                {
                  id: "status",
                  header: "Durum",
                  accessor: (row) => <StatusBadge status={countStatusKind(row.status)} label={row.status} />,
                },
                { id: "wh", header: "Depo", accessor: (row) => row.warehouseCode || "—" },
                { id: "loc", header: "Lokasyon", accessor: (row) => row.locationCode || "—" },
                { id: "items", header: "Kalem", accessor: (row) => String(row.itemCount) },
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
