import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { LoginForm } from "@/components/auth/login-form";
import { resetSessionStore } from "@/lib/auth/session-store";

describe("LoginForm", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    resetSessionStore();
  });

  it("shows a field error when username is empty", async () => {
    const user = userEvent.setup();
    const fetchMock = vi.fn();
    vi.stubGlobal("fetch", fetchMock);
    render(<LoginForm />);

    await user.click(screen.getByRole("button", { name: "Giriş yap" }));
    expect(await screen.findByText("Kullanıcı adı zorunludur.")).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("does not treat a failed login as success", async () => {
    const user = userEvent.setup();
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(
        JSON.stringify({
          title: "Kimlik doğrulanamadı",
          detail: "Kullanıcı adı veya parola geçersiz.",
          code: "UNAUTHENTICATED",
        }),
        { status: 401 },
      ),
    );
    vi.stubGlobal("fetch", fetchMock);

    render(<LoginForm />);
    await user.type(screen.getByLabelText(/Kullanıcı adı/), "admin");
    await user.type(screen.getByLabelText(/Parola/), "wrong");
    await user.click(screen.getByRole("button", { name: "Giriş yap" }));

    expect(await screen.findByText("Kullanıcı adı veya parola geçersiz.")).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(String(fetchMock.mock.calls[0][0])).toBe("/api/auth/login");
  });
});
