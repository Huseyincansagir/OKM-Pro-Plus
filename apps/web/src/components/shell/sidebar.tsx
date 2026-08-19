"use client";

import { forwardRef } from "react";
import { Factory } from "lucide-react";
import { NAVIGATION } from "@/config/navigation";
import { useSessionStore } from "@/lib/auth/session-store";
import { cn } from "@/lib/cn";
import type { Viewport } from "@/lib/viewport";

function initialsFrom(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) {
    return "KU";
  }
  if (parts.length === 1) {
    return parts[0].slice(0, 2).toUpperCase();
  }
  return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
}

export const Sidebar = forwardRef<
  HTMLElement,
  {
    currentHref?: string;
    collapsed: boolean;
    viewport: Viewport;
    mobileOpen: boolean;
    onNavigate?: () => void;
  }
>(function Sidebar(
  { currentHref = "/", collapsed, viewport, mobileOpen, onNavigate },
  ref,
) {
  const compact = collapsed && viewport !== "mobile";
  const hiddenOnMobile = viewport === "mobile" && !mobileOpen;
  const user = useSessionStore((state) => state.user);
  const userName = user?.displayName || user?.userName || "Kullanıcı";
  const roleLabel = user?.roles[0] || "Oturum açık";

  return (
    <aside
      ref={ref}
      id="app-sidebar"
      role="navigation"
      aria-label="Ana menü"
      aria-hidden={hiddenOnMobile || undefined}
      inert={hiddenOnMobile || undefined}
      className={cn(
        "flex h-full flex-col bg-navy-900 text-white/80 transition-[width,transform] duration-200",
        viewport === "mobile"
          ? cn(
              "fixed inset-y-0 left-0 z-40 w-[248px]",
              mobileOpen ? "translate-x-0" : "-translate-x-full",
            )
          : compact
            ? "w-[72px]"
            : "w-[248px]",
      )}
    >
      <div className={cn("flex items-center gap-3 px-4 pt-6 pb-4", compact && "justify-center px-2")}>
        <span className="grid h-9 w-9 place-items-center rounded-[11px] bg-teal-500 text-sm font-extrabold text-white">
          <Factory className="h-4 w-4" aria-hidden="true" />
        </span>
        {!compact ? (
          <div>
            <p className="text-sm font-extrabold text-white">Factory ERP</p>
            <p className="text-[10px] tracking-wide text-white/60">Üretim · Depo · Satış</p>
          </div>
        ) : null}
      </div>

      <div className="min-h-0 flex-1 overflow-y-auto px-3 pb-4">
        {NAVIGATION.map((section) => (
          <div key={section.label} className="mb-4">
            {!compact ? (
              <p className="px-3 pt-2 pb-1 text-[10px] font-extrabold tracking-[0.12em] text-white/50 uppercase">
                {section.label}
              </p>
            ) : null}
            <nav className="flex flex-col gap-1">
              {section.items.map((item) => {
                const active =
                  item.href === currentHref ||
                  (item.implemented === true &&
                    item.href !== "/" &&
                    currentHref.startsWith(`${item.href}/`));
                const Icon = item.icon;
                const ready = item.implemented === true;
                const className = cn(
                  "flex min-h-[41px] items-center gap-3 rounded-[11px] px-3 text-[13px]",
                  ready && active
                    ? "bg-teal-500 font-semibold text-white"
                    : "text-white/70",
                  ready && !active && "hover:bg-white/5",
                  !ready && "cursor-not-allowed opacity-50",
                  compact && "justify-center px-0",
                );

                if (!ready) {
                  return (
                    <span
                      key={item.label}
                      title="Bu ekran henüz bağlı değil"
                      aria-label={`${item.label} henüz bağlı değil`}
                      aria-disabled="true"
                      className={className}
                    >
                      <Icon className="h-[18px] w-[18px] shrink-0" aria-hidden="true" />
                      {!compact ? <span className="min-w-0 flex-1 truncate">{item.label}</span> : null}
                    </span>
                  );
                }

                return (
                  <a
                    key={item.label}
                    href={item.href}
                    title={compact ? item.label : undefined}
                    aria-label={compact ? item.label : undefined}
                    onClick={onNavigate}
                    aria-current={active ? "page" : undefined}
                    className={className}
                  >
                    <Icon className="h-[18px] w-[18px] shrink-0" aria-hidden="true" />
                    {!compact ? <span className="min-w-0 flex-1 truncate">{item.label}</span> : null}
                    {!compact && item.badge ? (
                      <span className="grid min-w-5 place-items-center rounded-md bg-white/15 px-1.5 text-[10px] text-white">
                        {item.badge}
                      </span>
                    ) : null}
                  </a>
                );
              })}
            </nav>
          </div>
        ))}
      </div>

      <div className={cn("border-t border-white/10 px-4 py-4", compact && "px-2")}>
        <div className={cn("flex items-center gap-3", compact && "justify-center")}>
          <span className="grid h-9 w-9 shrink-0 place-items-center rounded-full bg-teal-500 text-[11px] font-bold text-white">
            {initialsFrom(userName)}
          </span>
          {!compact ? (
            <div className="min-w-0">
              <p className="truncate text-sm font-semibold text-white">{userName}</p>
              <p className="truncate text-[11px] text-white/60">{roleLabel}</p>
            </div>
          ) : null}
        </div>
      </div>
    </aside>
  );
});
