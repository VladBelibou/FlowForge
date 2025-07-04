namespace ManufacturingScheduler.Core.Models.Requests
{
    public class ModelStateError
    {
        public string Field { get; set; } = "";
        public List<string> Errors { get; set; } = new();
    }
}