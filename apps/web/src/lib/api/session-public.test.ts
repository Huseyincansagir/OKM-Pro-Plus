import { describe, expect, it } from "vitest";
import { containsSecrets, toClientSession } from "@/lib/api/session-public";
import type { BackendAuthTokens } from "@/lib/api/types";

const tokens: BackendAuthTokens = {
  accessToken: "access-secret",
  accessTokenExpiresAt: "2026-08-19T12:00:00Z",
  refreshToken: "refresh-secret",
  refreshTokenExpiresAt: "2026-09-02T12:00:00Z",
  user: {
    id: "user-1",
    userName: "admin",
    displayName: "Yönetici",
    roles: ["system_admin"],
    permissions: ["order.read"],
  },
};

describe("session-public", () => {
  it("strips tokens from the client session payload", () => {
    const session = toClientSession(tokens);
    expect(session.user.userName).toBe("admin");
    expect(containsSecrets(session)).toBe(false);
    expect(session).not.toHaveProperty("accessToken");
    expect(session).not.toHaveProperty("refreshToken");
  });

  it("detects secret fields in raw backend payloads", () => {
    expect(containsSecrets(tokens)).toBe(true);
    expect(containsSecrets({ user: { id: "1" } })).toBe(false);
  });
});
