"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { FileText, Layers, Package, Truck } from "lucide-react";
import { AppShell } from "@/components/shell/app-shell";
import { KpiMetric } from "@/components/dashboard/kpi-metric";
import { ErrorState } from "@/components/states/error-state";
import { PermissionDenied } from "@/components/states/permission-denied";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader, CardTitle } from "@/components/ui/card";
import { DataTable } from "@/components/ui/data-table";
import { Dialog } from "@/components/ui/dialog";
import { StatusBadge } from "@/components/ui/status-badge";
import { ApiError } from "@/lib/api/types";
import { userFacingMessage } from "@/lib/api/auth-client";
import { useSessionStore } from "@/lib/auth/session-store";
import {
  canIssueDeliveryNote,
  deliveryNoteStatusKind,
  getDeliveryNote,
  issueDeliveryNote,
  type DeliveryNoteDetail,
} from "@/lib/shipping/delivery-notes";
import { createShipment } from "@/lib/shipping/shipments";
import { createInvoice } from "@/lib/finance/invoices";
import { Input } from "@/components/ui/input";

export function DeliveryNoteDetailBoard({ id }: { id: string }) {
  const router = useRouter();
  const user = useSessionStore((state) => state.user);
  const permissions = user?.permissions ?? [];
  const canRead = permissions.includes("delivery-note.read");
  const canIssue = permissions.includes("delivery-note.issue");
  const canCreateShipment = permissions.includes("shipment.create");
  const canCreateInvoice = permissions.includes("invoice.create");
  const [note, setNote] = useState<DeliveryNoteDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [denied, setDenied] = useState(false);
  const [loading, setLoading] = useState(canRead);
  const [reload, setReload] = useState(0);
  const [acting, setActing] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const [confirm, setConfirm] = useState<"issue" | "shipment" | "invoice" | null>(null);
  const [unitPrices, setUnitPrices] = useState<Record<string, number>>({});

  useEffect(() => {
    if (!canRead) {
      setLoading(false);
      return;
    }
    let cancelled = false;
    setLoading(true);
    setError(null);
    setDenied(false);
    getDeliveryNote(id)
      .then((result) => {
        if (!cancelled) setNote(result);
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

  async function runAction() {
    if (!note || !confirm) return;
    setActing(true);
    setActionError(null);
    try {
      if (confirm === "issue") {
        setNote(await issueDeliveryNote(note.id));
        setConfirm(null);
        return;
      }
      if (confirm === "invoice") {
        const invoiceableItems = note.items.filter((item) => (item.remainingToInvoice ?? 0) > 0);
        if (invoiceableItems.length === 0) {
          setActionError("Faturalanabilir kalan miktar bulunamadı.");
          return;
        }
        const createdInvoice = await createInvoice({
          customerId: note.customerId,
          currencyCode: "TRY",
          items: invoiceableItems.map((item) => ({
            deliveryNoteItemId: item.id,
            enteredQuantity: item.remainingToInvoice ?? item.quantityBase ?? 1,
            enteredPackagingId: null,
            viewMode: "Piece",
            unitPrice: unitPrices[item.id] ?? 0,
            taxCodeId: null,
          })),
        });
        setConfirm(null);
        router.push(`/cari/faturalar/${createdInvoice.id}`);
        return;
      }
      if (note.rowVersion === null) {
        setActionError("İrsaliye rowVersion yok; sevkiyat oluşturulamaz.");
        return;
      }
      const shipment = await createShipment({
        deliveryNoteId: note.id,
        expectedDeliveryNoteRowVersion: note.rowVersion,
      });
      setConfirm(null);
      router.push(`/sevkiyat/${shipment.id}`);
    } catch (caught) {
      setActionError(userFacingMessage(caught));
    } finally {
      setActing(false);
    }
  }

  return (
    <AppShell
      currentHref="/sevkiyat"
      breadcrumbs={[
        { label: "Çalışma alanı", href: "/dashboard" },
        { label: "Sevkiyat", href: "/sevkiyat" },
        { label: note?.documentNumber || "İrsaliye" },
      ]}
      pageTitle={note?.documentNumber || "İrsaliye"}
      pageDescription="GET /delivery-notes/{id}. Issue stok düşer (DeliveryIssue). Shipment yalnız Issued irsaliyeden oluşur."
      pageActions={
        <div className="flex flex-wrap gap-2">
          <Button variant="secondary" onClick={() => router.push("/sevkiyat")}>
            Sevkiyatlar
          </Button>
          {canRead ? (
            <Button variant="secondary" loading={loading} onClick={() => setReload((value) => value + 1)}>
              Yenile
            </Button>
          ) : null}
          {note && canIssue && canIssueDeliveryNote(note.status) ? (
            <Button onClick={() => setConfirm("issue")}>Kesinleştir</Button>
          ) : null}
          {note && canCreateShipment && note.status === "Issued" ? (
            <Button onClick={() => setConfirm("shipment")}>Sevkiyat oluştur</Button>
          ) : null}
          {note && canCreateInvoice && note.status === "Issued" && note.items.some((i) => (i.remainingToInvoice ?? 0) > 0) ? (
            <Button onClick={() => setConfirm("invoice")}>Fatura oluştur</Button>
          ) : null}
        </div>
      }
    >
      {!canRead ? (
        <PermissionDenied
          title="İrsaliye bu oturumda görünmez"
          description="delivery-note.read yok. Bu kontrol yalnızca görünürlük içindir; gerçek yetki backend’dedir."
        />
      ) : denied ? (
        <PermissionDenied />
      ) : error ? (
        <ErrorState title="İrsaliye yüklenemedi" description={error} onRetry={() => setReload((value) => value + 1)} />
      ) : loading || !note ? (
        <p className="text-sm text-slate-600">Yükleniyor…</p>
      ) : (
        <div className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <KpiMetric
              label="Durum"
              value={note.status}
              icon={FileText}
              tone="amber"
              caption="Sunucu status"
            />
            <KpiMetric
              label="Kalem"
              value={String(note.itemCount)}
              unit="satır"
              icon={Package}
              tone="navy"
              caption="Satır sayısı"
            />
            <KpiMetric
              label="Sipariş"
              value={note.salesOrderId.slice(0, 8) || "—"}
              icon={Layers}
              tone="navy"
              caption="salesOrderId"
            />
            <KpiMetric
              label="Sevk"
              value={note.status === "Issued" ? "Issued" : "—"}
              icon={Truck}
              tone="teal"
              caption="Stok issue anında düşer"
            />
          </div>
          <Card>
            <CardHeader>
              <CardTitle>Kalemler</CardTitle>
            </CardHeader>
            <CardBody>
              <StatusBadge status={deliveryNoteStatusKind(note.status)} label={note.status} />
              <div className="mt-3">
                <DataTable
                  columns={[
                    { id: "product", header: "Ürün", accessor: (row) => row.productId.slice(0, 8) },
                    {
                      id: "qty",
                      header: "Temel miktar",
                      accessor: (row) => (row.quantityBase === null ? "—" : String(row.quantityBase)),
                    },
                    {
                      id: "shipped",
                      header: "Sevk",
                      accessor: (row) => (row.shippedQty === null ? "—" : String(row.shippedQty)),
                    },
                    {
                      id: "invoice",
                      header: "Fatura kalan",
                      accessor: (row) =>
                        row.remainingToInvoice === null ? "—" : String(row.remainingToInvoice),
                    },
                  ]}
                  rows={note.items}
                  getRowId={(row) => row.id}
                />
              </div>
            </CardBody>
          </Card>
        </div>
      )}

      <Dialog
        open={confirm !== null}
        onOpenChange={(open) => {
          if (!open && !acting) setConfirm(null);
        }}
        title={
          confirm === "issue"
            ? "İrsaliyeyi kesinleştir"
            : confirm === "shipment"
            ? "Sevkiyat oluştur"
            : "Fatura oluştur"
        }
        description={
          confirm === "issue"
            ? "Rezerve stok düşer, DeliveryIssue hareketi yazılır. İkinci issue yapılmaz."
            : confirm === "shipment"
            ? "Issued irsaliyeden Preparing sevkiyat oluşur. Aynı irsaliye ikinci kez bağlanamaz."
            : "Kalan faturalanabilir miktarlar üzerinden taslak fatura (Draft) oluşturulur."
        }
        footer={
          <div className="flex justify-end gap-2">
            <Button variant="secondary" disabled={acting} onClick={() => setConfirm(null)}>
              Vazgeç
            </Button>
            <Button loading={acting} onClick={() => void runAction()}>
              {confirm === "issue" ? "Kesinleştir" : confirm === "shipment" ? "Oluştur" : "Faturayı oluştur"}
            </Button>
          </div>
        }
      >
        {actionError ? <Alert tone="danger" title="Komut başarısız">{actionError}</Alert> : null}
        {confirm === "invoice" && note ? (
          <div className="space-y-3 text-sm">
            <p className="text-slate-600">Kalem birim fiyatlarını kontrol edin:</p>
            {note.items
              .filter((item) => (item.remainingToInvoice ?? 0) > 0)
              .map((item) => (
                <div key={item.id} className="flex items-center justify-between gap-2 border-b border-slate-100 pb-2">
                  <div>
                    <p className="font-medium text-slate-800">Ürün: {item.productId.slice(0, 8)}</p>
                    <p className="text-xs text-slate-500">Miktar: {item.remainingToInvoice}</p>
                  </div>
                  <div className="w-32">
                    <Input
                      label="Birim fiyat"
                      type="number"
                      value={unitPrices[item.id] !== undefined ? String(unitPrices[item.id]) : "0"}
                      onChange={(e) => {
                        const val = parseFloat(e.target.value);
                        setUnitPrices((prev) => ({
                          ...prev,
                          [item.id]: isNaN(val) ? 0 : val,
                        }));
                      }}
                    />
                  </div>
                </div>
              ))}
          </div>
        ) : null}
      </Dialog>
    </AppShell>
  );
}
