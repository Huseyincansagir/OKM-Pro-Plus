import { NextResponse } from "next/server";
import { backendFetch, isBackendAuthTokens, readJsonSafe } from "@/lib/api/server/backend";
import { setAuthCookies } from "@/lib/api/server/cookies";
import { toClientSession } from "@/lib/api/session-public";

export async function POST(request: Request) {
  const payload = (await request.json().catch(() => null)) as {
    userName?: string;
    password?: string;
  } | null;

  if (!payload?.userName?.trim() || !payload.password) {
    return NextResponse.json(
      {
        title: "Geçersiz istek",
        detail: "Kullanıcı adı ve parola zorunludur.",
        status: 400,
        code: "INVALID_REQUEST",
      },
      { status: 400 },
    );
  }

  const upstream = await backendFetch("/auth/login", {
    method: "POST",
    body: JSON.stringify({
      userName: payload.userName.trim(),
      password: payload.password,
    }),
  });
  const body = await readJsonSafe(upstream);

  if (!upstream.ok || !isBackendAuthTokens(body)) {
    return NextResponse.json(body ?? { title: "Giriş başarısız", status: upstream.status }, {
      status: upstream.status,
    });
  }

  const response = NextResponse.json(toClientSession(body));
  setAuthCookies(response, body);
  return response;
}
