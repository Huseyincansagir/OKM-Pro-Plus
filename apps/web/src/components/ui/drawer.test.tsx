import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { describe, expect, it } from "vitest";
import { Button } from "@/components/ui/button";
import { Drawer } from "@/components/ui/drawer";

function DrawerHarness() {
  const [open, setOpen] = useState(false);

  return (
    <>
      <Button onClick={() => setOpen(true)}>Paneli aç</Button>
      <Drawer open={open} onOpenChange={setOpen} title="Örnek drawer">
        <p>Hızlı önizleme</p>
      </Drawer>
    </>
  );
}

describe("Drawer", () => {
  it("opens, traps focus, and returns focus after Escape", async () => {
    const user = userEvent.setup();
    render(<DrawerHarness />);

    const trigger = screen.getByRole("button", { name: "Paneli aç" });
    await user.click(trigger);

    const drawer = screen.getByRole("dialog", { name: "Örnek drawer" });
    expect(drawer).toBeInTheDocument();
    expect(drawer).toHaveAttribute("aria-modal", "true");
    expect(drawer.contains(document.activeElement)).toBe(true);

    await user.keyboard("{Escape}");
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(trigger).toHaveFocus();
  });
});
