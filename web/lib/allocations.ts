import type { AllocatedBlockDto } from "./types";

// Kayıt zamanına göre kararlı sıralama — hem spektrum görselleştirmesi hem de
// liste aynı sırayı kullanır, böylece bir bloğun rengi her iki yerde de eşleşir.
export function sortAllocations(
  allocations: AllocatedBlockDto[]
): AllocatedBlockDto[] {
  return [...allocations].sort(
    (a, b) =>
      new Date(a.allocationTime).getTime() -
      new Date(b.allocationTime).getTime()
  );
}
