namespace ManufacturingScheduler.Core.Models.Requests
{
    public class GetStatusRequest
    {
        public int? ScheduleId { get; set; }
        public bool IncludeSummary { get; set; } = false;
    }
}