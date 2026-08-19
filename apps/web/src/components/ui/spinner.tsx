import { cn } from "@/lib/cn";

export function Spinner({
  size = "md",
  className,
  label = "Yükleniyor",
}: {
  size?: "sm" | "md";
  className?: string;
  label?: string;
}) {
  return (
    <span
      role="status"
      aria-label={label}
      className={cn(
        "inline-block animate-spin rounded-full border-2 border-current border-r-transparent",
        size === "sm" ? "h-3.5 w-3.5" : "h-5 w-5",
        className,
      )}
    />
  );
}
