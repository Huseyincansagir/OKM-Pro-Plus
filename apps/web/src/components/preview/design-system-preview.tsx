"use client";

import { useState } from "react";
import { AppShell } from "@/components/shell/app-shell";
import { QuantityEntryPreview } from "@/components/quantity/quantity-entry-preview";
import { QuantityViewToggle } from "@/components/quantity/quantity-view-toggle";
import { EmptyState } from "@/components/states/empty-state";
import { ErrorState } from "@/components/states/error-state";
import { PermissionDenied } from "@/components/states/permission-denied";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader, CardTitle } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import { DataTable } from "@/components/ui/data-table";
import { Dialog } from "@/components/ui/dialog";
import { Drawer } from "@/components/ui/drawer";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { StatusBadge } from "@/components/ui/status-badge";
import { Tabs } from "@/components/ui/tabs";
import { useToast } from "@/components/ui/toast";
import { Tooltip } from "@/components/ui/tooltip";
import type { QuantityViewMode } from "@/types/quantity";

const CANONICAL_DISPLAY: Record<
  QuantityViewMode,
  { displayQuantity: string; displayUnit: string; conversionLabel: string }
> = {
  BaseUnit: {
    displayQuantity: "10.000",
    displayUnit: "adet",
    conversionLabel: "10.000 adet — sunucu display.baseUnit",
  },
  Packaging: {
    displayQuantity: "5",
    displayUnit: "Koli",
    conversionLabel: "5 Koli (10.000 adet) — sunucu display.packaging",
  },
  Breakdown: {
    displayQuantity: "5 Koli",
    displayUnit: "20 Paket",
    conversionLabel: "5 Koli → 20 Paket → 10.000 adet — sunucu display.breakdown",
  },
};

const sampleRows = [
  { id: "ornek-1", name: "Örnek kalem A", status: "pending" as const },
  { id: "ornek-2", name: "Örnek kalem B", status: "success" as const },
];

