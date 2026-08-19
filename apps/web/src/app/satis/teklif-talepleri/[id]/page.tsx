import { SessionGate } from "@/components/auth/session-gate";
import { QuoteRequestDetail } from "@/components/sales/quote-request-detail";

export const metadata = {
  title: "Teklif talebi — Factory ERP",
};

export default async function QuoteRequestDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  return (
    <SessionGate>
      <QuoteRequestDetail id={id} />
    </SessionGate>
  );
}
