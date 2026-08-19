import { SessionGate } from "@/components/auth/session-gate";
import { EmployeeBoard } from "@/components/hr/employee-board";

export const metadata = {
  title: "Personel — Factory ERP",
};

export default function PersonnelPage() {
  return (
    <SessionGate>
      <EmployeeBoard />
    </SessionGate>
  );
}
