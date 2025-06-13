using ManufacturingScheduler.Core.Models;

public enum OrderStatus
{
    Planned,
    InProgress,
    Completed,
    Cancelled
}

public class ProductionOrder
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime DueDate { get; set; }
    public int CustomerPriority { get; set; }
    public OrderStatus Status { get; set; }
    public List<MaterialRequirement> RequiredMaterials { get; set; } = new();
}
