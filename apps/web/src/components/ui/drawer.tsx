"use client";

import { useCallback, useId, useRef, type ReactNode } from "react";
import { X } from "lucide-react";
import { IconButton } from "@/components/ui/icon-button";
import { cn } from "@/lib/cn";
import { useFocusTrap } from "@/lib/use-focus-trap";

export type DrawerProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  children?: ReactNode;
  footer?: ReactNode;
  className?: string;
};

export function Drawer({
  open,
  onOpenChange,
  title,
  children,
  footer,
  className,
}: DrawerProps) {
  const panelRef = useRef<HTMLDivElement>(null);
  const titleId = useId();
  const close = useCallback(() => onOpenChange(false), [onOpenChange]);

  useFocusTrap(open, panelRef, close);

  if (!open) {
    return null;
  }

  return (
    <div className="fixed inset-0 z-50">
      <button
        type="button"
        aria-label="Paneli kapat"
        className="absolute inset-0 bg-navy-950/40"
        onClick={close}
      />
      <aside
        ref={panelRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        tabIndex={-1}
        className={cn(
          "absolute inset-x-0 bottom-0 flex max-h-[88vh] flex-col rounded-t-[22px] bg-white shadow-subtle",
          "md:inset-y-0 md:bottom-auto md:right-0 md:left-auto md:h-full md:w-[455px] md:max-h-none md:rounded-none md:rounded-l-2xl",
          className,
        )}
      >
        <div className="flex items-center justify-between border-b border-surface-200 px-5 py-4">
          <h2 id={titleId} className="text-base font-bold text-navy-950">
            {title}
          </h2>
          <IconButton label="Kapat" onClick={close} size="sm">
            <X className="h-4 w-4" />
          </IconButton>
        </div>
        <div className="min-h-0 flex-1 overflow-y-auto px-5 py-4">{children}</div>
        {footer ? (
          <div className="sticky bottom-0 border-t border-surface-200 bg-white px-5 py-4">
            {footer}
          </div>
        ) : null}
      </aside>
    </div>
  );
}
