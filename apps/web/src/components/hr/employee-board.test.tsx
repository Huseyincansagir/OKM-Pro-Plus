import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { EmployeeBoard } from "@/components/hr/employee-board";
import { resetSessionStore, useSessionStore } from "@/lib/auth/session-store";
import { listEmployees } from "@/lib/hr/employees";
import { setWindowWidth } from "@/test/viewport";

vi.mock("@/lib/hr/employees", async () => {
  const actual = await vi.importActual<typeof import("@/lib/hr/employees")>("@/lib/hr/employees");
  return { ...actual, listEmployees: vi.fn(), createEmployee: vi.fn() };
});

describe("EmployeeBoard", () => {
  beforeEach(() => {
    setWindowWidth(1280);
    resetSessionStore();
    vi.mocked(listEmployees).mockReset();
  });

  afterEach(() => {
    resetSessionStore();
  });

  it("skips the API without employee.read", async () => {
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Yusuf Kaya",
      roles: ["admin"],
      permissions: [],
    });
    render(<EmployeeBoard />);
    expect(await screen.findByText("Personel bu oturumda görünmez")).toBeInTheDocument();
    expect(listEmployees).not.toHaveBeenCalled();
  });

  it("renders staff rows without salary copy", async () => {
    useSessionStore.getState().setAuthenticated({
      id: "u1",
      userName: "admin",
      displayName: "Yusuf Kaya",
      roles: ["admin"],
      permissions: ["employee.read"],
    });
    vi.mocked(listEmployees).mockResolvedValue([
      {
        id: "e1",
        code: "PER-2026-000001",
        fullName: "Ali Kaya",
        title: "Operatör",
        department: "Depo",
        status: "Active",
        createdAt: "2026-08-19T00:00:00Z",
      },
    ]);
    render(<EmployeeBoard />);
    expect(await screen.findByText("Ali Kaya")).toBeInTheDocument();
    expect(screen.getByText("PER-2026-000001")).toBeInTheDocument();
    expect(screen.getByText(/Maaş\/puantaj bu dilimde yoktur/)).toBeInTheDocument();
    expect(screen.queryByRole("columnheader", { name: /^Maaş$/i })).not.toBeInTheDocument();
    expect(screen.queryByText(/₺/)).not.toBeInTheDocument();
  });
});
