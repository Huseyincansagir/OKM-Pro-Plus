import {
  AlertTriangle,
  CheckCircle2,
  CircleDashed,
  Clock3,
  Info,
  ShieldAlert,
} from "lucide-react";
import { Badge, type BadgeTone } from "@/components/ui/badge";
import { cn } from "@/lib/cn";

export type StatusKind =
  | "pending"
  | "active"
  | "success"
  | "critical"
  | "info"
  | "inactive";

const statusConfig: Record<
  StatusKind,
  { label: string; tone: BadgeTone; icon: typeof Clock3 }
> = {
  pending: { label: "Onay bekliyor", tone: "amber", icon: Clock3 },
  active: { label: "Hazırlanıyor", tone: "teal", icon: CircleDashed },
  success: { label: "Tamamlandı", tone: "success", icon: CheckCircle2 },
  critical: { label: "Kritik", tone: "danger", icon: ShieldAlert },
  info: { label: "İzleniyor", tone: "navy", icon: Info },
  inactive: { label: "Pasif", tone: "neutral", icon: AlertTriangle },
};

export function StatusBadge({
  status,
  label,
  className,
}: {
  status: StatusKind;
  label?: string;
  className?: string;
}) {
  const config = statusConfig[status];
  const Icon = config.icon;

  return (
    <Badge tone={config.tone} className={cn("min-h-6 px-2.5", className)}>
      <Icon className="h-3 w-3" aria-hidden="true" />
      <span>{label ?? config.label}</span>
    </Badge>
  );
}
