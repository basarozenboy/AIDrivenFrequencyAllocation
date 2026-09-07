"use client";

import * as React from "react";
import { Layers, Trash2, Zap } from "lucide-react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { UtilizationGauge } from "@/components/utilization-gauge";
import { ApiError } from "@/lib/api";
import type { AllocatedBlockDto } from "@/lib/types";

interface AllocationControlPanelProps {
  onAllocate: (bandwidthMHz: number) => Promise<AllocatedBlockDto>;
  onOpenBatchDialog: () => void;
  onRemoveSelected: () => Promise<void>;
  hasSelection: boolean;
  utilizationPercent: number;
  isBusy: boolean;
}

export function AllocationControlPanel({
  onAllocate,
  onOpenBatchDialog,
  onRemoveSelected,
  hasSelection,
  utilizationPercent,
  isBusy,
}: AllocationControlPanelProps) {
  const [bandwidth, setBandwidth] = React.useState("200");
  const [isRemoving, setIsRemoving] = React.useState(false);

  async function handleAllocate(e: React.FormEvent) {
    e.preventDefault();
    const mhz = Number.parseFloat(bandwidth);
    if (!Number.isFinite(mhz) || mhz <= 0) {
      toast.error("Geçersiz band genişliği!");
      return;
    }

    try {
      const block = await onAllocate(mhz);
      toast.success(
        `Frekans tahsis edildi: ${block.centerFrequencyGHz.toFixed(3)} GHz`
      );
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.message : "Uygun frekans bulunamadı!"
      );
    }
  }

  async function handleRemove() {
    setIsRemoving(true);
    try {
      await onRemoveSelected();
      toast.success("Tahsis kaldırıldı");
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "Kaldırma başarısız oldu");
    } finally {
      setIsRemoving(false);
    }
  }

  return (
    <Card className="flex w-full flex-col lg:w-[380px]">
      <CardHeader>
        <CardTitle>Tahsis Kontrolleri</CardTitle>
      </CardHeader>
      <CardContent className="flex flex-1 flex-col gap-5">
        <form onSubmit={handleAllocate} className="flex flex-col gap-3">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="bandwidth">Band Genişliği (MHz)</Label>
            <Input
              id="bandwidth"
              inputMode="decimal"
              value={bandwidth}
              onChange={(e) => setBandwidth(e.target.value)}
            />
          </div>
          <Button type="submit" disabled={isBusy} className="w-full">
            <Zap />
            Frekans Tahsis Et
          </Button>
        </form>

        <Button
          variant="secondary"
          disabled={isBusy}
          onClick={onOpenBatchDialog}
          className="w-full"
        >
          <Layers />
          Toplu Tahsis Et
        </Button>

        <Button
          variant="outline"
          disabled={!hasSelection || isRemoving}
          onClick={handleRemove}
          className="w-full border-destructive/40 text-destructive hover:bg-destructive/10 hover:text-destructive"
        >
          <Trash2 />
          Seçili Tahsisi Kaldır
        </Button>

        <Separator />

        <UtilizationGauge percent={utilizationPercent} />
      </CardContent>
    </Card>
  );
}
