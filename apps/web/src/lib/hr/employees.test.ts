import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";
import { createEmployee, listEmployees, mapEmployee } from "@/lib/hr/employees";

vi.mock("@/lib/api/client", () => ({
  apiRequest: vi.fn(),
}));

describe("employees", () => {
  beforeEach(() => {
    vi.mocked(apiRequest).mockReset();
  });

  it("does not invent salary fields", () => {
    const mapped = mapEmployee({ id: "e1", code: "PER-1", fullName: "Ali", salary: 1000 });
    expect(mapped.fullName).toBe("Ali");
    expect(mapped).not.toHaveProperty("salary");
  });

  it("lists GET /employees and rejects wrappers", async () => {
    vi.mocked(apiRequest).mockResolvedValue([{ id: "e1", fullName: "Ali" }]);
    const rows = await listEmployees();
    expect(apiRequest).toHaveBeenCalledWith({ path: "/employees", method: "GET" });
    expect(rows[0].fullName).toBe("Ali");
    vi.mocked(apiRequest).mockResolvedValue({ items: [] });
    await expect(listEmployees()).rejects.toBeInstanceOf(ApiError);
  });

  it("creates without a client-generated code", async () => {
    vi.mocked(apiRequest).mockResolvedValue({ id: "e1", code: "PER-2026-000001", fullName: "Ali" });
    await createEmployee({ fullName: "Ali" });
    const body = vi.mocked(apiRequest).mock.calls[0][0].body as Record<string, unknown>;
    expect(body).not.toHaveProperty("code");
    expect(body).not.toHaveProperty("salary");
  });
});
