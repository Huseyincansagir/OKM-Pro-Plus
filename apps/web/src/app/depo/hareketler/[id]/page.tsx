import { SessionGate } from "@/components/auth/session-gate";
import { StockMovementDetail } from "@/components/warehouse/stock-movement-detail";

export const metadata = {
  title: "Stok hareketi — Factory ERP",
};

export default async function StockMovementDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  return (
    <SessionGate>
      <StockMovementDetail id={id} />
    </SessionGate>
  );
}
