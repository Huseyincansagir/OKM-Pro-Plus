import { beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "@/lib/api/types";
import { apiRequest } from "@/lib/api/client";
import { getCurrentAccount, mapCurrentAccount } from "@/lib/sales/current-accounts";

vi.mock("@/lib/api/client", () => ({
  apiRequest: vi.fn(),
}));

describe("mapCurrentAccount", () => {
  it("keeps server totals and rejects a payload without amounts", () => {
    expect(
      mapCurrentAccount({
        customerId: "c1",
        currencyCode: "TRY",
        debitTotal: 10,
        creditTotal: 4,
        balance: 6,
        rowVersion: 1,
      }),
    ).toEqual({
      customerId: "c1",
      currencyCode: "TRY",
      debitTotal: 10,
      creditTotal: 4,
      balance: 6,
    });
    expect(() => mapCurrentAccount({ customerId: "c1", balance: 0 })).toThrow(ApiError);
  });
});

describe("getCurrentAccount", () => {
  beforeEach(() => {
    vi.mocked(apiRequest).mockReset();
  });

  it("returns null when the account does not exist", async () => {
    vi.mocked(apiRequest).mockRejectedValue(
      new ApiError({ kind: "not_found", status: 404, detail: "Yok" }),
    );
    await expect(getCurrentAccount("c1")).resolves.toBeNull();
  });

  it("rethrows permission errors", async () => {
    vi.mocked(apiRequest).mockRejectedValue(
      new ApiError({ kind: "permission_denied", status: 403, detail: "Yasak" }),
    );
    await expect(getCurrentAccount("c1")).rejects.toMatchObject({ kind: "permission_denied" });
  });
});
