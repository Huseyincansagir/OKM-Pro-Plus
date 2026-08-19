"use client";

import Link from "next/link";
import { Inbox, Layers, Truck } from "lucide-react";
import { Glyph } from "@/components/ui/glyph";
import { StatusBadge } from "@/components/ui/status-badge";
import { OperationsCollection } from "@/components/operations/operations-collection";
import { listShipments, type ShipmentRow } from "@/lib/operations/boards";
import { shipmentStatusKind } from "@/lib/shipping/shipments";

export function ShipmentList() {
  return (
    <OperationsCollection<ShipmentRow>
      currentHref="/sevkiyat"
      title="Sevkiyatlar"
      description="GET /shipments. Durumlar Preparing / Loaded / InTransit. Teslim komutu yoktur; Delivered uydurulmaz."
      permission="shipment.read"
      load={listShipments}
      emptyTitle="Sevkiyat yok"
      kpis={[
        { status: "Preparing", label: "Hazırlık", icon: Inbox, caption: "Preparing", tone: "amber" },
        { status: "Loaded", label: "Yüklü", icon: Truck, caption: "Loaded" },
        { status: "InTransit", label: "Yolda", icon: Truck, caption: "InTransit" },
        { label: "Toplam", icon: Layers, caption: "Pencere · 100", tone: "navy" },
      ]}
      columns={[
        {
          id: "id",
          header: "Belge",
          accessor: (row) => (
            <Link
              href={`/sevkiyat/${row.id}`}
              className="inline-flex items-center gap-2 font-semibold text-teal-600"
            >
              <Glyph icon={Truck} />
              {row.id.slice(0, 8)}
            </Link>
          ),
        },
        {
          id: "status",
          header: "Durum",
          accessor: (row) => (
            <StatusBadge status={shipmentStatusKind(row.status)} label={row.status} />
          ),
        },
        { id: "items", header: "Kalem", accessor: (row) => String(row.itemCount) },
        {
          id: "delivery",
          header: "İrsaliye",
          accessor: (row) =>
            row.deliveryNoteId ? (
              <Link className="text-teal-600" href={`/sevkiyat/irsaliyeler/${row.deliveryNoteId}`}>
                {row.deliveryNoteId.slice(0, 8)}
              </Link>
            ) : (
              "—"
            ),
        },
      ]}
    />
  );
}
