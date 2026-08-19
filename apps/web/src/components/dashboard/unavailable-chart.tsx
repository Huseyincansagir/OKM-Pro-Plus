import type { LucideIcon } from "lucide-react";
import { ChevronDown } from "lucide-react";
import { Card, CardBody, CardHeader, CardTitle } from "@/components/ui/card";

export function UnavailableChart({
  title,
  icon: Icon,
  legend,
  stats,
  reason,
}: {
  title: string;
  icon: LucideIcon;
  legend: string[];
  stats: { label: string; unit?: string }[];
  reason: string;
}) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>{title}</CardTitle>
        <span
          title="Dönem filtresi bağlı değil"
          className="inline-flex h-[30px] items-center gap-1 rounded-[9px] border border-surface-200 bg-white px-2.5 text-[11px] text-slate-600"
        >
          Son 30 Gün
          <ChevronDown className="h-3 w-3" aria-hidden="true" />
        </span>
      </CardHeader>
      <CardBody className="pt-3">
        <div className="mb-2 flex flex-wrap gap-3 text-[11px] text-slate-500">
          {legend.map((item) => (
            <span key={item} className="inline-flex items-center gap-1.5">
              <span className="h-1.5 w-3 rounded-full bg-teal-500/40" aria-hidden="true" />
              {item}
            </span>
          ))}
        </div>
        <div className="relative h-[180px] overflow-hidden rounded-xl bg-surface-50">
          <div className="absolute inset-x-6 top-4 bottom-8 flex flex-col justify-between" aria-hidden="true">
            <div className="border-t border-surface-200" />
            <div className="border-t border-surface-200" />
            <div className="border-t border-surface-200" />
            <div className="border-t border-surface-200" />
          </div>
          <div className="absolute inset-0 grid place-items-center px-6 text-center">
            <p className="inline-flex items-center gap-2 text-sm text-slate-600">
              <Icon className="h-4 w-4 text-slate-400" aria-hidden="true" />
              {reason}
            </p>
          </div>
        </div>
        <div className="mt-3 grid grid-cols-2 gap-2 border-t border-surface-200 pt-3 sm:grid-cols-4">
          {stats.map((stat) => (
            <div key={stat.label}>
              <p className="text-[10px] font-extrabold tracking-wide text-slate-500 uppercase">
                {stat.label}
              </p>
              <p className="mt-1 text-sm font-bold text-navy-950">
                —{stat.unit ? <span className="ml-1 text-xs font-semibold text-slate-500">{stat.unit}</span> : null}
              </p>
            </div>
          ))}
        </div>
      </CardBody>
    </Card>
  );
}
