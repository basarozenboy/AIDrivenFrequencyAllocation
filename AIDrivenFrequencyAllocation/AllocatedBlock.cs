namespace AIDrivenFrequencyAllocation
{
    public class AllocatedBlock
    {
        public double CenterFrequency { get; set; }
        public double Bandwidth { get; set; }
        public double StartFreq => CenterFrequency - Bandwidth / 2;
        public double EndFreq => CenterFrequency + Bandwidth / 2;
        public string AllocationId { get; set; }
        public DateTime AllocationTime { get; set; }
    }
}
