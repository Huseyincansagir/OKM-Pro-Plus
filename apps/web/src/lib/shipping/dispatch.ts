import { apiRequest } from "@/lib/api/client";

export type DispatchStop = {
  routeStopId: string;
  sequenceNo: number;
  status: string;
  proofRecipient: string | null;
};

export type DispatchRun = {
  id: string;
  shipmentId: string;
  loadPlanId: string;
  routePlanId: string;
  status: string;
  rowVersion: number | null;
  stops: DispatchStop[];
};

function asRecord(value: unknown): Record<string, unknown> {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : {};
}

function asFiniteNumber(value: unknown): number | null {
  return typeof value === "number" && Number.isFinite(value) ? value : null;
}

export function mapDispatchRun(raw: unknown): DispatchRun {
  const record = asRecord(raw);
  const stops = Array.isArray(record.stops) ? record.stops : [];
  return {
    id: String(record.id ?? ""),
    shipmentId: String(record.shipmentId ?? ""),
    loadPlanId: String(record.loadPlanId ?? ""),
    routePlanId: String(record.routePlanId ?? ""),
    status: String(record.status ?? ""),
    rowVersion: asFiniteNumber(record.rowVersion),
    stops: stops.map((item) => {
      const stop = asRecord(item);
      return {
        routeStopId: String(stop.routeStopId ?? ""),
        sequenceNo: typeof stop.sequenceNo === "number" ? stop.sequenceNo : 0,
        status: String(stop.status ?? ""),
        proofRecipient: typeof stop.proofRecipient === "string" ? stop.proofRecipient : null,
      };
    }),
  };
}

export async function listDispatchRuns(shipmentId: string): Promise<DispatchRun[]> {
  const raw = await apiRequest<unknown>({ path: `/shipments/${shipmentId}/dispatch-runs`, method: "GET" });
  return Array.isArray(raw) ? raw.map(mapDispatchRun) : [];
}

export type ShipmentPackageRow = {
  id: string;
  shipmentItemId: string;
  routeStopId: string | null;
  quantityBase: number | null;
  status: string;
  physicalSnapshot: string;
};

export type PackagePhysical = {
  lengthMm: number;
  widthMm: number;
  heightMm: number;
  tareWeightKg: number;
  grossWeightKg: number;
  volumeM3: number;
  maxStackCount: number | null;
};

export function mapShipmentPackage(raw: unknown): ShipmentPackageRow {
  const record = asRecord(raw);
  return {
    id: String(record.id ?? ""),
    shipmentItemId: String(record.shipmentItemId ?? ""),
    routeStopId: typeof record.routeStopId === "string" ? record.routeStopId : null,
    quantityBase: asFiniteNumber(record.quantityBase),
    status: String(record.status ?? ""),
    physicalSnapshot: typeof record.physicalSnapshot === "string" ? record.physicalSnapshot : "{}",
  };
}

export function physicalFromSnapshot(snapshot: string): PackagePhysical | null {
  try {
    const record = asRecord(JSON.parse(snapshot) as unknown);
    const lengthMm = asFiniteNumber(record.lengthMm) ?? 0;
    const widthMm = asFiniteNumber(record.widthMm) ?? 0;
    const heightMm = asFiniteNumber(record.heightMm) ?? 0;
    const tareWeightKg = asFiniteNumber(record.tareWeightKg) ?? 0;
    const grossWeightKg =
      asFiniteNumber(record.grossWeightKg) ?? asFiniteNumber(record.netWeightKg) ?? 0;
    const volumeM3 = asFiniteNumber(record.volumeM3) ?? 0;
    const maxStackCount = asFiniteNumber(record.maxStackCount);
    if (lengthMm <= 0 || widthMm <= 0 || heightMm <= 0 || volumeM3 <= 0 || grossWeightKg < tareWeightKg) {
      return null;
    }
    return {
      lengthMm,
      widthMm,
      heightMm,
      tareWeightKg,
      grossWeightKg,
      volumeM3,
      maxStackCount: maxStackCount !== null && maxStackCount > 0 ? maxStackCount : null,
    };
  } catch {
    return null;
  }
}

export async function listShipmentPackages(shipmentId: string): Promise<ShipmentPackageRow[]> {
  const raw = await apiRequest<unknown>({ path: `/shipments/${shipmentId}/packages`, method: "GET" });
  if (!Array.isArray(raw)) {
    return [];
  }
  return raw.map(mapShipmentPackage);
}

export async function listLoadPlans(shipmentId: string): Promise<LoadPlanSummary[]> {
  const raw = await apiRequest<unknown>({ path: `/shipments/${shipmentId}/load-plans`, method: "GET" });
  if (!Array.isArray(raw)) {
    return [];
  }
  return raw.map(mapLoadPlan);
}

