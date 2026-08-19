import type { ReactNode } from "react";
import { Inbox } from "lucide-react";
import { cn } from "@/lib/cn";

export function EmptyState({
  title,
  description,
  action,
  className,
}: {
  title: string;
  description: string;
  action?: ReactNode;
  className?: string;
}) {
  return (
    <div
      className={cn(
        "flex flex-col items-start gap-3 rounded-xl border border-dashed border-surface-200 bg-surface-50 px-4 py-6",
        className,
      )}
    >
      <Inbox className="h-5 w-5 text-slate-400" aria-hidden="true" />
      <div>
        <h2 className="text-sm font-semibold text-navy-950">{title}</h2>
        <p className="mt-1 text-sm text-slate-600">{description}</p>
      </div>
      {action}
    </div>
  );
}
