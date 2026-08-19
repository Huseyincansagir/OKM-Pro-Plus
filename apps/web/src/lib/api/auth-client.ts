"use client";

import { toApiError, userFacingMessage } from "@/lib/api/errors";
import { containsSecrets } from "@/lib/api/session-public";
import { useSessionStore } from "@/lib/auth/session-store";
import type { ClientSession } from "@/lib/api/types";

async function parseBody(response: Response): Promise<unknown> {
  const text = await response.text();
  if (!text) {
    return null;
  }
  try {
    return JSON.parse(text) as unknown;
  } catch {
    return { detail: text };
  }
}

function assertSafeSession(payload: unknown): ClientSession {
  if (containsSecrets(payload)) {
    throw new Error("Oturum yanıtı gizli alan içeremez.");
  }
  const record = payload as ClientSession;
  if (!record?.user?.id) {
    throw toApiError(500, payload, "Oturum yanıtı geçersiz.");
  }
  return {
    user: {
      id: record.user.id,
      userName: record.user.userName,
      displayName: record.user.displayName,
      roles: record.user.roles ?? [],
      permissions: record.user.permissions ?? [],
    },
    accessTokenExpiresAt: record.accessTokenExpiresAt,
  };
}

export async function login(userName: string, password: string): Promise<ClientSession> {
  const response = await fetch("/api/auth/login", {
    method: "POST",
    credentials: "same-origin",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ userName, password }),
  });
  const body = await parseBody(response);
  if (!response.ok) {
    throw toApiError(response.status, body, "Giriş başarısız.");
  }
  const session = assertSafeSession(body);
  useSessionStore.getState().setAuthenticated(session.user);
  return session;
}

export async function fetchCurrentSession(): Promise<ClientSession | null> {
  const response = await fetch("/api/auth/me", {
    method: "GET",
    credentials: "same-origin",
  });
  if (response.status === 401) {
    useSessionStore.getState().setAnonymous();
    return null;
  }
  const body = await parseBody(response);
  if (!response.ok) {
    throw toApiError(response.status, body);
  }
  const session = assertSafeSession(body);
  useSessionStore.getState().setAuthenticated(session.user);
  return session;
}

export async function logout(): Promise<void> {
  try {
    await fetch("/api/auth/logout", {
      method: "POST",
      credentials: "same-origin",
    });
  } finally {
    useSessionStore.getState().setAnonymous();
  }
}

export { userFacingMessage };
