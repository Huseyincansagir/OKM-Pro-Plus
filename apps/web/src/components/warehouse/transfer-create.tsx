"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { AppShell } from "@/components/shell/app-shell";
import { QuantityEntryPreview } from "@/components/quantity/quantity-entry-preview";
import { QuantityViewToggle } from "@/components/quantity/quantity-view-toggle";
import { PermissionDenied } from "@/components/states/permission-denied";
import { ErrorState } from "@/components/states/error-state";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { userFacingMessage } from "@/lib/api/auth-client";
import { useSessionStore } from "@/lib/auth/session-store";
import { getStaffProduct, listStaffProducts, type StaffProductDetail, type StaffProductSummary } from "@/lib/catalog/staff-products";
import { previewQuantity, type QuantityPreview } from "@/lib/catalog/quantity-preview";
import { listWarehouseLocations, listWarehouses, type WarehouseLocation, type WarehouseSummary } from "@/lib/warehouse/stocks";
import { createTransfer } from "@/lib/warehouse/transfers";
import type { QuantityViewMode } from "@/types/quantity";

export function TransferCreate() {
  const router = useRouter();
  const user = useSessionStore((state) => state.user);
  const permissions = user?.permissions ?? [];
  const canCreate = permissions.includes("stock-transfer.create");
  const canReadStock = permissions.includes("stock.read");
  const canReadProducts = permissions.includes("product.read");
  const [products, setProducts] = useState<StaffProductSummary[]>([]);
  const [product, setProduct] = useState<StaffProductDetail | null>(null);
  const [warehouses, setWarehouses] = useState<WarehouseSummary[]>([]);
  const [sourceLocations, setSourceLocations] = useState<WarehouseLocation[]>([]);
  const [targetLocations, setTargetLocations] = useState<WarehouseLocation[]>([]);
  const [productId, setProductId] = useState("");
  const [sourceWarehouseId, setSourceWarehouseId] = useState("");
  const [sourceLocationId, setSourceLocationId] = useState("");
  const [targetWarehouseId, setTargetWarehouseId] = useState("");
  const [targetLocationId, setTargetLocationId] = useState("");
  const [packagingId, setPackagingId] = useState("");
  const [enteredQuantity, setEnteredQuantity] = useState("1");
  const [viewMode, setViewMode] = useState<QuantityViewMode>("Packaging");
  const [preview, setPreview] = useState<QuantityPreview | null>(null);
  const [previewing, setPreviewing] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);

  useEffect(() => {
    if (!canCreate || !canReadStock || !canReadProducts) {
      return;
    }
    let cancelled = false;
    Promise.all([listStaffProducts(), listWarehouses()])
      .then(([productRows, warehouseRows]) => {
        if (cancelled) return;
        setProducts(productRows);
        setWarehouses(warehouseRows.filter((row) => row.isActive));
      })
      .catch((caught) => {
        if (!cancelled) setLoadError(userFacingMessage(caught));
      });
    return () => {
      cancelled = true;
    };
  }, [canCreate, canReadProducts, canReadStock]);

  useEffect(() => {
    if (!productId) {
      setProduct(null);
      return;
    }
    let cancelled = false;
    getStaffProduct(productId)
      .then((detail) => {
        if (!cancelled) {
          setProduct(detail);
          setPackagingId(detail.packagings[0]?.id ?? "");
        }
      })
      .catch((caught) => {
        if (!cancelled) setError(userFacingMessage(caught));
      });
    return () => {
      cancelled = true;
    };
  }, [productId]);

  useEffect(() => {
    if (!sourceWarehouseId) {
      setSourceLocations([]);
      setSourceLocationId("");
      return;
    }
    let cancelled = false;
    listWarehouseLocations(sourceWarehouseId)
      .then((rows) => {
        if (cancelled) return;
        setSourceLocations(rows.filter((row) => row.isActive));
        setSourceLocationId("");
      })
      .catch((caught) => {
        if (!cancelled) setError(userFacingMessage(caught));
      });
    return () => {
      cancelled = true;
    };
  }, [sourceWarehouseId]);

  useEffect(() => {
    if (!targetWarehouseId) {
      setTargetLocations([]);
      setTargetLocationId("");
      return;
    }
    let cancelled = false;
    listWarehouseLocations(targetWarehouseId)
      .then((rows) => {
        if (cancelled) return;
        setTargetLocations(rows.filter((row) => row.isActive));
        setTargetLocationId("");
      })
      .catch((caught) => {
        if (!cancelled) setError(userFacingMessage(caught));
      });
    return () => {
      cancelled = true;
    };
  }, [targetWarehouseId]);

  async function runPreview() {
    const quantity = Number(enteredQuantity);
    if (!productId || !Number.isFinite(quantity) || quantity <= 0) {
      setError("Girilen miktar sıfırdan büyük olmalıdır.");
      return;
    }
    setPreviewing(true);
    setError(null);
    try {
      const result = await previewQuantity({
        productId,
        enteredQuantity: quantity,
        enteredPackagingId: packagingId || null,
        viewMode,
        operationType: "WarehouseTransfer",
        warehouseId: sourceWarehouseId || null,
      });
      setPreview(result);
    } catch (caught) {
      setPreview(null);
      setError(userFacingMessage(caught));
    } finally {
      setPreviewing(false);
    }
  }

  async function submit() {
    const quantity = Number(enteredQuantity);
    if (!productId || !sourceWarehouseId || !sourceLocationId || !targetWarehouseId || !targetLocationId) {
      setError("Ürün, kaynak ve hedef konum zorunludur.");
      return;
    }
    if (!Number.isFinite(quantity) || quantity <= 0) {
      setError("Girilen miktar sıfırdan büyük olmalıdır.");
      return;
    }
    if (sourceWarehouseId === targetWarehouseId && sourceLocationId === targetLocationId) {
      setError("Kaynak ve hedef konum aynı olamaz.");
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      const created = await createTransfer({
        productId,
        sourceWarehouseId,
        sourceLocationId,
        targetWarehouseId,
        targetLocationId,
        enteredQuantity: quantity,
        enteredPackagingId: packagingId || null,
        viewMode,
      });
      router.push(`/depo/transferler/${created.id}`);
    } catch (caught) {
      setError(userFacingMessage(caught));
    } finally {
      setSubmitting(false);
    }
  }

  if (!canCreate) {
    return (
      <AppShell
        currentHref="/depo"
        breadcrumbs={[
          { label: "Çalışma alanı", href: "/dashboard" },
          { label: "Depo", href: "/depo" },
          { label: "Transferler", href: "/depo/transferler" },
          { label: "Yeni" },
        ]}
        pageTitle="Yeni transfer"
        pageDescription="POST /warehouse-transfers. quantityBase istemcide üretilmez."
      >
        <PermissionDenied
          title="Transfer bu oturumda açılamaz"
          description="stock-transfer.create yok. Bu kontrol yalnızca görünürlük içindir; gerçek yetki backend’dedir."
        />
      </AppShell>
    );
  }

  return (
    <AppShell
      currentHref="/depo"
      breadcrumbs={[
        { label: "Çalışma alanı", href: "/dashboard" },
        { label: "Depo", href: "/depo" },
        { label: "Transferler", href: "/depo/transferler" },
        { label: "Yeni" },
      ]}
      pageTitle="Yeni transfer"
      pageDescription="POST /warehouse-transfers. Temel miktar sunucu önizlemesinden gelir. Stok bu formda düşmez."
      pageActions={
        <div className="flex flex-wrap gap-2">
          <Button variant="secondary" onClick={() => router.push("/depo/transferler")}>
            Listeye dön
          </Button>
          <Button variant="secondary" loading={previewing} onClick={() => void runPreview()}>
            Önizle
          </Button>
          <Button loading={submitting} onClick={() => void submit()}>
            Taslak oluştur
          </Button>
        </div>
      }
    >
      {loadError ? (
        <ErrorState title="Form yüklenemedi" description={loadError} />
      ) : (
        <div className="grid gap-4 lg:grid-cols-2">
          <Card>
            <CardHeader>
              <CardTitle>Kaynak ve hedef</CardTitle>
            </CardHeader>
            <CardBody className="space-y-3">
              <Select
                label="Ürün"
                required
                value={productId}
                onChange={(event) => setProductId(event.target.value)}
                options={[
                  { value: "", label: "Seçin" },
                  ...products.map((row) => ({ value: row.id, label: `${row.code} — ${row.name}` })),
                ]}
              />
              <Select
                label="Ambalaj"
                value={packagingId}
                onChange={(event) => setPackagingId(event.target.value)}
                options={[
                  { value: "", label: "Temel birim" },
                  ...(product?.packagings ?? []).map((row) => ({
                    value: row.id,
                    label:
                      row.quantityInBaseUom === null
                        ? row.name
                        : `${row.name} (${row.quantityInBaseUom})`,
                  })),
                ]}
              />
              <Input
                label="Girilen miktar"
                type="number"
                min="0"
                step="any"
                required
                value={enteredQuantity}
                onChange={(event) => setEnteredQuantity(event.target.value)}
              />
              <QuantityViewToggle viewMode={viewMode} onViewModeChange={setViewMode} operationPackagingId={packagingId} />
              <Select
                label="Kaynak depo"
                required
                value={sourceWarehouseId}
                onChange={(event) => setSourceWarehouseId(event.target.value)}
                options={[
                  { value: "", label: "Seçin" },
                  ...warehouses.map((row) => ({ value: row.id, label: `${row.code} — ${row.name}` })),
                ]}
              />
              <Select
                label="Kaynak lokasyon"
                required
                value={sourceLocationId}
                onChange={(event) => setSourceLocationId(event.target.value)}
                options={[
                  { value: "", label: sourceWarehouseId ? "Seçin" : "Önce depo seçin" },
                  ...sourceLocations.map((row) => ({ value: row.id, label: `${row.code} — ${row.name}` })),
                ]}
              />
              <Select
                label="Hedef depo"
                required
                value={targetWarehouseId}
                onChange={(event) => setTargetWarehouseId(event.target.value)}
                options={[
                  { value: "", label: "Seçin" },
                  ...warehouses.map((row) => ({ value: row.id, label: `${row.code} — ${row.name}` })),
                ]}
              />
              <Select
                label="Hedef lokasyon"
                required
                value={targetLocationId}
                onChange={(event) => setTargetLocationId(event.target.value)}
                options={[
                  { value: "", label: targetWarehouseId ? "Seçin" : "Önce depo seçin" },
                  ...targetLocations.map((row) => ({ value: row.id, label: `${row.code} — ${row.name}` })),
                ]}
              />
            </CardBody>
          </Card>
          <Card>
            <CardHeader>
              <CardTitle>Sunucu önizleme</CardTitle>
            </CardHeader>
            <CardBody className="space-y-3">
              {error ? <Alert tone="danger" title="İşlem yapılamadı">{error}</Alert> : null}
              <QuantityEntryPreview
                isLoading={previewing}
                displayQuantity={preview?.enteredQuantity ?? (Number(enteredQuantity) || 0)}
                displayUnit={preview?.packagingName || "—"}
                baseQuantity={preview?.quantityBase === null || preview?.quantityBase === undefined ? "—" : preview.quantityBase}
                baseUnit="temel"
                conversionLabel={preview?.displayText || "Önizleme henüz yok"}
              />
              {preview?.availableBaseQuantity !== null && preview?.availableBaseQuantity !== undefined ? (
                <p className="text-sm text-slate-600">
                  Kaynak availableQtyBase: {String(preview.availableBaseQuantity)}
                </p>
              ) : (
                <p className="text-sm text-slate-600">Available, depo seçilip önizleme alınmadan yazılmaz.</p>
              )}
              {preview?.warnings.includes("INSUFFICIENT_AVAILABLE_STOCK") ? (
                <Alert tone="warning" title="Kullanılabilir stok yetersiz olabilir">
                  Bu bir uyarıdır. Complete sırasında sunucu tekrar kontrol eder.
                </Alert>
              ) : null}
              <p className="text-xs text-slate-500">
                viewMode yalnızca görünüm ve istek alanıdır. quantityBase sunucudadır.
              </p>
            </CardBody>
          </Card>
        </div>
      )}
    </AppShell>
  );
}
