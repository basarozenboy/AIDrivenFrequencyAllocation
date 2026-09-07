"use client";

import * as React from "react";
import { CheckCircle2, Dices, Layers, XCircle } from "lucide-react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Progress } from "@/components/ui/progress";
import { Separator } from "@/components/ui/separator";
import { ApiError } from "@/lib/api";
import type { BatchResultDto } from "@/lib/types";

interface BatchAllocationDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSubmit: (bandwidthsMHz: number[]) => Promise<BatchResultDto>;
}

// Hem yeni satırla hem virgülle ayrılmış bant genişliği girişini kabul eden
// esnek ayrıştırıcı (eski WinForms'daki metin/davranış tutarsızlığını giderir).
function parseBandwidths(text: string): number[] {
  return text
    .split(/[\n,]/)
    .map((token) => token.trim())
    .filter(Boolean)
    .map(Number.parseFloat)
    .filter((n) => Number.isFinite(n) && n > 0);
}

export function BatchAllocationDialog({
  open,
  onOpenChange,
  onSubmit,
}: BatchAllocationDialogProps) {
  const [text, setText] = React.useState("");
  const [count, setCount] = React.useState("6");
  const [minMHz, setMinMHz] = React.useState("10");
  const [maxMHz, setMaxMHz] = React.useState("50");
  const [result, setResult] = React.useState<BatchResultDto | null>(null);
  const [isSubmitting, setIsSubmitting] = React.useState(false);

  // Dialog her açıldığında formu sıfırlar. Bir `useEffect` yerine, render
  // sırasında önceki `open` değeriyle karşılaştırıp gerekirse state'i
  // güncelleyen React'ın önerdiği desen kullanılıyor (bkz. "Adjusting state
  // when a prop changes" — react.dev/learn/you-might-not-need-an-effect).
  const [prevOpen, setPrevOpen] = React.useState(open);
  if (open !== prevOpen) {
    setPrevOpen(open);
    if (open) {
      setText("");
      setResult(null);
    }
  }

  function handleRandomFill() {
    const n = Number.parseInt(count, 10);
    const min = Number.parseFloat(minMHz);
    const max = Number.parseFloat(maxMHz);

    if (!Number.isFinite(n) || n <= 0 || !Number.isFinite(min) || !Number.isFinite(max) || min >= max) {
      toast.error("Geçersiz hızlı doldurma değerleri");
      return;
    }

    const values = Array.from({ length: n }, () =>
      (min + (max - min) * Math.random()).toFixed(0)
    );
    setText(values.join("\n"));
  }

  async function handleSubmit() {
    const bandwidths = parseBandwidths(text);
    if (bandwidths.length === 0) {
      toast.error("Geçerli band genişliği bulunamadı!");
      return;
    }

    setIsSubmitting(true);
    try {
      const batchResult = await onSubmit(bandwidths);
      setResult(batchResult);
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.message : "Toplu tahsis başarısız oldu"
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-xl">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Layers className="size-5 text-primary" />
            Toplu Frekans Tahsisi
          </DialogTitle>
          <DialogDescription>
            Tahsis edilecek frekansların band genişliklerini girin (MHz).
            Satır satır ya da virgülle ayırabilirsiniz.
          </DialogDescription>
        </DialogHeader>

        {result ? (
          <BatchResultSummary result={result} />
        ) : (
          <div className="flex flex-col gap-4">
            <textarea
              value={text}
              onChange={(e) => setText(e.target.value)}
              placeholder={"20\n30\n40\n25, 35, 50"}
              rows={7}
              className="w-full resize-none rounded-lg border border-input bg-background px-3 py-2 font-mono text-sm shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            />

            <div className="flex flex-col gap-2 rounded-lg border border-dashed border-border p-3">
              <span className="flex items-center gap-1.5 text-sm font-medium text-muted-foreground">
                <Dices className="size-4" />
                Hızlı Doldurma
              </span>
              <div className="flex flex-wrap items-end gap-3">
                <div className="flex flex-col gap-1">
                  <Label htmlFor="rf-count" className="text-xs">Adet</Label>
                  <Input
                    id="rf-count"
                    className="h-9 w-16"
                    value={count}
                    onChange={(e) => setCount(e.target.value)}
                  />
                </div>
                <div className="flex flex-col gap-1">
                  <Label htmlFor="rf-min" className="text-xs">Min MHz</Label>
                  <Input
                    id="rf-min"
                    className="h-9 w-20"
                    value={minMHz}
                    onChange={(e) => setMinMHz(e.target.value)}
                  />
                </div>
                <div className="flex flex-col gap-1">
                  <Label htmlFor="rf-max" className="text-xs">Max MHz</Label>
                  <Input
                    id="rf-max"
                    className="h-9 w-20"
                    value={maxMHz}
                    onChange={(e) => setMaxMHz(e.target.value)}
                  />
                </div>
                <Button
                  type="button"
                  variant="secondary"
                  size="sm"
                  onClick={handleRandomFill}
                >
                  Rastgele Doldur
                </Button>
              </div>
            </div>
          </div>
        )}

        <DialogFooter>
          {result ? (
            <Button onClick={() => onOpenChange(false)}>Kapat</Button>
          ) : (
            <>
              <Button variant="outline" onClick={() => onOpenChange(false)}>
                İptal
              </Button>
              <Button onClick={handleSubmit} disabled={isSubmitting}>
                {isSubmitting ? "Tahsis ediliyor…" : "Toplu Tahsis Yap"}
              </Button>
            </>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function BatchResultSummary({ result }: { result: BatchResultDto }) {
  const score = Math.round(result.optimizationScore);
  const scoreColor =
    score >= 70 ? "bg-emerald-500" : score >= 40 ? "bg-amber-500" : "bg-destructive";

  return (
    <div className="flex flex-col gap-4">
      <div className="grid grid-cols-2 gap-3">
        <div className="flex items-center gap-2 rounded-lg bg-emerald-500/10 px-3 py-2 text-emerald-600 dark:text-emerald-400">
          <CheckCircle2 className="size-4 shrink-0" />
          <span className="text-sm font-medium">
            {result.successfulAllocations.length} başarılı
          </span>
        </div>
        <div className="flex items-center gap-2 rounded-lg bg-destructive/10 px-3 py-2 text-destructive">
          <XCircle className="size-4 shrink-0" />
          <span className="text-sm font-medium">
            {result.failedBandwidthsMHz.length} başarısız
          </span>
        </div>
      </div>

      <div className="flex flex-col gap-1.5">
        <div className="flex items-center justify-between text-sm">
          <span className="text-muted-foreground">Optimizasyon Skoru</span>
          <span className="font-semibold tabular-nums">{score}/100</span>
        </div>
        <Progress value={score} indicatorClassName={scoreColor} />
      </div>

      <p className="text-sm text-muted-foreground">
        Ortalama Boşluk:{" "}
        <span className="font-medium text-foreground">
          {(result.totalFragmentationGHz * 1000).toFixed(1)} MHz
        </span>
      </p>

      {result.successfulAllocations.length > 0 && (
        <>
          <Separator />
          <div className="flex max-h-40 flex-col gap-1 overflow-y-auto">
            {result.successfulAllocations.map((a) => (
              <div
                key={a.allocationId}
                className="flex items-center justify-between font-mono text-xs text-muted-foreground"
              >
                <span>{a.centerFrequencyGHz.toFixed(3)} GHz</span>
                <span>{a.bandwidthMHz.toFixed(0)} MHz</span>
              </div>
            ))}
          </div>
        </>
      )}

      {result.failedBandwidthsMHz.length > 0 && (
        <p className="text-xs text-muted-foreground">
          Tahsis edilemeyen bandlar:{" "}
          {result.failedBandwidthsMHz.map((b) => `${b.toFixed(0)} MHz`).join(", ")}
        </p>
      )}
    </div>
  );
}
