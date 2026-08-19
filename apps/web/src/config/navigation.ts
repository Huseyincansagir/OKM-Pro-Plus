import type { LucideIcon } from "lucide-react";
import {
  Bell,
  Boxes,
  ClipboardList,
  Factory,
  LayoutDashboard,
  Package,
  Settings,
  ShoppingCart,
  Truck,
  Users,
  Wallet,
} from "lucide-react";

export type NavSection = {
  label: string;
  items: NavItem[];
};

export type NavItem = {
  href: string;
  label: string;
  icon: LucideIcon;
  badge?: number;
  implemented?: boolean;
  activePrefix?: string;
};

export const NAVIGATION: NavSection[] = [
  {
    label: "Çalışma alanı",
    items: [
      { href: "/dashboard", label: "Dashboard", icon: LayoutDashboard, implemented: true },
      {
        href: "/satis/teklif-talepleri",
        label: "Satış",
        icon: ShoppingCart,
        implemented: true,
        activePrefix: "/satis",
      },
      { href: "/#urunler", label: "Ürünler", icon: Package },
      { href: "/#depo", label: "Depo", icon: Boxes },
      { href: "/#uretim", label: "Üretim", icon: Factory },
      { href: "/#sevkiyat", label: "Sevkiyat", icon: Truck },
      { href: "/#cari", label: "Cari ve Muhasebe", icon: Wallet },
      { href: "/#personel", label: "Personel", icon: Users },
    ],
  },
  {
    label: "Analiz",
    items: [
      { href: "/#raporlar", label: "Raporlar", icon: ClipboardList },
      { href: "/#bildirimler", label: "Bildirimler", icon: Bell },
    ],
  },
  {
    label: "Sistem",
    items: [{ href: "/#yonetim", label: "Yönetim", icon: Settings }],
  },
];
