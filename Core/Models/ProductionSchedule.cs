namespace ManufacturingScheduler.Core.Models
{
    public class ProductionSchedule
    {
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public List<ScheduleItem> ScheduleItems { get; set; } = new();
        public string? Explanation { get; set; }

        // Dynamisch berechnete Eigenschaften
        public DateTime EstimatedStartDate => ScheduleItems.Any()
            ? ScheduleItems.Min(item => item.StartTime)
            : DateTime.Now;

        public DateTime EstimatedEndDate => CalculateEndDate();

        public TimeSpan EstimatedDuration => EstimatedEndDate - EstimatedStartDate;

        public double CompletionPercentage => ScheduleItems.Any()
            ? ScheduleItems.Count(item => item.Status == ScheduleItemStatus.Completed) / (double)ScheduleItems.Count * 100
            : 0;

        public int CompletedItems => ScheduleItems.Count(item => item.Status == ScheduleItemStatus.Completed);
        public int PendingItems => ScheduleItems.Count(item => item.Status == ScheduleItemStatus.Planned);
        public int InProgressItems => ScheduleItems.Count(item => item.Status == ScheduleItemStatus.InProgress);

        private DateTime CalculateEndDate()
        {
            Console.WriteLine("DEBUG CALCULATE: Starting end date calculation");

            if (!ScheduleItems.Any())
            {
                Console.WriteLine("DEBUG CALCULATE: No schedule items, returning now");
                return DateTime.Now;
            }

            // Nur nicht abgeschlossene Elemente für Enddatumsberechnung berücksichtigen
            var activeItems = ScheduleItems.Where(item =>
                item.Status != ScheduleItemStatus.Completed &&
                item.Status != ScheduleItemStatus.Cancelled).ToList();

            Console.WriteLine($"DEBUG CALCULATE: Found {activeItems.Count} active items out of {ScheduleItems.Count} total");

            if (!activeItems.Any())
            {
                // Alle Elemente abgeschlossen - Enddatum ist die späteste Fertigstellungszeit
                var completedItems = ScheduleItems.Where(item => item.Status == ScheduleItemStatus.Completed);
                if (completedItems.Any())
                {
                    var latestCompletion = completedItems.Max(item => item.ActualEndTime ?? item.EndTime);
                    Console.WriteLine($"DEBUG CALCULATE: All completed, latest completion: {latestCompletion:MM/dd HH:mm}");
                    return latestCompletion;
                }

                Console.WriteLine("DEBUG CALCULATE: No completed items found, returning now");
                return DateTime.Now;
            }

            // Return the latest end time of active items
            var endDate = activeItems.Max(item => item.EndTime);
            Console.WriteLine($"DEBUG CALCULATE: Calculated end date from active items: {endDate:MM/dd HH:mm}");
            return endDate;
        }

        public void RecalculateSchedule()
        {
            Console.WriteLine("DEBUG RECALCULATE: Starting recalculation");

            var completedItems = ScheduleItems.Where(item => item.Status == ScheduleItemStatus.Completed).ToList();
            var pendingItems = ScheduleItems.Where(item => item.Status == ScheduleItemStatus.Planned).OrderBy(item => item.StartTime).ToList();

            Console.WriteLine($"DEBUG RECALCULATE: Found {completedItems.Count} completed items, {pendingItems.Count} pending items");

            if (!pendingItems.Any())
            {
                Console.WriteLine("DEBUG RECALCULATE: No pending items to reschedule");
                return;
            }

            // Find the earliest time we can start new work
            DateTime earliestAvailableTime;

            if (completedItems.Any())
            {
                // Use the LATEST actual completion time
                earliestAvailableTime = completedItems.Max(item => item.ActualEndTime ?? item.EndTime);
                Console.WriteLine($"DEBUG RECALCULATE: Latest completion time: {earliestAvailableTime:MM/dd HH:mm}");
            }
            else
            {
                earliestAvailableTime = DateTime.Now;
                Console.WriteLine($"DEBUG RECALCULATE: No completed items, using current time: {earliestAvailableTime:MM/dd HH:mm}");
            }

            // Einfacher Ansatz: ALLE ausstehenden Elemente sequenziell ab der frühesten verfügbaren Zeit neu planen
            var currentTime = earliestAvailableTime;

            foreach (var item in pendingItems)
            {
                var originalStart = item.StartTime;
                var originalEnd = item.EndTime;
                var duration = originalEnd - originalStart;

                // Nur neu planen, wenn wir früher als ursprünglich geplant starten können
                if (currentTime < originalStart)
                {
                    item.StartTime = currentTime;
                    item.EndTime = currentTime + duration;

                    Console.WriteLine($"DEBUG RECALCULATE: Item {item.Id} moved EARLIER from {originalStart:MM/dd HH:mm}-{originalEnd:MM/dd HH:mm} to {item.StartTime:MM/dd HH:mm}-{item.EndTime:MM/dd HH:mm}");

                    // Move to next available slot
                    currentTime = item.EndTime.AddMinutes(30);
                }
                else
                {
                    // Keep original schedule if we can't improve it
                    Console.WriteLine($"DEBUG RECALCULATE: Item {item.Id} kept original schedule {originalStart:MM/dd HH:mm}-{originalEnd:MM/dd HH:mm}");
                    currentTime = originalEnd.AddMinutes(30);
                }
            }

            Console.WriteLine($"DEBUG RECALCULATE: New estimated end date: {EstimatedEndDate:MM/dd HH:mm}");
        }
    }
}
