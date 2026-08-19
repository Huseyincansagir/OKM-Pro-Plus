import { ProductDetail } from "@/components/public/product-detail";
import { PublicShell } from "@/components/public/public-shell";

export default async function CatalogProductPage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;
  return (
    <PublicShell>
      <ProductDetail slug={slug} />
    </PublicShell>
  );
}
