import type { NextResponse } from "next/server";

export const ACCESS_COOKIE = "fe_access";
export const REFRESH_COOKIE = "fe_refresh";

const isProd = process.env.NODE_ENV === "production";

type CookieTarget = {
  cookies: {
    set: (
      name: string,
      value: string,
      options: {
        httpOnly: boolean;
        secure: boolean;
        sameSite: "lax";
        path: string;
        expires?: Date;
        maxAge?: number;
      },
    ) => void;
  };
};

export function setAuthCookies(
  response: CookieTarget | NextResponse,
  tokens: {
    accessToken: string;
    accessTokenExpiresAt: string;
    refreshToken: string;
    refreshTokenExpiresAt: string;
  },
) {
  response.cookies.set(ACCESS_COOKIE, tokens.accessToken, {
    httpOnly: true,
    secure: isProd,
    sameSite: "lax",
    path: "/",
    expires: new Date(tokens.accessTokenExpiresAt),
  });
  response.cookies.set(REFRESH_COOKIE, tokens.refreshToken, {
    httpOnly: true,
    secure: isProd,
    sameSite: "lax",
    path: "/api/auth",
    expires: new Date(tokens.refreshTokenExpiresAt),
  });
}

export function clearAuthCookies(response: CookieTarget | NextResponse) {
  response.cookies.set(ACCESS_COOKIE, "", {
    httpOnly: true,
    secure: isProd,
    sameSite: "lax",
    path: "/",
    maxAge: 0,
  });
  response.cookies.set(REFRESH_COOKIE, "", {
    httpOnly: true,
    secure: isProd,
    sameSite: "lax",
    path: "/api/auth",
    maxAge: 0,
  });
}
