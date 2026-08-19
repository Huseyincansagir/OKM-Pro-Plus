"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Building2, ClipboardCheck, FileText, Package, Wallet } from "lucide-react";
import { AppShell } from "@/components/shell/app-shell";
import { KpiMetric } from "@/components/dashboard/kpi-metric";
import { EmptyState } from "@/components/states/empty-state";
import { ErrorState } from "@/components/states/error-state";
import { PermissionDenied } from "@/components/states/permission-denied";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader, CardTitle } from "@/components/ui/card";
import { DataTable } from "@/components/ui/data-table";
import { Dialog } from "@/components/ui/dialog";
import { Glyph } from "@/components/ui/glyph";
import { Input } from "@/components/ui/input";
import { StatusBadge } from "@/components/ui/status-badge";
import { ApiError } from "@/lib/api/types";
import { userFacingMessage } from "@/lib/api/auth-client";
import { useSessionStore } from "@/lib/auth/session-store";
import {
  approveSalesOrder,
  canDecideSalesOrder,
  canSubmitSalesOrder,
  getSalesOrder,
  rejectSalesOrder,
  salesOrderStatusKind,
  salesOrderStatusLabel,
  submitSalesOrder,
  type SalesOrderDetail as SalesOrderDetailModel,
} from "@/lib/sales/orders";
import { createDeliveryNote } from "@/lib/shipping/delivery-notes";

function formatDateTime(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return iso || "—";
  }
  return new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "short",
    timeStyle: "short",
  }).format(date);
}

function formatMoney(value: number | null, currency: string): string {
  if (value === null || !currency) {
    return "—";
  }
  return new Intl.NumberFormat("tr-TR", {
    style: "currency",
    currency,
    maximumFractionDigits: 2,
  }).format(value);
}

