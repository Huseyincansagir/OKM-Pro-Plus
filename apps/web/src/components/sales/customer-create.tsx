"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Building2 } from "lucide-react";
import { AppShell } from "@/components/shell/app-shell";
import { PermissionDenied } from "@/components/states/permission-denied";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader, CardTitle } from "@/components/ui/card";
import { Glyph } from "@/components/ui/glyph";
import { Input } from "@/components/ui/input";
import { userFacingMessage } from "@/lib/api/auth-client";
import { useSessionStore } from "@/lib/auth/session-store";
import { createCustomer } from "@/lib/sales/customers";

export function CustomerCreate() {
  const router = useRouter();
  const user = useSessionStore((state) => state.user);
  const canCreate = (user?.permissions ?? []).includes("customer.create");
  const [legalName, setLegalName] = useState("");
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [taxNumber, setTaxNumber] = useState("");
  const [taxOffice, setTaxOffice] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function onSubmit() {
    if (!legalName.trim()) {
      setError("Unvan zorunludur.");
      return;
    }
    setError(null);
    setSubmitting(true);
    try {
      const created = await createCustomer({
        legalName: legalName.trim(),
        email: email.trim() || undefined,
        phone: phone.trim() || undefined,
        taxNumber: taxNumber.trim() || undefined,
        taxOffice: taxOffice.trim() || undefined,
      });
      router.push(`/satis/musteriler/${created.id}`);
    } catch (caught) {
      setError(userFacingMessage(caught));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <AppShell
      currentHref="/satis/musteriler"
      breadcrumbs={[
        { label: "Çalışma alanı", href: "/dashboard" },
        { label: "Satış", href: "/satis/teklif-talepleri" },
        { label: "Müşteriler", href: "/satis/musteriler" },
        { label: "Yeni kart" },
      ]}
      pageTitle="Yeni müşteri"
      pageDescription="Personel kartı Active açılır; teklif talebine bağlanabilir. Kod sunucu üretir."
      pageActions={
        <Button variant="secondary" onClick={() => router.push("/satis/musteriler")}>
          Listeye dön
        </Button>
      }
    >
      {!canCreate ? (
        <PermissionDenied
          title="Müşteri kartı açılamaz"
          description="customer.create yok. Bu kontrol yalnızca görünürlük içindir; gerçek yetki backend’dedir."
        />
      ) : (
        <Card className="max-w-xl">
          <CardHeader>
            <div className="flex items-center gap-2">
              <Glyph icon={Building2} />
              <CardTitle>Kart bilgileri</CardTitle>
            </div>
          </CardHeader>
          <CardBody>
            <form
              className="space-y-4"
              noValidate
              onSubmit={(event) => {
                event.preventDefault();
                void onSubmit();
              }}
            >
              {error ? (
                <Alert tone="danger" title="Kart açılamadı">
                  {error}
                </Alert>
              ) : null}
              <Input
                label="Unvan"
                name="legalName"
                required
                value={legalName}
                onChange={(event) => setLegalName(event.target.value)}
              />
              <Input
                label="E-posta"
                name="email"
                type="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
              />
              <Input
                label="Telefon"
                name="phone"
                value={phone}
                onChange={(event) => setPhone(event.target.value)}
              />
              <div className="grid gap-4 sm:grid-cols-2">
                <Input
                  label="Vergi no"
                  name="taxNumber"
                  value={taxNumber}
                  onChange={(event) => setTaxNumber(event.target.value)}
                />
                <Input
                  label="Vergi dairesi"
                  name="taxOffice"
                  value={taxOffice}
                  onChange={(event) => setTaxOffice(event.target.value)}
                />
              </div>
              <p className="text-xs text-slate-500">
                Public katalog talebi buradan otomatik müşteri yapmaz. Durum Active olur.
              </p>
              <Button type="submit" loading={submitting} disabled={submitting}>
                Kartı aç
              </Button>
            </form>
          </CardBody>
        </Card>
      )}
    </AppShell>
  );
}
