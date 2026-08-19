"use client";

import { create } from "zustand";
import type { QuoteBasketLine } from "@/lib/catalog/types";

function lineKey(line: Pick<QuoteBasketLine, "productId" | "enteredPackagingId">): string {
  return `${line.productId}:${line.enteredPackagingId ?? "base"}`;
}

type BasketState = {
  lines: QuoteBasketLine[];
  generalNote: string;
  addLine: (line: QuoteBasketLine) => void;
  updateQuantity: (key: string, enteredQuantity: number) => void;
  updateNote: (key: string, note: string) => void;
  removeLine: (key: string) => void;
  setGeneralNote: (note: string) => void;
  clear: () => void;
};

export const useQuoteBasketStore = create<BasketState>((set) => ({
  lines: [],
  generalNote: "",
  addLine: (incoming) =>
    set((state) => {
      const key = lineKey(incoming);
      const existing = state.lines.find((line) => lineKey(line) === key);
      if (!existing) {
        return { lines: [...state.lines, incoming] };
      }
      return {
        lines: state.lines.map((line) =>
          lineKey(line) === key
            ? {
                ...line,
                enteredQuantity: line.enteredQuantity + incoming.enteredQuantity,
                note: incoming.note || line.note,
                viewMode: incoming.viewMode,
              }
            : line,
        ),
      };
    }),
  updateQuantity: (key, enteredQuantity) =>
    set((state) => ({
      lines: state.lines.map((line) =>
        lineKey(line) === key ? { ...line, enteredQuantity } : line,
      ),
    })),
  updateNote: (key, note) =>
    set((state) => ({
      lines: state.lines.map((line) => (lineKey(line) === key ? { ...line, note } : line)),
    })),
  removeLine: (key) =>
    set((state) => ({
      lines: state.lines.filter((line) => lineKey(line) !== key),
    })),
  setGeneralNote: (generalNote) => set({ generalNote }),
  clear: () => set({ lines: [], generalNote: "" }),
}));

export function quoteLineKey(line: Pick<QuoteBasketLine, "productId" | "enteredPackagingId">) {
  return lineKey(line);
}

export function resetQuoteBasketStore() {
  useQuoteBasketStore.setState({ lines: [], generalNote: "" });
}

export function toQuoteRequestItems(lines: QuoteBasketLine[]) {
  return lines.map((line) => ({
    productId: line.productId,
    enteredQuantity: line.enteredQuantity,
    enteredPackagingId: line.enteredPackagingId,
    viewMode: line.viewMode,
  }));
}
