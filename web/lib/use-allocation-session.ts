"use client";

import { useCallback, useEffect, useState } from "react";
import * as api from "./api";
import { ApiError } from "./api";
import type { AllocatedBlockDto, BatchResultDto, RangeResponse } from "./types";

type Status = "loading" | "needs-setup" | "ready" | "error";

const POLL_INTERVAL_MS = 2000;

function messageFor(err: unknown): string {
  return err instanceof ApiError ? err.message : "Beklenmeyen bir hata oluştu";
}

export function useAllocationSession() {
  const [status, setStatus] = useState<Status>("loading");
  const [error, setError] = useState<string | null>(null);
  const [range, setRange] = useState<RangeResponse | null>(null);
  const [allocations, setAllocations] = useState<AllocatedBlockDto[]>([]);
  const [utilizationPercent, setUtilizationPercent] = useState(0);
  const [isBusy, setIsBusy] = useState(false);
  const [retryToken, setRetryToken] = useState(0);

  const refresh = useCallback(async () => {
    const [allocs, util] = await Promise.all([
      api.getAllocations(),
      api.getUtilization(),
    ]);
    setAllocations(allocs);
    setUtilizationPercent(util.utilizationPercent);
  }, []);

  useEffect(() => {
    let cancelled = false;

    (async () => {
      try {
        const existing = await api.getSession();
        if (cancelled) return;

        if (existing) {
          setRange(existing);
          setStatus("ready");
          await refresh();
        } else {
          setStatus("needs-setup");
        }
      } catch (err) {
        if (cancelled) return;
        setError(messageFor(err));
        setStatus("error");
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [refresh, retryToken]);

  const retry = useCallback(() => {
    setStatus("loading");
    setError(null);
    setRetryToken((t) => t + 1);
  }, []);

  useEffect(() => {
    if (status !== "ready") return;

    const id = setInterval(() => {
      refresh().catch(() => {
        // arka plan senkronizasyonu — geçici ağ hatalarını sessizce yok say
      });
    }, POLL_INTERVAL_MS);

    return () => clearInterval(id);
  }, [status, refresh]);

  const initialize = useCallback(
    async (minFreqGHz: number, maxFreqGHz: number) => {
      const initialized = await api.initSession(minFreqGHz, maxFreqGHz);
      setRange(initialized);
      setAllocations([]);
      setUtilizationPercent(0);
      setStatus("ready");
    },
    []
  );

  const allocate = useCallback(
    async (bandwidthMHz: number): Promise<AllocatedBlockDto> => {
      setIsBusy(true);
      try {
        const block = await api.allocate(bandwidthMHz);
        await refresh();
        return block;
      } finally {
        setIsBusy(false);
      }
    },
    [refresh]
  );

  const allocateBatch = useCallback(
    async (bandwidthsMHz: number[]): Promise<BatchResultDto> => {
      setIsBusy(true);
      try {
        const result = await api.allocateBatch(bandwidthsMHz);
        await refresh();
        return result;
      } finally {
        setIsBusy(false);
      }
    },
    [refresh]
  );

  const deallocate = useCallback(
    async (id: string) => {
      await api.deallocate(id);
      await refresh();
    },
    [refresh]
  );

  return {
    status,
    error,
    retry,
    range,
    allocations,
    utilizationPercent,
    isBusy,
    initialize,
    allocate,
    allocateBatch,
    deallocate,
    refresh,
  };
}

export type AllocationSession = ReturnType<typeof useAllocationSession>;
