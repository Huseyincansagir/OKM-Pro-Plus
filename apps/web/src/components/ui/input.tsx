import { forwardRef, type InputHTMLAttributes, type ReactNode } from "react";
import { cn } from "@/lib/cn";

export type InputProps = InputHTMLAttributes<HTMLInputElement> & {
  label: string;
  hint?: string;
  error?: string;
  trailing?: ReactNode;
};

export const Input = forwardRef<HTMLInputElement, InputProps>(function Input(
  { id, label, hint, error, trailing, className, required, disabled, ...props },
  ref,
) {
  const fieldId = id ?? props.name ?? label;
  const hintId = hint ? `${fieldId}-hint` : undefined;
  const errorId = error ? `${fieldId}-error` : undefined;

  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={fieldId} className="text-xs font-semibold text-navy-900">
        {label}
        {required ? (
          <span className="ml-1 text-danger-500" aria-hidden="true">
            *
          </span>
        ) : null}
      </label>
      <div className="relative">
        <input
          ref={ref}
          id={fieldId}
          required={required}
          disabled={disabled}
          aria-invalid={error ? true : undefined}
          aria-describedby={
            [errorId, hint && !error ? hintId : undefined].filter(Boolean).join(" ") ||
            undefined
          }
          className={cn(
            "h-[37px] w-full rounded-[9px] border bg-white px-3 text-sm text-navy-950",
            "max-md:h-[43px] placeholder:text-slate-400",
            "disabled:cursor-not-allowed disabled:bg-surface-100",
            error ? "border-danger-500" : "border-surface-200",
            trailing ? "pr-10" : null,
            className,
          )}
          {...props}
        />
        {trailing ? (
          <span className="pointer-events-none absolute inset-y-0 right-3 flex items-center text-slate-400">
            {trailing}
          </span>
        ) : null}
      </div>
      {hint && !error ? (
        <p id={hintId} className="text-[12px] text-slate-600">
          {hint}
        </p>
      ) : null}
      {error ? (
        <p id={errorId} className="text-[12px] text-danger-500">
          {error}
        </p>
      ) : null}
    </div>
  );
});
