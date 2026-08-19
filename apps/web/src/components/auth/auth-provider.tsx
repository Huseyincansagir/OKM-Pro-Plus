"use client";

import { useEffect, useState, type ReactNode } from "react";
import { fetchCurrentSession, userFacingMessage } from "@/lib/api/auth-client";
import { useSessionStore } from "@/lib/auth/session-store";
import { ErrorState } from "@/components/states/error-state";

export function AuthProvider({ children }: { children: ReactNode }) {
  const status = useSessionStore((state) => state.status);
  const [bootstrapError, setBootstrapError] = useState<string | null>(null);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    if (status !== "unknown") {
      return;
    }

    let cancelled = false;
    fetchCurrentSession()
      .then(() => {
        if (!cancelled) {
          setBootstrapError(null);
        }
      })
      .catch((error) => {
        if (!cancelled) {
          setBootstrapError(userFacingMessage(error));
        }
      });

    return () => {
      cancelled = true;
    };
  }, [attempt, status]);

  if (status === "unknown" && bootstrapError) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-surface-50 p-6">
        <ErrorState
          title="Oturum doğrulanamadı"
          description={bootstrapError}
          onRetry={() => {
            setBootstrapError(null);
            setAttempt((value) => value + 1);
          }}
        />
      </div>
    );
  }

  return children;
}
