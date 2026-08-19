"use client";

import { Inbox, Layers, Truck } from "lucide-react";
import { StatusBadge } from "@/components/ui/status-badge";
import { OperationsCollection } from "@/components/operations/operations-collection";
import { listShipments, shipmentStatusKind, type ShipmentRow } from "@/lib/operations/boards";

export function ShipmentList() {
  return (
    <OperationsCollection<ShipmentRow>
      currentHref="/sevkiyat"
      title="Sevkiyatlar"
      description="GET /shipments. Kalem sayısı sunucu items uzunluğudur. Liste en fazla 100 kayıttır."
      permission="shipment.read"
      load={listShipments}
      emptyTitle="Sevkiyat yok"
      kpis={[
        { status: "Draft", label: "Taslak", icon: Inbox, caption: "Draft", tone: "amber" },
        { status: "Dispatched", label: "Sevk", icon: Truck, caption: "Dispatched" },
        { status: "Delivered", label: "Teslim", icon: Truck, caption: "Delivered" },
        { label: "Toplam", icon: Layers, caption: "Pencere · 100", tone: "navy" },
      ]}
      columns={[
        { id: "id", header: "Id", accessor: (row) => row.id.slice(0, 8) },
        { id: "status", header: "Durum", accessor: (row) => (
          <StatusBadge status={shipmentStatusKind(row.status)} label={row.status} />
        ) },
        { id: "items", header: "Kalem", accessor: (row) => String(row.itemCount) },
        { id: "delivery", header: "İrsaliye", accessor: (row) => row.deliveryNoteId.slice(0, 8) || "—" },
      ]}
    />
  );
}
