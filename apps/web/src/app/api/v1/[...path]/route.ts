import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { backendFetch } from "@/lib/api/server/backend";
import { ACCESS_COOKIE } from "@/lib/api/server/cookies";

const BLOCKED_AUTH = new Set(["auth/login", "auth/refresh"]);

async function proxy(request: Request, pathSegments: string[]) {
  const joined = pathSegments.join("/");
  if (BLOCKED_AUTH.has(joined)) {
    return NextResponse.json(
      {
        title: "Bulunamadı",
        detail: "Kimlik uçları BFF üzerinden kullanılır.",
        status: 404,
        code: "RESOURCE_NOT_FOUND",
      },
      { status: 404 },
    );
  }

  const jar = await cookies();
  const accessToken = jar.get(ACCESS_COOKIE)?.value;
  const url = new URL(request.url);
  const search = url.search;
  const headers = new Headers();
  const correlation = request.headers.get("X-Correlation-Id");
  const idempotency = request.headers.get("Idempotency-Key");
  const ifMatch = request.headers.get("If-Match");
  if (correlation) headers.set("X-Correlation-Id", correlation);
  if (idempotency) headers.set("Idempotency-Key", idempotency);
  if (ifMatch) headers.set("If-Match", ifMatch);
  if (accessToken) headers.set("Authorization", `Bearer ${accessToken}`);

  const contentType = request.headers.get("Content-Type");
  if (contentType) headers.set("Content-Type", contentType);

  const hasBody = request.method !== "GET" && request.method !== "HEAD";
  const body = hasBody ? await request.text() : undefined;

  const upstream = await backendFetch(`/${joined}${search}`, {
    method: request.method,
    headers,
    body: body && body.length > 0 ? body : undefined,
  });

  const payload = await upstream.arrayBuffer();
  const responseHeaders = new Headers();
  const upstreamType = upstream.headers.get("Content-Type");
  if (upstreamType) responseHeaders.set("Content-Type", upstreamType);
  const upstreamCorrelation = upstream.headers.get("X-Correlation-Id");
  if (upstreamCorrelation) responseHeaders.set("X-Correlation-Id", upstreamCorrelation);

  return new NextResponse(payload, {
    status: upstream.status,
    headers: responseHeaders,
  });
}

type RouteContext = { params: Promise<{ path: string[] }> };

export async function GET(request: Request, context: RouteContext) {
  return proxy(request, (await context.params).path);
}

export async function POST(request: Request, context: RouteContext) {
  return proxy(request, (await context.params).path);
}

export async function PUT(request: Request, context: RouteContext) {
  return proxy(request, (await context.params).path);
}

export async function PATCH(request: Request, context: RouteContext) {
  return proxy(request, (await context.params).path);
}

export async function DELETE(request: Request, context: RouteContext) {
  return proxy(request, (await context.params).path);
}
