namespace ManufacturingScheduler.Core.Models
{
    public class MaintenanceWindow
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
