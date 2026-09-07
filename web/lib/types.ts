export interface RangeResponse {
  minFreqGHz: number;
  maxFreqGHz: number;
}

export interface AllocatedBlockDto {
  allocationId: string;
  centerFrequencyGHz: number;
  bandwidthMHz: number;
  startFreqGHz: number;
  endFreqGHz: number;
  allocationTime: string;
}

export interface BatchResultDto {
  successfulAllocations: AllocatedBlockDto[];
  failedBandwidthsMHz: number[];
  totalFragmentationGHz: number;
  optimizationScore: number;
}

export interface UtilizationResponse {
  utilizationPercent: number;
}

export interface ErrorResponse {
  message: string;
}