export type RoutePlanSummary = {
  id: string;
  status: string;
  version: number;
  rowVersion: number | null;
  vehicleId: string | null;
  driverId: string | null;
  stops: Array<{ id: string; sequenceNo: number; customerId: string; addressId: string; status: string }>;
};

export type LoadPlanSummary = {
  id: string;
  status: string;
  feasibilityStatus: string;
  routePlanId: string;
  vehicleId: string | null;
  vehicleCapacityId: string | null;
  rowVersion: number | null;
  inputSnapshotHash: string | null;
};

export type LoadPlanValidationRow = {
  id: string;
  severity: string;
  code: string;
  message: string;
  resolutionStatus: string;
};

export type VehicleFitCandidate = {
  vehicleId: string;
  vehicleCapacityId: string | null;
  candidateStatus: string;
  rejectionCode: string | null;
  fitScore: number | null;
  reasonText: string | null;
};

export function mapLoadPlan(raw: unknown): LoadPlanSummary {
  const record = asRecord(raw);
  return {
    id: String(record.id ?? ""),
    status: String(record.status ?? ""),
    feasibilityStatus: String(record.feasibilityStatus ?? ""),
    routePlanId: String(record.routePlanId ?? ""),
    vehicleId: typeof record.vehicleId === "string" ? record.vehicleId : null,
    vehicleCapacityId: typeof record.vehicleCapacityId === "string" ? record.vehicleCapacityId : null,
    rowVersion: asFiniteNumber(record.rowVersion),
    inputSnapshotHash: typeof record.inputSnapshotHash === "string" ? record.inputSnapshotHash : null,
  };
}

export type VehicleRow = {
  id: string;
  plateNumber: string;
  status: string;
  rowVersion: number | null;
};

export type DriverRow = {
  id: string;
  fullName: string;
  status: string;
  isActive: boolean;
};

export function mapRoutePlan(raw: unknown): RoutePlanSummary {
  const record = asRecord(raw);
  const stops = Array.isArray(record.stops) ? record.stops : [];
  return {
    id: String(record.id ?? ""),
    status: String(record.status ?? ""),
    version: typeof record.version === "number" ? record.version : 0,
    rowVersion: asFiniteNumber(record.rowVersion),
    vehicleId: typeof record.vehicleId === "string" ? record.vehicleId : null,
    driverId: typeof record.driverId === "string" ? record.driverId : null,
    stops: stops.map((item) => {
      const stop = asRecord(item);
      return {
        id: String(stop.id ?? ""),
        sequenceNo: typeof stop.sequenceNo === "number" ? stop.sequenceNo : 0,
        customerId: String(stop.customerId ?? ""),
        addressId: String(stop.addressId ?? ""),
        status: String(stop.status ?? ""),
      };
    }),
  };
}

export async function listRoutePlans(shipmentId: string): Promise<RoutePlanSummary[]> {
  const raw = await apiRequest<unknown>({ path: `/shipments/${shipmentId}/route-plans`, method: "GET" });
  return Array.isArray(raw) ? raw.map(mapRoutePlan) : [];
}

export async function createRoutePlan(shipmentId: string, expectedShipmentRowVersion: number): Promise<RoutePlanSummary> {
  return mapRoutePlan(
    await apiRequest<unknown>({
      path: `/shipments/${shipmentId}/route-plans`,
      method: "POST",
      body: { expectedShipmentRowVersion, plannedStartAt: null, plannedEndAt: null },
      idempotent: true,
    }),
  );
}

export async function replaceRouteStops(
  routePlanId: string,
  rowVersion: number,
  stops: Array<{ sequenceNo: number; customerId: string; addressId: string }>,
): Promise<RoutePlanSummary> {
  return mapRoutePlan(
    await apiRequest<unknown>({
      path: `/route-plans/${routePlanId}/stops/replace`,
      method: "POST",
      body: { stops: stops.map((stop) => ({ ...stop, plannedArrivalAt: null })) },
      ifMatch: String(rowVersion),
      idempotent: true,
    }),
  );
}

export async function assignRouteResources(
  routePlanId: string,
  rowVersion: number,
  vehicleId: string,
  driverId: string,
): Promise<RoutePlanSummary> {
  return mapRoutePlan(
    await apiRequest<unknown>({
      path: `/route-plans/${routePlanId}/assign-resources`,
      method: "POST",
      body: { vehicleId, driverId },
      ifMatch: String(rowVersion),
      idempotent: true,
    }),
  );
}

export async function planRoute(routePlanId: string, rowVersion: number): Promise<RoutePlanSummary> {
  return mapRoutePlan(
    await apiRequest<unknown>({
      path: `/route-plans/${routePlanId}/plan`,
      method: "POST",
      ifMatch: String(rowVersion),
      idempotent: true,
    }),
  );
}

