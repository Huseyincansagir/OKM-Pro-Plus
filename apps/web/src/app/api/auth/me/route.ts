import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { backendFetch, isBackendAuthTokens, readJsonSafe } from "@/lib/api/server/backend";
import { ACCESS_COOKIE, REFRESH_COOKIE, clearAuthCookies, setAuthCookies } from "@/lib/api/server/cookies";
import { clientSessionFromMe, toClientSession } from "@/lib/api/session-public";
import type { MeResponse } from "@/lib/api/types";

async function loadMe(accessToken: string): Promise<{ ok: boolean; status: number; body: unknown }> {
  const upstream = await backendFetch("/auth/me", {
    method: "GET",
    headers: { Authorization: `Bearer ${accessToken}` },
  });
  return { ok: upstream.ok, status: upstream.status, body: await readJsonSafe(upstream) };
}

export async function GET() {
  const jar = await cookies();
  const accessToken = jar.get(ACCESS_COOKIE)?.value;
  const refreshToken = jar.get(REFRESH_COOKIE)?.value;

  if (!accessToken && !refreshToken) {
    return NextResponse.json(
      { title: "Oturum yok", status: 401, code: "UNAUTHENTICATED" },
      { status: 401 },
    );
  }

  if (accessToken) {
    const me = await loadMe(accessToken);
    if (me.ok) {
      return NextResponse.json(clientSessionFromMe(me.body as MeResponse));
    }
    if (me.status !== 401 || !refreshToken) {
      const response = NextResponse.json(me.body ?? { status: me.status }, { status: me.status });
      if (me.status === 401) {
        clearAuthCookies(response);
      }
      return response;
    }
  }

  if (!refreshToken) {
    const response = NextResponse.json({ status: 401, code: "TOKEN_EXPIRED" }, { status: 401 });
    clearAuthCookies(response);
    return response;
  }

  const refresh = await backendFetch("/auth/refresh", {
    method: "POST",
    body: JSON.stringify({ refreshToken }),
  });
  const refreshBody = await readJsonSafe(refresh);
  if (!refresh.ok || !isBackendAuthTokens(refreshBody)) {
    const response = NextResponse.json(refreshBody ?? { status: 401 }, { status: 401 });
    clearAuthCookies(response);
    return response;
  }

  const me = await loadMe(refreshBody.accessToken);
  const payload = me.ok
    ? clientSessionFromMe(me.body as MeResponse)
    : toClientSession(refreshBody);
  const response = NextResponse.json(payload);
  setAuthCookies(response, refreshBody);
  return response;
}
