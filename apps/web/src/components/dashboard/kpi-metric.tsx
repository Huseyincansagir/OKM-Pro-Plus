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
}: {
  label: string;
  value: string;
  unit?: string;
  caption: string;
  icon: LucideIcon;
  tone?: keyof typeof iconWellClass;
  unavailable?: boolean;
}) {
  return (
    <Card>
      <CardBody>
        <div
          className="flex items-start gap-3"
          aria-label={unavailable ? `${label}: bağlı değil` : `${label}: ${value}`}
        >
          <span
            className={cn(
              "grid h-11 w-11 shrink-0 place-items-center rounded-2xl",
              iconWellClass[tone],
            )}
            aria-hidden="true"
          >
            <Icon className="h-5 w-5" />
          </span>
          <div className="min-w-0 flex-1">
            <p className="text-xs font-semibold text-slate-500">{label}</p>
            <p className="mt-1 flex items-baseline gap-1.5 text-[25px] font-extrabold tracking-tight text-navy-950">
              <span>{value}</span>
              {unit ? (
                <span className="text-sm font-semibold text-slate-500">{unit}</span>
              ) : null}
            </p>
            <p className="mt-1 text-xs text-slate-500">{caption}</p>
          </div>
        </div>
      </CardBody>
    </Card>
  );
}
