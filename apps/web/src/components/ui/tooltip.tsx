"use client";

import { useId, useState, type ReactNode } from "react";
import { cn } from "@/lib/cn";

export function Tooltip({
  content,
  children,
  className,
}: {
  content: string;
  children: ReactNode;
  className?: string;
}) {
  const id = useId();
  const [open, setOpen] = useState(false);

  return (
    <span
      className={cn("relative inline-flex", className)}
      onMouseEnter={() => setOpen(true)}
      onMouseLeave={() => setOpen(false)}
      onFocus={() => setOpen(true)}
      onBlur={() => setOpen(false)}
    >
      <span aria-describedby={open ? id : undefined}>{children}</span>
      {open ? (
        <span
          id={id}
          role="tooltip"
          className="absolute bottom-full left-1/2 z-20 mb-2 -translate-x-1/2 whitespace-nowrap rounded-md bg-navy-950 px-2 py-1 text-[11px] text-white"
        >
          {content}
        </span>
      ) : null}
    </span>
  );
}
