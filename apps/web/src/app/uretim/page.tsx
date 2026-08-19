import { SessionGate } from "@/components/auth/session-gate";
import { ProductionOrderList } from "@/components/operations/production-order-list";

export const metadata = {
  title: "Üretim — Factory ERP",
};

export default function ProductionPage() {
  return (
    <SessionGate>
      <ProductionOrderList />
    </SessionGate>
  );
}
