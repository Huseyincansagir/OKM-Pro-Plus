"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { AppShell } from "@/components/shell/app-shell";
import { KpiMetric } from "@/components/dashboard/kpi-metric";
import { ErrorState } from "@/components/states/error-state";
import { PermissionDenied } from "@/components/states/permission-denied";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader, CardTitle } from "@/components/ui/card";
import { DataTable } from "@/components/ui/data-table";
import { Dialog } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { StatusBadge } from "@/components/ui/status-badge";
import { ApiError } from "@/lib/api/types";
import { userFacingMessage } from "@/lib/api/auth-client";
import { useSessionStore } from "@/lib/auth/session-store";
import { listStaffProducts, type StaffProductSummary } from "@/lib/catalog/staff-products";
import {
  addStockCountItem,
  completeStockCount,
  countStatusKind,
  getStockCount,
  type StockCountRow,
} from "@/lib/warehouse/counts";
import { ClipboardList, Layers } from "lucide-react";

export function StockCountDetail({ id }: { id: string }) {
  const router = useRouter();
  const user = useSessionStore((state) => state.user);
  const permissions = user?.permissions ?? [];
  const canRead = permissions.includes("stock-count.read");
  const canManage = permissions.includes("stock-count.manage");
  const canComplete = permissions.includes("stock-count.complete");
  const [row, setRow] = useState<StockCountRow | null>(null);
  const [products, setProducts] = useState<StaffProductSummary[]>([]);
  const [productId, setProductId] = useState("");
  const [counted, setCounted] = useState("0");
  const [error, setError] = useState<string | null>(null);
  const [denied, setDenied] = useState(false);
  const [loading, setLoading] = useState(canRead);
  const [reload, setReload] = useState(0);
  const [acting, setActing] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const [confirm, setConfirm] = useState(false);

  useEffect(() => {
    if (!canRead) {
      setLoading(false);
      return;
    }
    let cancelled = false;
    setLoading(true);
    Promise.all([getStockCount(id), listStaffProducts().catch(() => [])])
      .then(([count, productRows]) => {
        if (cancelled) return;
        setRow(count);
        setProducts(productRows);
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

  async function addItem() {
    const qty = Number(counted);
    if (!productId || !Number.isFinite(qty) || qty < 0) {
      setActionError("Ürün ve sıfırdan küçük olmayan temel miktar zorunludur.");
      return;
    }
    setActing(true);
    setActionError(null);
    try {
      setRow(await addStockCountItem(id, { productId, countedQtyBase: qty }));
      setProductId("");
      setCounted("0");
    } catch (caught) {
      setActionError(userFacingMessage(caught));
    } finally {
      setActing(false);
    }
  }

  async function complete() {
    setActing(true);
    setActionError(null);
    try {
      setRow(await completeStockCount(id));
      setConfirm(false);
    } catch (caught) {
      setActionError(userFacingMessage(caught));
    } finally {
      setActing(false);
    }
  }

  const draft = row?.status === "Draft";

  return (
    <AppShell
      currentHref="/depo"
      breadcrumbs={[
        { label: "Çalışma alanı", href: "/dashboard" },
        { label: "Depo", href: "/depo" },
        { label: "Sayımlar", href: "/depo/sayimlar" },
        { label: row?.documentNumber || "Kart" },
      ]}
      pageTitle={row?.documentNumber || "Sayım"}
      pageDescription="Sayılan miktar temel birimdir. Complete stok satırını countedQtyBase yapar ve CountIn/CountOut yazar."
      pageActions={
        <div className="flex flex-wrap gap-2">
          <Button variant="secondary" onClick={() => router.push("/depo/sayimlar")}>
            Listeye dön
          </Button>
          {draft && canComplete ? <Button onClick={() => setConfirm(true)}>Tamamla</Button> : null}
        </div>
      }
    >
      {!canRead ? (
        <PermissionDenied
          title="Sayım bu oturumda görünmez"
          description="stock-count.read yok."
        />
      ) : denied ? (
        <PermissionDenied />
      ) : error ? (
        <ErrorState title="Sayım yüklenemedi" description={error} onRetry={() => setReload((value) => value + 1)} />
      ) : loading || !row ? (
        <p className="text-sm text-slate-600">Yükleniyor…</p>
      ) : (
        <div className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <KpiMetric label="Durum" value={row.status} icon={ClipboardList} tone="amber" caption="Sunucu status" />
            <KpiMetric label="Kalem" value={String(row.itemCount)} icon={Layers} tone="navy" caption="Satır" />
          </div>
          {draft && canManage ? (
            <Card>
              <CardHeader>
                <CardTitle>Kalem ekle</CardTitle>
              </CardHeader>
              <CardBody className="grid gap-3 md:grid-cols-3">
                <Select
                  label="Ürün"
                  required
                  value={productId}
                  onChange={(event) => setProductId(event.target.value)}
                  options={[
                    { value: "", label: "Seçin" },
                    ...products.map((item) => ({ value: item.id, label: `${item.code} — ${item.name}` })),
                  ]}
                />
                <Input
                  label="Sayılan (temel)"
                  type="number"
                  min="0"
                  step="any"
                  value={counted}
                  onChange={(event) => setCounted(event.target.value)}
                />
                <Button loading={acting} onClick={() => void addItem()}>
                  Ekle
                </Button>
                {actionError && !confirm ? (
                  <div className="md:col-span-3">
                    <Alert tone="danger" title="Kalem eklenemedi">{actionError}</Alert>
                  </div>
                ) : null}
              </CardBody>
            </Card>
          ) : null}
          <Card>
            <CardHeader>
              <CardTitle>Kalemler</CardTitle>
            </CardHeader>
            <CardBody>
              <StatusBadge status={countStatusKind(row.status)} label={row.status} />
              <div className="mt-3">
                <DataTable
                  columns={[
                    { id: "code", header: "Ürün", accessor: (item) => item.productCode || item.productId.slice(0, 8) },
                    {
                      id: "counted",
                      header: "Sayılan",
                      accessor: (item) => (item.countedQtyBase === null ? "—" : String(item.countedQtyBase)),
                    },
                    {
                      id: "system",
                      header: "Sistem",
                      accessor: (item) => (item.systemOnHandQtyBase === null ? "—" : String(item.systemOnHandQtyBase)),
                    },
                    {
                      id: "var",
                      header: "Fark",
                      accessor: (item) => (item.varianceQtyBase === null ? "—" : String(item.varianceQtyBase)),
                    },
                  ]}
                  rows={row.items}
                  getRowId={(item) => item.id}
                />
              </div>
            </CardBody>
          </Card>
        </div>
      )}
      <Dialog
        open={confirm}
        onOpenChange={(open) => {
          if (!open && !acting) setConfirm(false);
        }}
        title="Sayımı tamamla"
        description="Canlı on-hand countedQtyBase olur. Rezervenin altına inilmez. CountIn/CountOut yazılır."
        footer={
          <div className="flex justify-end gap-2">
            <Button variant="secondary" disabled={acting} onClick={() => setConfirm(false)}>
              Vazgeç
            </Button>
            <Button loading={acting} onClick={() => void complete()}>
              Tamamla
            </Button>
          </div>
        }
      >
        {actionError && confirm ? <Alert tone="danger" title="Tamamlanamadı">{actionError}</Alert> : null}
      </Dialog>
    </AppShell>
  );
}
