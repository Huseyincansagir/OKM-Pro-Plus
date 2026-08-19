"use client";

import { useEffect, useState } from "react";
import { Wifi, WifiOff } from "lucide-react";
import { cn } from "@/lib/cn";

export function ConnectionStatus() {
  const [online, setOnline] = useState(true);

  useEffect(() => {
    function sync() {
      setOnline(navigator.onLine);
    }

    sync();
    window.addEventListener("online", sync);
    window.addEventListener("offline", sync);
    return () => {
      window.removeEventListener("online", sync);
      window.removeEventListener("offline", sync);
    };
  }, []);

  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 rounded-full px-2 py-1 text-[11px] font-semibold",
        online ? "bg-success-100 text-success-500" : "bg-danger-100 text-danger-500",
      )}
    >
      {online ? (
        <Wifi className="h-3.5 w-3.5" aria-hidden="true" />
      ) : (
        <WifiOff className="h-3.5 w-3.5" aria-hidden="true" />
      )}
      {online ? "Bağlı" : "Çevrimdışı"}
    </span>
  );
}
