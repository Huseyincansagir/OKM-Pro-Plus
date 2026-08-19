"use client";

import { useEffect, useState } from "react";
import { ClipboardList, Globe2, Mail, Package, Phone, UserRound } from "lucide-react";
import { AppShell } from "@/components/shell/app-shell";
import { EmptyState } from "@/components/states/empty-state";
import { ErrorState } from "@/components/states/error-state";
import { PermissionDenied } from "@/components/states/permission-denied";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader, CardTitle } from "@/components/ui/card";
import { DataTable } from "@/components/ui/data-table";
import { Dialog } from "@/components/ui/dialog";
import { Glyph } from "@/components/ui/glyph";
import { KpiMetric } from "@/components/dashboard/kpi-metric";
import { Select } from "@/components/ui/select";
import { StatusBadge } from "@/components/ui/status-badge";
import { listCustomers, type CustomerSummary } from "@/lib/sales/customers";
import { ApiError } from "@/lib/api/types";
import { userFacingMessage } from "@/lib/api/auth-client";
import { useSessionStore } from "@/lib/auth/session-store";
import {
  canReviewQuoteRequest,
  getQuoteRequest,
  quoteRequestSourceLabel,
  quoteRequestStatusKind,
  quoteRequestStatusLabel,
  reviewQuoteRequest,
  type QuoteRequestDetail as QuoteRequestDetailModel,
} from "@/lib/dashboard/quote-requests";

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

