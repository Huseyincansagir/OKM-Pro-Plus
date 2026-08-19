import { SessionGate } from "@/components/auth/session-gate";
import { StockMovementList } from "@/components/warehouse/stock-movement-list";

export const metadata = {
  title: "Stok hareketleri — Factory ERP",
};

export default function StockMovementsPage() {
  return (
    <SessionGate>
      <StockMovementList />
    </SessionGate>
  );
}
