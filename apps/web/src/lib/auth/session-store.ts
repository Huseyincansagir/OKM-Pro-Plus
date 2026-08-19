"use client";

import { create } from "zustand";
import type { SessionUser } from "@/lib/api/types";

export type SessionStatus = "unknown" | "anonymous" | "authenticated";

type SessionState = {
  status: SessionStatus;
  user: SessionUser | null;
  setAuthenticated: (user: SessionUser) => void;
  setAnonymous: () => void;
};

export const useSessionStore = create<SessionState>((set) => ({
  status: "unknown",
  user: null,
  setAuthenticated: (user) => set({ status: "authenticated", user }),
  setAnonymous: () => set({ status: "anonymous", user: null }),
}));

export function resetSessionStore() {
  useSessionStore.setState({ status: "unknown", user: null });
}

export function hasPermission(code: string): boolean {
  const permissions = useSessionStore.getState().user?.permissions ?? [];
  return permissions.includes(code);
}
