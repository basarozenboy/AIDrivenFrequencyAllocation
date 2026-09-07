namespace AIDrivenFrequencyAllocation.Api;

public record RangeRequest(double MinFreqGHz, double MaxFreqGHz);

public record RangeResponse(double MinFreqGHz, double MaxFreqGHz);

public record AllocateRequest(double BandwidthMHz);

public record BatchAllocateRequest(List<double> BandwidthsMHz);

public record AllocatedBlockDto(
    string AllocationId,
    double CenterFrequencyGHz,
    double BandwidthMHz,
    double StartFreqGHz,
    double EndFreqGHz,
    DateTime AllocationTime);

public record BatchResultDto(
    List<AllocatedBlockDto> SuccessfulAllocations,
    List<double> FailedBandwidthsMHz,
    double TotalFragmentationGHz,
    double OptimizationScore);

public record UtilizationResponse(double UtilizationPercent);

public record ErrorResponse(string Message);
