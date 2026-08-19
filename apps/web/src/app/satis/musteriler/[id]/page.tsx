import { SessionGate } from "@/components/auth/session-gate";
import { CustomerDetail } from "@/components/sales/customer-detail";

export const metadata = {
  title: "Müşteri — Factory ERP",
};

export default async function CustomerDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  return (
    <SessionGate>
      <CustomerDetail id={id} />
    </SessionGate>
  );
}