export async function lockRoute(routePlanId: string, rowVersion: number): Promise<RoutePlanSummary> {
  return mapRoutePlan(
    await apiRequest<unknown>({
      path: `/route-plans/${routePlanId}/lock`,
      method: "POST",
      body: { confirmation: true },
      ifMatch: String(rowVersion),
      idempotent: true,
    }),
  );
}

export async function listVehicles(): Promise<VehicleRow[]> {
  const raw = await apiRequest<unknown>({ path: "/vehicles", method: "GET" });
  if (!Array.isArray(raw)) return [];
  return raw.map((item) => {
    const record = asRecord(item);
    return {
      id: String(record.id ?? ""),
      plateNumber: String(record.plateNumber ?? ""),
      status: String(record.status ?? ""),
      rowVersion: asFiniteNumber(record.rowVersion),
    };
  });
}

export async function listDrivers(): Promise<DriverRow[]> {
  const raw = await apiRequest<unknown>({ path: "/drivers", method: "GET" });
  if (!Array.isArray(raw)) return [];
  return raw.map((item) => {
    const record = asRecord(item);
    return {
      id: String(record.id ?? ""),
      fullName: String(record.fullName ?? ""),
      status: String(record.status ?? ""),
      isActive: record.isActive === true,
    };
  });
}

export async function confirmDispatch(dispatchRunId: string, rowVersion: number): Promise<DispatchRun> {
  return mapDispatchRun(
    await apiRequest<unknown>({
      path: `/dispatch-runs/${dispatchRunId}/confirm`,
      method: "POST",
      ifMatch: String(rowVersion),
      idempotent: true,
    }),
  );
}

export async function departDispatch(dispatchRunId: string, rowVersion: number): Promise<DispatchRun> {
  return mapDispatchRun(
    await apiRequest<unknown>({
      path: `/dispatch-runs/${dispatchRunId}/depart`,
      method: "POST",
      body: { occurredAt: new Date().toISOString(), locationText: null, latitude: null, longitude: null },
      ifMatch: String(rowVersion),
      idempotent: true,
    }),
  );
}

export async function arriveStop(
  dispatchRunId: string,
  routeStopId: string,
  rowVersion: number,
): Promise<DispatchRun> {
  return mapDispatchRun(
    await apiRequest<unknown>({
      path: `/dispatch-runs/${dispatchRunId}/stops/${routeStopId}/arrive`,
      method: "POST",
      body: { occurredAt: new Date().toISOString(), locationText: null, latitude: null, longitude: null },
      ifMatch: String(rowVersion),
      idempotent: true,
    }),
  );
}

export async function completeDispatch(dispatchRunId: string, rowVersion: number): Promise<DispatchRun> {
  return mapDispatchRun(
    await apiRequest<unknown>({
      path: `/dispatch-runs/${dispatchRunId}/complete`,
      method: "POST",
      body: { occurredAt: new Date().toISOString() },
      ifMatch: String(rowVersion),
      idempotent: true,
    }),
  );
}

export async function createShipmentPackage(
  shipmentId: string,
  input: { shipmentItemId: string; quantityBase: number },
): Promise<unknown> {
  return apiRequest<unknown>({
    path: `/shipments/${shipmentId}/packages`,
    method: "POST",
    body: {
      shipmentItemId: input.shipmentItemId,
      packagingId: null,
      routeStopId: null,
      packageType: "Parcel",
      packageCount: 1,
      quantityBasePerPackage: input.quantityBase,
      enteredQuantity: input.quantityBase,
      packageCode: null,
      splitAllowed: false,
    },
    idempotent: true,
  });
}

export async function createLoadPlan(
  shipmentId: string,
  input: {
    routePlanId: string;
    expectedRoutePlanVersion: number;
    expectedShipmentRowVersion: number;
    packages: ShipmentPackageRow[];
    fallbackStopId: string;
  },
): Promise<LoadPlanSummary> {
  const loadUnits = input.packages.map((pkg, index) => {
    const physical = physicalFromSnapshot(pkg.physicalSnapshot);
    if (!physical || pkg.quantityBase === null || pkg.quantityBase <= 0) {
      throw new Error("Paket fiziksel ölçüsü veya quantityBase yok; yük birimi uydurulmaz.");
    }
    const stopId = pkg.routeStopId ?? input.fallbackStopId;
    return {
      unitCode: `PAL-${String(index + 1).padStart(3, "0")}`,
      unitType: "Pallet",
      palletTypeId: null,
      isMixed: false,
      lengthMm: physical.lengthMm,
      widthMm: physical.widthMm,
      heightMm: physical.heightMm,
      tareWeightKg: physical.tareWeightKg,
      grossWeightKg: physical.grossWeightKg,
      volumeM3: physical.volumeM3,
      maxStackCount: physical.maxStackCount ?? 1,
      placementZone: null,
      unloadingPriority: index + 1,
      items: [
        {
          shipmentPackageId: pkg.id,
          shipmentItemId: pkg.shipmentItemId,
          quantityBase: pkg.quantityBase,
          stopAllocations: [
            { routeStopId: stopId, quantityBase: pkg.quantityBase, sequenceNo: 1 },
          ],
        },
      ],
    };
  });
  return mapLoadPlan(
    await apiRequest<unknown>({
      path: `/shipments/${shipmentId}/load-plans`,
      method: "POST",
      body: {
        routePlanId: input.routePlanId,
        expectedRoutePlanVersion: input.expectedRoutePlanVersion,
        expectedShipmentRowVersion: input.expectedShipmentRowVersion,
        loadUnits,
      },
      idempotent: true,
    }),
  );
}

