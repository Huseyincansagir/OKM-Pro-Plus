import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { OrderDetail } from "@/components/sales/order-detail";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import { getSalesOrder, submitSalesOrder } from "@/lib/sales/orders";
import { createDeliveryNote } from "@/lib/shipping/delivery-notes";
import { setWindowWidth } from "@/test/viewport";

vi.mock("@/lib/sales/orders", async () => {
  const actual = await vi.importActual<typeof import("@/lib/sales/orders")>(
    "@/lib/sales/orders",
  );
  return { ...actual, getSalesOrder: vi.fn(), submitSalesOrder: vi.fn() };
});
vi.mock("@/lib/shipping/delivery-notes", async () => {
  const actual = await vi.importActual<typeof import("@/lib/shipping/delivery-notes")>(
    "@/lib/shipping/delivery-notes",
  );
  return { ...actual, createDeliveryNote: vi.fn() };
});

const detail = {
  id: "o1",
  orderNumber: "SO-2026-000001",
  status: "Draft",
  customerId: "c1",
  customerCode: "MUS-1",
  customerLegalName: "Acme",
  currencyCode: "TRY",
  totalNet: 20000,
  totalTax: 0,
  totalGross: 20000,
  itemCount: 1,
  createdAt: "2026-08-19T10:00:00Z",
  rowVersion: 1,
  items: [
    {
      id: "i1",
      productId: "p1",
      enteredQuantity: 5,
      enteredPackagingId: "pkg",
      orderedQty: 10000,
      reservedQty: 0,
      shippedQty: 0,
      remainingQty: 10000,
      packagingName: "Koli",
      unitPrice: 2,
    },
  ],
};

function authenticate(permissions: string[]) {
  useSessionStore.getState().setAuthenticated({
    id: "u1",
    userName: "admin",
    displayName: "Yusuf Kaya",
    roles: ["admin"],
    permissions,
  });
}

describe("OrderDetail", () => {
  beforeEach(() => {
    setWindowWidth(1280);
    resetSessionStore();
    vi.mocked(getSalesOrder).mockReset();
    vi.mocked(submitSalesOrder).mockReset();
    vi.mocked(createDeliveryNote).mockReset();
  });

  afterEach(() => {
    resetSessionStore();
  });

  it("shows server remainingQty and hides submit without permission", async () => {
    authenticate(["order.read"]);
    vi.mocked(getSalesOrder).mockResolvedValue(detail);

    render(<OrderDetail id="o1" />);

    expect(await screen.findByRole("heading", { name: "SO-2026-000001" })).toBeInTheDocument();
    expect(screen.getAllByText("10000").length).toBeGreaterThan(0);
    expect(screen.getByText("Koli")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Onaya gönder" })).not.toBeInTheDocument();
  });

  it("submits a draft after confirmation", async () => {
    const user = userEvent.setup();
    authenticate(["order.read", "order.submit"]);
    vi.mocked(getSalesOrder).mockResolvedValue(detail);
    vi.mocked(submitSalesOrder).mockResolvedValue({
      ...detail,
      status: "PendingApproval",
    });

    render(<OrderDetail id="o1" />);

    await user.click(await screen.findByRole("button", { name: "Onaya gönder" }));
    const dialog = await screen.findByRole("dialog");
    await user.click(within(dialog).getByRole("button", { name: "Onaya gönder" }));
    expect(submitSalesOrder).toHaveBeenCalledWith("o1");
  });

  it("creates a delivery note from remainingQty in BaseUnit", async () => {
    const user = userEvent.setup();
    authenticate(["order.read", "delivery-note.create"]);
    vi.mocked(getSalesOrder).mockResolvedValue({ ...detail, status: "Approved" });
    vi.mocked(createDeliveryNote).mockResolvedValue({
      id: "dn1",
      documentNumber: "DN-1",
      salesOrderId: "o1",
      customerId: "c1",
      status: "Draft",
      issuedAt: null,
      itemCount: 1,
      rowVersion: 1,
      items: [],
    });

    render(<OrderDetail id="o1" />);
    await user.click(await screen.findByRole("button", { name: "İrsaliye oluştur" }));
    const dialog = await screen.findByRole("dialog");
    await user.click(within(dialog).getByRole("button", { name: "Taslak irsaliye" }));
    expect(createDeliveryNote).toHaveBeenCalledWith({
      salesOrderId: "o1",
      items: [
        {
          salesOrderItemId: "i1",
          enteredQuantity: 10000,
          enteredPackagingId: null,
          viewMode: "BaseUnit",
        },
      ],
    });
  });
});
