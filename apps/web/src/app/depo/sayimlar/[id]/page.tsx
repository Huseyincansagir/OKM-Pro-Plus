import { SessionGate } from "@/components/auth/session-gate";
import { StockCountDetail } from "@/components/warehouse/stock-count-detail";

export const metadata = {
  title: "Sayım — Factory ERP",
};

export default async function StockCountDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  return (
    <SessionGate>
      <StockCountDetail id={id} />
    </SessionGate>
  );
}
