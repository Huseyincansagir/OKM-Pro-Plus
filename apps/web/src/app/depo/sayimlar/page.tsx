import { SessionGate } from "@/components/auth/session-gate";
import { StockCountBoard } from "@/components/warehouse/stock-count-board";

export const metadata = {
  title: "Stok sayımı — Factory ERP",
};

export default function StockCountsPage() {
  return (
    <SessionGate>
      <StockCountBoard />
    </SessionGate>
  );
}
