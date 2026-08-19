import { Alert } from "@/components/ui/alert";
import { Skeleton } from "@/components/ui/skeleton";

export type QuantityEntryPreviewProps = {
  displayQuantity?: string | number;
  displayUnit?: string;
  baseQuantity?: string | number;
  baseUnit?: string;
  conversionLabel?: string;
  isLoading?: boolean;
  error?: string | null;
};

/**
 * Renders a canonical backend conversion result.
 * This component must not multiply, divide or invent quantityBase.
 */
export function QuantityEntryPreview({
  displayQuantity,
  displayUnit,
  baseQuantity,
  baseUnit,
  conversionLabel,
  isLoading = false,
  error = null,
}: QuantityEntryPreviewProps) {
  if (isLoading) {
    return (
      <div aria-busy="true" aria-label="Yükleniyor" className="space-y-2">
        <Skeleton className="h-5 w-40" />
        <Skeleton className="h-4 w-56" />
      </div>
    );
  }

  if (error) {
    return <Alert tone="danger" title="Miktar önizlemesi alınamadı">{error}</Alert>;
  }

  const display =
    displayQuantity !== undefined && displayUnit
      ? `${displayQuantity} ${displayUnit}`
      : null;
  const base =
    baseQuantity !== undefined && baseUnit
      ? `${baseQuantity} ${baseUnit}`
      : null;

  return (
    <div className="rounded-xl border border-surface-200 bg-surface-50 px-3 py-3">
      {display ? (
        <p className="text-sm font-semibold text-navy-950">Giriş: {display}</p>
      ) : null}
      {base ? (
        <p className="mt-1 text-sm text-navy-800">
          Temel karşılık: <span className="font-semibold">{base}</span>
        </p>
      ) : null}
      {conversionLabel ? (
        <p className="mt-1 text-xs text-slate-600">{conversionLabel}</p>
      ) : null}
    </div>
  );
}
