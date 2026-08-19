"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { Spinner } from "@/components/ui/spinner";
import { useSessionStore } from "@/lib/auth/session-store";

export function SessionGate({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const status = useSessionStore((state) => state.status);

  useEffect(() => {
    if (status === "anonymous") {
      router.replace("/giris");
    }
  }, [router, status]);

  if (status === "authenticated") {
    return children;
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-surface-50">
      <p className="inline-flex items-center gap-2 text-sm text-slate-600">
        <Spinner />
        Oturum kontrol ediliyor
      </p>
    </div>
  );
}
