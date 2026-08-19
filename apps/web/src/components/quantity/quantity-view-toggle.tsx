"use client";

import {
  QUANTITY_VIEW_MODE_LABELS,
  QUANTITY_VIEW_MODES,
  type QuantityViewMode,
} from "@/types/quantity";
import { cn } from "@/lib/cn";

export type QuantityViewToggleProps = {
  viewMode: QuantityViewMode;
  onViewModeChange: (mode: QuantityViewMode) => void;
  /**
   * Accepted so a parent can keep packaging on the same form object.
   * Never read for mutation; never included in onViewModeChange.
   */
  operationPackagingId?: string | null;
  disabled?: boolean;
  className?: string;
};

/**
 * Display-only quantity view switch.
 * Does not change operationPackagingId, entered quantity, quantityBase or stock.
 */
export function QuantityViewToggle({
  viewMode,
  onViewModeChange,
  disabled = false,
  className,
}: QuantityViewToggleProps) {
  function move(delta: number) {
    const index = QUANTITY_VIEW_MODES.indexOf(viewMode);
    const next =
      QUANTITY_VIEW_MODES[
        (index + delta + QUANTITY_VIEW_MODES.length) % QUANTITY_VIEW_MODES.length
      ];
    if (next !== viewMode) {
      onViewModeChange(next);
    }
  }

  return (
    <div
      role="radiogroup"
      aria-label="Miktar görünümü"
      className={cn(
        "inline-flex rounded-[10px] border border-surface-200 bg-white p-1",
        disabled && "opacity-60",
        className,
      )}
      onKeyDown={(event) => {
        if (disabled) {
          return;
        }
        if (event.key === "ArrowRight" || event.key === "ArrowDown") {
          event.preventDefault();
          move(1);
        }
        if (event.key === "ArrowLeft" || event.key === "ArrowUp") {
          event.preventDefault();
          move(-1);
        }
      }}
    >
      {QUANTITY_VIEW_MODES.map((mode) => {
        const selected = mode === viewMode;
        return (
          <button
            key={mode}
            type="button"
            role="radio"
            aria-checked={selected}
            tabIndex={selected ? 0 : -1}
            disabled={disabled}
            className={cn(
              "min-h-[37px] rounded-lg px-3 text-xs font-semibold max-md:min-h-[43px]",
              selected
                ? "bg-teal-500 text-white"
                : "text-navy-800 hover:bg-surface-50",
            )}
            onClick={() => {
              if (!disabled && mode !== viewMode) {
                onViewModeChange(mode);
              }
            }}
          >
            {QUANTITY_VIEW_MODE_LABELS[mode]}
          </button>
        );
      })}
    </div>
  );
}
