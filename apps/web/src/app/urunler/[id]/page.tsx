import { SessionGate } from "@/components/auth/session-gate";
import { ProductBoardDetail } from "@/components/catalog/product-board-detail";

export const metadata = {
  title: "Ürün — Factory ERP",
};

export default async function ProductDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  return (
    <SessionGate>
      <ProductBoardDetail id={id} />
    </SessionGate>
  );
}
