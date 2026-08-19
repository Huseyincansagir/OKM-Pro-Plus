import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { describe, expect, it, vi } from "vitest";
import { QuantityViewToggle } from "@/components/quantity/quantity-view-toggle";
import type { QuantityViewMode } from "@/types/quantity";

const INITIAL_OPERATION = Object.freeze({
  operationQuantity: 5,
  quantityBase: 10000,
  operationPackagingId: "pkg-case-1",
  stockQuantity: 50000,
});

function ToggleHarness() {
  const [viewMode, setViewMode] = useState<QuantityViewMode>("Packaging");
  const [operation] = useState(() => ({ ...INITIAL_OPERATION }));

  return (
    <div>
      <QuantityViewToggle
        viewMode={viewMode}
        onViewModeChange={setViewMode}
        operationPackagingId={operation.operationPackagingId}
      />
      <p>viewMode:{viewMode}</p>
      <p>operationQuantity:{operation.operationQuantity}</p>
      <p>quantityBase:{operation.quantityBase}</p>
      <p>operationPackagingId:{operation.operationPackagingId}</p>
      <p>stockQuantity:{operation.stockQuantity}</p>
    </div>
  );
}

function expectQuantityUnchanged() {
  expect(screen.getByText("operationQuantity:5")).toBeInTheDocument();
  expect(screen.getByText("quantityBase:10000")).toBeInTheDocument();
  expect(screen.getByText("operationPackagingId:pkg-case-1")).toBeInTheDocument();
  expect(screen.getByText("stockQuantity:50000")).toBeInTheDocument();
}

describe("QuantityViewToggle", () => {
  it("changes only viewMode and leaves persisted quantity fields untouched", async () => {
    const user = userEvent.setup();
    render(<ToggleHarness />);

    await user.click(screen.getByRole("radio", { name: "Temel Birim" }));

    expect(screen.getByText("viewMode:BaseUnit")).toBeInTheDocument();
    expectQuantityUnchanged();
  });

  it("passes only the next viewMode to onViewModeChange", async () => {
    const user = userEvent.setup();
    const onViewModeChange = vi.fn();

    render(
      <QuantityViewToggle
        viewMode="Packaging"
        onViewModeChange={onViewModeChange}
        operationPackagingId="pkg-case-1"
      />,
    );

    await user.click(screen.getByRole("radio", { name: "Kırılım" }));

    expect(onViewModeChange).toHaveBeenCalledTimes(1);
    expect(onViewModeChange).toHaveBeenCalledWith("Breakdown");
    expect(onViewModeChange.mock.calls[0]).toHaveLength(1);
  });

  it("does not notify when the selected viewMode is clicked again", async () => {
    const user = userEvent.setup();
    const onViewModeChange = vi.fn();

    render(
      <QuantityViewToggle viewMode="Packaging" onViewModeChange={onViewModeChange} />,
    );

    await user.click(screen.getByRole("radio", { name: "Ambalaj" }));
    expect(onViewModeChange).not.toHaveBeenCalled();
  });

  it("does not change viewMode when disabled", async () => {
    const user = userEvent.setup();
    const onViewModeChange = vi.fn();

    render(
      <QuantityViewToggle
        viewMode="Packaging"
        onViewModeChange={onViewModeChange}
        disabled
      />,
    );

    await user.click(screen.getByRole("radio", { name: "Temel Birim" }));
    expect(onViewModeChange).not.toHaveBeenCalled();
  });

  it("moves viewMode with arrow keys without touching quantity fields", async () => {
    const user = userEvent.setup();
    render(<ToggleHarness />);

    screen.getByRole("radio", { name: "Ambalaj" }).focus();
    await user.keyboard("{ArrowRight}");
    expect(screen.getByText("viewMode:Breakdown")).toBeInTheDocument();
    expectQuantityUnchanged();

    await user.keyboard("{ArrowLeft}");
    expect(screen.getByText("viewMode:Packaging")).toBeInTheDocument();
    expectQuantityUnchanged();
  });
});
