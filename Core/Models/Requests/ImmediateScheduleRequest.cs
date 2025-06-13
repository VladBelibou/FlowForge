namespace ManufacturingScheduler.Core.Models.Requests
{
    public class ImmediateScheduleRequest
    {
        public string SchedulerName { get; set; } = string.Empty;
        public int? DelayMinutes { get; set; } = 10; // Optional delay before starting (default 10 minutes)
    }
}
