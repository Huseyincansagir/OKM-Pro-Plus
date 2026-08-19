import { SessionGate } from "@/components/auth/session-gate";
import { WarehouseTransferList } from "@/components/operations/warehouse-transfer-list";

export const metadata = {
  title: "Transferler — Factory ERP",
};

export default function WarehouseTransfersPage() {
  return (
    <SessionGate>
      <WarehouseTransferList />
    </SessionGate>
  );
}