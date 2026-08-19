import type { BackendAuthTokens, ClientSession, MeResponse, SessionUser } from "@/lib/api/types";

export function toSessionUser(user: Partial<SessionUser> | undefined): SessionUser {
  return {
    id: user?.id ?? "",
    userName: user?.userName ?? "",
    displayName: user?.displayName ?? user?.userName ?? "",
    roles: user?.roles ?? [],
    permissions: user?.permissions ?? [],
  };
}

export function toClientSession(tokens: BackendAuthTokens): ClientSession {
  return {
    user: toSessionUser(tokens.user),
    accessTokenExpiresAt: tokens.accessTokenExpiresAt,
  };
}

export function clientSessionFromMe(payload: MeResponse): ClientSession {
  return { user: toSessionUser(payload.user) };
}

export function containsSecrets(payload: unknown): boolean {
  if (!payload || typeof payload !== "object") {
    return false;
  }
  const record = payload as Record<string, unknown>;
  return (
    "accessToken" in record ||
    "refreshToken" in record ||
    "access_token" in record ||
    "refresh_token" in record
  );
}
