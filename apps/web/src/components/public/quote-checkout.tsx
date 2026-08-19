"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { EmptyState } from "@/components/states/empty-state";
import { userFacingMessage } from "@/lib/api/auth-client";
import { submitPublicQuoteRequest } from "@/lib/catalog/catalog-client";
import { packagingDefinitionLabel } from "@/lib/catalog/map-product";
import {
  quoteLineKey,
  toQuoteRequestItems,
  useQuoteBasketStore,
} from "@/lib/catalog/quote-basket-store";
import type { QuoteRequestResult } from "@/lib/catalog/types";

export function QuoteCheckout() {
  const router = useRouter();
  const lines = useQuoteBasketStore((state) => state.lines);
  const generalNote = useQuoteBasketStore((state) => state.generalNote);
  const updateQuantity = useQuoteBasketStore((state) => state.updateQuantity);
  const updateNote = useQuoteBasketStore((state) => state.updateNote);
  const removeLine = useQuoteBasketStore((state) => state.removeLine);
  const setGeneralNote = useQuoteBasketStore((state) => state.setGeneralNote);
  const clear = useQuoteBasketStore((state) => state.clear);

  const [step, setStep] = useState<"review" | "contact" | "success">("review");
  const [companyName, setCompanyName] = useState("");
  const [contactName, setContactName] = useState("");
  const [phone, setPhone] = useState("");
  const [email, setEmail] = useState("");
  const [consent, setConsent] = useState(false);
  const [honeypot, setHoneypot] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [result, setResult] = useState<QuoteRequestResult | null>(null);

  if (result && step === "success") {
    return (
      <div className="mx-auto max-w-2xl px-4 py-10">
        <Alert tone="success" title="Teklif talebiniz alındı">
          Talep numaranız <strong>{result.requestNumber}</strong>. Şirketimiz inceleyip sizinle
          iletişime geçecektir. Bu işlem sipariş oluşturmaz.
        </Alert>
        <Button className="mt-6" onClick={() => router.push("/katalog")}>
          Kataloğa dön
        </Button>
      </div>
    );
  }

  if (lines.length === 0) {
    return (
      <div className="mx-auto max-w-3xl px-4 py-10">
        <EmptyState
          title="Henüz ürün eklemediniz"
          description="Teklif sepeti boş. Kataloğa gidip ürün ekleyebilirsiniz."
          action={
            <Button variant="secondary" onClick={() => router.push("/katalog")}>
              Kataloğa git
            </Button>
          }
        />
      </div>
    );
  }

  async function submit() {
    if (honeypot) {
      return;
    }
    setError(null);
    setSubmitting(true);
    try {
      const created = await submitPublicQuoteRequest({
        companyName,
        contactName,
        phone,
        email,
        items: toQuoteRequestItems(lines),
        note: generalNote || undefined,
        consentAccepted: consent,
      });
      clear();
      setResult(created);
      setStep("success");
    } catch (caught) {
      setError(
        `${userFacingMessage(caught)} Bilgileriniz korunuyor; lütfen tekrar deneyin.`,
      );
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="mx-auto max-w-4xl px-4 py-8">
      <h1 className="text-[28px] font-bold text-navy-950">Teklif sepetiniz</h1>
      <p className="mt-2 text-sm text-slate-600">
        Bu işlem sipariş oluşturmaz. Talebinizi inceleyip sizinle iletişime geçeceğiz.
      </p>

      {step === "review" ? (
        <div className="mt-6 space-y-4">
          {lines.map((line) => {
            const key = quoteLineKey(line);
            return (
              <article key={key} className="rounded-2xl border border-surface-200 bg-white p-4">
                <div className="flex flex-col gap-3 sm:flex-row sm:items-start">
                  <div className="min-w-0 flex-1">
                    <h2 className="font-semibold text-navy-950">{line.name}</h2>
                    <p className="text-xs text-slate-500">{line.code}</p>
                    <p className="mt-1 text-sm text-slate-600">
                      {line.enteredQuantity} {line.packagingName}
                    </p>
                    <p className="text-xs text-slate-500">
                      Katalog tanımı:{" "}
                      {packagingDefinitionLabel(
                        { name: line.packagingName, quantityInBaseUom: line.catalogQuantityInBaseUom },
                        line.baseUomCode,
                      )}
                      . Temel karşılık gönderimde hesaplanır.
                    </p>
                  </div>
                  <div className="grid gap-2 sm:w-52">
                    <Input
                      label="Miktar"
                      name={`qty-${key}`}
                      type="number"
                      min={1}
                      value={String(line.enteredQuantity)}
                      onChange={(event) =>
                        updateQuantity(key, Number(event.target.value) || line.enteredQuantity)
                      }
                    />
                    <Input
                      label="Satır notu"
                      name={`note-${key}`}
                      value={line.note}
                      onChange={(event) => updateNote(key, event.target.value)}
                    />
                    <Button size="sm" variant="ghost" onClick={() => removeLine(key)}>
                      Kaldır
                    </Button>
                  </div>
                </div>
              </article>
            );
          })}
          <Input
            label="Genel talep notu"
            name="generalNote"
            value={generalNote}
            onChange={(event) => setGeneralNote(event.target.value)}
          />
          <div className="flex flex-wrap gap-2">
            <Button variant="secondary" onClick={() => router.push("/katalog")}>
              Alışverişe devam et
            </Button>
            <Button onClick={() => setStep("contact")}>Bilgilerimi gir ve teklif iste</Button>
          </div>
        </div>
      ) : (
        <div className="mt-6 max-w-lg space-y-4">
          {error ? <Alert tone="danger" title="Talep gönderilemedi">{error}</Alert> : null}
          <Input
            label="Firma adı"
            name="companyName"
            required
            value={companyName}
            onChange={(event) => setCompanyName(event.target.value)}
          />
          <Input
            label="Yetkili adı soyadı"
            name="contactName"
            required
            value={contactName}
            onChange={(event) => setContactName(event.target.value)}
          />
          <Input
            label="Telefon"
            name="phone"
            required
            value={phone}
            onChange={(event) => setPhone(event.target.value)}
          />
          <Input
            label="E-posta"
            name="email"
            type="email"
            required
            value={email}
            onChange={(event) => setEmail(event.target.value)}
          />
          <div className="hidden" aria-hidden="true">
            <label>
              Website
              <input value={honeypot} onChange={(event) => setHoneypot(event.target.value)} tabIndex={-1} />
            </label>
          </div>
          <Checkbox
            label="İletişim bilgilerimin teklif amacıyla işlenmesini kabul ediyorum."
            checked={consent}
            onChange={(event) => setConsent(event.target.checked)}
          />
          <p className="text-xs text-slate-500">{lines.length} ürün gönderilecek. Fiyat veya stok bilgisi yoktur.</p>
          <div className="flex flex-wrap gap-2">
            <Button variant="secondary" onClick={() => setStep("review")}>
              Bilgileri düzenle
            </Button>
            <Button
              onClick={() => void submit()}
              loading={submitting}
              disabled={!consent || submitting}
            >
              Teklif talebini gönder
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
