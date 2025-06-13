namespace ManufacturingScheduler.Core.Models
{
    public class Machine
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsOperational { get; set; }
        public List<ProductCapability> ProductCapabilities { get; set; } = new();
        public List<MaintenanceWindow> ScheduledMaintenance { get; set; } = new();
    }
}
