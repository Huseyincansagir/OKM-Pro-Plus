import { SessionGate } from "@/components/auth/session-gate";
import { OperationsDashboard } from "@/components/dashboard/operations-dashboard";

export const metadata = {
  title: "Dashboard — Factory ERP",
};

export default function DashboardPage() {
  return (
    <SessionGate>
      <OperationsDashboard />
    </SessionGate>
  );
}
