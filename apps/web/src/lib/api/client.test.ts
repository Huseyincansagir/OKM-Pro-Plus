import { afterEach, describe, expect, it, vi } from "vitest";
import { apiRequest } from "@/lib/api/client";
import { resetRefreshFlight } from "@/lib/api/refresh";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";

describe("apiRequest", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    resetRefreshFlight();
    resetSessionStore();
  });

  it("single-flights refresh and retries an authenticated request once", async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(new Response(null, { status: 401 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ user: { id: "1" } }), { status: 200 }))
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ id: "order-1" }), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }),
      );

    vi.stubGlobal("fetch", fetchMock);

    const result = await apiRequest<{ id: string }>({
      path: "/orders/1",
      method: "GET",
    });

    expect(result).toEqual({ id: "order-1" });
    expect(fetchMock).toHaveBeenCalledTimes(3);
    expect(String(fetchMock.mock.calls[1][0])).toBe("/api/auth/refresh");
  });

  it("does not refresh after a 403", async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValue(new Response(JSON.stringify({ title: "Yasak" }), { status: 403 }));
    vi.stubGlobal("fetch", fetchMock);

    await expect(apiRequest({ path: "/orders/1", method: "GET" })).rejects.toMatchObject({
      kind: "permission_denied",
      status: 403,
    });
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it("sends a stable Idempotency-Key on POST retry", async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(new Response(null, { status: 401 }))
      .mockResolvedValueOnce(new Response(null, { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ id: "created" }), { status: 200 }));
    vi.stubGlobal("fetch", fetchMock);

    await apiRequest({
      path: "/orders",
      method: "POST",
      body: { note: "x" },
      idempotencyKey: "intent-1",
    });

    const firstHeaders = fetchMock.mock.calls[0][1].headers as Headers;
    const retryHeaders = fetchMock.mock.calls[2][1].headers as Headers;
    expect(firstHeaders.get("Idempotency-Key")).toBe("intent-1");
    expect(retryHeaders.get("Idempotency-Key")).toBe("intent-1");
  });

  it("clears the session when refresh fails", async () => {
    useSessionStore.getState().setAuthenticated({
      id: "1",
      userName: "admin",
      displayName: "Admin",
      roles: [],
      permissions: [],
    });
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(new Response(null, { status: 401 }))
      .mockResolvedValueOnce(new Response(null, { status: 401 }));
    vi.stubGlobal("fetch", fetchMock);

    await expect(apiRequest({ path: "/auth/me", method: "GET" })).rejects.toMatchObject({
      status: 401,
    });
    expect(useSessionStore.getState().status).toBe("anonymous");
  });

  it("clears the session when refresh throws a network error", async () => {
    useSessionStore.getState().setAuthenticated({
      id: "1",
      userName: "admin",
      displayName: "Admin",
      roles: [],
      permissions: [],
    });
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(new Response(null, { status: 401 }))
      .mockRejectedValueOnce(new Error("network"));
    vi.stubGlobal("fetch", fetchMock);

    await expect(apiRequest({ path: "/orders/1", method: "GET" })).rejects.toMatchObject({
      kind: "network",
    });
    expect(useSessionStore.getState().status).toBe("anonymous");
  });
});
