import { SessionGate } from "@/components/auth/session-gate";
import { ShipmentList } from "@/components/operations/shipment-list";

export const metadata = {
  title: "Sevkiyat — Factory ERP",
};

export default function ShipmentPage() {
  return (
    <SessionGate>
      <ShipmentList />
    </SessionGate>
  );
}
