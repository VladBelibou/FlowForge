namespace ManufacturingScheduler.Core.Models.Requests
{
    public class CompleteItemRequest
    {
        public int? ActualQuantity { get; set; }
        public DateTime? CompletionTime { get; set; }
        public string? Notes { get; set; }
    }
}
