import { SessionGate } from "@/components/auth/session-gate";
import { QuoteList } from "@/components/sales/quote-list";

export const metadata = {
  title: "Teklifler — Factory ERP",
};

export default function QuotesPage() {
  return (
    <SessionGate>
      <QuoteList />
    </SessionGate>
  );
}
