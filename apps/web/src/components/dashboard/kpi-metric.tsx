import type { LucideIcon } from "lucide-react";
import { Card, CardBody } from "@/components/ui/card";
import { cn } from "@/lib/cn";

const iconWellClass = {
  teal: "bg-teal-500/10 text-teal-700",
  amber: "bg-amber-100 text-amber-500",
  navy: "bg-navy-800/10 text-navy-800",
} as const;

export function KpiMetric({
  label,
  value,
  unit,
  caption,
  icon: Icon,
  tone = "teal",
  unavailable = false,
  secondary,
  showEmptyTrack = false,
}: {
  label: string;
  value: string;
  unit?: string;
  caption: string;
  icon: LucideIcon;
  tone?: keyof typeof iconWellClass;
  unavailable?: boolean;
  secondary?: string;
  showEmptyTrack?: boolean;
}) {
  return (
    <Card className="min-h-[112px]">
      <CardBody className="flex h-full flex-col gap-3">
        <div
          className="flex items-start gap-3"
          aria-label={unavailable ? `${label}: bağlı değil` : `${label}: ${value}`}
        >
          <span
            className={cn(
              "grid h-12 w-12 shrink-0 place-items-center rounded-2xl",
              iconWellClass[tone],
            )}
            aria-hidden="true"
          >
            <Icon className="h-5 w-5" />
          </span>
          <div className="min-w-0 flex-1">
            <p className="text-[12px] font-semibold text-slate-500">{label}</p>
            <p className="mt-1 flex min-h-8 items-baseline gap-1.5 text-[25px] font-extrabold tracking-tight text-navy-950">
              <span>{value}</span>
              {unit ? (
                <span className="text-sm font-semibold text-slate-500">{unit}</span>
              ) : null}
            </p>
            <p className="mt-0.5 min-h-4 text-xs text-slate-500">{secondary ?? "\u00a0"}</p>
          </div>
        </div>
        <div className="mt-auto flex min-h-7 items-center justify-between gap-3 border-t border-surface-200 pt-2">
          <p className="text-[11px] text-slate-500">{caption}</p>
          <span
            className={cn(
              "h-1.5 w-16 overflow-hidden rounded-full bg-surface-200",
              showEmptyTrack ? "opacity-100" : "opacity-0",
            )}
            aria-hidden="true"
          />
        </div>
      </CardBody>
    </Card>
  );
}