export function OrderDetail({ id }: { id: string }) {
  const router = useRouter();
  const user = useSessionStore((state) => state.user);
  const permissions = user?.permissions ?? [];
  const canRead = permissions.includes("order.read");
  const canSubmit = permissions.includes("order.submit");
  const canApprove = permissions.includes("order.approve");
  const canReject = permissions.includes("order.reject");
  const [detail, setDetail] = useState<SalesOrderDetailModel | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [denied, setDenied] = useState(false);
  const [loading, setLoading] = useState(canRead);
  const [reload, setReload] = useState(0);
  const canCreateNote = permissions.includes("delivery-note.create");
  const [submitOpen, setSubmitOpen] = useState(false);
  const [decideOpen, setDecideOpen] = useState<"approve" | "reject" | null>(null);
  const [noteOpen, setNoteOpen] = useState(false);
  const [rejectComment, setRejectComment] = useState("");
  const [acting, setActing] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  useEffect(() => {
    if (!canRead) {
      setLoading(false);
      return;
    }
    let cancelled = false;
    setLoading(true);
    setError(null);
    setDenied(false);
    getSalesOrder(id)
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

  async function confirmSubmit() {
    setActing(true);
    setActionError(null);
    try {
      const next = await submitSalesOrder(id);
      setDetail(next);
      setSubmitOpen(false);
    } catch (caught) {
      setActionError(userFacingMessage(caught));
    } finally {
      setActing(false);
    }
  }

  async function confirmDecide() {
    if (decideOpen === "reject" && !rejectComment.trim()) {
      setActionError("Red için gerekçe zorunludur.");
      return;
    }
    setActing(true);
    setActionError(null);
    try {
      const next =
        decideOpen === "approve"
          ? await approveSalesOrder(id)
          : await rejectSalesOrder(id, rejectComment.trim());
      setDetail(next);
      setDecideOpen(null);
      setRejectComment("");
    } catch (caught) {
      setActionError(userFacingMessage(caught));
    } finally {
      setActing(false);
    }
  }

  const submittable = Boolean(detail && canSubmit && canSubmitSalesOrder(detail.status));
  const decidable = Boolean(detail && canDecideSalesOrder(detail.status));
  const remainingLines =
    detail?.items.filter((item) => item.remainingQty !== null && item.remainingQty > 0) ?? [];
  const shippable = Boolean(
    detail &&
      canCreateNote &&
      ["Approved", "Preparing", "PartiallyShipped"].includes(detail.status) &&
      remainingLines.length > 0,
  );

  async function confirmDeliveryNote() {
    if (!detail) return;
    setActing(true);
    setActionError(null);
    try {
      const note = await createDeliveryNote({
        salesOrderId: detail.id,
        items: remainingLines.map((item) => ({
          salesOrderItemId: item.id,
          enteredQuantity: item.remainingQty as number,
          enteredPackagingId: null,
          viewMode: "BaseUnit",
        })),
      });
      setNoteOpen(false);
      router.push(`/sevkiyat/irsaliyeler/${note.id}`);
    } catch (caught) {
      setActionError(userFacingMessage(caught));
    } finally {
      setActing(false);
    }
  }

  return (
    <AppShell
      currentHref="/satis/siparisler"
      breadcrumbs={[
        { label: "Çalışma alanı", href: "/dashboard" },
        { label: "Satış", href: "/satis/teklif-talepleri" },
        { label: "Siparişler", href: "/satis/siparisler" },
        { label: detail?.orderNumber || "Belge" },
      ]}
      pageTitle={detail?.orderNumber || "Sipariş"}
      pageDescription="Temel karşılık ve kalan miktar sunucudan gelir. İstemci quantityBase üretmez."
      pageActions={
        <div className="flex flex-wrap gap-2">
          <Button variant="secondary" onClick={() => router.push("/satis/siparisler")}>
            Listeye dön
          </Button>
          <Button variant="secondary" onClick={() => setReload((value) => value + 1)} loading={loading}>
            Yenile
          </Button>
          {submittable ? <Button onClick={() => setSubmitOpen(true)}>Onaya gönder</Button> : null}
          {decidable && canApprove ? (
            <Button onClick={() => setDecideOpen("approve")}>Onayla</Button>
          ) : null}
          {decidable && canReject ? (
            <Button variant="danger" onClick={() => setDecideOpen("reject")}>
              Reddet
            </Button>
          ) : null}
          {shippable ? <Button onClick={() => setNoteOpen(true)}>İrsaliye oluştur</Button> : null}
        </div>
      }
    >
      {!canRead ? (
        <PermissionDenied
          title="Sipariş bu oturumda görünmez"
          description="order.read yok. Bu kontrol yalnızca görünürlük içindir; gerçek yetki backend’dedir."
        />
      ) : denied ? (
        <PermissionDenied />
      ) : error ? (
        <ErrorState
          title="Sipariş yüklenemedi"
          description={error}
          onRetry={() => setReload((value) => value + 1)}
        />
      ) : loading || !detail ? (
        <DataTable
          columns={[
            { id: "packaging", header: "Ambalaj", accessor: () => null },
            { id: "qty", header: "Girilen miktar", accessor: () => null },
          ]}
          rows={[]}
          getRowId={() => ""}
          loading
        />
      ) : (
        <div className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <KpiMetric
              label="Durum"
              value={salesOrderStatusLabel(detail.status)}
              icon={ClipboardCheck}
              tone="amber"
              caption={detail.status}
            />
            <KpiMetric
              label="Tutar"
              value={formatMoney(detail.totalGross, detail.currencyCode || "TRY")}
              icon={Wallet}
              tone="teal"
              caption="Sunucu totalGross"
            />
            <KpiMetric
              label="Kalem"
              value={String(detail.itemCount)}
              unit="satır"
              icon={Package}
              tone="navy"
              caption="Satır sayısı · miktar değil"
            />
            <KpiMetric
              label="Kayıt"
              value={formatDateTime(detail.createdAt)}
              icon={FileText}
              tone="teal"
              caption="Sunucu createdAt"
            />
          </div>

          <div className="grid gap-4 lg:grid-cols-2">
            <Card>
              <CardHeader>
                <div className="flex items-center gap-2">
                  <Glyph icon={Building2} />
                  <CardTitle>Müşteri</CardTitle>
                </div>
              </CardHeader>
              <CardBody className="space-y-3 text-sm text-slate-600">
                <p className="text-navy-950">
                  {detail.customerCode
                    ? `${detail.customerCode} · ${detail.customerLegalName}`
                    : detail.customerLegalName || "—"}
                </p>
                {detail.customerId ? (
                  <Link href={`/satis/musteriler/${detail.customerId}`} className="font-semibold text-teal-600">
                    Rehber kartı
                  </Link>
                ) : null}
              </CardBody>
            </Card>
            <Card>
              <CardHeader>
                <CardTitle>Sonraki adım</CardTitle>
                <StatusBadge
                  status={salesOrderStatusKind(detail.status)}
                  label={salesOrderStatusLabel(detail.status)}
                />
              </CardHeader>
              <CardBody className="space-y-3">
                {detail.status === "Draft" ? (
                  <Alert tone="info" title="Taslak">
                    Onaya gönderme rezervasyon yapmaz. Onay stok ayırır.
                  </Alert>
                ) : null}
                {detail.status === "PendingApproval" ? (
                  <Alert tone="warning" title="Onay bekliyor">
                    Onay kullanılabilir stok yoksa reddedilir. Sahte başarı yok.
                  </Alert>
                ) : null}
                {!canSubmit && detail.status === "Draft" ? (
                  <p className="text-sm text-slate-600">order.submit yok. Buton gizlidir.</p>
                ) : null}
              </CardBody>
            </Card>
          </div>

          {detail.items.length === 0 ? (
            <EmptyState title="Kalem yok" description="Bu siparişte ürün satırı bulunmuyor." />
          ) : (
            <Card>
              <CardHeader>
                <div className="flex items-center gap-2">
                  <Glyph icon={Package} />
                  <CardTitle>Kalemler</CardTitle>
                </div>
              </CardHeader>
              <CardBody className="pt-3">
                <DataTable
                  columns={[
                    {
                      id: "packaging",
                      header: "Ambalaj",
                      accessor: (row) => (
                        <span className="inline-flex items-center gap-2">
                          <Glyph icon={Package} />
                          {row.packagingName}
                        </span>
                      ),
                    },
                    {
                      id: "qty",
                      header: "Girilen miktar",
                      accessor: (row) => String(row.enteredQuantity),
                    },
                    {
                      id: "ordered",
                      header: "Temel sipariş",
                      accessor: (row) => (row.orderedQty === null ? "—" : String(row.orderedQty)),
                    },
                    {
                      id: "remaining",
                      header: "Kalan",
                      accessor: (row) => (row.remainingQty === null ? "—" : String(row.remainingQty)),
                    },
                    {
                      id: "price",
                      header: "Birim fiyat",
                      accessor: (row) => formatMoney(row.unitPrice, detail.currencyCode || "TRY"),
                    },
                  ]}
                  rows={detail.items}
                  getRowId={(row) => row.id}
                />
                <p className="mt-3 text-xs text-slate-500">
                  Temel sipariş ve kalan, sunucu `orderedQty` / `remainingQty` alanlarıdır.
                </p>
              </CardBody>
            </Card>
          )}
        </div>
      )}

      <Dialog
        open={submitOpen}
        onOpenChange={setSubmitOpen}
        title="Siparişi onaya gönder"
        description="Durum PendingApproval olur. Stok henüz ayrılmaz."
        footer={
          <>
            <Button variant="secondary" onClick={() => setSubmitOpen(false)}>
              Vazgeç
            </Button>
            <Button loading={acting} onClick={() => void confirmSubmit()}>
              Onaya gönder
            </Button>
          </>
        }
      >
        {actionError && submitOpen ? (
          <Alert tone="danger" title="Gönderilemedi">
            {actionError}
          </Alert>
        ) : (
          <p className="text-sm text-slate-600">Onaylayan kişi stok ve rezervasyonu kontrol eder.</p>
        )}
      </Dialog>

      <Dialog
        open={decideOpen !== null}
        onOpenChange={(open) => {
          if (!open) {
            setDecideOpen(null);
            setRejectComment("");
            setActionError(null);
          }
        }}
        title={decideOpen === "reject" ? "Siparişi reddet" : "Siparişi onayla"}
        description={
          decideOpen === "reject"
            ? "Gerekçe zorunludur. Durum Cancelled olur."
            : "Onay kullanılabilir stok varsa rezervasyon açar."
        }
        footer={
          <>
            <Button variant="secondary" onClick={() => setDecideOpen(null)}>
              Vazgeç
            </Button>
            <Button
              variant={decideOpen === "reject" ? "danger" : "primary"}
              loading={acting}
              onClick={() => void confirmDecide()}
            >
              {decideOpen === "reject" ? "Reddet" : "Onayla"}
            </Button>
          </>
        }
      >
        {decideOpen === "reject" ? (
          <Input
            label="Gerekçe"
            name="rejectComment"
            required
            value={rejectComment}
            onChange={(event) => setRejectComment(event.target.value)}
          />
        ) : (
          <p className="text-sm text-slate-600">Yetersiz stokta backend onaylamaz.</p>
        )}
        {actionError && decideOpen ? (
          <Alert tone="danger" title="İşlem kaydedilemedi">
            {actionError}
          </Alert>
        ) : null}
      </Dialog>

      <Dialog
        open={noteOpen}
        onOpenChange={(open) => {
          if (!open) {
            setNoteOpen(false);
            setActionError(null);
          }
        }}
        title="İrsaliye oluştur"
        description="Kalan temel miktar (remainingQty) BaseUnit olarak gönderilir. İstemci ambalaj çarpanı uygulamaz."
        footer={
          <>
            <Button variant="secondary" onClick={() => setNoteOpen(false)}>
              Vazgeç
            </Button>
            <Button loading={acting} onClick={() => void confirmDeliveryNote()}>
              Taslak irsaliye
            </Button>
          </>
        }
      >
        <p className="text-sm text-slate-600">{remainingLines.length} kalan kalem. Stok issue anında düşer.</p>
        {actionError && noteOpen ? (
          <Alert tone="danger" title="İrsaliye oluşmadı">
            {actionError}
          </Alert>
        ) : null}
      </Dialog>
    </AppShell>
  );
}
