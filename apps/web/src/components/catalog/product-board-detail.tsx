"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Globe2, Layers, Package } from "lucide-react";
import { AppShell } from "@/components/shell/app-shell";
import { KpiMetric } from "@/components/dashboard/kpi-metric";
import { EmptyState } from "@/components/states/empty-state";
import { ErrorState } from "@/components/states/error-state";
import { PermissionDenied } from "@/components/states/permission-denied";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader, CardTitle } from "@/components/ui/card";
import { DataTable } from "@/components/ui/data-table";
import { Glyph } from "@/components/ui/glyph";
import { StatusBadge } from "@/components/ui/status-badge";
import { ApiError } from "@/lib/api/types";
import { userFacingMessage } from "@/lib/api/auth-client";
import { useSessionStore } from "@/lib/auth/session-store";
import {
  getStaffProduct,
  staffProductStatusKind,
  staffProductStatusLabel,
  type StaffProductDetail,
} from "@/lib/catalog/staff-products";

export function ProductBoardDetail({ id }: { id: string }) {
  const router = useRouter();
  const user = useSessionStore((state) => state.user);
  const canRead = (user?.permissions ?? []).includes("product.read");
  const [detail, setDetail] = useState<StaffProductDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [denied, setDenied] = useState(false);
  const [loading, setLoading] = useState(canRead);
  const [reload, setReload] = useState(0);

  useEffect(() => {
    if (!canRead) {
      setLoading(false);
      return;
    }
    let cancelled = false;
    setLoading(true);
    setError(null);
    setDenied(false);
    getStaffProduct(id)
      .then((result) => {
        if (!cancelled) setDetail(result);
      })
      .catch((caught) => {
        if (cancelled) return;
        if (caught instanceof ApiError && caught.kind === "permission_denied") {
          setDenied(true);
          return;
        }
        setError(userFacingMessage(caught));
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [canRead, id, reload]);

  return (
    <AppShell
      currentHref="/urunler"
      breadcrumbs={[
        { label: "Çalışma alanı", href: "/dashboard" },
        { label: "Ürünler", href: "/urunler" },
        { label: detail?.code || "Kart" },
      ]}
      pageTitle={detail?.name || "Ürün"}
      pageDescription="Ambalaj katsayıları sunucudan gelir. Stok ve fiyat bu kartta uydurulmaz."
      pageActions={
        <div className="flex flex-wrap gap-2">
          <Button variant="secondary" onClick={() => router.push("/urunler")}>
            Listeye dön
          </Button>
          <Button variant="secondary" loading={loading} onClick={() => setReload((value) => value + 1)}>
            Yenile
          </Button>
        </div>
      }
    >
      {!canRead ? (
        <PermissionDenied
          title="Ürün bu oturumda görünmez"
          description="product.read yok. Bu kontrol yalnızca görünürlük içindir; gerçek yetki backend’dedir."
        />
      ) : denied ? (
        <PermissionDenied />
      ) : error ? (
        <ErrorState
          title="Ürün yüklenemedi"
          description={error}
          onRetry={() => setReload((value) => value + 1)}
        />
      ) : loading || !detail ? (
        <DataTable
          columns={[{ id: "pkg", header: "Ambalaj", accessor: () => null }]}
          rows={[]}
          getRowId={() => ""}
          loading
        />
      ) : (
        <div className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <KpiMetric
              label="Kod"
              value={detail.code}
              icon={Package}
              tone="navy"
              caption="GET /products/{id}"
            />
            <KpiMetric
              label="Durum"
              value={staffProductStatusLabel(detail)}
              icon={Globe2}
              tone="teal"
              caption={detail.isActive ? "isActive" : "pasif"}
            />
            <KpiMetric
              label="Temel birim"
              value={detail.baseUomName || "—"}
              icon={Layers}
              tone="teal"
              caption="Sunucu baseUom"
            />
            <KpiMetric
              label="Ambalaj"
              value={String(detail.packagingCount)}
              unit="seviye"
              icon={Package}
              tone="navy"
              caption="Geçerli satırlar"
            />
          </div>

          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <Glyph icon={Package} />
                <CardTitle>Kart</CardTitle>
              </div>
              <StatusBadge
                status={staffProductStatusKind(detail)}
                label={staffProductStatusLabel(detail)}
              />
            </CardHeader>
            <CardBody className="space-y-2 text-sm text-slate-600">
              <p className="text-navy-950">{detail.description || "Açıklama yok."}</p>
              <p>Kategori: {detail.categoryName || "—"}</p>
              <p>Ölçü: {detail.sizeLabel || "—"}</p>
            </CardBody>
          </Card>

          <Alert tone="info" title="Stok bu kartta yok">
            GET /stocks bu dilimde listelenmez. 0 adet yazılmaz.
          </Alert>

          {detail.packagings.length === 0 ? (
            <EmptyState title="Ambalaj yok" description="Geçerli ambalaj satırı bulunmuyor." />
          ) : (
            <Card>
              <CardHeader>
                <CardTitle>Ambalajlar</CardTitle>
              </CardHeader>
              <CardBody className="pt-3">
                <DataTable
                  columns={[
                    {
                      id: "name",
                      header: "Ad",
                      accessor: (row) => (
                        <span className="inline-flex items-center gap-2">
                          <Glyph icon={Package} />
                          {row.name}
                        </span>
                      ),
                    },
                    { id: "level", header: "Seviye", accessor: (row) => row.level },
                    {
                      id: "base",
                      header: "Temel karşılık",
                      accessor: (row) =>
                        row.quantityInBaseUom === null ? "—" : String(row.quantityInBaseUom),
                    },
                    {
                      id: "sellable",
                      header: "Satılabilir",
                      accessor: (row) => (row.isSellable ? "Evet" : "Hayır"),
                    },
                  ]}
                  rows={detail.packagings}
                  getRowId={(row) => row.id}
                />
              </CardBody>
            </Card>
          )}
        </div>
      )}
    </AppShell>
  );
}
