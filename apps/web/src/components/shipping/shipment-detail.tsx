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
import { Dialog } from "@/components/ui/dialog";
import { StatusBadge } from "@/components/ui/status-badge";
import { ApiError } from "@/lib/api/types";
import { userFacingMessage } from "@/lib/api/auth-client";
import { useSessionStore } from "@/lib/auth/session-store";
import { getShipment, shipmentStatusKind, type ShipmentDetail } from "@/lib/shipping/shipments";
import {
  arriveStop,
  assignRouteResources,
  completeDispatch,
  completeLoadVerification,
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
  prepareDispatchRun,
  replaceRouteStops,
  scanLoadVerificationPackage,
  startLoadVerification,
  type DispatchRun,
  type DriverRow,
  type LoadPlanSummary,
  type RoutePlanSummary,
  type ShipmentPackageRow,
  type VehicleRow,
} from "@/lib/shipping/dispatch";
import { getCustomer } from "@/lib/sales/customers";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { LoadPlanWizard } from "@/components/shipping/load-plan-wizard";

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
  const canLoadPlan = permissions.includes("shipment.load-plan");
  const canLoadVerify = permissions.includes("shipment.load-verify");
  const canVehicleFit = permissions.includes("shipment.vehicle-fit");
  const canPlanLock = permissions.includes("shipment.plan-lock");
  const canPlanOverride = permissions.includes("shipment.plan-override");
  const [row, setRow] = useState<ShipmentDetail | null>(null);
  const [dispatchRun, setDispatchRun] = useState<DispatchRun | null>(null);
  const [packages, setPackages] = useState<ShipmentPackageRow[]>([]);
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
  const [dispatchModalOpen, setDispatchModalOpen] = useState(false);
  const [verifyModalOpen, setVerifyModalOpen] = useState(false);
  const [scannedBarcodes, setScannedBarcodes] = useState<string[]>([]);
  const [barcodeInput, setBarcodeInput] = useState("");
  const [verifyError, setVerifyError] = useState<string | null>(null);

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

  const lockedLoadPlan = loadPlans.find((plan) => plan.status === "Locked");
  const activeRoutePlan =
    routePlans.find((plan) => plan.status === "Locked" || plan.status === "InProgress" || plan.status === "Draft" || plan.status === "Planned") ??
    routePlans[0] ??
    null;
  const isRouteLocked = activeRoutePlan?.status === "Locked";
  const isLoadPlanLocked = Boolean(lockedLoadPlan);
  const isShipmentLoaded = row?.status === "Loaded";
  const canPerformLoadVerification =
    canLoadVerify && isRouteLocked && isLoadPlanLocked && row?.status === "Preparing" && packages.length > 0;

  const nextStepText = !row
    ? ""
    : row.status === "Delivered"
    ? "Sevkiyat ve teslimat başarıyla tamamlandı."
    : row.status === "Cancelled"
    ? "Sevkiyat iptal edilmiştir."
    : dispatchRun?.status === "InTransit"
    ? "Durak teslimatlarını (POD) işleyin ve rotayı tamamlayın."
    : dispatchRun?.status === "Dispatched"
    ? "Aracın yola çıkışını kaydedin (Yola çık)."
    : dispatchRun?.status === "Prepared"
    ? "Seferi onaylayın (Seferi onayla)."
    : row.status === "Loaded" && !dispatchRun
    ? "Sefer hazırlama işlemini başlatın (Sefer hazırla)."
    : lockedLoadPlan && row.status === "Preparing"
    ? "Yükleme doğrulamasını (Loaded) tamamlayın."
    : isRouteLocked && !lockedLoadPlan
    ? "Yük planı sihirbazı ile araç atayıp planı kilitleyin."
    : activeRoutePlan && activeRoutePlan.status !== "Locked"
    ? "Rota kaynaklarını atayın, planlayın ve rotayı kilitleyin."
    : "Rota ve durak kaydı oluşturun.";

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
          <div className="rounded-lg border border-teal-500/20 bg-teal-500/5 p-3 text-sm flex items-center justify-between gap-2">
            <div>
              <span className="font-semibold text-teal-800">Sıradaki Adım: </span>
              <span className="text-slate-700">{nextStepText}</span>
            </div>
            <StatusBadge status={shipmentStatusKind(row.status)} label={row.status} />
          </div>

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
              {row.status === "Preparing" && canLoadPlan && row.rowVersion !== null ? (
                <LoadPlanWizard
                  shipmentId={row.id}
                  shipmentRowVersion={row.rowVersion}
                  packages={packages}
                  routePlan={activeRoutePlan}
                  vehicles={vehicles}
                  canCreate={canLoadPlan}
                  canFit={canVehicleFit}
                  canLock={canPlanLock}
                  canOverride={canPlanOverride}
                  onChanged={() => setReload((value) => value + 1)}
                />
              ) : null}
              {activeRoutePlan && canRoute && activeRoutePlan.rowVersion !== null ? (
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
                        assignRouteResources(activeRoutePlan.id, activeRoutePlan.rowVersion as number, vehicleId, driverId),
                      )
                    }
                  >
                    Kaynak ata
                  </Button>
                  <Button
                    variant="secondary"
                    loading={acting}
                    onClick={() => void run(() => planRoute(activeRoutePlan.id, activeRoutePlan.rowVersion as number))}
                  >
                    Planla
                  </Button>
                  {canLockRoute ? (
                    <Button
                      loading={acting}
                      onClick={() => void run(() => lockRoute(activeRoutePlan.id, activeRoutePlan.rowVersion as number))}
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
                  <div className="flex flex-wrap items-center gap-2">
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
                      <div className="flex flex-wrap items-center gap-2">
                        <Button
                          variant="secondary"
                          loading={acting}
                          disabled={
                            acting ||
                            dispatchRun.stops.some((stop) => stop.status === "Pending" || stop.status === "Arrived")
                          }
                          onClick={() => void run(() => completeDispatch(dispatchRun.id, dispatchRun.rowVersion as number))}
                        >
                          Rotayı tamamla
                        </Button>
                        {dispatchRun.stops.some((stop) => stop.status === "Pending" || stop.status === "Arrived") ? (
                          <span className="text-xs text-amber-700">
                            (Bekleyen {dispatchRun.stops.filter((s) => s.status === "Pending" || s.status === "Arrived").length} durak var)
                          </span>
                        ) : null}
                      </div>
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
                <div className="space-y-3">
                  <p className="text-slate-600">Bu sevkiyata bağlı aktif sefer yok.</p>
                  <div className="flex flex-wrap gap-2">
                    {canPerformLoadVerification && row.rowVersion !== null ? (
                      <Button
                        variant="secondary"
                        loading={acting}
                        onClick={() => setVerifyModalOpen(true)}
                      >
                        Yüklemeyi tamamla (Loaded)
                      </Button>
                    ) : null}

                    {canDispatch && isRouteLocked && isLoadPlanLocked ? (
                      <Button
                        loading={acting}
                        disabled={
                          acting ||
                          !isShipmentLoaded ||
                          row.rowVersion === null ||
                          activeRoutePlan.rowVersion === null ||
                          !lockedLoadPlan ||
                          lockedLoadPlan.rowVersion === null
                        }
                        onClick={() => setDispatchModalOpen(true)}
                      >
                        Sefer hazırla
                      </Button>
                    ) : null}
                  </div>

                  {!isRouteLocked ? (
                    <p className="text-xs text-amber-700">Sefer hazırlamak için rota kilitlenmelidir.</p>
                  ) : null}
                  {isRouteLocked && !isLoadPlanLocked ? (
                    <p className="text-xs text-amber-700">Sefer hazırlamak için yük planı oluşturulup kilitlenmelidir.</p>
                  ) : null}
                  {isRouteLocked && isLoadPlanLocked && row.status === "Preparing" ? (
                    <p className="text-xs text-amber-700">Sefer hazırlamadan önce yükleme doğrulaması (Loaded) tamamlanmalıdır.</p>
                  ) : null}
                </div>
              )}
            </CardBody>
          </Card>

          <Dialog
            open={dispatchModalOpen}
            onOpenChange={(next) => {
              if (!acting) setDispatchModalOpen(next);
            }}
            title="Sefer hazırlama"
            description="Kilitli rota ve yük planı üzerinden sefer (DispatchRun) oluşturulur."
            footer={
              <div className="flex justify-end gap-2">
                <Button variant="secondary" disabled={acting} onClick={() => setDispatchModalOpen(false)}>
                  Vazgeç
                </Button>
                <Button
                  loading={acting}
                  onClick={() =>
                    void run(async () => {
                      if (
                        !activeRoutePlan ||
                        !lockedLoadPlan ||
                        row.rowVersion === null ||
                        activeRoutePlan.rowVersion === null ||
                        lockedLoadPlan.rowVersion === null
                      ) {
                        throw new Error("Eksik versiyon veya plan.");
                      }
                      const selectedVehicle = lockedLoadPlan.vehicleId || activeRoutePlan.vehicleId || vehicleId;
                      const selectedDriver = activeRoutePlan.driverId || driverId;
                      if (!selectedVehicle) {
                        throw new Error("Sefer için araç seçilmelidir.");
                      }
                      if (!selectedDriver) {
                        throw new Error("Sefer için şoför seçilmelidir.");
                      }
                      const createdRun = await prepareDispatchRun(activeRoutePlan.id, {
                        shipmentId: row.id,
                        loadPlanId: lockedLoadPlan.id,
                        vehicleId: selectedVehicle,
                        driverId: selectedDriver,
                        stops: activeRoutePlan.stops.map((s) => ({ routeStopId: s.id, sequenceNo: s.sequenceNo })),
                        expectedLoadPlanRowVersion: lockedLoadPlan.rowVersion,
                        expectedShipmentRowVersion: row.rowVersion,
                        expectedRoutePlanRowVersion: activeRoutePlan.rowVersion,
                      });
                      setDispatchRun(createdRun);
                      setDispatchModalOpen(false);
                    })
                  }
                >
                  Seferi oluştur
                </Button>
              </div>
            }
          >
            <div className="space-y-2 text-sm">
              <div className="rounded-lg border border-slate-200 bg-slate-50 p-3 space-y-1">
                <p>
                  <strong>Araç:</strong>{" "}
                  {vehicles.find(
                    (v) => v.id === (lockedLoadPlan?.vehicleId || activeRoutePlan?.vehicleId || vehicleId),
                  )?.plateNumber || "Seçilmedi"}
                </p>
                <p>
                  <strong>Şoför:</strong>{" "}
                  {drivers.find((d) => d.id === (activeRoutePlan?.driverId || driverId))?.fullName || "Seçilmedi"}
                </p>
                <p>
                  <strong>Rota:</strong> {activeRoutePlan?.status} ({activeRoutePlan?.stops.length ?? 0} durak)
                </p>
                <p>
                  <strong>Yük planı:</strong> {lockedLoadPlan?.id.slice(0, 8)} ({lockedLoadPlan?.status})
                </p>
                <p>
                  <strong>Paketler:</strong> {packages.length} paket
                </p>
                <p>
                  <strong>Sevkiyat durumu:</strong> {row.status}
                </p>
              </div>
            </div>
          </Dialog>

          <Dialog
            open={verifyModalOpen}
            onOpenChange={(next) => {
              if (!acting) {
                setVerifyModalOpen(next);
                if (!next) {
                  setScannedBarcodes([]);
                  setBarcodeInput("");
                  setVerifyError(null);
                }
              }
            }}
            title="Yükleme doğrulaması"
            description="Paketlerin araca fiilen yüklendiğini barkod ile tek tek veya toplu olarak doğrulayın."
            footer={
              <div className="flex justify-end gap-2">
                <Button
                  variant="secondary"
                  disabled={acting}
                  onClick={() => {
                    setVerifyModalOpen(false);
                    setScannedBarcodes([]);
                    setBarcodeInput("");
                    setVerifyError(null);
                  }}
                >
                  Vazgeç
                </Button>
                <Button
                  loading={acting}
                  onClick={() =>
                    void run(async () => {
                      if (!lockedLoadPlan || lockedLoadPlan.rowVersion === null) {
                        throw new Error("Kilitli yük planı bulunamadı.");
                      }
                      const activePkgs = packages.filter((p) => p.status !== "Cancelled");
                      const session = await startLoadVerification(lockedLoadPlan.id, lockedLoadPlan.rowVersion);
                      let currentSessionRowVersion = session.rowVersion ?? 1;
                      for (const pkg of activePkgs) {
                        const barcode = pkg.packageCode || pkg.id;
                        await scanLoadVerificationPackage(session.id, currentSessionRowVersion, barcode);
                        currentSessionRowVersion++;
                      }
                      await completeLoadVerification(session.id, currentSessionRowVersion);
                      setVerifyModalOpen(false);
                      setScannedBarcodes([]);
                      setBarcodeInput("");
                    })
                  }
                >
                  Yüklemeyi onayla (Loaded)
                </Button>
              </div>
            }
          >
            <div className="space-y-3 text-sm">
              {verifyError ? <Alert tone="danger" title="Doğrulama Hatası">{verifyError}</Alert> : null}
              
              <div className="flex items-end gap-2">
                <div className="flex-1">
                  <Input
                    label="Barkod / Paket Kodu Okutun"
                    placeholder="Paket barkodunu okutun veya yazın..."
                    value={barcodeInput}
                    onChange={(e) => setBarcodeInput(e.target.value)}
                    onKeyDown={(e) => {
                      if (e.key === "Enter") {
                        e.preventDefault();
                        const trimmed = barcodeInput.trim();
                        if (!trimmed) return;
                        const match = packages.find(
                          (p) => p.status !== "Cancelled" && (p.packageCode === trimmed || p.id === trimmed)
                        );
                        if (!match) {
                          setVerifyError(`"${trimmed}" kodlu paket bu sevkiyatta bulunamadı.`);
                        } else {
                          setVerifyError(null);
                          if (!scannedBarcodes.includes(match.id)) {
                            setScannedBarcodes((prev) => [...prev, match.id]);
                          }
                          setBarcodeInput("");
                        }
                      }
                    }}
                  />
                </div>
                <Button
                  type="button"
                  variant="secondary"
                  onClick={() => {
                    const trimmed = barcodeInput.trim();
                    if (!trimmed) return;
                    const match = packages.find(
                      (p) => p.status !== "Cancelled" && (p.packageCode === trimmed || p.id === trimmed)
                    );
                    if (!match) {
                      setVerifyError(`"${trimmed}" kodlu paket bu sevkiyatta bulunamadı.`);
                    } else {
                      setVerifyError(null);
                      if (!scannedBarcodes.includes(match.id)) {
                        setScannedBarcodes((prev) => [...prev, match.id]);
                      }
                      setBarcodeInput("");
                    }
                  }}
                >
                  Okut
                </Button>
                <Button
                  type="button"
                  variant="secondary"
                  onClick={() => {
                    const activePkgs = packages.filter((p) => p.status !== "Cancelled");
                    setScannedBarcodes(activePkgs.map((p) => p.id));
                    setVerifyError(null);
                  }}
                >
                  Tümünü Doğrula
                </Button>
              </div>

              <div className="rounded-lg border border-slate-200 divide-y divide-slate-100 max-h-48 overflow-y-auto">
                {packages
                  .filter((p) => p.status !== "Cancelled")
                  .map((pkg) => {
                    const isScanned = scannedBarcodes.includes(pkg.id);
                    return (
                      <div key={pkg.id} className="flex items-center justify-between p-2 text-xs">
                        <div>
                          <span className="font-mono font-medium text-slate-800">
                            {pkg.packageCode || pkg.id.slice(0, 8)}
                          </span>
                          <span className="text-slate-500 ml-2">Miktar: {pkg.quantityBase}</span>
                        </div>
                        {isScanned ? (
                          <span className="px-2 py-0.5 rounded bg-emerald-100 text-emerald-800 font-medium">
                            Doğrulandı
                          </span>
                        ) : (
                          <span className="px-2 py-0.5 rounded bg-slate-100 text-slate-600 font-medium">
                            Bekliyor
                          </span>
                        )}
                      </div>
                    );
                  })}
              </div>

              <p className="text-xs text-slate-500">
                Toplam {packages.filter((p) => p.status !== "Cancelled").length} paketten {scannedBarcodes.length} tanesi tarandı.
              </p>
            </div>
          </Dialog>
        </div>
      )}
    </AppShell>
  );
}

