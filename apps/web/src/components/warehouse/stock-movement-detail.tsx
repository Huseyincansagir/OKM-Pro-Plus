"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { ArrowDownLeft, ArrowUpRight, Layers, Package } from "lucide-react";
import { AppShell } from "@/components/shell/app-shell";
import { KpiMetric } from "@/components/dashboard/kpi-metric";
import { ErrorState } from "@/components/states/error-state";
import { PermissionDenied } from "@/components/states/permission-denied";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader, CardTitle } from "@/components/ui/card";
import { StatusBadge } from "@/components/ui/status-badge";
import { ApiError } from "@/lib/api/types";
import { userFacingMessage } from "@/lib/api/auth-client";
import { useSessionStore } from "@/lib/auth/session-store";
import {
  formatMovementInstant,
  getStockMovement,
  movementEffectKind,
  movementEffectLabel,
  movementTypeLabel,
  type StockMovementRow,
} from "@/lib/warehouse/stock-movements";

export function StockMovementDetail({ id }: { id: string }) {
  const router = useRouter();
  const user = useSessionStore((state) => state.user);
  const canRead = (user?.permissions ?? []).includes("stock.read");
  const [row, setRow] = useState<StockMovementRow | null>(null);
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
    getStockMovement(id)
      .then((result) => {
        if (!cancelled) setRow(result);
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
  }, [canRead, id, reload]);

  const ready = Boolean(row) && !loading && !error && !denied;
  const title = row ? movementTypeLabel(row.movementType) : "Stok hareketi";

  return (
    <AppShell
      currentHref="/depo"
      breadcrumbs={[
        { label: "Çalışma alanı", href: "/dashboard" },
        { label: "Depo", href: "/depo" },
        { label: "Hareketler", href: "/depo/hareketler" },
        { label: row?.id.slice(0, 8) || "Kart" },
      ]}
      pageTitle={title}
      pageDescription="GET /stock-movements/{id}. quantityBase ve effect sunucudan gelir. Ambalaj snapshot’ı miktara çevrilmez."
      pageActions={
        <div className="flex flex-wrap gap-2">
          <Button variant="secondary" onClick={() => router.push("/depo/hareketler")}>
            Listeye dön
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
          label="Yön"
          value={ready && row ? movementEffectLabel(row.effect) : "—"}
          icon={row?.effect === "Out" ? ArrowUpRight : ArrowDownLeft}
          tone={row?.effect === "Out" ? "amber" : "teal"}
          unavailable={!ready}
          caption="Sunucu effect"
        />
        <KpiMetric
          label="Temel miktar"
          value={ready && row && row.quantityBase !== null ? String(row.quantityBase) : "—"}
          icon={Layers}
          tone="navy"
          unavailable={!ready}
          caption="quantityBase · işaret yok"
        />
        <KpiMetric
          label="Ürün"
          value={ready && row ? row.productCode || "—" : "—"}
          icon={Package}
          tone="navy"
          unavailable={!ready}
          caption={row?.productName || "productCode"}
        />
        <KpiMetric
          label="Depo"
          value={ready && row ? row.warehouseCode || "—" : "—"}
          icon={Layers}
          tone="navy"
          unavailable={!ready}
          caption={row?.locationCode || "lokasyon yok"}
        />
      </div>

      <Card className="mt-4">
        <CardHeader>
          <CardTitle>Hareket</CardTitle>
        </CardHeader>
        <CardBody className="space-y-3 text-sm">
          {!canRead ? (
            <PermissionDenied
              title="Hareket bu oturumda görünmez"
              description="stock.read yok. Bu kontrol yalnızca görünürlük içindir; gerçek yetki backend’dedir."
            />
          ) : denied ? (
            <PermissionDenied />
          ) : error ? (
            <ErrorState
              title="Hareket yüklenemedi"
              description={error}
              onRetry={() => setReload((value) => value + 1)}
            />
          ) : loading || !row ? (
            <p className="text-slate-600">Yükleniyor…</p>
          ) : (
            <>
              <div className="flex flex-wrap items-center gap-2">
                <StatusBadge
                  status={movementEffectKind(row.effect)}
                  label={movementEffectLabel(row.effect)}
                />
                <span>{movementTypeLabel(row.movementType)}</span>
              </div>
              <dl className="grid gap-3 sm:grid-cols-2">
                <div>
                  <dt className="text-xs uppercase tracking-wide text-slate-500">Kaynak</dt>
                  <dd>{row.sourceEntityType || "—"}</dd>
                </div>
                <div>
                  <dt className="text-xs uppercase tracking-wide text-slate-500">Kaynak id</dt>
                  <dd className="break-all">{row.sourceEntityId || "—"}</dd>
                </div>
                <div>
                  <dt className="text-xs uppercase tracking-wide text-slate-500">Ters kayıt</dt>
                  <dd className="break-all">{row.reversedFromId || "—"}</dd>
                </div>
                <div>
                  <dt className="text-xs uppercase tracking-wide text-slate-500">Zaman</dt>
                  <dd>{formatMovementInstant(row.createdAt)}</dd>
                </div>
              </dl>
              <div>
                <p className="text-xs uppercase tracking-wide text-slate-500">Ambalaj snapshot</p>
                {row.packagingSnapshot ? (
                  <pre className="mt-1 overflow-x-auto rounded-md bg-slate-50 p-3 text-xs text-slate-800">
                    {row.packagingSnapshot}
                  </pre>
                ) : (
                  <p>—</p>
                )}
              </div>
            </>
          )}
        </CardBody>
      </Card>
    </AppShell>
  );
}
