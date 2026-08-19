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

export async function listLoadPlans(shipmentId: string): Promise<Array<{ id: string; status: string }>> {
  const raw = await apiRequest<unknown>({ path: `/shipments/${shipmentId}/load-plans`, method: "GET" });
  if (!Array.isArray(raw)) {
    return [];
  }
  return raw.map((item) => {
    const record = asRecord(item);
    return { id: String(record.id ?? ""), status: String(record.status ?? "") };
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
