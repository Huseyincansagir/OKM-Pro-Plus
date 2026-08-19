"use client";

import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { packagingDefinitionLabel } from "@/lib/catalog/map-product";
import type { PublicProduct } from "@/lib/catalog/types";

export function ProductCard({ product }: { product: PublicProduct }) {
  const router = useRouter();
  const primaryPackaging = product.packagings[0];

  return (
    <article className="flex flex-col overflow-hidden rounded-2xl border border-surface-200 bg-white shadow-subtle">
      <div className="grid h-40 place-items-center bg-surface-100 text-sm text-slate-500">
        {product.primaryImageUrl ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img src={product.primaryImageUrl} alt="" className="h-full w-full object-cover" />
        ) : (
          <span>Görsel yok</span>
        )}
      </div>
      <div className="flex flex-1 flex-col p-4">
        <h2 className="text-[15px] font-semibold text-navy-950">{product.name}</h2>
        <p className="mt-1 text-xs text-slate-500">
          {product.code}
          {product.sizeLabel ? ` · ${product.sizeLabel}` : ""}
        </p>
        {product.description ? (
          <p className="mt-2 line-clamp-2 text-sm text-slate-600">{product.description}</p>
        ) : null}
        {primaryPackaging ? (
          <p className="mt-3 text-xs text-slate-600">
            {packagingDefinitionLabel(primaryPackaging, product.baseUomCode)}
          </p>
        ) : null}
        <div className="mt-auto pt-4">
          <Button className="w-full" size="sm" onClick={() => router.push(`/katalog/${product.slug}`)}>
            Teklife ekle
          </Button>
        </div>
      </div>
    </article>
  );
}
