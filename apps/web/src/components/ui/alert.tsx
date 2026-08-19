import { AlertTriangle, CheckCircle2, Info, XCircle } from "lucide-react";
import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

export type AlertTone = "info" | "success" | "warning" | "danger";

const toneClass: Record<AlertTone, string> = {
  info: "border-navy-800/20 bg-navy-800/5 text-navy-900",
  success: "border-success-500/20 bg-success-100 text-success-500",
  warning: "border-amber-500/20 bg-amber-100 text-amber-500",
  danger: "border-danger-500/20 bg-danger-100 text-danger-500",
};

const icons = {
  info: Info,
  success: CheckCircle2,
  warning: AlertTriangle,
  danger: XCircle,
};

export function Alert({
  tone = "info",
  title,
  children,
  className,
}: {
  tone?: AlertTone;
  title: string;
  children?: ReactNode;
  className?: string;
}) {
  const Icon = icons[tone];

  return (
    <div
      role="status"
      className={cn(
        "flex gap-3 rounded-xl border px-3 py-3 text-sm",
        toneClass[tone],
        className,
      )}
    >
      <Icon className="mt-0.5 h-4 w-4 shrink-0" aria-hidden="true" />
      <div>
        <p className="font-semibold">{title}</p>
        {children ? <div className="mt-1 text-[13px] opacity-90">{children}</div> : null}
      </div>
    </div>
  );
}
