"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Boxes, Layers, Package, Warehouse } from "lucide-react";
import { AppShell } from "@/components/shell/app-shell";
import { KpiMetric } from "@/components/dashboard/kpi-metric";
import { EmptyState } from "@/components/states/empty-state";
import { ErrorState } from "@/components/states/error-state";
import { PermissionDenied } from "@/components/states/permission-denied";
import { Button } from "@/components/ui/button";
import { Card, CardBody } from "@/components/ui/card";
import { DataTable } from "@/components/ui/data-table";
import { Glyph } from "@/components/ui/glyph";
import { ApiError } from "@/lib/api/types";
import { userFacingMessage } from "@/lib/api/auth-client";
import { useSessionStore } from "@/lib/auth/session-store";
import { listStocks, listWarehouses, type StockRow } from "@/lib/warehouse/stocks";

export function StockList() {
  const router = useRouter();
  const user = useSessionStore((state) => state.user);
  const permissions = user?.permissions ?? [];
  const canRead = permissions.includes("stock.read");
  const [rows, setRows] = useState<StockRow[] | null>(null);
  const [warehouseCount, setWarehouseCount] = useState<number | null>(null);
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
    Promise.all([listStocks(), listWarehouses()])
      .then(([stocks, warehouses]) => {
        if (cancelled) return;
        setRows(stocks);
        setWarehouseCount(warehouses.length);
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
  const reserved = rows?.filter((row) => (row.reservedQtyBase ?? 0) > 0).length ?? 0;
  const empty = rows?.filter((row) => row.availableQtyBase === 0).length ?? 0;
  const total = rows?.length ?? 0;

  return (
    <AppShell
      currentHref="/depo"
      breadcrumbs={[
        { label: "Çalışma alanı", href: "/dashboard" },
        { label: "Depo" },
      ]}
      pageTitle="Stok"
      pageDescription="GET /stocks. Kullanılabilir miktar sunucu availableQtyBase alanıdır. Liste en fazla 100 kayıttır."
      pageActions={
        <div className="flex flex-wrap gap-2">
          <Button variant="secondary" onClick={() => router.push("/depo/sayimlar")}>
            Sayımlar
          </Button>
          <Button variant="secondary" onClick={() => router.push("/depo/hareketler")}>
            Hareketler
          </Button>
          <Button variant="secondary" onClick={() => router.push("/depo/transferler")}>
            Transferler
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
          label="Satır"
          value={ready ? String(total) : "—"}
          unit="kayıt"
          icon={Package}
          tone="navy"
          unavailable={!ready}
          caption="GET /stocks · pencere"
        />
        <KpiMetric
          label="Rezerve"
          value={ready ? String(reserved) : "—"}
          unit="satır"
          icon={Layers}
          tone="amber"
          unavailable={!ready}
          caption="reservedQtyBase > 0"
        />
        <KpiMetric
          label="Sıfır available"
          value={ready ? String(empty) : "—"}
          unit="satır"
          icon={Boxes}
          tone="teal"
          unavailable={!ready}
          caption="Sunucu availableQtyBase = 0"
        />
        <KpiMetric
          label="Depo"
          value={ready && warehouseCount !== null ? String(warehouseCount) : "—"}
          unit="kart"
          icon={Warehouse}
          tone="navy"
          unavailable={!ready}
          caption="GET /warehouses"
        />
      </div>

      <Card className="mt-4">
        <CardBody>
          {!canRead ? (
            <PermissionDenied
              title="Stok bu oturumda görünmez"
              description="stock.read yok. Bu kontrol yalnızca görünürlük içindir; gerçek yetki backend’dedir."
            />
          ) : denied ? (
            <PermissionDenied />
          ) : error ? (
            <ErrorState
              title="Stok yüklenemedi"
              description={error}
              onRetry={() => setReload((value) => value + 1)}
            />
          ) : loading ? (
            <DataTable
              columns={[
                { id: "code", header: "Ürün", accessor: () => null },
                { id: "onHand", header: "Eldeki", accessor: () => null },
              ]}
              rows={[]}
              getRowId={() => ""}
              loading
            />
          ) : !rows || rows.length === 0 ? (
            <EmptyState title="Stok satırı yok" description="Bu pencerede stok kaydı yok." />
          ) : (
            <DataTable
              columns={[
                {
                  id: "code",
                  header: "Ürün",
                  accessor: (row) => (
                    <span className="inline-flex items-center gap-2">
                      <Glyph icon={Package} />
                      {row.productCode || row.productId.slice(0, 8)}
                    </span>
                  ),
                },
                { id: "name", header: "Ad", accessor: (row) => row.productName || "—" },
                {
                  id: "wh",
                  header: "Depo",
                  accessor: (row) => row.warehouseCode || "—",
                },
                { id: "loc", header: "Lokasyon", accessor: (row) => row.locationCode || "—" },
                {
                  id: "onHand",
                  header: "Eldeki",
                  accessor: (row) => (row.onHandQtyBase === null ? "—" : String(row.onHandQtyBase)),
                },
                {
                  id: "reserved",
                  header: "Rezerve",
                  accessor: (row) => (row.reservedQtyBase === null ? "—" : String(row.reservedQtyBase)),
                },
                {
                  id: "available",
                  header: "Kullanılabilir",
                  accessor: (row) => (row.availableQtyBase === null ? "—" : String(row.availableQtyBase)),
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
