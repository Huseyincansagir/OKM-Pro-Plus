import type { ReactNode } from "react";

export function PageHeader({
  title,
  description,
  actions,
}: {
  title: string;
  description?: string;
  actions?: ReactNode;
}) {
  return (
    <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-start">
      <div className="min-w-0 flex-1">
        <h1 className="text-[28px] leading-tight font-bold tracking-tight text-navy-950 max-md:text-[22px]">
          {title}
        </h1>
        {description ? (
          <p className="mt-2 max-w-3xl text-sm text-slate-600">{description}</p>
        ) : null}
      </div>
      {actions ? <div className="flex shrink-0 flex-wrap gap-2">{actions}</div> : null}
    </div>
  );
}
