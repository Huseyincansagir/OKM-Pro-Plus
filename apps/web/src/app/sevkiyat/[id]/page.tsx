import { SessionGate } from "@/components/auth/session-gate";
import { ShipmentDetailBoard } from "@/components/shipping/shipment-detail";

export const metadata = {
  title: "Sevkiyat — Factory ERP",
};

export default async function ShipmentDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  return (
    <SessionGate>
      <ShipmentDetailBoard id={id} />
    </SessionGate>
  );
}
