"use client";

import { useSyncExternalStore } from "react";

const emptySubscribe = () => () => {};

// Sunucuda/hidrasyon sırasında false, hidrasyondan hemen sonra true döner.
// `useEffect` içinde setState çağırmak yerine React'ın harici depo (external
// store) mekanizmasını kullanır — hidrasyon uyuşmazlığı yaratmadan "istemcide
// miyiz" bilgisini verir.
export function useIsClient(): boolean {
  return useSyncExternalStore(
    emptySubscribe,
    () => true,
    () => false
  );
}
