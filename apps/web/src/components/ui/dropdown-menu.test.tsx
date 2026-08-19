import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { Button } from "@/components/ui/button";
import { DropdownMenu } from "@/components/ui/dropdown-menu";

describe("DropdownMenu", () => {
  it("opens from the trigger, selects an item, and closes on Escape", async () => {
    const user = userEvent.setup();
    const onSelect = vi.fn();

    render(
      <DropdownMenu
        label="Kullanıcı menüsü"
        trigger={<Button>Menüyü aç</Button>}
        items={[{ id: "profil", label: "Profil", onSelect }]}
      />,
    );

    const trigger = screen.getByRole("button", { name: "Menüyü aç" });
    await user.click(trigger);
    expect(trigger).toHaveAttribute("aria-expanded", "true");
    expect(screen.getByRole("menu", { name: "Kullanıcı menüsü" })).toBeInTheDocument();

    await user.click(screen.getByRole("menuitem", { name: "Profil" }));
    expect(onSelect).toHaveBeenCalledTimes(1);
    expect(screen.queryByRole("menu")).not.toBeInTheDocument();

    await user.click(trigger);
    await user.keyboard("{Escape}");
    expect(screen.queryByRole("menu")).not.toBeInTheDocument();
  });
});
