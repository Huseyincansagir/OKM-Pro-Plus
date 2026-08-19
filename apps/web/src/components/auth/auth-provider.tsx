"use client";

import { useEffect, type ReactNode } from "react";
import { fetchCurrentSession } from "@/lib/api/auth-client";
import { useSessionStore } from "@/lib/auth/session-store";

export function AuthProvider({ children }: { children: ReactNode }) {
  const status = useSessionStore((state) => state.status);
  const setAnonymous = useSessionStore((state) => state.setAnonymous);

  useEffect(() => {
    if (status !== "unknown") {
      return;
    }

    fetchCurrentSession().catch(() => {
      setAnonymous();
    });
  }, [setAnonymous, status]);

  return children;
}
