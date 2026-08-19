"use client";

import { DropdownMenu } from "@/components/ui/dropdown-menu";

export function UserMenu({
  name = "Önizleme Kullanıcısı",
  initials = "ÖK",
}: {
  name?: string;
  initials?: string;
}) {
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
            {initials}
          </span>
          <span className="hidden text-xs font-semibold text-navy-800 lg:inline">
            {name}
          </span>
        </button>
      }
      items={[
        {
          id: "profile",
          label: "Profil sonraki slice’ta",
          onSelect: () => undefined,
        },
      ]}
    />
  );
}
