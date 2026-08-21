import { SessionGate } from "@/components/auth/session-gate";
import { InvoiceDetailBoard } from "@/components/finance/invoice-detail";

export const metadata = {
  title: "Fatura Detayı — Factory ERP",
};

export default async function InvoiceDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return (
    <SessionGate>
      <InvoiceDetailBoard id={id} />
    </SessionGate>
  );
}
