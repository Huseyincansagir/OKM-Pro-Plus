import { SessionGate } from "@/components/auth/session-gate";
import { TransferDetail } from "@/components/warehouse/transfer-detail";

export const metadata = {
  title: "Transfer — Factory ERP",
};

export default async function WarehouseTransferDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  return (
    <SessionGate>
      <TransferDetail id={id} />
    </SessionGate>
  );
}
