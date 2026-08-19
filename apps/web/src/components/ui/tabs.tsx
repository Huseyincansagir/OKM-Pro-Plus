"use client";

import { cn } from "@/lib/cn";

export type TabItem = { id: string; label: string };

export function Tabs({
  tabs,
  value,
  onValueChange,
}: {
  tabs: TabItem[];
  value: string;
  onValueChange: (id: string) => void;
}) {
  return (
    <div role="tablist" className="flex gap-1 border-b border-surface-200">
      {tabs.map((tab) => {
        const selected = tab.id === value;
        return (
          <button
            key={tab.id}
            type="button"
            role="tab"
            aria-selected={selected}
            id={`tab-${tab.id}`}
            className={cn(
              "relative px-3.5 py-3 text-xs font-semibold",
              selected ? "text-teal-700" : "text-slate-500 hover:text-navy-800",
            )}
            onClick={() => onValueChange(tab.id)}
          >
            {tab.label}
            {selected ? (
              <span className="absolute inset-x-3 -bottom-px h-0.5 rounded-full bg-teal-500" />
            ) : null}
          </button>
        );
      })}
    </div>
  );
}
