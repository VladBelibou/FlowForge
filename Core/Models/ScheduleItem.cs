namespace ManufacturingScheduler.Core.Models
{
    public class ScheduleItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int MachineId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int PlannedQuantity { get; set; }
        public ProductionOrder? Order { get; set; }
        public Machine? Machine { get; set; }

        // Statusverfolgung
        public ScheduleItemStatus Status { get; set; } = ScheduleItemStatus.Planned;
        public DateTime? ActualStartTime { get; set; }
        public DateTime? ActualEndTime { get; set; }
        public int ActualQuantity { get; set; }
        public string? Notes { get; set; }

        // Berechnete Eigenschaften
        public TimeSpan PlannedDuration => EndTime - StartTime;
        public TimeSpan? ActualDuration => ActualEndTime.HasValue && ActualStartTime.HasValue
            ? ActualEndTime.Value - ActualStartTime.Value
            : null;

        public bool IsAheadOfSchedule => ActualEndTime.HasValue && ActualEndTime.Value < EndTime;
        public bool IsBehindSchedule => ActualEndTime.HasValue && ActualEndTime.Value > EndTime;
    }
}
