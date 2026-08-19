import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { describe, expect, it } from "vitest";
import { Button } from "@/components/ui/button";
import { Dialog } from "@/components/ui/dialog";

function DialogHarness() {
  const [open, setOpen] = useState(false);

  return (
    <>
      <Button onClick={() => setOpen(true)}>Pencereyi aç</Button>
      <Dialog
        open={open}
        onOpenChange={setOpen}
        title="Onay"
        description="Bu işlem örnektir."
      >
        <p>Etki özeti</p>
      </Dialog>
    </>
  );
}

describe("Dialog", () => {
  it("opens, traps focus, and returns focus after Escape", async () => {
    const user = userEvent.setup();
    render(<DialogHarness />);

    const trigger = screen.getByRole("button", { name: "Pencereyi aç" });
    await user.click(trigger);

    const dialog = screen.getByRole("dialog", { name: "Onay" });
    expect(dialog).toBeInTheDocument();
    expect(dialog).toHaveAttribute("aria-modal", "true");
    expect(dialog.contains(document.activeElement)).toBe(true);

    await user.keyboard("{Escape}");
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(trigger).toHaveFocus();
  });
});
