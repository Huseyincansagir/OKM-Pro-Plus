"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Layers, Package, Truck } from "lucide-react";
import { AppShell } from "@/components/shell/app-shell";
import { KpiMetric } from "@/components/dashboard/kpi-metric";
import { ErrorState } from "@/components/states/error-state";
import { PermissionDenied } from "@/components/states/permission-denied";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader, CardTitle } from "@/components/ui/card";
import { DataTable } from "@/components/ui/data-table";
import { StatusBadge } from "@/components/ui/status-badge";
import { ApiError } from "@/lib/api/types";
import { userFacingMessage } from "@/lib/api/auth-client";
import { useSessionStore } from "@/lib/auth/session-store";
import { getShipment, shipmentStatusKind, type ShipmentDetail } from "@/lib/shipping/shipments";

export function ShipmentDetailBoard({ id }: { id: string }) {
  const router = useRouter();
  const user = useSessionStore((state) => state.user);
  const canRead = (user?.permissions ?? []).includes("shipment.read");
  const [row, setRow] = useState<ShipmentDetail | null>(null);
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
    getShipment(id)
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

  return (
    <AppShell
      currentHref="/sevkiyat"
      breadcrumbs={[
        { label: "Çalışma alanı", href: "/dashboard" },
        { label: "Sevkiyat", href: "/sevkiyat" },
        { label: row?.id.slice(0, 8) || "Kart" },
      ]}
      pageTitle="Sevkiyat"
      pageDescription="GET /shipments/{id}. API durumları Preparing, Loaded, InTransit. Teslim komutu ve POD bu dilimde yoktur."
      pageActions={
        <div className="flex flex-wrap gap-2">
          <Button variant="secondary" onClick={() => router.push("/sevkiyat")}>
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
      {!canRead ? (
        <PermissionDenied
          title="Sevkiyat bu oturumda görünmez"
          description="shipment.read yok. Bu kontrol yalnızca görünürlük içindir; gerçek yetki backend’dedir."
        />
      ) : denied ? (
        <PermissionDenied />
      ) : error ? (
        <ErrorState title="Sevkiyat yüklenemedi" description={error} onRetry={() => setReload((value) => value + 1)} />
      ) : loading || !row ? (
        <p className="text-sm text-slate-600">Yükleniyor…</p>
      ) : (
        <div className="space-y-4">
          <Alert tone="info" title="Teslim henüz yok">
            Rota complete sonrası belge InTransit kalır. Deliver / POD API’si yoktur; sahte teslim gösterilmez.
          </Alert>
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <KpiMetric
              label="Durum"
              value={row.status}
              icon={Truck}
              tone="amber"
              caption="Sunucu status"
            />
            <KpiMetric
              label="Kalem"
              value={String(row.itemCount)}
              unit="satır"
              icon={Package}
              tone="navy"
              caption="items uzunluğu"
            />
            <KpiMetric
              label="İrsaliye"
              value={row.deliveryNoteId.slice(0, 8) || "—"}
              icon={Layers}
              tone="teal"
              caption="deliveryNoteId"
            />
            <KpiMetric
              label="Müşteri"
              value={row.customerId.slice(0, 8) || "—"}
              icon={Layers}
              tone="navy"
              caption="customerId"
            />
          </div>
          <Card>
            <CardHeader>
              <CardTitle>Kalemler</CardTitle>
            </CardHeader>
            <CardBody>
              <div className="mb-3">
                <StatusBadge status={shipmentStatusKind(row.status)} label={row.status} />
              </div>
              <p className="mb-3 text-sm">
                İrsaliye:{" "}
                <Link className="font-semibold text-teal-600" href={`/sevkiyat/irsaliyeler/${row.deliveryNoteId}`}>
                  {row.deliveryNoteId.slice(0, 8)}
                </Link>
              </p>
              <DataTable
                columns={[
                  { id: "product", header: "Ürün", accessor: (line) => line.productId.slice(0, 8) },
                  {
                    id: "qty",
                    header: "Temel miktar",
                    accessor: (line) => (line.quantityBase === null ? "—" : String(line.quantityBase)),
                  },
                ]}
                rows={row.items}
                getRowId={(line) => line.id}
              />
            </CardBody>
          </Card>
        </div>
      )}
    </AppShell>
  );
}
