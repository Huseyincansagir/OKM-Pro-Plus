"use client";

import { Factory, Inbox, Layers, Play } from "lucide-react";
import { StatusBadge } from "@/components/ui/status-badge";
import { OperationsCollection } from "@/components/operations/operations-collection";
import { listProductionOrders, productionStatusKind, type ProductionRow } from "@/lib/operations/boards";

export function ProductionOrderList() {
  return (
    <OperationsCollection<ProductionRow>
      currentHref="/uretim"
      title="Üretim emirleri"
      description="GET /production/orders. Kalan miktar sunucu remainingQuantityBase alanıdır."
      permission="production.read"
      load={listProductionOrders}
      emptyTitle="Üretim emri yok"
      kpis={[
        { status: "Planned", label: "Planlı", icon: Inbox, caption: "Planned", tone: "amber" },
        { status: "InProgress", label: "Üretimde", icon: Play, caption: "InProgress" },
        { status: "Completed", label: "Bitti", icon: Factory, caption: "Completed" },
        { label: "Toplam", icon: Layers, caption: "Pencere · 100", tone: "navy" },
      ]}
      columns={[
        { id: "id", header: "Id", accessor: (row) => row.id.slice(0, 8) },
        { id: "status", header: "Durum", accessor: (row) => (
          <StatusBadge status={productionStatusKind(row.status)} label={row.status} />
        ) },
        {
          id: "planned",
          header: "Plan",
          accessor: (row) => (row.plannedQuantityBase === null ? "—" : String(row.plannedQuantityBase)),
        },
        {
          id: "remaining",
          header: "Kalan",
          accessor: (row) => (row.remainingQuantityBase === null ? "—" : String(row.remainingQuantityBase)),
        },
      ]}
    />
  );
}
