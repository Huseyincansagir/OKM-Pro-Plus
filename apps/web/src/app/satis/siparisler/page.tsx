import { SessionGate } from "@/components/auth/session-gate";
import { OrderList } from "@/components/sales/order-list";

export const metadata = {
  title: "Siparişler — Factory ERP",
};

export default function OrdersPage() {
  return (
    <SessionGate>
      <OrderList />
    </SessionGate>
  );
}
