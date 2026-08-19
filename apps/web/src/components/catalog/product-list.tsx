"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Globe2, Layers, Package, ShieldOff } from "lucide-react";
import { AppShell } from "@/components/shell/app-shell";
import { KpiMetric } from "@/components/dashboard/kpi-metric";
import { EmptyState } from "@/components/states/empty-state";
import { ErrorState } from "@/components/states/error-state";
import { PermissionDenied } from "@/components/states/permission-denied";
import { Button } from "@/components/ui/button";
import { Card, CardBody } from "@/components/ui/card";
import { DataTable } from "@/components/ui/data-table";
import { Glyph } from "@/components/ui/glyph";
import { StatusBadge } from "@/components/ui/status-badge";
import { ApiError } from "@/lib/api/types";
import { userFacingMessage } from "@/lib/api/auth-client";
import { useSessionStore } from "@/lib/auth/session-store";
import {
  listStaffProducts,
  staffProductStatusKind,
  staffProductStatusLabel,
  type StaffProductSummary,
} from "@/lib/catalog/staff-products";

export function ProductList() {
  const router = useRouter();
  const user = useSessionStore((state) => state.user);
  const permissions = user?.permissions ?? [];
  const canRead = permissions.includes("product.read");
  const [rows, setRows] = useState<StaffProductSummary[] | null>(null);
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
    listStaffProducts()
      .then((result) => {
        if (!cancelled) setRows(result);
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
  }, [canRead, reload]);

  const ready = Boolean(rows) && !loading && !error && !denied;
  const active = rows?.filter((row) => row.isActive).length ?? 0;
  const publicCount = rows?.filter((row) => row.isActive && row.isPublic).length ?? 0;
  const internal = rows?.filter((row) => row.isActive && !row.isPublic).length ?? 0;
  const total = rows?.length ?? 0;

  return (
    <AppShell
      currentHref="/urunler"
      breadcrumbs={[
        { label: "Çalışma alanı", href: "/dashboard" },
        { label: "Ürünler" },
      ]}
      pageTitle="Ürünler"
      pageDescription="İç katalog. Public ve iç kayıtlar birlikte gelir. Stok/fiyat bu listede yoktur."
      pageActions={
        <div className="flex flex-wrap gap-2">
          <Button variant="secondary" onClick={() => router.push("/satis/siparisler")}>
            Siparişler
          </Button>
          {canRead ? (
            <Button variant="secondary" loading={loading} onClick={() => setReload((value) => value + 1)}>
              Yenile
            </Button>
          ) : null}
        </div>
      }
    >
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <KpiMetric
          label="Aktif"
          value={ready ? String(active) : "—"}
          unit="kart"
          icon={Package}
          tone="teal"
          unavailable={!ready}
          caption="GET /products · isActive"
        />
        <KpiMetric
          label="Public"
          value={ready ? String(publicCount) : "—"}
          unit="kart"
          icon={Globe2}
          tone="teal"
          unavailable={!ready}
          caption="GET /products · isPublic"
        />
        <KpiMetric
          label="Yalnız iç"
          value={ready ? String(internal) : "—"}
          unit="kart"
          icon={ShieldOff}
          tone="navy"
          unavailable={!ready}
          caption="Aktif ve public değil"
        />
        <KpiMetric
          label="Toplam"
          value={ready ? String(total) : "—"}
          unit="kayıt"
          icon={Layers}
          tone="navy"
          unavailable={!ready}
          caption="Liste penceresi · en fazla 100"
        />
      </div>

      <Card className="mt-4">
        <CardBody>
          {!canRead ? (
            <PermissionDenied
              title="Ürünler bu oturumda görünmez"
              description="product.read yok. Bu kontrol yalnızca görünürlük içindir; gerçek yetki backend’dedir."
            />
          ) : denied ? (
            <PermissionDenied />
          ) : error ? (
            <ErrorState
              title="Ürünler yüklenemedi"
              description={error}
              onRetry={() => setReload((value) => value + 1)}
            />
          ) : loading ? (
            <DataTable
              columns={[
                { id: "code", header: "Kod", accessor: () => null },
                { id: "name", header: "Ad", accessor: () => null },
              ]}
              rows={[]}
              getRowId={() => ""}
              loading
            />
          ) : !rows || rows.length === 0 ? (
            <EmptyState title="Ürün yok" description="Bu pencerede ürün kartı yok." />
          ) : (
            <DataTable
              columns={[
                {
                  id: "code",
                  header: "Kod",
                  accessor: (row) => (
                    <Link
                      href={`/urunler/${row.id}`}
                      className="inline-flex items-center gap-2 font-semibold text-teal-600"
                    >
                      <Glyph icon={Package} />
                      {row.code}
                    </Link>
                  ),
                },
                {
                  id: "name",
                  header: "Ad",
                  accessor: (row) => row.name,
                },
                {
                  id: "category",
                  header: "Kategori",
                  accessor: (row) => row.categoryName || "—",
                },
                {
                  id: "uom",
                  header: "Temel birim",
                  accessor: (row) => row.baseUomName || "—",
                },
                {
                  id: "packaging",
                  header: "Ambalaj",
                  accessor: (row) => String(row.packagingCount),
                },
                {
                  id: "status",
                  header: "Durum",
                  accessor: (row) => (
                    <StatusBadge
                      status={staffProductStatusKind(row)}
                      label={staffProductStatusLabel(row)}
                    />
                  ),
                },
              ]}
              rows={rows}
              getRowId={(row) => row.id}
            />
          )}
        </CardBody>
      </Card>
    </AppShell>
  );
}
