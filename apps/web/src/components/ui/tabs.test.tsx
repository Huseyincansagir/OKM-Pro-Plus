import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { describe, expect, it } from "vitest";
import { Tabs } from "@/components/ui/tabs";

function TabsHarness() {
  const [value, setValue] = useState("bilesenler");

  return (
    <Tabs
      value={value}
      onValueChange={setValue}
      tabs={[
        { id: "bilesenler", label: "Bileşenler" },
        { id: "tablo", label: "Tablo" },
      ]}
    />
  );
}

describe("Tabs", () => {
  it("marks the selected tab and notifies on change", async () => {
    const user = userEvent.setup();
    render(<TabsHarness />);

    expect(screen.getByRole("tab", { name: "Bileşenler" })).toHaveAttribute(
      "aria-selected",
      "true",
    );

    await user.click(screen.getByRole("tab", { name: "Tablo" }));
    expect(screen.getByRole("tab", { name: "Tablo" })).toHaveAttribute(
      "aria-selected",
      "true",
    );
    expect(screen.getByRole("tab", { name: "Bileşenler" })).toHaveAttribute(
      "aria-selected",
      "false",
    );
  });
});
