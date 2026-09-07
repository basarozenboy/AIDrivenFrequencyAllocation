// Kategorik blok paleti — açık ve koyu temada okunaklı, 10 renk, döngüsel.
export const SPECTRUM_PALETTE = [
  "#3b82f6", // blue-500
  "#f43f5e", // rose-500
  "#10b981", // emerald-500
  "#f59e0b", // amber-500
  "#8b5cf6", // violet-500
  "#f97316", // orange-500
  "#ec4899", // pink-500
  "#06b6d4", // cyan-500
  "#d946ef", // fuchsia-500
  "#eab308", // yellow-500
] as const;

export function colorForIndex(index: number): string {
  return SPECTRUM_PALETTE[index % SPECTRUM_PALETTE.length];
}
