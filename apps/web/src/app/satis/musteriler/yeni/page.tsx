import { SessionGate } from "@/components/auth/session-gate";
import { CustomerCreate } from "@/components/sales/customer-create";

export const metadata = {
  title: "Yeni müşteri — Factory ERP",
};

export default function NewCustomerPage() {
  return (
    <SessionGate>
      <CustomerCreate />
    </SessionGate>
  );
}
