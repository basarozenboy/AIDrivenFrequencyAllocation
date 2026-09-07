namespace AIDrivenFrequencyAllocation.Api;

public class AllocationSessionService
{
    private readonly object _lock = new();
    private IntelligentFrequencyAllocator? _allocator;

    public bool IsInitialized => _allocator is not null;

    public RangeResponse Initialize(double minFreqGHz, double maxFreqGHz)
    {
        lock (_lock)
        {
            _allocator = new IntelligentFrequencyAllocator(minFreqGHz, maxFreqGHz);
            return new RangeResponse(minFreqGHz, maxFreqGHz);
        }
    }

    public RangeResponse? GetRange()
    {
        lock (_lock)
        {
            return _allocator is null
                ? null
                : new RangeResponse(_allocator.MinFrequency, _allocator.MaxFrequency);
        }
    }

    public AllocatedBlockDto? Allocate(double bandwidthMHz)
    {
        lock (_lock)
        {
            EnsureInitialized();

            var request = new FrequencyAllocationRequest
            {
                Bandwidth = bandwidthMHz / 1000.0,
                RequestId = Guid.NewGuid().ToString("N")[..8],
                RequestTime = DateTime.Now,
                Priority = 1
            };

            var block = _allocator!.AllocateFrequency(request);
            return block is null ? null : ToDto(block);
        }
    }

    public BatchResultDto AllocateBatch(IReadOnlyList<double> bandwidthsMHz)
    {
        lock (_lock)
        {
            EnsureInitialized();

            var requests = bandwidthsMHz.Select(mhz => new FrequencyAllocationRequest
            {
                Bandwidth = mhz / 1000.0,
                RequestId = Guid.NewGuid().ToString("N")[..8],
                RequestTime = DateTime.Now,
                Priority = 1
            }).ToList();

            var result = _allocator!.AllocateBatch(requests);

            return new BatchResultDto(
                result.SuccessfulAllocations.Select(ToDto).ToList(),
                result.FailedRequests.Select(r => r.Bandwidth * 1000.0).ToList(),
                result.TotalFragmentation,
                result.OptimizationScore);
        }
    }

    public List<AllocatedBlockDto> GetAllocations()
    {
        lock (_lock)
        {
            EnsureInitialized();
            return _allocator!.GetAllocatedBlocks().Select(ToDto).ToList();
        }
    }

    public bool Deallocate(string id)
    {
        lock (_lock)
        {
            EnsureInitialized();
            var existed = _allocator!.GetAllocatedBlocks().Any(b => b.AllocationId == id);
            _allocator.DeallocateFrequency(id);
            return existed;
        }
    }

    public double GetUtilization()
    {
        lock (_lock)
        {
            EnsureInitialized();
            return _allocator!.GetSpectrumUtilization();
        }
    }

    private void EnsureInitialized()
    {
        if (_allocator is null)
            throw new InvalidOperationException("Session not initialized");
    }

    private static AllocatedBlockDto ToDto(AllocatedBlock b) => new(
        b.AllocationId,
        b.CenterFrequency,
        b.Bandwidth * 1000.0,
        b.StartFreq,
        b.EndFreq,
        b.AllocationTime);
}
