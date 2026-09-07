"use client";

import { Loader2, Radio, RefreshCw, WifiOff } from "lucide-react";

import { Dashboard } from "@/components/dashboard";
import { RangeSetupScreen } from "@/components/range-setup-screen";
import { Button } from "@/components/ui/button";
import { useAllocationSession } from "@/lib/use-allocation-session";

export default function Home() {
  const session = useAllocationSession();

  if (session.status === "loading") {
    return (
      <div className="flex flex-1 flex-col items-center justify-center gap-3 text-muted-foreground">
        <div className="relative flex size-12 items-center justify-center rounded-full bg-primary/10 text-primary">
          <Radio className="size-6" />
          <Loader2 className="absolute -right-1 -top-1 size-5 animate-spin rounded-full bg-background p-0.5" />
        </div>
        <p className="text-sm">Yükleniyor…</p>
      </div>
    );
  }

  if (session.status === "error") {
    return (
      <div className="flex flex-1 flex-col items-center justify-center gap-3 p-6 text-center">
        <div className="flex size-12 items-center justify-center rounded-full bg-destructive/10 text-destructive">
          <WifiOff className="size-6" />
        </div>
        <p className="max-w-sm text-sm font-medium text-foreground">
          {session.error ?? "Web API'ye ulaşılamadı."}
        </p>
        <p className="max-w-sm text-xs text-muted-foreground">
          Backend&apos;in (AIDrivenFrequencyAllocation.Api) çalıştığından ve{" "}
          <code className="rounded bg-muted px-1 py-0.5">
            NEXT_PUBLIC_API_BASE_URL
          </code>{" "}
          değerinin doğru olduğundan emin olun.
        </p>
        <Button onClick={session.retry} size="sm" className="mt-1">
          <RefreshCw />
          Tekrar Dene
        </Button>
      </div>
    );
  }

  if (session.status === "needs-setup") {
    return <RangeSetupScreen onSubmit={session.initialize} />;
  }

  return <Dashboard session={session} />;
}
