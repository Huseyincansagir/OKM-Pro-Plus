import { CircleAlert } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/cn";

export function ErrorState({
  title,
  description,
  onRetry,
  className,
}: {
  title: string;
  description: string;
  onRetry?: () => void;
  className?: string;
}) {
  return (
    <div
      role="alert"
      className={cn(
        "flex flex-col items-start gap-3 rounded-xl border border-danger-100 bg-danger-100/40 px-4 py-6",
        className,
      )}
    >
      <CircleAlert className="h-5 w-5 text-danger-500" aria-hidden="true" />
      <div>
        <h2 className="text-sm font-semibold text-navy-950">{title}</h2>
        <p className="mt-1 text-sm text-slate-600">{description}</p>
      </div>
      {onRetry ? (
        <Button variant="secondary" size="sm" onClick={onRetry}>
          Tekrar dene
        </Button>
      ) : null}
    </div>
  );
}
