"use client";

import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/states/empty-state";
import { ErrorState } from "@/components/states/error-state";
import { Input } from "@/components/ui/input";
import { ProductCard } from "@/components/public/product-card";
import { Skeleton } from "@/components/ui/skeleton";
import { userFacingMessage } from "@/lib/api/auth-client";
import { listPublicProducts } from "@/lib/catalog/catalog-client";
import type { PublicProductPage } from "@/lib/catalog/types";

export function CatalogBrowser() {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [data, setData] = useState<PublicProductPage | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [reloadToken, setReloadToken] = useState(0);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);
    listPublicProducts({ search: search.trim() || undefined, page, pageSize: 24 })
      .then((result) => {
        if (!cancelled) setData(result);
      })
      .catch((caught) => {
        if (!cancelled) setError(userFacingMessage(caught));
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [page, reloadToken, search]);

  return (
    <div className="mx-auto max-w-6xl px-4 py-8">
      <p className="text-sm font-semibold text-teal-600">Teklif kataloğu</p>
      <h1 className="mt-1 text-[28px] font-bold text-navy-950">Ürünler</h1>
      <p className="mt-2 max-w-2xl text-sm text-slate-600">
        İhtiyacınız olan ürünleri seçin, miktarını belirtin ve teklif talebi gönderin. Bu
        işlem sipariş veya ödeme oluşturmaz.
      </p>

      <form
        className="mt-6 max-w-md"
        onSubmit={(event) => {
          event.preventDefault();
          const form = new FormData(event.currentTarget);
          setPage(1);
          setSearch(String(form.get("search") ?? ""));
        }}
      >
        <Input
          label="Ürün ara"
          name="search"
          defaultValue={search}
          placeholder="Ürün adı, kod veya kategori"
        />
        <Button type="submit" variant="secondary" size="sm" className="mt-2">
          Ara
        </Button>
      </form>

      {loading ? (
        <div className="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <Skeleton className="h-72" />
          <Skeleton className="h-72" />
          <Skeleton className="h-72" />
        </div>
      ) : error ? (
        <div className="mt-8">
          <ErrorState
            title="Katalog yüklenemedi"
            description={error}
            onRetry={() => setReloadToken((value) => value + 1)}
          />
        </div>
      ) : !data || data.items.length === 0 ? (
        <div className="mt-8">
          <EmptyState
            title="Ürün bulunamadı"
            description="Aramanızı değiştirerek tekrar deneyin."
          />
        </div>
      ) : (
        <>
          <div className="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {data.items.map((product) => (
              <ProductCard key={product.id} product={product} />
            ))}
          </div>
          <div className="mt-6 flex items-center justify-between text-sm text-slate-600">
            <span>
              Sayfa {data.page} · {data.totalCount} ürün
            </span>
            <div className="flex gap-2">
              <Button
                size="sm"
                variant="secondary"
                disabled={page <= 1}
                onClick={() => setPage((value) => value - 1)}
              >
                Önceki
              </Button>
              <Button
                size="sm"
                variant="secondary"
                disabled={!data.hasNextPage}
                onClick={() => setPage((value) => value + 1)}
              >
                Sonraki
              </Button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
