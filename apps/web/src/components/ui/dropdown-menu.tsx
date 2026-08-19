"use client";

import {
  cloneElement,
  isValidElement,
  useEffect,
  useId,
  useRef,
  useState,
  type MouseEvent,
  type ReactElement,
} from "react";
import { cn } from "@/lib/cn";

export type DropdownItem = {
  id: string;
  label: string;
  onSelect: () => void;
  danger?: boolean;
};

type TriggerProps = {
  onClick?: (event: MouseEvent<HTMLElement>) => void;
  "aria-expanded"?: boolean;
  "aria-haspopup"?: "menu";
  "aria-controls"?: string;
};

export function DropdownMenu({
  trigger,
  items,
  align = "end",
  label,
}: {
  trigger: ReactElement<TriggerProps>;
  items: DropdownItem[];
  align?: "start" | "end";
  label: string;
}) {
  const [open, setOpen] = useState(false);
  const rootRef = useRef<HTMLDivElement>(null);
  const menuId = useId();

  useEffect(() => {
    if (!open) {
      return;
    }

    function onPointerDown(event: globalThis.MouseEvent) {
      if (!rootRef.current?.contains(event.target as Node)) {
        setOpen(false);
      }
    }

    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        setOpen(false);
      }
    }

    document.addEventListener("mousedown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("mousedown", onPointerDown);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [open]);

  if (!isValidElement(trigger)) {
    throw new Error("DropdownMenu trigger bir React elementi olmalıdır.");
  }

  const triggerEl = cloneElement(trigger, {
    "aria-expanded": open,
    "aria-haspopup": "menu",
    "aria-controls": menuId,
    onClick: (event: MouseEvent<HTMLElement>) => {
      trigger.props.onClick?.(event);
      setOpen((value) => !value);
    },
  });

  return (
    <div ref={rootRef} className="relative">
      {triggerEl}
      {open ? (
        <div
          id={menuId}
          role="menu"
          aria-label={label}
          className={cn(
            "absolute top-full z-30 mt-2 min-w-44 rounded-xl border border-surface-200 bg-white py-1 shadow-subtle",
            align === "end" ? "right-0" : "left-0",
          )}
        >
          {items.map((item) => (
            <button
              key={item.id}
              type="button"
              role="menuitem"
              className={cn(
                "flex w-full px-3 py-2 text-left text-sm",
                item.danger ? "text-danger-500" : "text-navy-900",
                "hover:bg-surface-50",
              )}
              onClick={() => {
                item.onSelect();
                setOpen(false);
              }}
            >
              {item.label}
            </button>
          ))}
        </div>
      ) : null}
    </div>
  );
}
