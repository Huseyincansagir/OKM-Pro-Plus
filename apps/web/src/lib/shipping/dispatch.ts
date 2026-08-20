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

export async function listShipmentPackages(shipmentId: string): Promise<Array<{ id: string; status: string }>> {
  const raw = await apiRequest<unknown>({ path: `/shipments/${shipmentId}/packages`, method: "GET" });
  if (!Array.isArray(raw)) {
    return [];
  }
  return raw.map((item) => {
    const record = asRecord(item);
    return { id: String(record.id ?? ""), status: String(record.status ?? "") };
  });
}

export async function listLoadPlans(shipmentId: string): Promise<LoadPlanSummary[]> {
  const raw = await apiRequest<unknown>({ path: `/shipments/${shipmentId}/load-plans`, method: "GET" });
  if (!Array.isArray(raw)) {
    return [];
  }
  return raw.map((item) => {
    const record = asRecord(item);
    return {
      id: String(record.id ?? ""),
      status: String(record.status ?? ""),
      routePlanId: String(record.routePlanId ?? ""),
      vehicleId: typeof record.vehicleId === "string" ? record.vehicleId : null,
      rowVersion: asFiniteNumber(record.rowVersion),
    };
  });
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
  routePlanId: string;
  vehicleId: string | null;
  rowVersion: number | null;
};

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
