"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { FileText, Package } from "lucide-react";
import { AppShell } from "@/components/shell/app-shell";
import { EmptyState } from "@/components/states/empty-state";
import { ErrorState } from "@/components/states/error-state";
import { PermissionDenied } from "@/components/states/permission-denied";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader, CardTitle } from "@/components/ui/card";
import { Glyph } from "@/components/ui/glyph";
import { Input } from "@/components/ui/input";
import { userFacingMessage } from "@/lib/api/auth-client";
import { ApiError } from "@/lib/api/types";
import { useSessionStore } from "@/lib/auth/session-store";
import {
  canCreateQuoteFromRequest,
  createQuote,
} from "@/lib/sales/quotes";
import {
  getQuoteRequest,
  type QuoteRequestDetail,
} from "@/lib/dashboard/quote-requests";
import { getCustomerPriceContext } from "@/lib/sales/customers";

export function QuoteCreate({ quoteRequestId }: { quoteRequestId: string | null }) {
  const router = useRouter();
  const user = useSessionStore((state) => state.user);
  const permissions = user?.permissions ?? [];
  const canCreate = permissions.includes("quote.create");
  const canReadRequest = permissions.includes("quote-request.read");
  const canResolvePrice = permissions.includes("price.resolve");
  const [request, setRequest] = useState<QuoteRequestDetail | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [denied, setDenied] = useState(false);
  const [loading, setLoading] = useState(Boolean(canCreate && canReadRequest && quoteRequestId));
  const [prices, setPrices] = useState<Record<string, string>>({});
  const [listPrices, setListPrices] = useState<Record<string, number | null>>({});
  const [priceListCode, setPriceListCode] = useState("");
  const [validUntil, setValidUntil] = useState("");
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (!canCreate || !canReadRequest || !quoteRequestId) {
      setLoading(false);
      return;
    }
    let cancelled = false;
    setLoading(true);
    setLoadError(null);
    setDenied(false);
    getQuoteRequest(quoteRequestId)
      .then(async (result) => {
        if (cancelled) return;
        setRequest(result);
        const nextPrices = Object.fromEntries(result.items.map((item) => [item.id, ""]));
        if (canResolvePrice && result.customerId) {
          try {
            const context = await getCustomerPriceContext(result.customerId);
            if (cancelled) return;
            setPriceListCode(context.priceListCode);
            const suggested: Record<string, number | null> = {};
            for (const item of result.items) {
              const match = context.prices.find(
                (price) =>
                  price.productId === item.productId
                  && (price.packagingId === item.enteredPackagingId
                    || (price.packagingId === null && !item.enteredPackagingId)),
              ) ?? context.prices.find(
                (price) => price.productId === item.productId && price.packagingId === null,
              );
              suggested[item.id] = match?.unitPrice ?? null;
              if (match?.unitPrice != null) {
                nextPrices[item.id] = String(match.unitPrice);
              }
            }
            setListPrices(suggested);
          } catch {
            if (!cancelled) setListPrices({});
          }
        }
        setPrices(nextPrices);
      })
      .catch((caught) => {
        if (cancelled) return;
        if (caught instanceof ApiError && caught.kind === "permission_denied") {
          setDenied(true);
          return;
        }
        setLoadError(userFacingMessage(caught));
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [canCreate, canReadRequest, canResolvePrice, quoteRequestId]);

  async function onSubmit() {
    if (!request) {
      return;
    }
    const items = request.items.map((item) => {
      const raw = (prices[item.id] ?? "").trim().replace(",", ".");
      const parsed = raw === "" ? Number.NaN : Number(raw);
      return { quoteRequestItemId: item.id, unitPrice: parsed };
    });
    if (items.some((item) => !Number.isFinite(item.unitPrice) || item.unitPrice < 0)) {
      setSubmitError("Her kalem için sıfır veya pozitif birim fiyat girin.");
      return;
    }
    setSubmitError(null);
    setSubmitting(true);
    try {
      const created = await createQuote({
        quoteRequestId: request.id,
        currencyCode: "TRY",
        validUntil: validUntil ? new Date(validUntil).toISOString() : undefined,
        items,
      });
      router.push(`/satis/teklifler/${created.id}`);
    } catch (caught) {
      setSubmitError(userFacingMessage(caught));
    } finally {
      setSubmitting(false);
    }
  }

  const convertible = request
    ? canCreateQuoteFromRequest(request.status, request.customerId)
    : false;

  return (
    <AppShell
      currentHref="/satis/teklifler"
      breadcrumbs={[
        { label: "Çalışma alanı", href: "/dashboard" },
        { label: "Satış", href: "/satis/teklif-talepleri" },
        { label: "Teklifler", href: "/satis/teklifler" },
        { label: "Yeni belge" },
      ]}
      pageTitle="Teklif oluştur"
      pageDescription="Yalnızca incelenmiş ve müşteri bağlı talepten. Birim fiyatı personel girer; kod ve tutar sunucu üretir."
      pageActions={
        <Button variant="secondary" onClick={() => router.push("/satis/teklifler")}>
          Listeye dön
        </Button>
      }
    >
      {!canCreate ? (
        <PermissionDenied
          title="Teklif belgesi oluşturulamaz"
          description="quote.create yok. Bu kontrol yalnızca görünürlük içindir; gerçek yetki backend’dedir."
        />
      ) : !quoteRequestId ? (
        <EmptyState
          title="Talep seçilmedi"
          description="Teklif belgesi ürün listesinden açılmaz. İncelemedeki teklif talebinden ilerleyin."
        />
      ) : !canReadRequest ? (
        <PermissionDenied
          title="Talep okunamadı"
          description="quote-request.read yok. Talep kalemleri olmadan teklif formu açılmaz."
        />
      ) : denied ? (
        <PermissionDenied />
      ) : loadError ? (
        <ErrorState title="Talep yüklenemedi" description={loadError} />
      ) : loading || !request ? (
        <Card className="max-w-3xl">
          <CardBody>
            <p className="text-sm text-slate-600">Talep yükleniyor…</p>
          </CardBody>
        </Card>
      ) : !convertible ? (
        <Alert tone="warning" title="Talep dönüştürülemez">
          Teklif yalnızca InReview ve Active müşteri bağlı talepten oluşur. Durum: {request.status}.
        </Alert>
      ) : request.items.length === 0 ? (
        <EmptyState title="Kalem yok" description="Bu talepte fiyatlanacak satır yok." />
      ) : (
        <Card className="max-w-3xl">
          <CardHeader>
            <div className="flex items-center gap-2">
              <Glyph icon={FileText} />
              <CardTitle>{request.requestNumber}</CardTitle>
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
              {submitError ? (
                <Alert tone="danger" title="Teklif kaydedilemedi">
                  {submitError}
                </Alert>
              ) : (
                <Alert tone="info" title="Fiyat personel girer">
                  Satır tutarı kayıt sonrası sunucudan gelir. quantityBase tarayıcıda üretilmez.
                </Alert>
              )}
              <div className="space-y-3">
                {request.items.map((item) => (
                  <div
                    key={item.id}
                    className="grid gap-3 rounded-[12px] border border-surface-200 p-3 sm:grid-cols-[1fr_140px]"
                  >
                    <div className="space-y-1 text-sm text-slate-600">
                      <p className="inline-flex items-center gap-2 font-semibold text-navy-950">
                        <Glyph icon={Package} />
                        {item.packagingName}
                      </p>
                      <p>Girilen miktar: {item.enteredQuantity}</p>
                      <p>
                        Temel karşılık:{" "}
                        {item.quantityBase === null ? "—" : String(item.quantityBase)}
                      </p>
                    </div>
                    <Input
                      label="Birim fiyat"
                      name={`unitPrice-${item.id}`}
                      type="number"
                      inputMode="decimal"
                      min={0}
                      step="0.01"
                      required
                      value={prices[item.id] ?? ""}
                      hint={
                        listPrices[item.id] == null
                          ? priceListCode
                            ? `Liste ${priceListCode}: bu kalemde fiyat yok`
                            : "Liste fiyatı yok; personel girer"
                          : `Liste ${priceListCode || ""}: ${listPrices[item.id]}`
                      }
                      onChange={(event) =>
                        setPrices((current) => ({
                          ...current,
                          [item.id]: event.target.value,
                        }))
                      }
                    />
                  </div>
                ))}
              </div>
              <Input
                label="Geçerlilik (isteğe bağlı)"
                name="validUntil"
                type="datetime-local"
                value={validUntil}
                onChange={(event) => setValidUntil(event.target.value)}
              />
              <Button type="submit" loading={submitting} disabled={submitting}>
                Taslak teklif oluştur
              </Button>
            </form>
          </CardBody>
        </Card>
      )}
    </AppShell>
  );
}
