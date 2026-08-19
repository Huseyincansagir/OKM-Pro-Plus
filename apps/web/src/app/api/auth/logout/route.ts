import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { backendFetch } from "@/lib/api/server/backend";
import { ACCESS_COOKIE, REFRESH_COOKIE, clearAuthCookies } from "@/lib/api/server/cookies";

export async function POST() {
  const jar = await cookies();
  const accessToken = jar.get(ACCESS_COOKIE)?.value;
  const refreshToken = jar.get(REFRESH_COOKIE)?.value;

  if (accessToken) {
    await backendFetch("/auth/logout", {
      method: "POST",
      headers: {
        Authorization: `Bearer ${accessToken}`,
      },
      body: JSON.stringify({ refreshToken: refreshToken ?? "" }),
    }).catch(() => undefined);
  }

  const response = new NextResponse(null, { status: 204 });
  clearAuthCookies(response);
  return response;
}
