"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { Alert } from "@/components/ui/alert";
import { ErrorState } from "@/components/states/error-state";
import { QuantityViewToggle } from "@/components/quantity/quantity-view-toggle";
import { getPublicProduct } from "@/lib/catalog/catalog-client";
import { packagingDefinitionLabel, sellablePackagings } from "@/lib/catalog/map-product";
import { useQuoteBasketStore } from "@/lib/catalog/quote-basket-store";
import type { PublicProduct } from "@/lib/catalog/types";
import type { QuantityViewMode } from "@/types/quantity";
import { userFacingMessage } from "@/lib/api/auth-client";

export function ProductDetail({ slug }: { slug: string }) {
  const router = useRouter();
  const addLine = useQuoteBasketStore((state) => state.addLine);
  const [product, setProduct] = useState<PublicProduct | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [enteredQuantity, setEnteredQuantity] = useState("1");
  const [packagingId, setPackagingId] = useState<string>("");
  const [viewMode, setViewMode] = useState<QuantityViewMode>("Packaging");
  const [note, setNote] = useState("");
  const [added, setAdded] = useState(false);

  useEffect(() => {
    getPublicProduct(slug)
      .then((result) => {
        setProduct(result);
        const options = sellablePackagings(result);
        setPackagingId(options[0]?.id ?? "");
      })
      .catch((caught) => setError(userFacingMessage(caught)));
  }, [slug]);

  const options = useMemo(() => (product ? sellablePackagings(product) : []), [product]);
  const selected = options.find((item) => item.id === packagingId) ?? options[0];

  if (error) {
    return (
      <div className="mx-auto max-w-5xl px-4 py-8">
        <ErrorState title="Ürün yüklenemedi" description={error} />
      </div>
    );
  }

  if (!product) {
    return <div className="mx-auto max-w-5xl px-4 py-16 text-sm text-slate-600">Ürün yükleniyor…</div>;
  }

  function addToBasket() {
    if (!product || !selected) {
      return;
    }
    const quantity = Number(enteredQuantity);
    if (!Number.isFinite(quantity) || quantity <= 0) {
      return;
    }
    addLine({
      productId: product.id,
      slug: product.slug,
      name: product.name,
      code: product.code,
      primaryImageUrl: product.primaryImageUrl,
      enteredQuantity: quantity,
      enteredPackagingId: selected.id,
      packagingName: selected.name,
      catalogQuantityInBaseUom: selected.quantityInBaseUom,
      baseUomCode: product.baseUomCode,
      viewMode,
      note,
    });
    setAdded(true);
  }

  return (
    <div className="mx-auto max-w-5xl px-4 py-8">
      <button type="button" className="text-sm font-semibold text-teal-700" onClick={() => router.push("/katalog")}>
        ← Ürünlere dön
      </button>
      <div className="mt-6 grid gap-8 lg:grid-cols-2">
        <div className="grid min-h-72 place-items-center rounded-2xl border border-surface-200 bg-surface-100 text-slate-500">
          {product.primaryImageUrl ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img src={product.primaryImageUrl} alt="" className="h-full w-full rounded-2xl object-cover" />
          ) : (
            "Görsel yok"
          )}
        </div>
        <div>
          <h1 className="text-[28px] font-bold text-navy-950">{product.name}</h1>
          <p className="mt-1 text-sm text-slate-500">
            {product.code}
            {product.sizeLabel ? ` · ${product.sizeLabel}` : ""}
          </p>
          {product.description ? <p className="mt-4 text-sm text-slate-600">{product.description}</p> : null}
          <ul className="mt-4 space-y-1 text-sm text-slate-600">
            <li>Temel birim: {product.baseUomName || product.baseUomCode}</li>
            {product.packagings.map((packaging) => (
              <li key={packaging.id}>{packagingDefinitionLabel(packaging, product.baseUomCode)}</li>
            ))}
          </ul>

          <div className="mt-6 space-y-3">
            <p className="text-xs font-semibold text-slate-500">Görünüm (işlem birimini değiştirmez)</p>
            <QuantityViewToggle
              viewMode={viewMode}
              onViewModeChange={setViewMode}
              operationPackagingId={packagingId}
            />
            <Input
              label="Miktar"
              name="quantity"
              type="number"
              min={selected?.allowPartial ? 0.01 : 1}
              step={selected?.allowPartial ? "0.01" : "1"}
              value={enteredQuantity}
              onChange={(event) => setEnteredQuantity(event.target.value)}
            />
            <Select
              label="İşlem ambalajı"
              name="packaging"
              value={packagingId}
              onChange={(event) => setPackagingId(event.target.value)}
              options={options.map((item) => ({
                value: item.id,
                label: item.name,
              }))}
            />
            {selected ? (
              <p className="text-xs text-slate-600">
                Katalog tanımı: {packagingDefinitionLabel(selected, product.baseUomCode)}. Temel karşılık
                gönderimde sunucu tarafından hesaplanır.
              </p>
            ) : null}
            <Input
              label="Ürün notu"
              name="note"
              value={note}
              onChange={(event) => setNote(event.target.value)}
              hint="Baskı, renk veya teslimat notu yazabilirsiniz."
            />
            <Button onClick={addToBasket}>Teklife ekle</Button>
            {added ? (
              <Alert tone="success" title="Teklif sepetine eklendi">
                Bu bir sipariş değildir. Sepeti kontrol edip talep gönderebilirsiniz.
                <div className="mt-2">
                  <Button size="sm" variant="secondary" onClick={() => router.push("/katalog/sepet")}>
                    Sepete git
                  </Button>
                </div>
              </Alert>
            ) : null}
          </div>
        </div>
      </div>
    </div>
  );
}
