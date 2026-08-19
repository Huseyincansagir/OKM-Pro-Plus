"use client";

import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { cn } from "@/lib/cn";

export type ToastTone = "success" | "warning" | "danger" | "info";

export type ToastMessage = {
  id: string;
  title: string;
  description?: string;
  tone?: ToastTone;
};

type ToastContextValue = {
  toasts: ToastMessage[];
  pushToast: (toast: Omit<ToastMessage, "id">) => void;
  dismissToast: (id: string) => void;
};

const ToastContext = createContext<ToastContextValue | null>(null);

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastMessage[]>([]);

  const dismissToast = useCallback((id: string) => {
    setToasts((current) => current.filter((toast) => toast.id !== id));
  }, []);

  const pushToast = useCallback(
    (toast: Omit<ToastMessage, "id">) => {
      const id = `${Date.now()}-${Math.random().toString(16).slice(2)}`;
      setToasts((current) => [...current, { ...toast, id }]);
      window.setTimeout(() => dismissToast(id), 5000);
    },
    [dismissToast],
  );

  const value = useMemo(
    () => ({ toasts, pushToast, dismissToast }),
    [toasts, pushToast, dismissToast],
  );

  return (
    <ToastContext.Provider value={value}>
      {children}
      <div
        className="pointer-events-none fixed right-4 bottom-4 z-[60] flex w-[min(100%-2rem,330px)] flex-col gap-2"
        aria-live="polite"
        aria-relevant="additions"
      >
        {toasts.map((toast) => (
          <div
            key={toast.id}
            className={cn(
              "pointer-events-auto rounded-xl border bg-white px-4 py-3 shadow-subtle",
              toast.tone === "danger" && "border-danger-500/30",
              toast.tone === "warning" && "border-amber-500/30",
              toast.tone === "success" && "border-success-500/30",
              (!toast.tone || toast.tone === "info") && "border-surface-200",
            )}
          >
            <div className="flex items-start justify-between gap-3">
              <div>
                <p className="text-sm font-semibold text-navy-950">{toast.title}</p>
                {toast.description ? (
                  <p className="mt-1 text-xs text-slate-600">{toast.description}</p>
                ) : null}
              </div>
              <button
                type="button"
                className="text-xs font-semibold text-slate-500"
                onClick={() => dismissToast(toast.id)}
              >
                Kapat
              </button>
            </div>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast() {
  const context = useContext(ToastContext);
  if (!context) {
    throw new Error("useToast ToastProvider içinde kullanılmalıdır.");
  }
  return context;
}
