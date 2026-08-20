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
import {
  arriveStop,
  assignRouteResources,
  completeDispatch,
  confirmDispatch,
  createRoutePlan,
  createShipmentPackage,
  deliverStop,
  departDispatch,
  listDispatchRuns,
  listDrivers,
  listLoadPlans,
  listRoutePlans,
  listShipmentPackages,
  listVehicles,
  lockRoute,
  planRoute,
  replaceRouteStops,
  type DispatchRun,
  type DriverRow,
  type LoadPlanSummary,
  type RoutePlanSummary,
  type VehicleRow,
} from "@/lib/shipping/dispatch";
import { getCustomer } from "@/lib/sales/customers";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";

export function ShipmentDetailBoard({ id }: { id: string }) {
  const router = useRouter();
  const user = useSessionStore((state) => state.user);
  const permissions = user?.permissions ?? [];
  const canRead = permissions.includes("shipment.read");
  const canExecute = permissions.includes("shipment.route-execute");
  const canDispatch = permissions.includes("shipment.dispatch");
  const canDepart = permissions.includes("shipment.depart");
  const canRoute = permissions.includes("shipment.route-manage");
  const canLockRoute = permissions.includes("shipment.route-lock");
  const canPackage = permissions.includes("shipment.package-manage");
  const [row, setRow] = useState<ShipmentDetail | null>(null);
  const [dispatchRun, setDispatchRun] = useState<DispatchRun | null>(null);
  const [packages, setPackages] = useState<Array<{ id: string; status: string }>>([]);
  const [loadPlans, setLoadPlans] = useState<LoadPlanSummary[]>([]);
  const [routePlans, setRoutePlans] = useState<RoutePlanSummary[]>([]);
  const [vehicles, setVehicles] = useState<VehicleRow[]>([]);
  const [drivers, setDrivers] = useState<DriverRow[]>([]);
  const [vehicleId, setVehicleId] = useState("");
  const [driverId, setDriverId] = useState("");
  const [recipient, setRecipient] = useState("");
  const [acting, setActing] = useState(false);
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
    Promise.all([
      getShipment(id),
      listDispatchRuns(id).catch(() => []),
      listShipmentPackages(id).catch(() => []),
      listLoadPlans(id).catch(() => []),
      listRoutePlans(id).catch(() => []),
      listVehicles().catch(() => []),
      listDrivers().catch(() => []),
    ])
      .then(([result, runs, packageRows, planRows, routeRows, vehicleRows, driverRows]) => {
        if (cancelled) return;
        setRow(result);
        setDispatchRun(runs[0] ?? null);
        setPackages(packageRows);
        setLoadPlans(planRows);
        setRoutePlans(routeRows);
        setVehicles(vehicleRows);
        setDrivers(driverRows.filter((item) => item.isActive));
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

  async function run(action: () => Promise<unknown>) {
    setActing(true);
    setError(null);
    try {
      await action();
      setReload((value) => value + 1);
    } catch (caught) {
      setError(userFacingMessage(caught));
    } finally {
      setActing(false);
    }
  }

  return (
    <AppShell
      currentHref="/sevkiyat"
      breadcrumbs={[
        { label: "Çalışma alanı", href: "/dashboard" },
        { label: "Sevkiyat", href: "/sevkiyat" },
        { label: row?.id.slice(0, 8) || "Kart" },
      ]}
      pageTitle="Sevkiyat"
      pageDescription="GET /shipments/{id}. Teslim: Arrived durakta recipient ile POST .../deliver. POD imza dosyası yok; metin kanıtı."
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
          {row.status !== "Delivered" && row.status !== "PartiallyDelivered" ? (
            <Alert tone="info" title="Teslim POD ile yazılır">
              Complete, teslim kanıtı olan duraklarda shipment’ı Delivered yapar. Kanıt yoksa InTransit kalır.
            </Alert>
          ) : null}
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
          <Card>
            <CardHeader>
              <CardTitle>Paket / yük planı / sefer</CardTitle>
            </CardHeader>
            <CardBody className="space-y-3 text-sm">
              <p>
                Paket: {packages.length} · Rota: {routePlans.map((plan) => plan.status).join(", ") || "yok"} ·
                Yük planı: {loadPlans.map((plan) => plan.status).join(", ") || "yok"}
              </p>
              {row.status === "Preparing" && canRoute && row.rowVersion !== null ? (
                <div className="flex flex-wrap gap-2">
                  <Button
                    loading={acting}
                    onClick={() =>
                      void run(async () => {
                        const plan = await createRoutePlan(row.id, row.rowVersion as number);
                        const customer = await getCustomer(row.customerId);
                        const address =
                          customer.addresses.find((item) => item.isDefault && item.isActive) ??
                          customer.addresses.find((item) => item.isActive);
                        if (!address || plan.rowVersion === null) {
                          throw new Error("Müşteri teslim adresi yok; durak yazılamaz.");
                        }
                        await replaceRouteStops(plan.id, plan.rowVersion, [
                          { sequenceNo: 1, customerId: row.customerId, addressId: address.id },
                        ]);
                      })
                    }
                  >
                    Rota + durak
                  </Button>
                  {canPackage ? (
                    <Button
                      variant="secondary"
                      loading={acting}
                      onClick={() =>
                        void run(async () => {
                          for (const item of row.items) {
                            if (item.quantityBase === null) continue;
                            await createShipmentPackage(row.id, {
                              shipmentItemId: item.id,
                              quantityBase: item.quantityBase,
                            });
                          }
                        })
                      }
                    >
                      Kalemlerden paket
                    </Button>
                  ) : null}
                </div>
              ) : null}
              {routePlans[0] && canRoute && routePlans[0].rowVersion !== null ? (
                <div className="grid gap-2 md:grid-cols-2">
                  <Select
                    label="Araç"
                    value={vehicleId}
                    onChange={(event) => setVehicleId(event.target.value)}
                    options={[
                      { value: "", label: "Seçin" },
                      ...vehicles.map((item) => ({
                        value: item.id,
                        label: `${item.plateNumber} (${item.status})`,
                      })),
                    ]}
                  />
                  <Select
                    label="Şoför"
                    value={driverId}
                    onChange={(event) => setDriverId(event.target.value)}
                    options={[
                      { value: "", label: "Seçin" },
                      ...drivers.map((item) => ({ value: item.id, label: item.fullName })),
                    ]}
                  />
                  <Button
                    variant="secondary"
                    loading={acting}
                    onClick={() =>
                      void run(() =>
                        assignRouteResources(routePlans[0].id, routePlans[0].rowVersion as number, vehicleId, driverId),
                      )
                    }
                  >
                    Kaynak ata
                  </Button>
                  <Button
                    variant="secondary"
                    loading={acting}
                    onClick={() => void run(() => planRoute(routePlans[0].id, routePlans[0].rowVersion as number))}
                  >
                    Planla
                  </Button>
                  {canLockRoute ? (
                    <Button
                      loading={acting}
                      onClick={() => void run(() => lockRoute(routePlans[0].id, routePlans[0].rowVersion as number))}
                    >
                      Rotayı kilitle
                    </Button>
                  ) : null}
                </div>
              ) : null}
              {dispatchRun ? (
                <div className="space-y-2">
                  <p>
                    Sefer {dispatchRun.id.slice(0, 8)} · {dispatchRun.status}
                  </p>
                  <div className="flex flex-wrap gap-2">
                    {canDispatch && dispatchRun.status === "Prepared" && dispatchRun.rowVersion !== null ? (
                      <Button
                        loading={acting}
                        onClick={() => void run(() => confirmDispatch(dispatchRun.id, dispatchRun.rowVersion as number))}
                      >
                        Seferi onayla
                      </Button>
                    ) : null}
                    {canDepart && dispatchRun.status === "Dispatched" && dispatchRun.rowVersion !== null ? (
                      <Button
                        loading={acting}
                        onClick={() => void run(() => departDispatch(dispatchRun.id, dispatchRun.rowVersion as number))}
                      >
                        Yola çık
                      </Button>
                    ) : null}
                    {canExecute && dispatchRun.status === "InTransit" && dispatchRun.rowVersion !== null
                      ? dispatchRun.stops
                          .filter((stop) => stop.status === "Pending")
                          .slice(0, 1)
                          .map((stop) => (
                            <Button
                              key={stop.routeStopId}
                              variant="secondary"
                              loading={acting}
                              onClick={() =>
                                void run(() =>
                                  arriveStop(dispatchRun.id, stop.routeStopId, dispatchRun.rowVersion as number),
                                )
                              }
                            >
                              Durağa var #{stop.sequenceNo}
                            </Button>
                          ))
                      : null}
                    {canExecute && dispatchRun.status === "InTransit" && dispatchRun.rowVersion !== null ? (
                      <Button
                        variant="secondary"
                        loading={acting}
                        onClick={() => void run(() => completeDispatch(dispatchRun.id, dispatchRun.rowVersion as number))}
                      >
                        Rotayı tamamla
                      </Button>
                    ) : null}
                  </div>
                  <DataTable
                    columns={[
                      { id: "seq", header: "Sıra", accessor: (stop) => String(stop.sequenceNo) },
                      { id: "st", header: "Durum", accessor: (stop) => stop.status },
                      { id: "pod", header: "Teslim alan", accessor: (stop) => stop.proofRecipient || "—" },
                    ]}
                    rows={dispatchRun.stops}
                    getRowId={(stop) => stop.routeStopId}
                  />
                  {canExecute && dispatchRun.rowVersion !== null
                    ? dispatchRun.stops
                        .filter((stop) => stop.status === "Arrived")
                        .map((stop) => (
                          <div key={stop.routeStopId} className="flex flex-wrap items-end gap-2">
                            <Input
                              label={`Teslim alan #${stop.sequenceNo}`}
                              value={recipient}
                              onChange={(event) => setRecipient(event.target.value)}
                            />
                            <Button
                              onClick={() => {
                                void deliverStop(dispatchRun.id, stop.routeStopId, {
                                  recipientName: recipient,
                                  rowVersion: dispatchRun.rowVersion as number,
                                })
                                  .then((next) => {
                                    setDispatchRun(next);
                                    setReload((value) => value + 1);
                                  })
                                  .catch((caught) => setError(userFacingMessage(caught)));
                              }}
                            >
                              Teslim yaz
                            </Button>
                          </div>
                        ))
                    : null}
                </div>
              ) : (
                <p>Bu sevkiyata bağlı sefer yok.</p>
              )}
            </CardBody>
          </Card>
        </div>
      )}
    </AppShell>
  );
}
