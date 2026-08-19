import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { AppShell } from "@/components/shell/app-shell";
import { setWindowWidth } from "@/test/viewport";

describe("AppShell", () => {
  it("renders sidebar, topbar and content together", async () => {
    setWindowWidth(1280);
    render(
      <AppShell pageTitle="Önizleme" pageDescription="AppShell içerik alanı">
        <p>İçerik gövdesi</p>
      </AppShell>,
    );

    expect(screen.getByLabelText("Ana menü")).toBeInTheDocument();
    expect(screen.getByRole("banner")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Önizleme" })).toBeInTheDocument();
    expect(screen.getByText("İçerik gövdesi")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByLabelText("Menüyü daralt")).toBeInTheDocument();
    });
  });

  it("collapses the sidebar on desktop", async () => {
    const user = userEvent.setup();
    setWindowWidth(1280);
    render(
      <AppShell pageTitle="Daralt">
        <p>Gövde</p>
      </AppShell>,
    );

    const collapse = await screen.findByLabelText("Menüyü daralt");
    await user.click(collapse);
    expect(screen.getByLabelText("Menüyü genişlet")).toBeInTheDocument();
    expect(screen.getByLabelText("Dashboard")).toBeInTheDocument();
  });

  it("starts collapsed on tablet", async () => {
    setWindowWidth(900);
    render(
      <AppShell pageTitle="Tablet">
        <p>Gövde</p>
      </AppShell>,
    );

    expect(await screen.findByLabelText("Menüyü genişlet")).toBeInTheDocument();
  });

  it("opens and closes the mobile drawer with Escape", async () => {
    const user = userEvent.setup();
    setWindowWidth(375);
    render(
      <AppShell pageTitle="Mobil">
        <p>Gövde</p>
      </AppShell>,
    );

    const openButton = await screen.findByLabelText("Menüyü aç");
    await user.click(openButton);
    expect(screen.getByLabelText("Ana menü")).not.toHaveAttribute("aria-hidden");

    await user.keyboard("{Escape}");
    expect(screen.getByLabelText("Ana menü")).toHaveAttribute("aria-hidden", "true");
  });
});
