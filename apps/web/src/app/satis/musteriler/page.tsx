import { SessionGate } from "@/components/auth/session-gate";
import { CustomerList } from "@/components/sales/customer-list";

export const metadata = {
  title: "Müşteriler — Factory ERP",
};

export default function CustomersPage() {
  return (
    <SessionGate>
      <CustomerList />
    </SessionGate>
  );
}
