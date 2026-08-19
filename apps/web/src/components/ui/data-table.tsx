"use client";

import { ChevronDown, ChevronUp } from "lucide-react";
import type { ReactNode } from "react";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/states/empty-state";
import { ErrorState } from "@/components/states/error-state";

export type DataTableColumn<T> = {
  id: string;
  header: string;
  accessor: (row: T) => ReactNode;
  sortable?: boolean;
};

export type DataTableSort = {
  id: string;
  direction: "asc" | "desc";
};

export type DataTableProps<T> = {
  columns: DataTableColumn<T>[];
  rows: T[];
  getRowId: (row: T) => string;
  loading?: boolean;
  error?: string | null;
  emptyTitle?: string;
  emptyDescription?: string;
  onRetry?: () => void;
  sort?: DataTableSort | null;
  onSortChange?: (sort: DataTableSort) => void;
  selectedIds?: string[];
  onSelectionChange?: (ids: string[]) => void;
  page?: number;
  pageSize?: number;
  totalCount?: number;
  onPageChange?: (page: number) => void;
};

/**
 * Controlled DataTable foundation. Parent owns sort, selection and pagination.
 * This component does not fetch data or convert quantities.
 */
export function DataTable<T>({
  columns,
  rows,
  getRowId,
  loading = false,
  error = null,
  emptyTitle = "Kayıt yok",
  emptyDescription = "Bu listede gösterilecek kayıt bulunmuyor.",
  onRetry,
  sort = null,
  onSortChange,
  selectedIds,
  onSelectionChange,
  page = 1,
  pageSize = 10,
  totalCount,
  onPageChange,
}: DataTableProps<T>) {
  const selectable = Boolean(onSelectionChange);
  const selected = selectedIds ?? [];
  const total = totalCount ?? rows.length;
  const pageCount = Math.max(1, Math.ceil(total / pageSize));

  function toggleAll() {
    if (!onSelectionChange) {
      return;
    }
    const ids = rows.map(getRowId);
    const allSelected = ids.every((id) => selected.includes(id));
    onSelectionChange(allSelected ? [] : ids);
  }

  function toggleRow(id: string) {
    if (!onSelectionChange) {
      return;
    }
    onSelectionChange(
      selected.includes(id)
        ? selected.filter((item) => item !== id)
        : [...selected, id],
    );
  }

  return (
    <div className="overflow-hidden rounded-2xl border border-surface-200 bg-white">
      <div className="overflow-x-auto">
        <table className="min-w-full border-collapse text-left text-[13px]">
          <thead className="sticky top-0 bg-surface-50">
            <tr>
              {selectable ? (
                <th className="w-10 px-3 py-3">
                  <Checkbox
                    label="Tüm satırları seç"
                    className="sr-only-label"
                    checked={
                      rows.length > 0 &&
                      rows.every((row) => selected.includes(getRowId(row)))
                    }
                    onChange={toggleAll}
                  />
                </th>
              ) : null}
              {columns.map((column) => {
                const active = sort?.id === column.id;
                return (
                  <th
                    key={column.id}
                    className="px-3.5 py-3 text-[10px] font-extrabold tracking-wide text-slate-500 uppercase"
                  >
                    {column.sortable && onSortChange ? (
                      <button
                        type="button"
                        className="inline-flex items-center gap-1"
                        onClick={() =>
                          onSortChange({
                            id: column.id,
                            direction:
                              active && sort.direction === "asc" ? "desc" : "asc",
                          })
                        }
                      >
                        {column.header}
                        {active && sort.direction === "asc" ? (
                          <ChevronUp className="h-3 w-3" />
                        ) : (
                          <ChevronDown className="h-3 w-3" />
                        )}
                      </button>
                    ) : (
                      column.header
                    )}
                  </th>
                );
              })}
            </tr>
          </thead>
          <tbody>
            {loading ? (
              Array.from({ length: 4 }).map((_, index) => (
                <tr key={index}>
                  {(selectable ? [0, ...columns] : columns).map((column, colIndex) => (
                    <td key={typeof column === "number" ? "sel" : column.id + colIndex} className="px-3.5 py-3">
                      <Skeleton className="h-4 w-full" />
                    </td>
                  ))}
                </tr>
              ))
            ) : error ? (
              <tr>
                <td colSpan={columns.length + (selectable ? 1 : 0)} className="p-6">
                  <ErrorState title="Tablo yüklenemedi" description={error} onRetry={onRetry} />
                </td>
              </tr>
            ) : rows.length === 0 ? (
              <tr>
                <td colSpan={columns.length + (selectable ? 1 : 0)} className="p-6">
                  <EmptyState title={emptyTitle} description={emptyDescription} />
                </td>
              </tr>
            ) : (
              rows.map((row) => {
                const id = getRowId(row);
                return (
                  <tr key={id} className="border-t border-surface-100">
                    {selectable ? (
                      <td className="px-3 py-3">
                        <Checkbox
                          label={`${id} satırını seç`}
                          className="sr-only-label"
                          checked={selected.includes(id)}
                          onChange={() => toggleRow(id)}
                        />
                      </td>
                    ) : null}
                    {columns.map((column) => (
                      <td key={column.id} className="px-3.5 py-3 text-navy-950">
                        {column.accessor(row)}
                      </td>
                    ))}
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>
      {onPageChange ? (
        <div className="flex items-center justify-between border-t border-surface-200 px-4 py-3 text-xs text-slate-600">
          <span>
            Sayfa {page} / {pageCount}
          </span>
          <div className="flex gap-2">
            <Button
              size="sm"
              variant="secondary"
              disabled={page <= 1}
              onClick={() => onPageChange(page - 1)}
            >
              Önceki
            </Button>
            <Button
              size="sm"
              variant="secondary"
              disabled={page >= pageCount}
              onClick={() => onPageChange(page + 1)}
            >
              Sonraki
            </Button>
          </div>
        </div>
      ) : null}
    </div>
  );
}
