"use client";

import { ArrowLeftRight, CheckCircle2, Inbox, Layers } from "lucide-react";
import { StatusBadge } from "@/components/ui/status-badge";
import { OperationsCollection } from "@/components/operations/operations-collection";
import { listTransfers, transferStatusKind, type TransferRow } from "@/lib/operations/boards";

export function WarehouseTransferList() {
  return (
    <OperationsCollection<TransferRow>
      currentHref="/depo/transferler"
      title="Depo transferleri"
      description="GET /warehouse-transfers. quantityBase sunucudan gelir. Liste en fazla 100 kayıttır."
      permission="stock-transfer.read"
      load={listTransfers}
      emptyTitle="Transfer yok"
      kpis={[
        { status: "Draft", label: "Taslak", icon: Inbox, caption: "Draft", tone: "amber" },
        { status: "Completed", label: "Tamam", icon: CheckCircle2, caption: "Completed" },
        { status: "Cancelled", label: "İptal", icon: ArrowLeftRight, caption: "Cancelled", tone: "navy" },
        { label: "Toplam", icon: Layers, caption: "Pencere · 100", tone: "navy" },
      ]}
      columns={[
        { id: "id", header: "Id", accessor: (row) => row.id.slice(0, 8) },
        { id: "status", header: "Durum", accessor: (row) => (
          <StatusBadge status={transferStatusKind(row.status)} label={row.status} />
        ) },
        {
          id: "qty",
          header: "Temel miktar",
          accessor: (row) => (row.quantityBase === null ? "—" : String(row.quantityBase)),
        },
        { id: "entered", header: "Girilen", accessor: (row) => String(row.enteredQuantity) },
      ]}
    />
  );
}
