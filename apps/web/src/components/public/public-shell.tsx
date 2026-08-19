"use client";

import Link from "next/link";
import { useState, type ReactNode } from "react";
import { Menu, X } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { IconButton } from "@/components/ui/icon-button";
import { useQuoteBasketStore } from "@/lib/catalog/quote-basket-store";

export function PublicShell({ children }: { children: ReactNode }) {
  const [open, setOpen] = useState(false);
  const count = useQuoteBasketStore((state) => state.lines.length);

  return (
    <div className="min-h-screen bg-surface-50">
      <header className="border-b border-surface-200 bg-white">
        <div className="mx-auto flex h-16 max-w-6xl items-center gap-4 px-4">
          <Link href="/katalog" className="flex items-center gap-2">
            <span className="grid h-9 w-9 place-items-center rounded-[11px] bg-teal-500 text-sm font-extrabold text-white">
              F
            </span>
            <span>
              <span className="block text-sm font-extrabold text-navy-950">Factory ERP</span>
              <span className="block text-[10px] text-slate-500">Ürün kataloğu</span>
            </span>
          </Link>
          <nav className="ml-6 hidden items-center gap-4 text-sm font-semibold text-navy-800 md:flex">
            <Link href="/katalog">Ürünler</Link>
          </nav>
          <div className="ml-auto flex items-center gap-2">
            <Link
              href="/katalog/sepet"
              className="inline-flex h-[37px] items-center gap-2 rounded-[10px] border border-surface-200 bg-white px-3 text-xs font-semibold text-navy-800"
            >
              Teklif sepeti
              {count > 0 ? <Badge tone="teal">{count}</Badge> : null}
            </Link>
            <span className="md:hidden">
              <IconButton label={open ? "Menüyü kapat" : "Menüyü aç"} onClick={() => setOpen((v) => !v)}>
                {open ? <X className="h-4 w-4" /> : <Menu className="h-4 w-4" />}
              </IconButton>
            </span>
          </div>
        </div>
        {open ? (
          <nav className="border-t border-surface-200 px-4 py-3 md:hidden">
            <Link href="/katalog" className="block py-2 text-sm font-semibold" onClick={() => setOpen(false)}>
              Ürünler
            </Link>
            <Link href="/katalog/sepet" className="block py-2 text-sm font-semibold" onClick={() => setOpen(false)}>
              Teklif sepeti
            </Link>
          </nav>
        ) : null}
      </header>
      {children}
    </div>
  );
}
