import type { LucideIcon } from "lucide-react";
import { cn } from "@/lib/cn";

const toneClass = {
  teal: "bg-teal-500/10 text-teal-700",
  amber: "bg-amber-100 text-amber-500",
  navy: "bg-navy-800/10 text-navy-800",
  neutral: "bg-surface-100 text-slate-500",
} as const;

export function Glyph({
  icon: Icon,
  tone = "teal",
  size = "sm",
}: {
  icon: LucideIcon;
  tone?: keyof typeof toneClass;
  size?: "sm" | "md";
}) {
  return (
    <span
      className={cn(
        "grid shrink-0 place-items-center",
        size === "sm" ? "h-7 w-7 rounded-lg" : "h-12 w-12 rounded-2xl",
        toneClass[tone],
      )}
      aria-hidden="true"
    >
      <Icon className={size === "sm" ? "h-3.5 w-3.5" : "h-5 w-5"} />
    </span>
  );
}
