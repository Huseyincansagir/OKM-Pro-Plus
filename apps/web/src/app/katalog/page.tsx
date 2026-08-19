import { CatalogBrowser } from "@/components/public/catalog-browser";
import { PublicShell } from "@/components/public/public-shell";

export const metadata = {
  title: "Ürün kataloğu — Factory ERP",
  description: "Ürünleri inceleyin ve teklif talebi oluşturun.",
};

export default function CatalogPage() {
  return (
    <PublicShell>
      <CatalogBrowser />
    </PublicShell>
  );
}
