import { forwardRef, type ButtonHTMLAttributes } from "react";
import { Spinner } from "@/components/ui/spinner";
import { cn } from "@/lib/cn";

export type ButtonVariant = "primary" | "secondary" | "ghost" | "danger";
export type ButtonSize = "sm" | "md" | "lg";

export type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: ButtonVariant;
  size?: ButtonSize;
  loading?: boolean;
};

const variantClass: Record<ButtonVariant, string> = {
  primary: "bg-teal-500 text-white shadow-subtle hover:bg-teal-600",
  secondary:
    "border border-surface-200 bg-white text-navy-800 hover:bg-surface-50",
  ghost: "bg-teal-500/10 text-teal-700 hover:bg-teal-500/15",
  danger: "bg-danger-500 text-white hover:bg-danger-500/90",
};

const sizeClass: Record<ButtonSize, string> = {
  sm: "min-h-[30px] px-2.5 text-[11px]",
  md: "min-h-[37px] px-3.5 text-xs md:min-h-[37px] max-md:min-h-[43px]",
  lg: "min-h-[43px] px-4 text-sm",
};

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  function Button(
    {
      className,
      variant = "primary",
      size = "md",
      loading = false,
      disabled,
      children,
      type = "button",
      ...props
    },
    ref,
  ) {
    const isDisabled = disabled || loading;

    return (
      <button
        ref={ref}
        type={type}
        className={cn(
          "inline-flex items-center justify-center gap-2 rounded-[10px] font-semibold transition-colors",
          "disabled:cursor-not-allowed disabled:opacity-60",
          variantClass[variant],
          sizeClass[size],
          className,
        )}
        disabled={isDisabled}
        aria-busy={loading || undefined}
        {...props}
      >
        {loading ? <Spinner size="sm" className="text-current" /> : null}
        {children}
      </button>
    );
  },
);
