import { SessionGate } from "@/components/auth/session-gate";
import { OrderDetail } from "@/components/sales/order-detail";

export const metadata = {
  title: "Sipariş — Factory ERP",
};

export default async function OrderDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  return (
    <SessionGate>
      <OrderDetail id={id} />
    </SessionGate>
  );
}
