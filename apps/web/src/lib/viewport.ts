export type Viewport = "mobile" | "tablet" | "desktop";

export const MOBILE_MAX = 767;
export const TABLET_MAX = 1023;

export function viewportFromWidth(width: number): Viewport {
  if (width <= MOBILE_MAX) {
    return "mobile";
  }

  if (width <= TABLET_MAX) {
    return "tablet";
  }

  return "desktop";
}
