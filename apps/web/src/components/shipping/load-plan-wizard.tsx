"use client";

import { useState } from "react";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Dialog } from "@/components/ui/dialog";
import { Select } from "@/components/ui/select";
import { StatusBadge } from "@/components/ui/status-badge";
import { userFacingMessage } from "@/lib/api/auth-client";
import {
  assignLoadPlanVehicle,
  createLoadPlan,
  evaluateVehicleFit,
  getLoadPlan,
  lockLoadPlan,
  physicalFromSnapshot,
  validateLoadPlan,
  type LoadPlanSummary,
  type LoadPlanValidationRow,
  type RoutePlanSummary,
  type ShipmentPackageRow,
  type VehicleFitCandidate,
  type VehicleRow,
} from "@/lib/shipping/dispatch";

export function LoadPlanWizard({
  shipmentId,
  shipmentRowVersion,
  packages,
  routePlan,
  vehicles,
  canCreate,
  canFit,
  canLock,
  canOverride,
  onChanged,
}: {
  shipmentId: string;
  shipmentRowVersion: number;
  packages: ShipmentPackageRow[];
  routePlan: RoutePlanSummary | null;
  vehicles: VehicleRow[];
  canCreate: boolean;
  canFit: boolean;
  canLock: boolean;
  canOverride: boolean;
  onChanged: () => void;
}) {
  const [open, setOpen] = useState(false);
  const [acting, setActing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [plan, setPlan] = useState<LoadPlanSummary | null>(null);
  const [candidates, setCandidates] = useState<VehicleFitCandidate[]>([]);
  const [selected, setSelected] = useState("");
  const [results, setResults] = useState<LoadPlanValidationRow[]>([]);
  const [lockConfirm, setLockConfirm] = useState(false);

  const usablePackages = packages.filter((pkg) => pkg.status !== "Cancelled");
  const fallbackStopId = routePlan?.stops[0]?.id ?? "";
  const missingPhysical = usablePackages.some((pkg) => !physicalFromSnapshot(pkg.physicalSnapshot) || pkg.quantityBase === null);
  const ready =
    Boolean(routePlan && fallbackStopId && usablePackages.length > 0 && !missingPhysical && shipmentRowVersion);

  async function run(action: () => Promise<void>) {
    setActing(true);
    setError(null);
    try {
      await action();
      onChanged();
    } catch (caught) {
      setError(userFacingMessage(caught));
    } finally {
      setActing(false);
    }
  }

  function plate(vehicleId: string): string {
    return vehicles.find((item) => item.id === vehicleId)?.plateNumber || vehicleId.slice(0, 8);
  }

  const openHard = results.some((row) => row.severity === "HardError" && row.resolutionStatus === "Open");
  const openWarnings = results.filter((row) => row.severity === "Warning" && row.resolutionStatus === "Open");
  const selectable = candidates.filter((item) => item.candidateStatus !== "Rejected" && item.vehicleCapacityId);

  if (!canCreate) {
    return null;
  }

  return (
    <>
      <Button
        variant="secondary"
        onClick={() => {
          setOpen(true);
          setError(null);
        }}
      >
        Yük planı
      </Button>
      <Dialog
        open={open}
        onOpenChange={(next) => {
          if (!acting) setOpen(next);
        }}
        title="Yük planı"
        description="Paketlerden taslak birim, araç adayı, doğrulama, kilit. FFD optimalite iddiası yok."
        footer={
          <div className="flex justify-end gap-2">
            <Button variant="secondary" disabled={acting} onClick={() => setOpen(false)}>
              Kapat
            </Button>
          </div>
        }
      >
        <div className="space-y-3 text-sm">
          <p>
            Paket {usablePackages.length} · Rota {routePlan?.status || "yok"} · Durak{" "}
            {routePlan?.stops.length ?? 0}
          </p>
          {!ready ? (
            <Alert tone="warning" title="Önkoşul eksik">
              Kilitlenebilir plan için rota durak, paket ve sunucu fiziksel ölçü gerekir. Ölçü uydurulmaz.
            </Alert>
          ) : null}
          {error ? (
            <Alert tone="danger" title="Komut başarısız">
              {error}
            </Alert>
          ) : null}
          {plan ? (
            <p>
              Plan {plan.id.slice(0, 8)} · {plan.status} · {plan.feasibilityStatus || "—"}
            </p>
          ) : null}
          <div className="flex flex-wrap gap-2">
            <Button
              disabled={!ready || acting}
              loading={acting}
              onClick={() =>
                void run(async () => {
                  if (!routePlan) return;
                  setPlan(
                    await createLoadPlan(shipmentId, {
                      routePlanId: routePlan.id,
                      expectedRoutePlanVersion: routePlan.version,
                      expectedShipmentRowVersion: shipmentRowVersion,
                      packages: usablePackages,
                      fallbackStopId,
                    }),
                  );
                  setCandidates([]);
                  setResults([]);
                  setSelected("");
                })
              }
            >
              Taslak oluştur
            </Button>
            {canFit ? (
              <Button
                variant="secondary"
                disabled={!plan || plan.rowVersion === null || acting}
                loading={acting}
                onClick={() =>
                  void run(async () => {
                    if (!plan || plan.rowVersion === null) return;
                    setCandidates(await evaluateVehicleFit(shipmentId, plan.id, plan.rowVersion));
                    setPlan(await getLoadPlan(plan.id));
                  })
                }
              >
                Adayları değerlendir
              </Button>
            ) : null}
          </div>
          {candidates.length > 0 ? (
            <div className="space-y-2">
              <ul className="space-y-1">
                {candidates.map((item) => (
                  <li key={`${item.vehicleId}-${item.vehicleCapacityId ?? "none"}`}>
                    {plate(item.vehicleId)} · {item.candidateStatus}
                    {item.rejectionCode ? ` · ${item.rejectionCode}` : ""}
                    {item.fitScore !== null ? ` · skor ${item.fitScore}` : ""}
                  </li>
                ))}
              </ul>
              <Select
                label="Aday araç"
                value={selected}
                onChange={(event) => setSelected(event.target.value)}
                options={[
                  { value: "", label: "Seçin" },
                  ...selectable.map((item) => ({
                    value: `${item.vehicleId}|${item.vehicleCapacityId}`,
                    label: `${plate(item.vehicleId)} (${item.candidateStatus})`,
                  })),
                ]}
              />
              <Button
                variant="secondary"
                disabled={!plan || plan.rowVersion === null || !selected || acting}
                loading={acting}
                onClick={() =>
                  void run(async () => {
                    if (!plan || plan.rowVersion === null) return;
                    const [vehicleId, vehicleCapacityId] = selected.split("|");
                    setPlan(await assignLoadPlanVehicle(plan.id, plan.rowVersion, vehicleId, vehicleCapacityId));
                  })
                }
              >
                Aracı ata
              </Button>
            </div>
          ) : null}
          {plan?.vehicleId ? (
            <Button
              variant="secondary"
              disabled={plan.rowVersion === null || acting}
              loading={acting}
              onClick={() =>
                void run(async () => {
                  if (plan.rowVersion === null) return;
                  const next = await validateLoadPlan(plan.id, plan.rowVersion);
                  setPlan(next.plan);
                  setResults(next.results);
                })
              }
            >
              Doğrula
            </Button>
          ) : null}
          {results.length > 0 ? (
            <ul className="space-y-1">
              {results.map((row) => (
                <li key={row.id} className="flex items-center gap-2">
                  <StatusBadge
                    status={row.severity === "HardError" ? "critical" : row.severity === "Warning" ? "pending" : "info"}
                    label={row.code}
                  />
                  <span>{row.message}</span>
                </li>
              ))}
            </ul>
          ) : null}
          {canLock && plan && plan.status !== "Locked" ? (
            <Button
              disabled={acting || plan.rowVersion === null || results.length === 0 || openHard}
              onClick={() => setLockConfirm(true)}
            >
              Kilitle
            </Button>
          ) : null}
          {openHard ? (
            <Alert tone="danger" title="Kilitlenemez">
              Açık hard error var. Override yok; plan yeniden düzenlenir.
            </Alert>
          ) : null}
          {openWarnings.length > 0 && !canOverride ? (
            <Alert tone="warning" title="Uyarı açık">
              shipment.plan-override yok; kilit için uyarı kapanmalı.
            </Alert>
          ) : null}
        </div>
      </Dialog>
      <Dialog
        open={lockConfirm}
        onOpenChange={(next) => {
          if (!acting) setLockConfirm(next);
        }}
        title="Yük planını kilitle"
        description="Depo onayı gerekir. Hard error varken kilitlenmez."
        footer={
          <div className="flex justify-end gap-2">
            <Button variant="secondary" disabled={acting} onClick={() => setLockConfirm(false)}>
              Vazgeç
            </Button>
            <Button
              loading={acting}
              disabled={openHard || (openWarnings.length > 0 && !canOverride)}
              onClick={() =>
                void run(async () => {
                  if (!plan || plan.rowVersion === null) return;
                  if (openWarnings.length > 0 && !canOverride) {
                    throw new Error("Uyarı override yetkisi yok.");
                  }
                  setPlan(
                    await lockLoadPlan(
                      plan.id,
                      plan.rowVersion,
                      openWarnings.map((row) => ({
                        validationResultId: row.id,
                        action: "override",
                        reason: "Depo sorumlusu onayı",
                      })),
                    ),
                  );
                  setLockConfirm(false);
                  setOpen(false);
                })
              }
            >
              Onayla ve kilitle
            </Button>
          </div>
        }
      />
    </>
  );
}
