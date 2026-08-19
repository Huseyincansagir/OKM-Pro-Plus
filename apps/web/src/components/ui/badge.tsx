import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

export type BadgeTone = "teal" | "amber" | "danger" | "success" | "navy" | "neutral";

const toneClass: Record<BadgeTone, string> = {
  teal: "bg-teal-500/10 text-teal-700",
  amber: "bg-amber-100 text-amber-500",
  danger: "bg-danger-100 text-danger-500",
  success: "bg-success-100 text-success-500",
  navy: "bg-navy-800/10 text-navy-800",
  neutral: "bg-surface-100 text-slate-600",
};

export function Badge({
  children,
  tone = "neutral",
  className,
}: {
  children: ReactNode;
  tone?: BadgeTone;
  className?: string;
}) {
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[10px] font-bold",
        toneClass[tone],
        className,
      )}
    >
      {children}
    </span>
  );
}
