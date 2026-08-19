import type { LucideIcon } from "lucide-react";
import { ChevronRight } from "lucide-react";
import { Card, CardBody, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";

export function RailListCard({
  title,
  icon: Icon,
  columns,
  reason,
}: {
  title: string;
  icon: LucideIcon;
  columns: string[];
  reason: string;
}) {
  return (
    <Card>
      <CardHeader>
        <div className="flex min-w-0 items-center gap-2">
          <span className="grid h-8 w-8 place-items-center rounded-xl bg-teal-500/10 text-teal-700" aria-hidden="true">
            <Icon className="h-4 w-4" />
          </span>
          <CardTitle className="truncate">{title}</CardTitle>
        </div>
        <span className="inline-flex items-center gap-1">
          <Badge tone="neutral">—</Badge>
          <ChevronRight className="h-4 w-4 text-slate-400" aria-hidden="true" />
        </span>
      </CardHeader>
      <CardBody className="pt-3">
        <table className="w-full text-left text-[12px]">
          <thead>
            <tr className="text-[10px] font-extrabold tracking-wide text-slate-500 uppercase">
              {columns.map((column) => (
                <th key={column} className="pb-2 font-extrabold">
                  {column}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            <tr>
              <td colSpan={columns.length} className="border-t border-surface-200 py-4 text-sm text-slate-600">
                {reason}
              </td>
            </tr>
          </tbody>
        </table>
        <p className="mt-1 text-xs font-semibold text-teal-600/70" title="Liste ekranı henüz bağlı değil">
          Tümünü Gör
        </p>
      </CardBody>
    </Card>
  );
}
