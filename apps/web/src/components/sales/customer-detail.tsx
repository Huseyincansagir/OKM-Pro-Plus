"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Building2, Mail, Phone, Scale, Tag, UserRound, Wallet } from "lucide-react";
import { AppShell } from "@/components/shell/app-shell";
import { KpiMetric } from "@/components/dashboard/kpi-metric";
import { EmptyState } from "@/components/states/empty-state";
import { ErrorState } from "@/components/states/error-state";
import { PermissionDenied } from "@/components/states/permission-denied";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader, CardTitle } from "@/components/ui/card";
import { Glyph } from "@/components/ui/glyph";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { StatusBadge, type StatusKind } from "@/components/ui/status-badge";
import { ApiError } from "@/lib/api/types";
import { userFacingMessage } from "@/lib/api/auth-client";
import { useSessionStore } from "@/lib/auth/session-store";
import {
  assignCustomerPriceGroup,
  createCustomerContact,
  customerStatusLabel,
  getCustomer,
  listCustomerEmails,
  listCustomerPriceGroups,
  sendCustomerEmail,
  type CustomerCard,
  type CustomerOutboundEmail,
  type PriceGroupOption,
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
  const canUpdate = permissions.includes("customer.update");
  const canMessage = permissions.includes("customer.message");
  const canReadPrice = permissions.includes("price.read");
  const [customer, setCustomer] = useState<CustomerCard | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [denied, setDenied] = useState(false);
  const [loading, setLoading] = useState(canRead);
  const [reload, setReload] = useState(0);
  const [account, setAccount] = useState<CurrentAccountSummary | null>(null);
  const [accountMissing, setAccountMissing] = useState(false);
  const [accountDenied, setAccountDenied] = useState(false);
  const [accountError, setAccountError] = useState<string | null>(null);
  const [accountLoading, setAccountLoading] = useState(canReadAccount);
  const [groups, setGroups] = useState<PriceGroupOption[]>([]);
  const [selectedGroupId, setSelectedGroupId] = useState("");
  const [groupError, setGroupError] = useState<string | null>(null);
  const [assigningGroup, setAssigningGroup] = useState(false);
  const [contactName, setContactName] = useState("");
  const [contactEmail, setContactEmail] = useState("");
  const [contactPhone, setContactPhone] = useState("");
  const [contactError, setContactError] = useState<string | null>(null);
  const [savingContact, setSavingContact] = useState(false);
  const [emailSubject, setEmailSubject] = useState("");
  const [emailBody, setEmailBody] = useState("");
  const [emailError, setEmailError] = useState<string | null>(null);
  const [sendingEmail, setSendingEmail] = useState(false);
  const [emails, setEmails] = useState<CustomerOutboundEmail[]>([]);

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

  useEffect(() => {
    if (!canReadPrice) {
      return;
    }
    let cancelled = false;
    listCustomerPriceGroups()
      .then((rows) => {
        if (!cancelled) setGroups(rows);
      })
      .catch(() => {
        if (!cancelled) setGroups([]);
      });
    return () => {
      cancelled = true;
    };
  }, [canReadPrice]);

  useEffect(() => {
    if (!canMessage) {
      return;
    }
    let cancelled = false;
    listCustomerEmails(id)
      .then((rows) => {
        if (!cancelled) setEmails(rows);
      })
      .catch(() => {
        if (!cancelled) setEmails([]);
      });
    return () => {
      cancelled = true;
    };
  }, [canMessage, id, reload]);

  async function saveContact() {
    if (!contactName.trim()) {
      setContactError("Yetkili adı zorunludur.");
      return;
    }
    setSavingContact(true);
    setContactError(null);
    try {
      await createCustomerContact(id, {
        fullName: contactName.trim(),
        email: contactEmail.trim() || undefined,
        phone: contactPhone.trim() || undefined,
        isPrimary: false,
      });
      setContactName("");
      setContactEmail("");
      setContactPhone("");
      setReload((value) => value + 1);
    } catch (caught) {
      setContactError(userFacingMessage(caught));
    } finally {
      setSavingContact(false);
    }
  }

  async function savePriceGroup() {
    if (!selectedGroupId) {
      setGroupError("Fiyat grubu seçin.");
      return;
    }
    setAssigningGroup(true);
    setGroupError(null);
    try {
      await assignCustomerPriceGroup(id, selectedGroupId);
      setReload((value) => value + 1);
    } catch (caught) {
      setGroupError(userFacingMessage(caught));
    } finally {
      setAssigningGroup(false);
    }
  }

  async function submitEmail() {
    if (!emailSubject.trim() || !emailBody.trim()) {
      setEmailError("Konu ve metin zorunludur.");
      return;
    }
    setSendingEmail(true);
    setEmailError(null);
    try {
      const sent = await sendCustomerEmail(id, {
        subject: emailSubject.trim(),
        body: emailBody.trim(),
      });
      setEmailSubject("");
      setEmailBody("");
      setEmails((current) => [sent, ...current]);
    } catch (caught) {
      setEmailError(userFacingMessage(caught));
    } finally {
      setSendingEmail(false);
    }
  }

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
      pageDescription="Rehber kartı: iletişim ve fiyat grubu cari hesaptan ayrıdır. Bakiye yoksa ₺0 yazılmaz."
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

          <div className="grid gap-4 lg:grid-cols-2">
            <Card>
              <CardHeader>
                <div className="flex items-center gap-2">
                  <Glyph icon={UserRound} />
                  <CardTitle>Yetkililer</CardTitle>
                </div>
              </CardHeader>
              <CardBody className="space-y-3 text-sm text-slate-600">
                {customer.contacts.length === 0 ? (
                  <p>Kayıtlı yetkili yok.</p>
                ) : (
                  customer.contacts.map((contact) => (
                    <p key={contact.id} className="flex items-center gap-2 text-navy-950">
                      <Glyph icon={UserRound} tone="navy" />
                      {contact.fullName}
                      {contact.email ? ` · ${contact.email}` : ""}
                      {contact.isPrimary ? " · birincil" : ""}
                    </p>
                  ))
                )}
                {canUpdate ? (
                  <form
                    className="space-y-3 border-t border-surface-200 pt-3"
                    onSubmit={(event) => {
                      event.preventDefault();
                      void saveContact();
                    }}
                  >
                    {contactError ? (
                      <Alert tone="danger" title="Yetkili eklenemedi">
                        {contactError}
                      </Alert>
                    ) : null}
                    <Input
                      label="Yetkili adı"
                      name="contactName"
                      required
                      value={contactName}
                      onChange={(event) => setContactName(event.target.value)}
                    />
                    <div className="grid gap-3 sm:grid-cols-2">
                      <Input
                        label="E-posta"
                        name="contactEmail"
                        value={contactEmail}
                        onChange={(event) => setContactEmail(event.target.value)}
                      />
                      <Input
                        label="Telefon"
                        name="contactPhone"
                        value={contactPhone}
                        onChange={(event) => setContactPhone(event.target.value)}
                      />
                    </div>
                    <Button type="submit" loading={savingContact}>
                      Yetkili ekle
                    </Button>
                  </form>
                ) : (
                  <p>customer.update yok. Yetkili ekleme gizlidir.</p>
                )}
              </CardBody>
            </Card>
            <Card>
              <CardHeader>
                <div className="flex items-center gap-2">
                  <Glyph icon={Tag} />
                  <CardTitle>Fiyat grubu</CardTitle>
                </div>
              </CardHeader>
              <CardBody className="space-y-3 text-sm text-slate-600">
                <p className="text-navy-950">
                  {customer.priceGroupCode
                    ? `${customer.priceGroupCode} · ${customer.priceGroupName || "grup"} · liste ${customer.priceListCode || "—"}`
                    : "Atanmış fiyat grubu yok. Teklifte personel fiyatı girer."}
                </p>
                <Alert tone="info" title="Cariye bağlı değil">
                  Vadeli/peşin ayrımı ticari listedir; bakiye veya riskten üretilmez.
                </Alert>
                {canUpdate && canReadPrice && groups.length > 0 ? (
                  <>
                    {groupError ? (
                      <Alert tone="danger" title="Grup atanamadı">
                        {groupError}
                      </Alert>
                    ) : null}
                    <Select
                      label="Fiyat grubu"
                      name="priceGroupId"
                      value={selectedGroupId}
                      onChange={(event) => setSelectedGroupId(event.target.value)}
                      options={[
                        { value: "", label: "Seçin" },
                        ...groups.map((group) => ({
                          value: group.id,
                          label: `${group.code} · ${group.name}`,
                        })),
                      ]}
                    />
                    <Button onClick={() => void savePriceGroup()} loading={assigningGroup}>
                      Grubu ata
                    </Button>
                  </>
                ) : null}
              </CardBody>
            </Card>
          </div>

          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <Glyph icon={Mail} />
                <CardTitle>E-posta</CardTitle>
              </div>
            </CardHeader>
            <CardBody className="space-y-3">
              {!canMessage ? (
                <p className="text-sm text-slate-600">
                  customer.message yok. Gönderim gizlidir; yetki backend’dedir.
                </p>
              ) : (
                <form
                  className="space-y-3"
                  onSubmit={(event) => {
                    event.preventDefault();
                    void submitEmail();
                  }}
                >
                  {emailError ? (
                    <Alert tone="danger" title="E-posta kuyruğa alınamadı">
                      {emailError}
                    </Alert>
                  ) : (
                    <Alert tone="info" title="Kayıtlı adrese gider">
                      SMTP yoksa durum Queued kalır; gönderildi uydurulmaz.
                    </Alert>
                  )}
                  <Input
                    label="Konu"
                    name="emailSubject"
                    required
                    value={emailSubject}
                    onChange={(event) => setEmailSubject(event.target.value)}
                  />
                  <Input
                    label="Metin"
                    name="emailBody"
                    required
                    value={emailBody}
                    onChange={(event) => setEmailBody(event.target.value)}
                  />
                  <Button type="submit" loading={sendingEmail}>
                    Kuyruğa al
                  </Button>
                </form>
              )}
              {emails.length > 0 ? (
                <ul className="space-y-2 text-sm text-slate-600">
                  {emails.map((item) => (
                    <li key={item.id}>
                      {item.subject} · {item.status}
                      {item.lastError ? ` · ${item.lastError}` : ""}
                    </li>
                  ))}
                </ul>
              ) : null}
            </CardBody>
          </Card>

          <Alert tone="info" title="Sahte ekstre yok">
            Hareket listesi endpoint’i yok. Yalnızca anlık borç/alacak/bakiye gösterilir.
          </Alert>
        </div>
      )}
    </AppShell>
  );
}
