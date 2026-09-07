"use client";

import * as React from "react";
import { Radio } from "lucide-react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ThemeToggle } from "@/components/theme-toggle";

interface RangeSetupScreenProps {
  onSubmit: (minFreqGHz: number, maxFreqGHz: number) => Promise<void>;
}

export function RangeSetupScreen({ onSubmit }: RangeSetupScreenProps) {
  const [minFreq, setMinFreq] = React.useState("2");
  const [maxFreq, setMaxFreq] = React.useState("18");
  const [error, setError] = React.useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = React.useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);

    const min = Number.parseFloat(minFreq);
    const max = Number.parseFloat(maxFreq);

    if (!Number.isFinite(min) || !Number.isFinite(max) || min >= max) {
      setError("Geçersiz frekans aralığı!");
      return;
    }

    setIsSubmitting(true);
    try {
      await onSubmit(min, max);
      toast.success("Frekans aralığı ayarlandı");
    } catch {
      setError("Oturum başlatılamadı. Web API'nin çalıştığından emin olun.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="relative flex min-h-svh flex-1 items-center justify-center overflow-hidden bg-background p-6">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_20%_20%,color-mix(in_oklch,var(--primary)_18%,transparent),transparent_45%),radial-gradient(circle_at_80%_80%,color-mix(in_oklch,var(--primary)_12%,transparent),transparent_45%)]"
      />

      <div className="absolute right-6 top-6">
        <ThemeToggle />
      </div>

      <Card className="relative z-10 w-full max-w-md">
        <CardHeader className="items-center text-center">
          <div className="mb-2 flex size-12 items-center justify-center rounded-full bg-primary/10 text-primary">
            <Radio className="size-6" />
          </div>
          <CardTitle className="text-xl">Frekans Aralığı Ayarları</CardTitle>
          <CardDescription>
            Akıllı Frekans Tahsis Sistemi&apos;ni başlatmak için spektrum
            aralığını girin.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="min-freq">Min Frekans (GHz)</Label>
                <Input
                  id="min-freq"
                  inputMode="decimal"
                  value={minFreq}
                  onChange={(e) => setMinFreq(e.target.value)}
                  required
                />
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="max-freq">Max Frekans (GHz)</Label>
                <Input
                  id="max-freq"
                  inputMode="decimal"
                  value={maxFreq}
                  onChange={(e) => setMaxFreq(e.target.value)}
                  required
                />
              </div>
            </div>

            {error && (
              <p role="alert" className="text-sm font-medium text-destructive">
                {error}
              </p>
            )}

            <Button type="submit" size="lg" disabled={isSubmitting}>
              {isSubmitting ? "Başlatılıyor…" : "Başlat"}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
