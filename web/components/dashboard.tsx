"use client";

import * as React from "react";
import { Radio } from "lucide-react";

import { AllocationControlPanel } from "@/components/allocation-control-panel";
import { AllocationList } from "@/components/allocation-list";
import { BatchAllocationDialog } from "@/components/batch-allocation-dialog";
import { SpectrumVisualizer } from "@/components/spectrum-visualizer";
import { ThemeToggle } from "@/components/theme-toggle";
import { Badge } from "@/components/ui/badge";
import { colorForIndex } from "@/lib/colors";
import { sortAllocations } from "@/lib/allocations";
import type { AllocationSession } from "@/lib/use-allocation-session";

interface DashboardProps {
  session: AllocationSession;
}

export function Dashboard({ session }: DashboardProps) {
  const { range, allocations, utilizationPercent, allocate, allocateBatch, deallocate } =
    session;

  const [selectedId, setSelectedId] = React.useState<string | null>(null);
  const [batchDialogOpen, setBatchDialogOpen] = React.useState(false);
  const [lastScore, setLastScore] = React.useState<number | null>(null);

  const sorted = React.useMemo(() => sortAllocations(allocations), [allocations]);
  const colorMap = React.useMemo(() => {
    const map = new Map<string, string>();
    sorted.forEach((block, index) => map.set(block.allocationId, colorForIndex(index)));
    return map;
  }, [sorted]);

  // Kaldırılmış bir tahsis seçili kalmasın diye, seçimi her render'da mevcut
  // tahsis listesine göre türetiyoruz (ayrı bir efekt yerine).
  const validSelectedId =
    selectedId !== null && allocations.some((a) => a.allocationId === selectedId)
      ? selectedId
      : null;

  if (!range) return null;

  async function handleBatchSubmit(bandwidthsMHz: number[]) {
    const result = await allocateBatch(bandwidthsMHz);
    setLastScore(result.optimizationScore);
    return result;
  }

  async function handleRemoveSelected() {
    if (!validSelectedId) return;
    await deallocate(validSelectedId);
    setSelectedId(null);
  }

  return (
    <div className="mx-auto flex w-full max-w-6xl flex-1 flex-col gap-6 p-4 sm:p-6">
      <header className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-2.5">
          <div className="flex size-9 items-center justify-center rounded-lg bg-primary/10 text-primary">
            <Radio className="size-5" />
          </div>
          <h1 className="text-lg font-semibold tracking-tight">
            Akıllı Frekans Tahsis Sistemi
          </h1>
        </div>
        <div className="flex items-center gap-3">
          <Badge variant="outline">
            {range.minFreqGHz.toFixed(1)} – {range.maxFreqGHz.toFixed(1)} GHz
          </Badge>
          <ThemeToggle />
        </div>
      </header>

      <SpectrumVisualizer
        range={range}
        allocations={sorted}
        colorMap={colorMap}
        selectedId={validSelectedId}
        onSelect={(id) => setSelectedId((current) => (current === id ? null : id))}
      />

      <div className="flex flex-1 flex-col gap-6 lg:flex-row">
        <AllocationList
          allocations={sorted}
          colorMap={colorMap}
          selectedId={validSelectedId}
          onSelect={setSelectedId}
        />
        <AllocationControlPanel
          onAllocate={allocate}
          onOpenBatchDialog={() => setBatchDialogOpen(true)}
          onRemoveSelected={handleRemoveSelected}
          hasSelection={validSelectedId !== null}
          utilizationPercent={utilizationPercent}
          isBusy={session.isBusy}
        />
      </div>

      <footer className="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-border bg-card px-4 py-3 text-sm text-muted-foreground">
        <span>
          Aralık: {range.minFreqGHz.toFixed(1)} – {range.maxFreqGHz.toFixed(1)} GHz
        </span>
        <span>Toplam Tahsis: {allocations.length}</span>
        <span>
          Son Optimizasyon Skoru:{" "}
          {lastScore === null ? "—" : `${lastScore.toFixed(1)}/100`}
        </span>
      </footer>

      <BatchAllocationDialog
        open={batchDialogOpen}
        onOpenChange={setBatchDialogOpen}
        onSubmit={handleBatchSubmit}
      />
    </div>
  );
}
