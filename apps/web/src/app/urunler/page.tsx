import { SessionGate } from "@/components/auth/session-gate";
import { ProductList } from "@/components/catalog/product-list";

export const metadata = {
  title: "Ürünler — Factory ERP",
};

export default function ProductsPage() {
  return (
    <SessionGate>
      <ProductList />
    </SessionGate>
  );
}
