"use client";

import { toApiError } from "@/lib/api/errors";
import { createCorrelationId, createIdempotencyKey, requiresIdempotencyKey } from "@/lib/api/headers";
import { refreshSession } from "@/lib/api/refresh";
import { useSessionStore } from "@/lib/auth/session-store";
import { ApiError, type ApiRequestOptions } from "@/lib/api/types";

const CLIENT_API_BASE = "/api/v1";

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

export async function apiRequest<T>(options: ApiRequestOptions): Promise<T> {
  const method = options.method ?? (options.body ? "POST" : "GET");
  const path = options.path.startsWith("/") ? options.path : `/${options.path}`;
  const headers = new Headers();
  headers.set("Accept", "application/json");
  headers.set("X-Correlation-Id", createCorrelationId());

  if (options.body !== undefined) {
    headers.set("Content-Type", "application/json");
  }

  const needsIdempotency =
    options.idempotent || requiresIdempotencyKey(method, path);
  const idempotencyKey = needsIdempotency
    ? (options.idempotencyKey ?? createIdempotencyKey())
    : undefined;
  if (idempotencyKey) {
    headers.set("Idempotency-Key", idempotencyKey);
  }
  if (options.ifMatch) {
    headers.set("If-Match", options.ifMatch);
  }

  const response = await fetch(`${CLIENT_API_BASE}${path}`, {
    method,
    headers,
    body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
    credentials: "same-origin",
  });

  if (
    response.status === 401 &&
    options.auth !== false &&
    !options.skipRefresh
  ) {
    const refreshed = await refreshSession();
    if (refreshed) {
      return apiRequest<T>({
        ...options,
        idempotencyKey,
        skipRefresh: true,
      });
    }
    useSessionStore.getState().setAnonymous();
    const body = await parseBody(response);
    throw toApiError(401, body, "Oturum süresi doldu.");
  }

  if (response.status === 403) {
    const body = await parseBody(response);
    throw toApiError(403, body, "Bu işlem için yetkiniz yok.");
  }

  if (!response.ok) {
    const body = await parseBody(response);
    throw toApiError(response.status, body);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await parseBody(response)) as T;
}

export function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError;
}
