import { SessionGate } from "@/components/auth/session-gate";
import { QuoteDetail } from "@/components/sales/quote-detail";

export const metadata = {
  title: "Teklif — Factory ERP",
};

export default async function QuoteDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  return (
    <SessionGate>
      <QuoteDetail id={id} />
    </SessionGate>
  );
}
