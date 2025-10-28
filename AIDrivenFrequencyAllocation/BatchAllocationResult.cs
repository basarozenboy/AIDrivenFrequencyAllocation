using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIDrivenFrequencyAllocation
{
    public class BatchAllocationResult
    {
        public List<AllocatedBlock> SuccessfulAllocations { get; set; }
        public List<FrequencyAllocationRequest> FailedRequests { get; set; }
        public double TotalFragmentation { get; set; }
        public double OptimizationScore { get; set; }
    }
}
