import { SessionGate } from "@/components/auth/session-gate";
import { DeliveryNoteDetailBoard } from "@/components/shipping/delivery-note-detail";

export const metadata = {
  title: "İrsaliye — Factory ERP",
};

export default async function DeliveryNoteDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  return (
    <SessionGate>
      <DeliveryNoteDetailBoard id={id} />
    </SessionGate>
  );
}
