let inFlight: Promise<boolean> | null = null;

export async function refreshSession(): Promise<boolean> {
  if (!inFlight) {
    inFlight = (async () => {
      const response = await fetch("/api/auth/refresh", {
        method: "POST",
        credentials: "same-origin",
      });
      return response.ok;
    })().finally(() => {
      inFlight = null;
    });
  }

  return inFlight;
}

export function resetRefreshFlight() {
  inFlight = null;
}
