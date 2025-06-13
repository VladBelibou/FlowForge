namespace ManufacturingScheduler.Core.Models
{
    public class MaterialRequirement
    {
        public int Id { get; set; }
        public int MaterialId { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public int QuantityRequired { get; set; }
    }
}
