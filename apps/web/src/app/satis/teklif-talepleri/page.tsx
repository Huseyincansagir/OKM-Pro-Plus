import { SessionGate } from "@/components/auth/session-gate";
import { QuoteRequestList } from "@/components/sales/quote-request-list";

export const metadata = {
  title: "Teklif talepleri — Factory ERP",
};

export default function QuoteRequestsPage() {
  return (
    <SessionGate>
      <QuoteRequestList />
    </SessionGate>
  );
}
