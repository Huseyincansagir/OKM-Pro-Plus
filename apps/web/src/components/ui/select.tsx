import { forwardRef, type SelectHTMLAttributes } from "react";
import { cn } from "@/lib/cn";

export type SelectOption = { value: string; label: string };

export type SelectProps = SelectHTMLAttributes<HTMLSelectElement> & {
  label: string;
  options: SelectOption[];
  hint?: string;
  error?: string;
};

export const Select = forwardRef<HTMLSelectElement, SelectProps>(function Select(
  { id, label, options, hint, error, className, required, disabled, ...props },
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
      <select
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
          "max-md:h-[43px] disabled:cursor-not-allowed disabled:bg-surface-100",
          error ? "border-danger-500" : "border-surface-200",
          className,
        )}
        {...props}
      >
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
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
