"use client";

import { List } from "lucide-react";

import { Card, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import type { AllocatedBlockDto } from "@/lib/types";

interface AllocationListProps {
  allocations: AllocatedBlockDto[];
  colorMap: Map<string, string>;
  selectedId: string | null;
  onSelect: (id: string | null) => void;
}

export function AllocationList({
  allocations,
  colorMap,
  selectedId,
  onSelect,
}: AllocationListProps) {
  return (
    <Card className="flex flex-1 flex-col">
      <CardHeader className="flex-row items-center justify-between gap-2 space-y-0">
        <CardTitle className="flex items-center gap-2">
          <List className="size-4 text-primary" />
          Tahsisler
        </CardTitle>
        <Badge variant="secondary">{allocations.length}</Badge>
      </CardHeader>
      <div className="flex-1 overflow-y-auto px-3 pb-3">
        {allocations.length === 0 ? (
          <p className="px-3 py-8 text-center text-sm text-muted-foreground">
            Henüz tahsis yok
          </p>
        ) : (
          <ul className="flex flex-col gap-1">
            {allocations.map((block) => {
              const isSelected = block.allocationId === selectedId;
              const color = colorMap.get(block.allocationId) ?? "currentColor";

              return (
                <li key={block.allocationId}>
                  <button
                    type="button"
                    onClick={() =>
                      onSelect(isSelected ? null : block.allocationId)
                    }
                    className={cn(
                      "flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-left text-sm transition-colors",
                      isSelected
                        ? "bg-primary/10 ring-1 ring-primary/40"
                        : "hover:bg-accent"
                    )}
                  >
                    <span
                      aria-hidden
                      className="size-2.5 shrink-0 rounded-full"
                      style={{ backgroundColor: color }}
                    />
                    <span className="font-mono text-xs text-muted-foreground">
                      {block.allocationId}
                    </span>
                    <span className="font-medium">
                      {block.centerFrequencyGHz.toFixed(3)} GHz
                    </span>
                    <Badge variant="outline" className="ml-auto shrink-0">
                      {block.bandwidthMHz.toFixed(0)} MHz
                    </Badge>
                    <span className="hidden shrink-0 font-mono text-xs text-muted-foreground sm:inline">
                      [{block.startFreqGHz.toFixed(3)} – {block.endFreqGHz.toFixed(3)}]
                    </span>
                  </button>
                </li>
              );
            })}
          </ul>
        )}
      </div>
    </Card>
  );
}
