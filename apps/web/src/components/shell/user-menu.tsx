"use client";

import { useRouter } from "next/navigation";
import { DropdownMenu } from "@/components/ui/dropdown-menu";
import { logout } from "@/lib/api/auth-client";
import { useSessionStore } from "@/lib/auth/session-store";

function initialsFrom(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) {
    return "KU";
  }
  if (parts.length === 1) {
    return parts[0].slice(0, 2).toUpperCase();
  }
  return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
}

export function UserMenu() {
  const router = useRouter();
  const user = useSessionStore((state) => state.user);
  const name = user?.displayName || user?.userName || "Kullanıcı";

  return (
    <DropdownMenu
      label="Kullanıcı menüsü"
      trigger={
        <button
          type="button"
          className="flex h-9 items-center gap-2 rounded-full"
          aria-label={`${name} menüsü`}
        >
          <span className="grid h-9 w-9 place-items-center rounded-full bg-teal-500/15 text-xs font-bold text-teal-700">
            {initialsFrom(name)}
          </span>
          <span className="hidden text-xs font-semibold text-navy-800 lg:inline">
            {name}
          </span>
        </button>
      }
      items={[
        {
          id: "logout",
          label: "Çıkış yap",
          danger: true,
          onSelect: () => {
            void logout().then(() => router.replace("/giris"));
          },
        },
      ]}
    />
  );
}
