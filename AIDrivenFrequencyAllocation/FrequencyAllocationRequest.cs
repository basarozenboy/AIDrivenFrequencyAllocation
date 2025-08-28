namespace AIDrivenFrequencyAllocation
{
    public class FrequencyAllocationRequest
    {
        public double Bandwidth { get; set; } // MHz
        public string RequestId { get; set; }
        public DateTime RequestTime { get; set; }
        public int Priority { get; set; } = 1;
    }
}
