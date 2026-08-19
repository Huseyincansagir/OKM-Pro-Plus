import { render, screen } from "@testing-library/react";
import { usePathname } from "next/navigation";
import { afterEach, describe, expect, it, vi } from "vitest";
import { AuthProvider } from "@/components/auth/auth-provider";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";

describe("AuthProvider", () => {
  afterEach(() => {
    vi.mocked(usePathname).mockReturnValue("/");
    vi.unstubAllGlobals();
    resetSessionStore();
  });

  it("does not mark the session anonymous when /me fails with a server error", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify({ title: "Sunucu hatası", detail: "Oturum doğrulanamadı." }), {
          status: 500,
        }),
      ),
    );

    render(
      <AuthProvider>
        <p>Uygulama gövdesi</p>
      </AuthProvider>,
    );

    expect(await screen.findByText("Oturum doğrulanamadı")).toBeInTheDocument();
    expect(screen.queryByText("Uygulama gövdesi")).not.toBeInTheDocument();
    expect(useSessionStore.getState().status).toBe("unknown");
  });

  it("marks the session anonymous after a 401 without looping into an error screen", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(new Response(null, { status: 401 })),
    );

    render(
      <AuthProvider>
        <p>Uygulama gövdesi</p>
      </AuthProvider>,
    );

    expect(await screen.findByText("Uygulama gövdesi")).toBeInTheDocument();
    expect(useSessionStore.getState().status).toBe("anonymous");
  });

  it("does not take down the public catalog when /me fails", async () => {
    vi.mocked(usePathname).mockReturnValue("/katalog");
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify({ title: "Sunucu hatası" }), { status: 500 }),
      ),
    );

    render(
      <AuthProvider>
        <p>Katalog gövdesi</p>
      </AuthProvider>,
    );

    expect(await screen.findByText("Katalog gövdesi")).toBeInTheDocument();
    expect(screen.queryByText("Oturum doğrulanamadı")).not.toBeInTheDocument();
  });
});
