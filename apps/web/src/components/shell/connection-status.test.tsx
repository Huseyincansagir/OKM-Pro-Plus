import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { ConnectionStatus } from "@/components/shell/connection-status";

function setOnline(value: boolean) {
  Object.defineProperty(window.navigator, "onLine", {
    configurable: true,
    value,
  });
}

describe("ConnectionStatus", () => {
  it("shows Bağlı when the browser is online", async () => {
    setOnline(true);
    render(<ConnectionStatus />);
    expect(await screen.findByText("Bağlı")).toBeInTheDocument();
  });

  it("switches to Çevrimdışı on the offline event", async () => {
    setOnline(true);
    render(<ConnectionStatus />);
    expect(await screen.findByText("Bağlı")).toBeInTheDocument();

    setOnline(false);
    window.dispatchEvent(new Event("offline"));
    expect(await screen.findByText("Çevrimdışı")).toBeInTheDocument();
  });
});
