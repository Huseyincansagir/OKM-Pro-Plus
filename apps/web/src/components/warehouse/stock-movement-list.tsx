"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { ArrowDownLeft, ArrowUpRight, Layers, RotateCcw } from "lucide-react";
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
  formatMovementInstant,
  listStockMovements,
  movementEffectKind,
  movementEffectLabel,
  movementTypeLabel,
  type StockMovementRow,
} from "@/lib/warehouse/stock-movements";

export function StockMovementList() {
  const router = useRouter();
  const user = useSessionStore((state) => state.user);
  const canRead = (user?.permissions ?? []).includes("stock.read");
  const [rows, setRows] = useState<StockMovementRow[] | null>(null);
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
    listStockMovements()
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
  const inbound = rows?.filter((row) => row.effect === "In").length ?? 0;
  const outbound = rows?.filter((row) => row.effect === "Out").length ?? 0;
  const reversed = rows?.filter((row) => Boolean(row.reversedFromId)).length ?? 0;
  const total = rows?.length ?? 0;

  return (
    <AppShell
      currentHref="/depo"
      breadcrumbs={[
        { label: "Çalışma alanı", href: "/dashboard" },
        { label: "Depo", href: "/depo" },
        { label: "Hareketler" },
      ]}
      pageTitle="Stok hareketleri"
      pageDescription="GET /stock-movements. Miktar sunucu quantityBase alanıdır; işaretli bakiye veya kartex toplamı üretilmez. Liste en fazla 100 kayıttır."
      pageActions={
        <div className="flex flex-wrap gap-2">
          <Button variant="secondary" onClick={() => router.push("/depo")}>
            Stok
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
          icon={Layers}
          tone="navy"
          unavailable={!ready}
          caption="GET /stock-movements · pencere"
        />
        <KpiMetric
          label="Giriş"
          value={ready ? String(inbound) : "—"}
          unit="satır"
          icon={ArrowDownLeft}
          tone="teal"
          unavailable={!ready}
          caption="effect = In"
        />
        <KpiMetric
          label="Çıkış"
          value={ready ? String(outbound) : "—"}
          unit="satır"
          icon={ArrowUpRight}
          tone="amber"
          unavailable={!ready}
          caption="effect = Out"
        />
        <KpiMetric
          label="Ters kayıt"
          value={ready ? String(reversed) : "—"}
          unit="satır"
          icon={RotateCcw}
          tone="navy"
          unavailable={!ready}
          caption="reversedFromId dolu"
        />
      </div>

      <Card className="mt-4">
        <CardBody>
          {!canRead ? (
            <PermissionDenied
              title="Hareketler bu oturumda görünmez"
              description="stock.read yok. Bu kontrol yalnızca görünürlük içindir; gerçek yetki backend’dedir."
            />
          ) : denied ? (
            <PermissionDenied />
          ) : error ? (
            <ErrorState
              title="Hareketler yüklenemedi"
              description={error}
              onRetry={() => setReload((value) => value + 1)}
            />
          ) : loading ? (
            <DataTable
              columns={[
                { id: "type", header: "Tür", accessor: () => null },
                { id: "qty", header: "Temel miktar", accessor: () => null },
              ]}
              rows={[]}
              getRowId={() => ""}
              loading
            />
          ) : !rows || rows.length === 0 ? (
            <EmptyState
              title="Hareket yok"
              description="Bu pencerede stok hareketi yok. Transfer, üretim girişi veya irsaliye çıkışı henüz yazılmamış olabilir."
            />
          ) : (
            <DataTable
              columns={[
                {
                  id: "type",
                  header: "Tür",
                  accessor: (row) => (
                    <Link
                      href={`/depo/hareketler/${row.id}`}
                      className="inline-flex items-center gap-2 font-semibold text-teal-600"
                    >
                      <Glyph icon={Layers} />
                      {movementTypeLabel(row.movementType)}
                    </Link>
                  ),
                },
                {
                  id: "effect",
                  header: "Yön",
                  accessor: (row) => (
                    <StatusBadge
                      status={movementEffectKind(row.effect)}
                      label={movementEffectLabel(row.effect)}
                    />
                  ),
                },
                {
                  id: "product",
                  header: "Ürün",
                  accessor: (row) => row.productCode || row.productId.slice(0, 8) || "—",
                },
                { id: "wh", header: "Depo", accessor: (row) => row.warehouseCode || "—" },
                { id: "loc", header: "Lokasyon", accessor: (row) => row.locationCode || "—" },
                {
                  id: "qty",
                  header: "Temel miktar",
                  accessor: (row) => (row.quantityBase === null ? "—" : String(row.quantityBase)),
                },
                {
                  id: "source",
                  header: "Kaynak",
                  accessor: (row) => row.sourceEntityType || "—",
                },
                {
                  id: "when",
                  header: "Zaman",
                  accessor: (row) => formatMovementInstant(row.createdAt),
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
