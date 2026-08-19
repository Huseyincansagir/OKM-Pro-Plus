"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Building2, Mail, Phone, Scale, Wallet } from "lucide-react";
import { AppShell } from "@/components/shell/app-shell";
import { KpiMetric } from "@/components/dashboard/kpi-metric";
import { EmptyState } from "@/components/states/empty-state";
import { ErrorState } from "@/components/states/error-state";
import { PermissionDenied } from "@/components/states/permission-denied";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader, CardTitle } from "@/components/ui/card";
import { Glyph } from "@/components/ui/glyph";
import { StatusBadge, type StatusKind } from "@/components/ui/status-badge";
import { ApiError } from "@/lib/api/types";
import { userFacingMessage } from "@/lib/api/auth-client";
import { useSessionStore } from "@/lib/auth/session-store";
import {
  customerStatusLabel,
  getCustomer,
  type CustomerSummary,
} from "@/lib/sales/customers";
import {
  getCurrentAccount,
  type CurrentAccountSummary,
} from "@/lib/sales/current-accounts";

function statusKind(status: string): StatusKind {
  if (status === "Active") return "success";
  if (status === "Candidate") return "pending";
  if (status === "Blocked") return "critical";
  return "inactive";
}

function formatMoney(value: number, currency: string): string {
  return new Intl.NumberFormat("tr-TR", {
    style: "currency",
    currency,
    maximumFractionDigits: 2,
  }).format(value);
}

