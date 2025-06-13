namespace ManufacturingScheduler.Core.Models.Requests
{
    public class CreateScheduleRequest
    {
        public DateTime? StartDate { get; set; }
        public int? DelayMinutes { get; set; }
        public string SchedulerName { get; set; } = string.Empty;
    }
}
