"use client";

import { useCallback, useEffect, useRef, useState, type ReactNode } from "react";
import { useFocusTrap } from "@/lib/use-focus-trap";
import { Breadcrumb, type BreadcrumbItem } from "@/components/shell/breadcrumb";
import { PageHeader } from "@/components/shell/page-header";
import { Sidebar } from "@/components/shell/sidebar";
import { Topbar } from "@/components/shell/topbar";
import { viewportFromWidth, type Viewport } from "@/lib/viewport";

export type AppShellProps = {
  children: ReactNode;
  breadcrumbs?: BreadcrumbItem[];
  pageTitle?: string;
  pageDescription?: string;
  pageActions?: ReactNode;
  currentHref?: string;
};

export function AppShell({
  children,
  breadcrumbs,
  pageTitle,
  pageDescription,
  pageActions,
  currentHref = "/",
}: AppShellProps) {
  const [viewport, setViewport] = useState<Viewport>("desktop");
  const [collapsed, setCollapsed] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const viewportRef = useRef<Viewport>("desktop");
  const sidebarRef = useRef<HTMLElement>(null);
  const closeMobile = useCallback(() => setMobileOpen(false), []);

  useEffect(() => {
    function sync() {
      const next = viewportFromWidth(window.innerWidth);
      const previous = viewportRef.current;
      viewportRef.current = next;
      setViewport(next);

      if (previous === next) {
        return;
      }

      if (next === "tablet") {
        setCollapsed(true);
      }

      if (next === "desktop") {
        setCollapsed(false);
      }

      if (next !== "mobile") {
        setMobileOpen(false);
      }
    }

    sync();
    window.addEventListener("resize", sync);
    return () => window.removeEventListener("resize", sync);
  }, []);

  useFocusTrap(viewport === "mobile" && mobileOpen, sidebarRef, closeMobile);

  return (
    <div className="flex min-h-screen bg-surface-50">
      <Sidebar
        ref={sidebarRef}
        currentHref={currentHref}
        collapsed={collapsed}
        viewport={viewport}
        mobileOpen={mobileOpen}
        onNavigate={closeMobile}
      />
      {viewport === "mobile" && mobileOpen ? (
        <button
          type="button"
          aria-label="Menüyü kapat"
          className="fixed inset-0 z-30 bg-navy-950/40"
          onClick={() => setMobileOpen(false)}
        />
      ) : null}
      <div className="flex min-w-0 flex-1 flex-col">
        <Topbar
          viewport={viewport}
          collapsed={collapsed}
          onToggleSidebar={() => setCollapsed((value) => !value)}
          onOpenMobile={() => setMobileOpen(true)}
        />
        <main className="flex-1 px-4 py-6 lg:px-8">
          {breadcrumbs ? <Breadcrumb items={breadcrumbs} /> : null}
          {pageTitle ? (
            <PageHeader
              title={pageTitle}
              description={pageDescription}
              actions={pageActions}
            />
          ) : null}
          {children}
        </main>
      </div>
    </div>
  );
}
