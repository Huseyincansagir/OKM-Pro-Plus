import { NextResponse, type NextRequest } from "next/server";
import { ACCESS_COOKIE, REFRESH_COOKIE } from "@/lib/api/server/cookies";

function hasSession(request: NextRequest): boolean {
  return Boolean(request.cookies.get(ACCESS_COOKIE)?.value || request.cookies.get(REFRESH_COOKIE)?.value);
}

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;
  const session = hasSession(request);

  if (pathname === "/giris" && session) {
    return NextResponse.redirect(new URL("/dashboard", request.url));
  }

  const isPublic =
    pathname === "/giris" ||
    pathname.startsWith("/katalog") ||
    pathname.startsWith("/api/auth/login") ||
    pathname.startsWith("/api/auth/refresh") ||
    pathname.startsWith("/api/auth/logout");

  if (!session && !isPublic && !pathname.startsWith("/api/")) {
    return NextResponse.redirect(new URL("/giris", request.url));
  }

  return NextResponse.next();
}

export const config = {
  matcher: ["/((?!_next/static|_next/image|favicon.ico).*)"],
};
