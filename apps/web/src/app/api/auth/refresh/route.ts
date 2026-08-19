import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { backendFetch, isBackendAuthTokens, readJsonSafe } from "@/lib/api/server/backend";
import { REFRESH_COOKIE, clearAuthCookies, setAuthCookies } from "@/lib/api/server/cookies";
import { toClientSession } from "@/lib/api/session-public";

export async function POST() {
  const jar = await cookies();
  const refreshToken = jar.get(REFRESH_COOKIE)?.value;

  if (!refreshToken) {
    const response = NextResponse.json(
      {
        title: "Oturum yok",
        detail: "Oturum süresi dolmuş veya iptal edilmiş.",
        status: 401,
        code: "TOKEN_EXPIRED",
      },
      { status: 401 },
    );
    clearAuthCookies(response);
    return response;
  }

  const upstream = await backendFetch("/auth/refresh", {
    method: "POST",
    body: JSON.stringify({ refreshToken }),
  });
  const body = await readJsonSafe(upstream);

  if (!upstream.ok || !isBackendAuthTokens(body)) {
    const response = NextResponse.json(
      body ?? { title: "Oturum yenilenemedi", status: upstream.status },
      { status: upstream.status },
    );
    clearAuthCookies(response);
    return response;
  }

  const response = NextResponse.json(toClientSession(body));
  setAuthCookies(response, body);
  return response;
}