export function QuoteRequestDetail({ id }: { id: string }) {
  const user = useSessionStore((state) => state.user);
  const permissions = user?.permissions ?? [];
  const canRead = permissions.includes("quote-request.read");
  const canReview = permissions.includes("quote-request.review");
  const canReadCustomers = permissions.includes("customer.read");
  const [detail, setDetail] = useState<QuoteRequestDetailModel | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [denied, setDenied] = useState(false);
  const [loading, setLoading] = useState(canRead);
  const [reload, setReload] = useState(0);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [reviewing, setReviewing] = useState(false);
  const [reviewError, setReviewError] = useState<string | null>(null);
  const [customers, setCustomers] = useState<CustomerSummary[]>([]);
  const [selectedCustomerId, setSelectedCustomerId] = useState("");

  useEffect(() => {
    if (!canRead) {
      setLoading(false);
      return;
    }
    let cancelled = false;
    setLoading(true);
    setError(null);
    setDenied(false);
    getQuoteRequest(id)
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

  useEffect(() => {
    if (!canReadCustomers) {
      return;
    }
    let cancelled = false;
    listCustomers()
      .then((rows) => {
        if (!cancelled) setCustomers(rows.filter((row) => row.status === "Active"));
      })
      .catch(() => {
        if (!cancelled) setCustomers([]);
      });
    return () => {
      cancelled = true;
    };
  }, [canReadCustomers]);

  async function confirmReview() {
    setReviewing(true);
    setReviewError(null);
    try {
      const next = await reviewQuoteRequest(id, selectedCustomerId || null);
      setDetail(next);
      setConfirmOpen(false);
    } catch (caught) {
      setReviewError(userFacingMessage(caught));
    } finally {
      setReviewing(false);
    }
  }

  const reviewable = Boolean(detail && canReviewQuoteRequest(detail.status));

  return (
    <AppShell
      currentHref="/satis/teklif-talepleri"
      breadcrumbs={[
        { label: "Çalışma alanı", href: "/dashboard" },
        { label: "Satış", href: "/satis/teklif-talepleri" },
        { label: detail?.requestNumber || "Talep" },
      ]}
      pageTitle={detail?.requestNumber || "Teklif talebi"}
      pageDescription="Talep kalemlerindeki temel karşılık sunucudan gelir; tarayıcı quantityBase üretmez."
      pageActions={
        <div className="flex flex-wrap gap-2">
          <Button variant="secondary" onClick={() => setReload((value) => value + 1)} loading={loading}>
            Yenile
          </Button>
          {canReview && reviewable ? (
            <Button onClick={() => setConfirmOpen(true)}>İncelemeye al</Button>
          ) : null}
        </div>
      }
    >
      {!canRead ? (
        <PermissionDenied
          title="Teklif talebi bu oturumda görünmez"
          description="quote-request.read yok. Bu kontrol yalnızca görünürlük içindir; gerçek yetki backend’dedir."
        />
      ) : denied ? (
        <PermissionDenied />
      ) : error ? (
        <ErrorState
          title="Teklif talebi yüklenemedi"
          description={error}
          onRetry={() => setReload((value) => value + 1)}
        />
      ) : loading || !detail ? (
        <DataTable
          columns={[
            { id: "packaging", header: "Ambalaj", accessor: () => null },
            { id: "qty", header: "Girilen miktar", accessor: () => null },
            { id: "base", header: "Temel karşılık", accessor: () => null },
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
              value={quoteRequestStatusLabel(detail.status)}
              icon={ClipboardList}
              tone="amber"
              caption={detail.status}
            />
            <KpiMetric
              label="Kaynak"
              value={quoteRequestSourceLabel(detail.source)}
              icon={Globe2}
              tone="teal"
              caption="Public katalog veya iç kayıt"
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
              label="Oluşturma"
              value={formatDateTime(detail.createdAt)}
              icon={ClipboardList}
              tone="teal"
              caption="Sunucu createdAt"
            />
          </div>

          <div className="grid gap-4 lg:grid-cols-2">
            <Card>
              <CardHeader>
                <div className="flex items-center gap-2">
                  <Glyph icon={UserRound} />
                  <CardTitle>Aday</CardTitle>
                </div>
              </CardHeader>
              <CardBody className="space-y-3 text-sm text-slate-600">
                <p className="flex items-center gap-2 text-navy-950">
                  <Glyph icon={UserRound} tone="navy" />
                  {detail.candidateName || "—"}
                </p>
                <p className="flex items-center gap-2">
                  <Glyph icon={Mail} tone="navy" />
                  {detail.candidateEmail || "—"}
                </p>
                <p className="flex items-center gap-2">
                  <Glyph icon={Phone} tone="navy" />
                  {detail.candidatePhone || "—"}
                </p>
              </CardBody>
            </Card>
            <Card>
              <CardHeader>
                <CardTitle>Sonraki adım</CardTitle>
                <StatusBadge
                  status={quoteRequestStatusKind(detail.status)}
                  label={quoteRequestStatusLabel(detail.status)}
                />
              </CardHeader>
              <CardBody className="space-y-3">
                {detail.customerId ? (
                  <p className="text-sm text-navy-950">
                    Bağlı müşteri kimliği: {detail.customerId}
                  </p>
                ) : (
                  <Alert tone="info" title="Henüz müşteri bağlı değil">
                    İnceleme yalnızca Status=Active kartlara bağlar. Aday kartı burada oluşturulmaz.
                  </Alert>
                )}
                {!canReview ? (
                  <p className="text-sm text-slate-600">
                    quote-request.review yok. Buton gizlidir; yetki backend’dedir.
                  </p>
                ) : null}
                {!canReadCustomers ? (
                  <p className="text-sm text-slate-600">
                    customer.read yok. Müşteri seçici görünmez; yetki backend’dedir.
                  </p>
                ) : null}
              </CardBody>
            </Card>
          </div>

          {detail.items.length === 0 ? (
            <EmptyState title="Kalem yok" description="Bu talepte ürün satırı bulunmuyor." />
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
                      id: "base",
                      header: "Temel karşılık",
                      accessor: (row) =>
                        row.quantityBase === null ? "—" : String(row.quantityBase),
                    },
                  ]}
                  rows={detail.items}
                  getRowId={(row) => row.id}
                />
                <p className="mt-3 text-xs text-slate-500">
                  Temel karşılık sunucu `quantityBase` alanıdır. İstemci çarpma/bölme yapmaz.
                </p>
              </CardBody>
            </Card>
          )}
        </div>
      )}

      <Dialog
        open={confirmOpen}
        onOpenChange={setConfirmOpen}
        title="Talebi incelemeye al"
        description="Durum InReview olur. Sipariş veya teklif belgesi oluşmaz. Yalnızca Active müşteri bağlanır."
        footer={
          <>
            <Button variant="secondary" onClick={() => setConfirmOpen(false)}>
              Vazgeç
            </Button>
            <Button loading={reviewing} onClick={() => void confirmReview()}>
              İncelemeye al
            </Button>
          </>
        }
      >
        {canReadCustomers && customers.length > 0 ? (
          <Select
            label="Aktif müşteri"
            name="customerId"
            value={selectedCustomerId}
            onChange={(event) => setSelectedCustomerId(event.target.value)}
            options={[
              { value: "", label: "Bağlama (customerId null)" },
              ...customers.map((customer) => ({
                value: customer.id,
                label: `${customer.customerCode} · ${customer.legalName}`,
              })),
            ]}
            hint="Backend yalnızca Active kart kabul eder."
          />
        ) : (
          <p className="text-sm text-slate-600">
            Aktif müşteri seçilemedi. Gövde customerId: null gider.
          </p>
        )}
        {reviewError ? (
          <Alert tone="danger" title="İnceleme kaydedilemedi">
            {reviewError}
          </Alert>
        ) : null}
      </Dialog>
    </AppShell>
  );
}
