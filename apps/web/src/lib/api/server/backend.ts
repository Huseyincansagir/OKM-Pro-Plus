import type { BackendAuthTokens } from "@/lib/api/types";

export function getBackendApiBaseUrl(): string {
  const configured =
    process.env.FACTORY_ERP_API_BASE_URL ?? process.env.NEXT_PUBLIC_API_BASE_URL;
  return (configured ?? "http://localhost:8080/api/v1").replace(/\/$/, "");
}

export async function backendFetch(
  path: string,
  init: RequestInit = {},
): Promise<Response> {
  const url = `${getBackendApiBaseUrl()}${path.startsWith("/") ? path : `/${path}`}`;
  const headers = new Headers(init.headers);
  if (!headers.has("X-Correlation-Id")) {
    headers.set("X-Correlation-Id", crypto.randomUUID());
  }
  if (init.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  return fetch(url, {
    ...init,
    headers,
    cache: "no-store",
  });
}

export async function readJsonSafe(response: Response): Promise<unknown> {
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

export function isBackendAuthTokens(value: unknown): value is BackendAuthTokens {
  if (!value || typeof value !== "object") {
    return false;
  }
  const record = value as Record<string, unknown>;
  return (
    typeof record.accessToken === "string" &&
    typeof record.refreshToken === "string" &&
    typeof record.user === "object" &&
    record.user !== null
  );
}
