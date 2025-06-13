namespace ManufacturingScheduler.Core.Models
{
    public class ProductCapability
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int SetupTimeMinutes { get; set; }
        public int ProductionRatePerHour { get; set; }
    }
}
