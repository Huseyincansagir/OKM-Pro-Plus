import { ShieldOff } from "lucide-react";
import { cn } from "@/lib/cn";

export function PermissionDenied({
  title = "Bu işlem için yetkiniz yok",
  description = "Gerekli yetki backend tarafından doğrulanır. Görünür butonlar tek başına yetki vermez.",
  className,
}: {
  title?: string;
  description?: string;
  className?: string;
}) {
  return (
    <div
      role="alert"
      className={cn(
        "flex flex-col items-start gap-3 rounded-xl border border-surface-200 bg-surface-50 px-4 py-6",
        className,
      )}
    >
      <ShieldOff className="h-5 w-5 text-navy-800" aria-hidden="true" />
      <div>
        <h2 className="text-sm font-semibold text-navy-950">{title}</h2>
        <p className="mt-1 text-sm text-slate-600">{description}</p>
      </div>
    </div>
  );
}
