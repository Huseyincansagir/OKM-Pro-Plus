"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { ArrowLeftRight, Layers, Package } from "lucide-react";
import { AppShell } from "@/components/shell/app-shell";
import { KpiMetric } from "@/components/dashboard/kpi-metric";
import { ErrorState } from "@/components/states/error-state";
import { PermissionDenied } from "@/components/states/permission-denied";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader, CardTitle } from "@/components/ui/card";
import { Dialog } from "@/components/ui/dialog";
import { StatusBadge } from "@/components/ui/status-badge";
import { ApiError } from "@/lib/api/types";
import { userFacingMessage } from "@/lib/api/auth-client";
import { useSessionStore } from "@/lib/auth/session-store";
import {
  cancelTransfer,
  completeTransfer,
  getTransfer,
  transferStatusKind,
  type TransferRow,
} from "@/lib/warehouse/transfers";

export function TransferDetail({ id }: { id: string }) {
  const router = useRouter();
  const user = useSessionStore((state) => state.user);
  const permissions = user?.permissions ?? [];
  const canRead = permissions.includes("stock-transfer.read");
  const canComplete = permissions.includes("stock-transfer.complete");
  const canCancel = permissions.includes("stock-transfer.cancel");
  const [row, setRow] = useState<TransferRow | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [denied, setDenied] = useState(false);
  const [loading, setLoading] = useState(canRead);
  const [reload, setReload] = useState(0);
  const [acting, setActing] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const [confirm, setConfirm] = useState<"complete" | "cancel" | null>(null);

  useEffect(() => {
    if (!canRead) {
      setLoading(false);
      return;
    }
    let cancelled = false;
    setLoading(true);
    setError(null);
    setDenied(false);
    getTransfer(id)
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

  const draft = row?.status === "Draft";

  async function runAction() {
    if (!confirm) return;
    setActing(true);
    setActionError(null);
    try {
      const next = confirm === "complete" ? await completeTransfer(id) : await cancelTransfer(id);
      setRow(next);
      setConfirm(null);
    } catch (caught) {
      setActionError(userFacingMessage(caught));
    } finally {
      setActing(false);
    }
  }

  return (
    <AppShell
      currentHref="/depo"
      breadcrumbs={[
        { label: "Çalışma alanı", href: "/dashboard" },
        { label: "Depo", href: "/depo" },
        { label: "Transferler", href: "/depo/transferler" },
        { label: row?.id.slice(0, 8) || "Kart" },
      ]}
      pageTitle="Transfer"
      pageDescription="GET /warehouse-transfers/{id}. Stok yalnız complete ile hareket eder. quantityBase sunucudadır."
      pageActions={
        <div className="flex flex-wrap gap-2">
          <Button variant="secondary" onClick={() => router.push("/depo/transferler")}>
            Listeye dön
          </Button>
          {canRead ? (
            <Button variant="secondary" loading={loading} onClick={() => setReload((value) => value + 1)}>
              Yenile
            </Button>
          ) : null}
          {draft && canComplete ? <Button onClick={() => setConfirm("complete")}>Tamamla</Button> : null}
          {draft && canCancel ? (
            <Button variant="danger" onClick={() => setConfirm("cancel")}>
              İptal
            </Button>
          ) : null}
        </div>
      }
    >
      {!canRead ? (
        <PermissionDenied
          title="Transfer bu oturumda görünmez"
          description="stock-transfer.read yok. Bu kontrol yalnızca görünürlük içindir; gerçek yetki backend’dedir."
        />
      ) : denied ? (
        <PermissionDenied />
      ) : error ? (
        <ErrorState title="Transfer yüklenemedi" description={error} onRetry={() => setReload((value) => value + 1)} />
      ) : loading || !row ? (
        <p className="text-sm text-slate-600">Yükleniyor…</p>
      ) : (
        <div className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <KpiMetric
              label="Durum"
              value={row.status}
              icon={ArrowLeftRight}
              tone="amber"
              caption="Sunucu status"
            />
            <KpiMetric
              label="Temel miktar"
              value={row.quantityBase === null ? "—" : String(row.quantityBase)}
              icon={Layers}
              tone="navy"
              caption="quantityBase"
            />
            <KpiMetric
              label="Girilen"
              value={String(row.enteredQuantity)}
              icon={Package}
              tone="teal"
              caption={row.viewMode || "viewMode"}
            />
            <KpiMetric
              label="Ürün"
              value={row.productCode || "—"}
              icon={Package}
              tone="navy"
              caption={row.productId.slice(0, 8)}
            />
          </div>
          <Card>
            <CardHeader>
              <CardTitle>Konumlar</CardTitle>
            </CardHeader>
            <CardBody className="space-y-3">
              <StatusBadge status={transferStatusKind(row.status)} label={row.status} />
              <dl className="grid gap-3 sm:grid-cols-2 text-sm">
                <div>
                  <dt className="text-xs uppercase tracking-wide text-slate-500">Kaynak</dt>
                  <dd>
                    {row.sourceWarehouseCode || "—"} / {row.sourceLocationCode || "—"}
                  </dd>
                </div>
                <div>
                  <dt className="text-xs uppercase tracking-wide text-slate-500">Hedef</dt>
                  <dd>
                    {row.targetWarehouseCode || "—"} / {row.targetLocationCode || "—"}
                  </dd>
                </div>
              </dl>
            </CardBody>
          </Card>
        </div>
      )}

      <Dialog
        open={confirm !== null}
        onOpenChange={(open) => {
          if (!open && !acting) setConfirm(null);
        }}
        title={confirm === "complete" ? "Transferi tamamla" : "Transferi iptal et"}
        description={
          confirm === "complete"
            ? "Kaynak stok düşer, hedef stok artar, iki StockMovement yazılır. Bu işlem geri alınamaz."
            : "Yalnızca taslak iptal edilir. Stok hareketi yazılmaz."
        }
        footer={
          <div className="flex justify-end gap-2">
            <Button variant="secondary" disabled={acting} onClick={() => setConfirm(null)}>
              Vazgeç
            </Button>
            <Button
              variant={confirm === "cancel" ? "danger" : "primary"}
              loading={acting}
              onClick={() => void runAction()}
            >
              {confirm === "complete" ? "Tamamla" : "İptal et"}
            </Button>
          </div>
        }
      >
        {actionError ? <Alert tone="danger" title="Komut başarısız">{actionError}</Alert> : null}
      </Dialog>
    </AppShell>
  );
}
