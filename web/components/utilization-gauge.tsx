import { Gauge } from "lucide-react";

import { Progress } from "@/components/ui/progress";
import { cn } from "@/lib/utils";

interface UtilizationGaugeProps {
  percent: number;
}

export function UtilizationGauge({ percent }: UtilizationGaugeProps) {
  const clamped = Math.min(100, Math.max(0, percent));
  const level =
    clamped > 80 ? "high" : clamped > 50 ? "medium" : ("low" as const);

  const colorClass = {
    high: "text-destructive",
    medium: "text-amber-500",
    low: "text-emerald-500",
  }[level];

  const indicatorClass = {
    high: "bg-destructive",
    medium: "bg-amber-500",
    low: "bg-emerald-500",
  }[level];

  return (
    <div className="flex flex-col gap-2">
      <div className="flex items-center justify-between text-sm font-medium">
        <span className="flex items-center gap-1.5 text-muted-foreground">
          <Gauge className="size-4" />
          Spektrum Kullanımı
        </span>
        <span className={cn("font-semibold tabular-nums", colorClass)}>
          {clamped.toFixed(1)}%
        </span>
      </div>
      <Progress value={clamped} indicatorClassName={indicatorClass} />
    </div>
  );
}
