namespace ManufacturingScheduler.Core.Models
{
    public class ScheduleSummary
    {
        public int ScheduleId { get; set; }
        public DateTime OriginalEndDate { get; set; }
        public DateTime CurrentEndDate { get; set; }
        public TimeSpan TimeSaved { get; set; }
        public double CompletionPercentage { get; set; }
        public int CompletedItems { get; set; }
        public int TotalItems { get; set; }
        public List<string> CompletedOrderNames { get; set; } = new();
        public List<string> RemainingOrderNames { get; set; } = new();
    }
}