export function CustomerDetail({ id }: { id: string }) {
  const router = useRouter();
  const user = useSessionStore((state) => state.user);
  const permissions = user?.permissions ?? [];
  const canRead = permissions.includes("customer.read");
  const canReadAccount = permissions.includes("current-account.read");
  const [customer, setCustomer] = useState<CustomerSummary | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [denied, setDenied] = useState(false);
  const [loading, setLoading] = useState(canRead);
  const [reload, setReload] = useState(0);
  const [account, setAccount] = useState<CurrentAccountSummary | null>(null);
  const [accountMissing, setAccountMissing] = useState(false);
  const [accountDenied, setAccountDenied] = useState(false);
  const [accountError, setAccountError] = useState<string | null>(null);
  const [accountLoading, setAccountLoading] = useState(canReadAccount);

  useEffect(() => {
    if (!canRead) {
      setLoading(false);
      return;
    }
    let cancelled = false;
    setLoading(true);
    setError(null);
    setDenied(false);
    getCustomer(id)
      .then((result) => {
        if (!cancelled) setCustomer(result);
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
    if (!canReadAccount) {
      setAccountLoading(false);
      return;
    }
    let cancelled = false;
    setAccountLoading(true);
    setAccount(null);
    setAccountMissing(false);
    setAccountDenied(false);
    setAccountError(null);
    getCurrentAccount(id)
      .then((result) => {
        if (cancelled) return;
        if (result === null) {
          setAccountMissing(true);
          return;
        }
        setAccount(result);
      })
      .catch((caught) => {
        if (cancelled) return;
        if (caught instanceof ApiError && caught.kind === "permission_denied") {
          setAccountDenied(true);
          return;
        }
        setAccountError(userFacingMessage(caught));
      })
      .finally(() => {
        if (!cancelled) setAccountLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [canReadAccount, id, reload]);

  return (
    <AppShell
      currentHref="/satis/musteriler"
      breadcrumbs={[
        { label: "Çalışma alanı", href: "/dashboard" },
        { label: "Satış", href: "/satis/teklif-talepleri" },
        { label: "Müşteriler", href: "/satis/musteriler" },
        { label: customer?.customerCode || "Kart" },
      ]}
      pageTitle={customer?.legalName || "Müşteri"}
      pageDescription="Cari bakiye yalnızca GET /current-accounts yanıtıdır. Hesap yoksa ₺0 yazılmaz."
      pageActions={
        <div className="flex flex-wrap gap-2">
          <Button variant="secondary" onClick={() => router.push("/satis/musteriler")}>
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
          title="Müşteri bu oturumda görünmez"
          description="customer.read yok. Bu kontrol yalnızca görünürlük içindir; gerçek yetki backend’dedir."
        />
      ) : denied ? (
        <PermissionDenied />
      ) : error ? (
        <ErrorState
          title="Müşteri yüklenemedi"
          description={error}
          onRetry={() => setReload((value) => value + 1)}
        />
      ) : loading || !customer ? (
        <p className="text-sm text-slate-600">Müşteri yükleniyor…</p>
      ) : (
        <div className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <KpiMetric
              label="Kod"
              value={customer.customerCode}
              icon={Building2}
              tone="navy"
              caption="GET /customers/{id}"
            />
            <KpiMetric
              label="Durum"
              value={customerStatusLabel(customer.status)}
              icon={Scale}
              tone="teal"
              caption={customer.status}
            />
            {account ? (
              <>
                <KpiMetric
                  label="Borç"
                  value={formatMoney(account.debitTotal, account.currencyCode)}
                  icon={Wallet}
                  tone="amber"
                  caption="GET /current-accounts/{id} · debitTotal"
                />
                <KpiMetric
                  label="Bakiye"
                  value={formatMoney(account.balance, account.currencyCode)}
                  icon={Wallet}
                  tone="teal"
                  caption="GET /current-accounts/{id} · balance"
                />
              </>
            ) : (
              <>
                <KpiMetric
                  label="Borç"
                  value="—"
                  icon={Wallet}
                  tone="amber"
                  unavailable
                  caption={
                    accountLoading
                      ? "Cari sorgulanıyor"
                      : accountDenied
                        ? "current-account.read yok"
                        : accountMissing
                          ? "Cari hesap henüz yok"
                          : "Cari bağlı değil"
                  }
                />
                <KpiMetric
                  label="Bakiye"
                  value="—"
                  icon={Wallet}
                  tone="teal"
                  unavailable
                  caption="Hesap yokken ₺0 yazılmaz"
                />
              </>
            )}
          </div>

          <div className="grid gap-4 lg:grid-cols-2">
            <Card>
              <CardHeader>
                <div className="flex items-center gap-2">
                  <Glyph icon={Building2} />
                  <CardTitle>İletişim</CardTitle>
                </div>
                <StatusBadge
                  status={statusKind(customer.status)}
                  label={customerStatusLabel(customer.status)}
                />
              </CardHeader>
              <CardBody className="space-y-3 text-sm text-slate-600">
                <p className="flex items-center gap-2">
                  <Glyph icon={Mail} tone="navy" />
                  {customer.email || "—"}
                </p>
                <p className="flex items-center gap-2">
                  <Glyph icon={Phone} tone="navy" />
                  {customer.phone || "—"}
                </p>
              </CardBody>
            </Card>
            <Card>
              <CardHeader>
                <div className="flex items-center gap-2">
                  <Glyph icon={Wallet} />
                  <CardTitle>Cari hesap</CardTitle>
                </div>
              </CardHeader>
              <CardBody>
                {!canReadAccount ? (
                  <p className="text-sm text-slate-600">
                    current-account.read yok. Cari çağrısı yapılmaz; yetki backend’dedir.
                  </p>
                ) : accountDenied ? (
                  <PermissionDenied title="Cari hesap görülemez" />
                ) : accountError ? (
                  <ErrorState
                    title="Cari hesap alınamadı"
                    description={accountError}
                    onRetry={() => setReload((value) => value + 1)}
                  />
                ) : accountMissing ? (
                  <EmptyState
                    title="Cari hesap yok"
                    description="Bu müşteri için GET /current-accounts kaydı dönmedi. Sıfır bakiye uydurulmaz."
                  />
                ) : account ? (
                  <dl className="grid grid-cols-2 gap-3 text-sm">
                    <div>
                      <dt className="text-xs font-semibold text-slate-500">Alacak</dt>
                      <dd className="mt-1 font-semibold text-navy-950">
                        {formatMoney(account.creditTotal, account.currencyCode)}
                      </dd>
                    </div>
                    <div>
                      <dt className="text-xs font-semibold text-slate-500">Para birimi</dt>
                      <dd className="mt-1 font-semibold text-navy-950">{account.currencyCode}</dd>
                    </div>
                  </dl>
                ) : (
                  <p className="text-sm text-slate-600">Cari sorgulanıyor…</p>
                )}
              </CardBody>
            </Card>
          </div>

          <Alert tone="info" title="Sahte ekstre yok">
            Hareket listesi endpoint’i yok. Yalnızca anlık borç/alacak/bakiye gösterilir.
          </Alert>
        </div>
      )}
    </AppShell>
  );
}
