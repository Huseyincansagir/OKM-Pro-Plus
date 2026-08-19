"use client";

import { Bell } from "lucide-react";
import { DropdownMenu } from "@/components/ui/dropdown-menu";
import { IconButton } from "@/components/ui/icon-button";

export function NotificationArea({ unreadCount = 0 }: { unreadCount?: number }) {
  return (
    <DropdownMenu
      label="Bildirimler"
      trigger={
        <IconButton label="Bildirimler" className="relative">
          <Bell className="h-4 w-4" />
          {unreadCount > 0 ? (
            <span className="absolute -top-1 -right-1 grid h-4 min-w-4 place-items-center rounded-full bg-danger-500 px-1 text-[10px] font-bold text-white">
              {unreadCount}
            </span>
          ) : null}
        </IconButton>
      }
      items={[
        {
          id: "empty",
          label:
            unreadCount > 0
              ? `${unreadCount} örnek bildirim — API bağlı değil`
              : "Bildirim API’si henüz bağlanmadı",
          onSelect: () => undefined,
        },
      ]}
    />
  );
}
