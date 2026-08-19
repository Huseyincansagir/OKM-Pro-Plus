import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { CustomerCreate } from "@/components/sales/customer-create";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import { createCustomer } from "@/lib/sales/customers";
import { setWindowWidth } from "@/test/viewport";

vi.mock("@/lib/sales/customers", async () => {
  const actual = await vi.importActual<typeof import("@/lib/sales/customers")>(
    "@/lib/sales/customers",
  );
  return { ...actual, createCustomer: vi.fn() };
});

function authenticate(permissions: string[]) {
  useSessionStore.getState().setAuthenticated({
    id: "u1",
    userName: "admin",
    displayName: "Yusuf Kaya",
    roles: ["admin"],
    permissions,
  });
}

describe("CustomerCreate", () => {
  beforeEach(() => {
    setWindowWidth(1280);
    resetSessionStore();
    vi.mocked(createCustomer).mockReset();
  });

  afterEach(() => {
    resetSessionStore();
  });

  it("hides the form without customer.create", async () => {
    authenticate(["customer.read"]);
    render(<CustomerCreate />);
    expect(await screen.findByText("Müşteri kartı açılamaz")).toBeInTheDocument();
    expect(createCustomer).not.toHaveBeenCalled();
  });

  it("does not submit an empty legal name", async () => {
    const user = userEvent.setup();
    authenticate(["customer.create"]);
    render(<CustomerCreate />);
    await user.click(screen.getByRole("button", { name: "Kartı aç" }));
    expect(await screen.findByText("Unvan zorunludur.")).toBeInTheDocument();
    expect(createCustomer).not.toHaveBeenCalled();
  });

  it("submits staff fields without inventing a code", async () => {
    const user = userEvent.setup();
    authenticate(["customer.create"]);
    vi.mocked(createCustomer).mockResolvedValue({
      id: "c-new",
      customerCode: "MUS-2026-000001",
      legalName: "Acme Gıda",
      status: "Active",
      email: "a@b.com",
      phone: "",
      createdAt: "2026-08-19T00:00:00Z",
      primaryContactName: "",
      priceGroupCode: "",
      priceGroupName: "",
    });

    render(<CustomerCreate />);
    await user.type(screen.getByLabelText(/Unvan/), "Acme Gıda");
    await user.type(screen.getByLabelText(/E-posta/), "a@b.com");
    await user.click(screen.getByRole("button", { name: "Kartı aç" }));

    expect(createCustomer).toHaveBeenCalledWith({
      legalName: "Acme Gıda",
      email: "a@b.com",
      phone: undefined,
      taxNumber: undefined,
      taxOffice: undefined,
    });
  });
});