export async function getLoadPlan(loadPlanId: string): Promise<LoadPlanSummary> {
  return mapLoadPlan(await apiRequest<unknown>({ path: `/load-plans/${loadPlanId}`, method: "GET" }));
}

export async function evaluateVehicleFit(
  shipmentId: string,
  loadPlanId: string,
  rowVersion: number,
): Promise<VehicleFitCandidate[]> {
  const raw = asRecord(
    await apiRequest<unknown>({
      path: `/shipments/${shipmentId}/vehicle-fit/evaluate`,
      method: "POST",
      body: {
        loadPlanId,
        expectedLoadPlanRowVersion: rowVersion,
        vehicleIds: null,
        algorithmVersion: null,
        parameterSet: null,
      },
      idempotent: true,
    }),
  );
  const evaluations = Array.isArray(raw.evaluations) ? raw.evaluations : [];
  return evaluations.map((item) => {
    const record = asRecord(item);
    return {
      vehicleId: String(record.vehicleId ?? ""),
      vehicleCapacityId: typeof record.vehicleCapacityId === "string" ? record.vehicleCapacityId : null,
      candidateStatus: String(record.candidateStatus ?? ""),
      rejectionCode: typeof record.rejectionCode === "string" ? record.rejectionCode : null,
      fitScore: asFiniteNumber(record.fitScore),
      reasonText: typeof record.reasonText === "string" ? record.reasonText : null,
    };
  });
}

export async function assignLoadPlanVehicle(
  loadPlanId: string,
  rowVersion: number,
  vehicleId: string,
  vehicleCapacityId: string,
): Promise<LoadPlanSummary> {
  return mapLoadPlan(
    await apiRequest<unknown>({
      path: `/load-plans/${loadPlanId}/assign-vehicle`,
      method: "POST",
      body: { vehicleId, vehicleCapacityId },
      ifMatch: String(rowVersion),
      idempotent: true,
    }),
  );
}

export async function validateLoadPlan(
  loadPlanId: string,
  rowVersion: number,
): Promise<{ plan: LoadPlanSummary; results: LoadPlanValidationRow[] }> {
  const raw = asRecord(
    await apiRequest<unknown>({
      path: `/load-plans/${loadPlanId}/validate`,
      method: "POST",
      body: {},
      ifMatch: String(rowVersion),
      idempotent: true,
    }),
  );
  const results = Array.isArray(raw.results) ? raw.results : [];
  return {
    plan: mapLoadPlan(raw.loadPlan),
    results: results.map((item) => {
      const record = asRecord(item);
      return {
        id: String(record.id ?? ""),
        severity: String(record.severity ?? ""),
        code: String(record.code ?? ""),
        message: String(record.message ?? ""),
        resolutionStatus: String(record.resolutionStatus ?? ""),
      };
    }),
  };
}

export async function lockLoadPlan(
  loadPlanId: string,
  rowVersion: number,
  warningResolutions: Array<{ validationResultId: string; action: string; reason: string }>,
): Promise<LoadPlanSummary> {
  return mapLoadPlan(
    await apiRequest<unknown>({
      path: `/load-plans/${loadPlanId}/lock`,
      method: "POST",
      body: { approval: true, warningResolutions },
      ifMatch: String(rowVersion),
      idempotent: true,
    }),
  );
}

export async function deliverStop(
  dispatchRunId: string,
  routeStopId: string,
  input: { recipientName: string; note?: string; rowVersion: number },
): Promise<DispatchRun> {
  return mapDispatchRun(
    await apiRequest<unknown>({
      path: `/dispatch-runs/${dispatchRunId}/stops/${routeStopId}/deliver`,
      method: "POST",
      body: {
        occurredAt: new Date().toISOString(),
        recipientName: input.recipientName,
        note: input.note || null,
      },
      ifMatch: String(input.rowVersion),
      idempotent: true,
    }),
  );
}
