import { SessionGate } from "@/components/auth/session-gate";
import { TransferCreate } from "@/components/warehouse/transfer-create";

export const metadata = {
  title: "Yeni transfer — Factory ERP",
};

export default function NewWarehouseTransferPage() {
  return (
    <SessionGate>
      <TransferCreate />
    </SessionGate>
  );
}
