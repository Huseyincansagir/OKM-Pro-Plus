import { describe, expect, it } from "vitest";
import { normalizeApiError } from "@/lib/api/errors";

describe("normalizeApiError", () => {
  it("classifies status first even when the body is empty", () => {
    expect(normalizeApiError(401, null).kind).toBe("unauthenticated");
    expect(normalizeApiError(403, {}).kind).toBe("permission_denied");
    expect(normalizeApiError(404, {}).kind).toBe("not_found");
    expect(normalizeApiError(409, {}).kind).toBe("conflict");
    expect(normalizeApiError(422, {}).kind).toBe("business_rule");
    expect(normalizeApiError(500, {}).kind).toBe("unexpected");
  });

  it("reads ProblemDetails extensions from the top level and nested map", () => {
    const top = normalizeApiError(401, {
      title: "Kimlik doğrulanamadı",
      detail: "Parola geçersiz.",
      code: "UNAUTHENTICATED",
      requestId: "req-1",
    });
    expect(top.code).toBe("UNAUTHENTICATED");
    expect(top.requestId).toBe("req-1");

    const nested = normalizeApiError(400, {
      title: "Geçersiz istek",
      extensions: { code: "INVALID_REQUEST", correlationId: "cid-2" },
    });
    expect(nested.code).toBe("INVALID_REQUEST");
    expect(nested.correlationId).toBe("cid-2");
  });
});
