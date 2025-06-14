namespace ManufacturingScheduler.Core.Models.Requests
{
    public class ImmediateScheduleRequest
    {
        public string SchedulerName { get; set; } = string.Empty;
        public int? DelayMinutes { get; set; } = 10; // Optionale Verzögerung vor dem Start (Standard 10 Minuten)
    }
}
