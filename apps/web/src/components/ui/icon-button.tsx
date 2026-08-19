import { forwardRef, type ButtonHTMLAttributes } from "react";
import { Spinner } from "@/components/ui/spinner";
import { cn } from "@/lib/cn";

export type IconButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  label: string;
  loading?: boolean;
  size?: "sm" | "md";
};

export const IconButton = forwardRef<HTMLButtonElement, IconButtonProps>(
  function IconButton(
    {
      className,
      label,
      loading = false,
      disabled,
      children,
      size = "md",
      type = "button",
      ...props
    },
    ref,
  ) {
    return (
      <button
        ref={ref}
        type={type}
        aria-label={label}
        disabled={disabled || loading}
        aria-busy={loading || undefined}
        className={cn(
          "inline-flex items-center justify-center rounded-[10px] border border-surface-200 bg-white text-navy-800",
          "hover:bg-surface-50 disabled:cursor-not-allowed disabled:opacity-60",
          size === "sm" ? "h-8 w-8" : "h-9 w-9 max-md:h-11 max-md:w-11",
          className,
        )}
        {...props}
      >
        {loading ? <Spinner size="sm" /> : children}
      </button>
    );
  },
);
