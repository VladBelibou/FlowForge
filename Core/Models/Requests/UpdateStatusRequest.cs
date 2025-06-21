using ManufacturingScheduler.Core.Models;

namespace ManufacturingScheduler.Core.Models.Requests
{
    public class UpdateStatusRequest
    {
        public int? ScheduleId { get; set; }
        public int? ItemId { get; set; }
        public ScheduleItemStatus Status { get; set; }
        public DateTime? ActualStartTime { get; set; }
        public DateTime? ActualEndTime { get; set; }
        public int? ActualQuantity { get; set; }
        public string? Notes { get; set; }
    }
}
