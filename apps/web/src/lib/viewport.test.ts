import { describe, expect, it } from "vitest";
import { viewportFromWidth } from "@/lib/viewport";

describe("viewportFromWidth", () => {
  it("maps the ERP responsive breakpoints", () => {
    expect(viewportFromWidth(320)).toBe("mobile");
    expect(viewportFromWidth(767)).toBe("mobile");
    expect(viewportFromWidth(768)).toBe("tablet");
    expect(viewportFromWidth(1023)).toBe("tablet");
    expect(viewportFromWidth(1024)).toBe("desktop");
    expect(viewportFromWidth(1280)).toBe("desktop");
  });
});
