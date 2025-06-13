namespace ManufacturingScheduler.Core.Models
{
    public class SchedulingInterpretation
    {
        public List<ScheduleChange> SuggestedChanges { get; set; } = new();
        public string ExplanationText { get; set; } = string.Empty;
        public bool IsValid { get; set; }
    }
}
