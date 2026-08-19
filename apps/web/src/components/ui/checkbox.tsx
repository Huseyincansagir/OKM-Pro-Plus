import { forwardRef, type InputHTMLAttributes } from "react";
import { cn } from "@/lib/cn";

export type CheckboxProps = Omit<InputHTMLAttributes<HTMLInputElement>, "type"> & {
  label: string;
  hint?: string;
};

export const Checkbox = forwardRef<HTMLInputElement, CheckboxProps>(
  function Checkbox({ id, label, hint, className, disabled, ...props }, ref) {
    const fieldId = id ?? props.name ?? label;

    return (
      <label
        htmlFor={fieldId}
        className={cn(
          "inline-flex items-start gap-2 text-sm text-navy-900",
          disabled ? "cursor-not-allowed opacity-60" : "cursor-pointer",
          className,
        )}
      >
        <input
          ref={ref}
          id={fieldId}
          type="checkbox"
          disabled={disabled}
          className="mt-0.5 h-4 w-4 rounded border-surface-200 text-teal-600"
          {...props}
        />
        <span>
          <span className="font-medium">{label}</span>
          {hint ? (
            <span className="mt-0.5 block text-[12px] text-slate-600">{hint}</span>
          ) : null}
        </span>
      </label>
    );
  },
);
