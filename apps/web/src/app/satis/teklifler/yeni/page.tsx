import { SessionGate } from "@/components/auth/session-gate";
import { QuoteCreate } from "@/components/sales/quote-create";

export const metadata = {
  title: "Yeni teklif — Factory ERP",
};

export default async function NewQuotePage({
  searchParams,
}: {
  searchParams: Promise<{ quoteRequestId?: string }>;
}) {
  const { quoteRequestId } = await searchParams;
  return (
    <SessionGate>
      <QuoteCreate quoteRequestId={quoteRequestId ?? null} />
    </SessionGate>
  );
}
