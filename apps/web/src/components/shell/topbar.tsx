"use client";

import { Menu, PanelLeftClose, PanelLeftOpen, Search } from "lucide-react";
import { IconButton } from "@/components/ui/icon-button";
import { ConnectionStatus } from "@/components/shell/connection-status";
import { NotificationArea } from "@/components/shell/notification-area";
import { UserMenu } from "@/components/shell/user-menu";
import type { Viewport } from "@/lib/viewport";

export function Topbar({
  viewport,
  collapsed,
  onToggleSidebar,
  onOpenMobile,
}: {
  viewport: Viewport;
  collapsed: boolean;
  onToggleSidebar: () => void;
  onOpenMobile: () => void;
}) {
  return (
    <header className="flex h-[73px] items-center gap-3 border-b border-surface-200 bg-white/95 px-4 lg:px-8 max-md:h-16">
      {viewport === "mobile" ? (
        <IconButton label="Menüyü aç" onClick={onOpenMobile}>
          <Menu className="h-4 w-4" />
        </IconButton>
      ) : (
        <IconButton
          label={collapsed ? "Menüyü genişlet" : "Menüyü daralt"}
          onClick={onToggleSidebar}
        >
          {collapsed ? (
            <PanelLeftOpen className="h-4 w-4" />
          ) : (
            <PanelLeftClose className="h-4 w-4" />
          )}
        </IconButton>
      )}

      <label className="relative hidden min-w-0 flex-1 md:block md:max-w-xs">
        <span className="sr-only">Sipariş, ürün veya müşteri ara</span>
        <Search
          className="pointer-events-none absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2 text-slate-400"
          aria-hidden="true"
        />
        <input
          disabled
          placeholder="Sipariş, ürün veya müşteri ara"
          className="h-[38px] w-full rounded-[10px] border border-surface-200 bg-surface-50 pr-3 pl-9 text-xs text-slate-500"
        />
      </label>

      <div className="ml-auto flex items-center gap-2">
        <span className="hidden h-[34px] items-center rounded-[9px] border border-surface-200 bg-white px-2.5 text-xs text-slate-600 sm:inline-flex">
          Merkez Depo
        </span>
        <ConnectionStatus />
        <NotificationArea unreadCount={0} />
        <UserMenu />
      </div>
    </header>
  );
}
