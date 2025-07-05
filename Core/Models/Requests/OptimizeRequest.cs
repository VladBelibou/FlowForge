namespace ManufacturingScheduler.Core.Models.Requests
{
    public class OptimizeRequest
    {
        public string? NaturalLanguageRequest { get; set; }
        public int? ScheduleId { get; set; }
    }
}