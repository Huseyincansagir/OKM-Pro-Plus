import { SessionGate } from "@/components/auth/session-gate";
import { FinanceBoard } from "@/components/finance/finance-board";

export const metadata = {
  title: "Cari — Factory ERP",
};

export default function FinancePage() {
  return (
    <SessionGate>
      <FinanceBoard />
    </SessionGate>
  );
}