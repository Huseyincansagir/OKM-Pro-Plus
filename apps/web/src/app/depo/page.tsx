import { SessionGate } from "@/components/auth/session-gate";
import { StockList } from "@/components/warehouse/stock-list";

export const metadata = {
  title: "Stok — Factory ERP",
};

export default function WarehousePage() {
  return (
    <SessionGate>
      <StockList />
    </SessionGate>
  );
}
