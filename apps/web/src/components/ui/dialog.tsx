"use client";

import { useCallback, useId, useRef, type ReactNode } from "react";
import { X } from "lucide-react";
import { IconButton } from "@/components/ui/icon-button";
import { cn } from "@/lib/cn";
import { useFocusTrap } from "@/lib/use-focus-trap";

export type DialogProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  description?: string;
  children?: ReactNode;
  footer?: ReactNode;
  className?: string;
};

export function Dialog({
  open,
  onOpenChange,
  title,
  description,
  children,
  footer,
  className,
}: DialogProps) {
  const panelRef = useRef<HTMLDivElement>(null);
  const titleId = useId();
  const descriptionId = description ? `${titleId}-description` : undefined;
  const close = useCallback(() => onOpenChange(false), [onOpenChange]);

  useFocusTrap(open, panelRef, close);

  if (!open) {
    return null;
  }

  return (
    <div className="fixed inset-0 z-50 flex items-end justify-center p-4 sm:items-center">
      <button
        type="button"
        aria-label="Pencereyi kapat"
        className="absolute inset-0 bg-navy-950/40"
        onClick={close}
      />
      <div
        ref={panelRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={descriptionId}
        tabIndex={-1}
        className={cn(
          "relative z-10 w-full max-w-[520px] rounded-[18px] border border-surface-200 bg-white p-5 shadow-subtle",
          className,
        )}
      >
        <div className="mb-4 flex items-start justify-between gap-3">
          <div>
            <h2 id={titleId} className="text-lg font-bold text-navy-950">
              {title}
            </h2>
            {description ? (
              <p id={descriptionId} className="mt-1 text-sm text-slate-600">
                {description}
              </p>
            ) : null}
          </div>
          <IconButton label="Kapat" onClick={close} size="sm">
            <X className="h-4 w-4" />
          </IconButton>
        </div>
        {children}
        {footer ? <div className="mt-5 flex justify-end gap-2">{footer}</div> : null}
      </div>
    </div>
  );
}
