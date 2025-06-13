namespace ManufacturingScheduler.Core.Models
{
    public class ScheduleChange
    {
        public int OrderId { get; set; }
        public DateTime? NewStartTime { get; set; }
        public DateTime? NewEndTime { get; set; }
        public int? NewMachineId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public ScheduleItemStatus? NewStatus { get; set; }
    }
}
