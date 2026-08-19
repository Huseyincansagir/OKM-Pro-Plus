import { SessionGate } from "@/components/auth/session-gate";
import { WarehouseTransferList } from "@/components/operations/warehouse-transfer-list";

export const metadata = {
  title: "Depo — Factory ERP",
};

export default function WarehousePage() {
  return (
    <SessionGate>
      <WarehouseTransferList />
    </SessionGate>
  );
}