export function DesignSystemPreview() {
  const { pushToast } = useToast();
  const [viewMode, setViewMode] = useState<QuantityViewMode>("Packaging");
  const [operationPackagingId] = useState("pkg-case-demo");
  const [enteredQuantity] = useState(5);
  const [quantityBase] = useState("10.000");
  const [stockQuantity] = useState("50.000");
  const [dialogOpen, setDialogOpen] = useState(false);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [tab, setTab] = useState("bilesenler");
  const [sort, setSort] = useState<{ id: string; direction: "asc" | "desc" }>({
    id: "name",
    direction: "asc",
  });
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [page, setPage] = useState(1);
  const display = CANONICAL_DISPLAY[viewMode];

  return (
    <AppShell
      currentHref="/"
      breadcrumbs={[
        { label: "Çalışma alanı", href: "/" },
        { label: "Tasarım sistemi" },
      ]}
      pageTitle="Tasarım sistemi önizlemesi"
      pageDescription="WEB SLICE 002 — AppShell, ortak bileşenler ve miktar görünümü. İş verisi veya API bağlanmamıştır."
      pageActions={
        <>
          <Button variant="secondary" onClick={() => setDrawerOpen(true)}>
            Drawer aç
          </Button>
          <Button onClick={() => setDialogOpen(true)}>Onay penceresi</Button>
        </>
      }
    >
      <Alert tone="info" title="Bu sayfa işletme ekranı değildir">
        Dashboard, sipariş ve stok verileri sonraki slice’larda bağlanacaktır.
      </Alert>

      <div className="mt-6">
        <Tabs
          value={tab}
          onValueChange={setTab}
          tabs={[
            { id: "bilesenler", label: "Bileşenler" },
            { id: "tablo", label: "Tablo" },
            { id: "durumlar", label: "Durumlar" },
          ]}
        />
      </div>

      {tab === "bilesenler" ? (
        <div className="mt-6 grid gap-4 lg:grid-cols-2">
          <Card>
            <CardHeader>
              <CardTitle>Durum ve aksiyon</CardTitle>
            </CardHeader>
            <CardBody className="flex flex-wrap gap-2">
              <StatusBadge status="pending" />
              <StatusBadge status="active" />
              <StatusBadge status="success" />
              <StatusBadge status="critical" />
              <StatusBadge status="info" />
              <StatusBadge status="inactive" />
              <Tooltip content="Sayfadaki primary action başlıktaki onay penceresidir">
                <Button
                  size="sm"
                  variant="secondary"
                  onClick={() =>
                    pushToast({
                      title: "Önizleme bildirimi",
                      description: "Kayıt API’si bağlı değil.",
                      tone: "info",
                    })
                  }
                >
                  Toast göster
                </Button>
              </Tooltip>
            </CardBody>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Form alanları</CardTitle>
            </CardHeader>
            <CardBody className="grid gap-3">
              <Input
                label="Örnek alan"
                name="ornek"
                placeholder="Değer girin"
                hint="Bu alan yalnızca bileşen önizlemesidir."
              />
              <Select
                label="Örnek seçim"
                name="secim"
                options={[
                  { value: "a", label: "Seçenek A" },
                  { value: "b", label: "Seçenek B" },
                ]}
              />
              <Checkbox label="Örnek onay" hint="İş kuralı değiştirmez." />
            </CardBody>
          </Card>

          <Card className="lg:col-span-2">
            <CardHeader>
              <CardTitle>Miktar görünümü</CardTitle>
            </CardHeader>
            <CardBody className="space-y-3">
              <p className="text-sm text-slate-600">
                Toggle yalnızca görünümü değiştirir. İşlem ambalajı, girilen miktar,
                temel karşılık ve stok aynı kalır.
              </p>
              <QuantityViewToggle
                viewMode={viewMode}
                onViewModeChange={setViewMode}
                operationPackagingId={operationPackagingId}
              />
              <dl className="grid gap-2 text-sm sm:grid-cols-2">
                <div>
                  <dt className="text-slate-500">viewMode</dt>
                  <dd className="font-semibold">{viewMode}</dd>
                </div>
                <div>
                  <dt className="text-slate-500">operationPackagingId</dt>
                  <dd className="font-semibold">{operationPackagingId}</dd>
                </div>
                <div>
                  <dt className="text-slate-500">Girilen miktar</dt>
                  <dd className="font-semibold">{enteredQuantity}</dd>
                </div>
                <div>
                  <dt className="text-slate-500">quantityBase / stok</dt>
                  <dd className="font-semibold">
                    {quantityBase} adet / {stockQuantity} adet
                  </dd>
                </div>
              </dl>
              <QuantityEntryPreview
                displayQuantity={display.displayQuantity}
                displayUnit={display.displayUnit}
                baseQuantity={quantityBase}
                baseUnit="adet"
                conversionLabel={display.conversionLabel}
              />
            </CardBody>
          </Card>
        </div>
      ) : null}

      {tab === "tablo" ? (
        <div className="mt-6">
          <DataTable
            columns={[
              { id: "name", header: "Kayıt", accessor: (row) => row.name, sortable: true },
              {
                id: "status",
                header: "Durum",
                accessor: (row) => <StatusBadge status={row.status} />,
              },
            ]}
            rows={sampleRows}
            getRowId={(row) => row.id}
            sort={sort}
            onSortChange={setSort}
            selectedIds={selectedIds}
            onSelectionChange={setSelectedIds}
            page={page}
            pageSize={10}
            totalCount={sampleRows.length}
            onPageChange={setPage}
          />
        </div>
      ) : null}

      {tab === "durumlar" ? (
        <div className="mt-6 grid gap-4 lg:grid-cols-3">
          <EmptyState
            title="Kayıt yok"
            description="Bu liste örnek boş durumudur."
            action={
              <Button size="sm" variant="secondary">
                İlk kaydı sonraki slice’ta ekle
              </Button>
            }
          />
          <ErrorState
            title="Sunucu yanıt vermedi"
            description="Teknik ayrıntı kullanıcıya gösterilmez."
            onRetry={() => undefined}
          />
          <PermissionDenied />
        </div>
      ) : null}

      <Dialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        title="Kritik işlem onayı"
        description="Bu pencere örnek onayıdır. Stok veya cari hareket oluşturmaz."
        footer={
          <>
            <Button variant="secondary" onClick={() => setDialogOpen(false)}>
              Vazgeç
            </Button>
            <Button variant="danger" onClick={() => setDialogOpen(false)}>
              Onayla
            </Button>
          </>
        }
      >
        <p className="text-sm text-slate-600">
          Etki özeti: örnek kayıt. Gerçek belge veya miktar değişmez.
        </p>
      </Dialog>

      <Drawer open={drawerOpen} onOpenChange={setDrawerOpen} title="Örnek drawer">
        <p className="text-sm text-slate-600">
          Hızlı önizleme alanı. API ve iş ekranları WEB SLICE 003 sonrası bağlanır.
        </p>
      </Drawer>
    </AppShell>
  );
}
