using ManufacturingScheduler.Core.Interfaces;
using ManufacturingScheduler.Core.Models;

namespace ManufacturingScheduler.Application.Services
{
    using ManufacturingScheduler.Core.Interfaces;
    using ManufacturingScheduler.Core.Models;
    public class SchedulingService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IMachineRepository _machineRepository;
        private readonly IScheduleRepository _scheduleRepository;
        private readonly ISchedulingAlgorithm _schedulingAlgorithm;
        private readonly IChatGptService _chatGptService;

        public SchedulingService(
            IOrderRepository orderRepository,
            IMachineRepository machineRepository,
            IScheduleRepository scheduleRepository,
            ISchedulingAlgorithm schedulingAlgorithm,
            IChatGptService chatGptService)
        {
            _orderRepository = orderRepository;
            _machineRepository = machineRepository;
            _scheduleRepository = scheduleRepository;
            _schedulingAlgorithm = schedulingAlgorithm;
            _chatGptService = chatGptService;
        }

        public async Task<ProductionSchedule> CreateOptimizedScheduleAsync(DateTime startDate, string schedulerName)
        {
            var orders = await _orderRepository.GetActiveOrdersAsync();
            var machines = await _machineRepository.GetOperationalMachinesAsync();
            var schedule = _schedulingAlgorithm.CreateSchedule(orders, machines, startDate);

            schedule.CreatedBy = schedulerName;
            schedule.CreatedDate = DateTime.Now;

            await _scheduleRepository.SaveScheduleAsync(schedule);
            return schedule;
        }

        public async Task<ProductionSchedule> CompleteScheduleItemAsync(
            int scheduleId,
            int itemId,
            int? actualQuantity,
            DateTime? completionTime,
            string? notes = null)
        {
            var schedule = await GetScheduleByIdAsync(scheduleId);
            var item = schedule.ScheduleItems.FirstOrDefault(i => i.Id == itemId);

            if (item == null)
                throw new ArgumentException($"Schedule item {itemId} not found");

            // Elementstatus aktualisieren
            item.Status = ScheduleItemStatus.Completed;
            item.ActualEndTime = completionTime ?? DateTime.Now;

            // Nullable-Werte korrekt behandeln
            if (actualQuantity.HasValue)
            {
                item.ActualQuantity = actualQuantity.Value;
            }
            else
            {
                item.ActualQuantity = item.PlannedQuantity;
            }

            item.Notes = notes;

                // Geplante Menge verwenden, falls tatsächliche nicht angegeben
                item.ActualQuantity = actualQuantity ?? item.PlannedQuantity;

            Console.WriteLine($"DEBUG SERVICE: Completing item {itemId} at {completionTime:MM/dd HH:mm}");
            Console.WriteLine($"DEBUG SERVICE: Original end time was {item.EndTime:MM/dd HH:mm}");

            // Elementstatus aktualisieren
            item.Status = ScheduleItemStatus.Completed;
            item.ActualEndTime = completionTime;
            item.ActualQuantity = actualQuantity ?? item.PlannedQuantity;
            item.Notes = notes;

            Console.WriteLine($"DEBUG SERVICE: Item status updated to {item.Status}");

            // Store original end date for comparison
            var originalEndDate = schedule.EstimatedEndDate;
            Console.WriteLine($"DEBUG SERVICE: Original estimated end date: {originalEndDate:MM/dd HH:mm}");

            // Recalculate the entire schedule
            schedule.RecalculateSchedule();

            // Get new end date after recalculation
            var newEndDate = schedule.EstimatedEndDate;
            Console.WriteLine($"DEBUG SERVICE: New estimated end date: {newEndDate:MM/dd HH:mm}");

            // Update explanation with the change
            var timeSaved = originalEndDate - newEndDate;
            if (timeSaved.TotalMinutes > 0)
            {
                schedule.Explanation = $"Item {itemId} completed early. Schedule updated - saved {timeSaved.TotalHours:F1} hours. New end date: {newEndDate:MM/dd HH:mm}";
            }
            else if (timeSaved.TotalMinutes < 0)
            {
                schedule.Explanation = $"Item {itemId} completed late. Schedule updated - added {Math.Abs(timeSaved.TotalHours):F1} hours. New end date: {newEndDate:MM/dd HH:mm}";
            }
            else
            {
                schedule.Explanation = $"Item {itemId} completed on time. New end date: {newEndDate:MM/dd HH:mm}";
            }

            Console.WriteLine($"DEBUG SERVICE: Updated explanation: {schedule.Explanation}");

            await _scheduleRepository.SaveScheduleAsync(schedule);
            return schedule;
        }

        // Update schedule item status
        public async Task<ProductionSchedule> UpdateScheduleItemStatusAsync(
            int scheduleId,
            int itemId,
            ScheduleItemStatus status,
            DateTime? actualStartTime = null,
            DateTime? actualEndTime = null,
            int? actualQuantity = null,
            string? notes = null)
        {
            var schedule = await GetScheduleByIdAsync(scheduleId);
            var item = schedule.ScheduleItems.FirstOrDefault(i => i.Id == itemId);

            if (item == null)
                throw new ArgumentException($"Schedule item {itemId} not found");

            // Update the item
            item.Status = status;
            if (actualStartTime.HasValue) item.ActualStartTime = actualStartTime;
            // AUTO-SETZEN auf aktuelle Zeit, falls nicht angegeben und Status abgeschlossen
            if (status == ScheduleItemStatus.Completed && !actualEndTime.HasValue)
            {
                item.ActualEndTime = DateTime.Now;
                Console.WriteLine($"DEBUG: Auto-set completion time to {DateTime.Now:MM/dd HH:mm:ss}");
            }
            else if (actualEndTime.HasValue)
            {
                item.ActualEndTime = actualEndTime;
            }
            if (actualQuantity.HasValue) item.ActualQuantity = actualQuantity.Value;
            if (!string.IsNullOrEmpty(notes)) item.Notes = notes;

            // Recalculate schedule if item was completed or cancelled
            if (status == ScheduleItemStatus.Completed || status == ScheduleItemStatus.Cancelled)
            {
                schedule.RecalculateSchedule();
                schedule.Explanation = $"Schedule updated due to status change. " +
                                     $"New end date: {schedule.EstimatedEndDate:MM/dd HH:mm}";
            }

            await _scheduleRepository.SaveScheduleAsync(schedule);
            return schedule;
        }

        // Get schedule summary
        public async Task<ScheduleSummary> GetScheduleSummaryAsync(int scheduleId)
        {
            var schedule = await GetScheduleByIdAsync(scheduleId);
            var originalEndDate = schedule.ScheduleItems.Any()
                ? schedule.ScheduleItems.Max(i => i.EndTime)
                : DateTime.Now;

            return new ScheduleSummary
            {
                ScheduleId = scheduleId,
                OriginalEndDate = originalEndDate,
                CurrentEndDate = schedule.EstimatedEndDate,
                TimeSaved = originalEndDate - schedule.EstimatedEndDate,
                CompletionPercentage = schedule.CompletionPercentage,
                CompletedItems = schedule.CompletedItems,
                TotalItems = schedule.ScheduleItems.Count,
                CompletedOrderNames = schedule.ScheduleItems
                    .Where(i => i.Status == ScheduleItemStatus.Completed)
                    .Select(i => i.Order?.ProductName ?? $"Order {i.OrderId}")
                    .ToList(),
                RemainingOrderNames = schedule.ScheduleItems
                    .Where(i => i.Status == ScheduleItemStatus.Planned || i.Status == ScheduleItemStatus.InProgress)
                    .Select(i => i.Order?.ProductName ?? $"Order {i.OrderId}")
                    .ToList()
            };
        }

        public async Task<ProductionSchedule> GetScheduleByIdAsync(int scheduleId)
        {
            // Vereinfachte Version - ordnungsgemäße ID-Suche möglich
            var currentSchedule = await _scheduleRepository.GetCurrentScheduleAsync();

            if (currentSchedule.Id == scheduleId)
            {
                return currentSchedule;
            }

            throw new ArgumentException($"Schedule with ID {scheduleId} not found");
        }

        public async Task<ProductionSchedule> ProcessNaturalLanguageRequestAsync(string naturalLanguageRequest, ProductionSchedule currentSchedule)
        {
            var interpretationResult = await _chatGptService.InterpretSchedulingRequestAsync(naturalLanguageRequest, currentSchedule);
            var updatedSchedule = ApplySchedulingChanges(currentSchedule, interpretationResult);
            await _scheduleRepository.SaveScheduleAsync(updatedSchedule);
            return updatedSchedule;
        }

        public async Task<string> GetScheduleInsightsAsync(ProductionSchedule schedule)
        {
            return await _chatGptService.AnalyzeScheduleAsync(schedule);
        }

        public async Task<ProductionSchedule> GetCurrentScheduleAsync()
        {
            return await _scheduleRepository.GetCurrentScheduleAsync();
        }

        public async Task DeleteScheduleAsync(int scheduleId)
        {
            await _scheduleRepository.DeleteScheduleAsync(scheduleId);
        }


        private ProductionSchedule ApplySchedulingChanges(ProductionSchedule schedule, SchedulingInterpretation interpretation)
        {
            var updatedSchedule = new ProductionSchedule
            {
                Id = schedule.Id,
                CreatedDate = DateTime.Now,
                CreatedBy = schedule.CreatedBy + " (Updated)",
                ScheduleItems = new List<ScheduleItem>(schedule.ScheduleItems),
                Explanation = interpretation.ExplanationText
            };

            foreach (var change in interpretation.SuggestedChanges)
            {
                var item = updatedSchedule.ScheduleItems.FirstOrDefault(i => i.OrderId == change.OrderId);
                if (item != null)
                {
                    Console.WriteLine($"DEBUG APPLY: Applying changes to Order {change.OrderId}");

                    if (change.NewStartTime.HasValue)
                    {
                        Console.WriteLine($"DEBUG APPLY: Changing start time from {item.StartTime:MM/dd HH:mm} to {change.NewStartTime.Value:MM/dd HH:mm}");
                        item.StartTime = change.NewStartTime.Value;
                    }

                    if (change.NewEndTime.HasValue)
                    {
                        Console.WriteLine($"DEBUG APPLY: Changing end time from {item.EndTime:MM/dd HH:mm} to {change.NewEndTime.Value:MM/dd HH:mm}");
                        item.EndTime = change.NewEndTime.Value;
                    }

                    if (change.NewMachineId.HasValue)
                    {
                        Console.WriteLine($"DEBUG APPLY: Changing machine from {item.MachineId} to {change.NewMachineId.Value}");
                        item.MachineId = change.NewMachineId.Value;
                    }

                    // Apply status changes
                    if (change.NewStatus.HasValue)
                    {
                        Console.WriteLine($"DEBUG APPLY: Changing status from {item.Status} to {change.NewStatus.Value}");
                        item.Status = change.NewStatus.Value;

                        // Auto-set timestamps based on status
                        if (change.NewStatus.Value == ScheduleItemStatus.InProgress && !item.ActualStartTime.HasValue)
                        {
                            item.ActualStartTime = DateTime.Now;
                            Console.WriteLine($"DEBUG APPLY: Auto-set actual start time to {DateTime.Now:MM/dd HH:mm}");
                        }

                        if (change.NewStatus.Value == ScheduleItemStatus.Completed && !item.ActualEndTime.HasValue)
                        {
                            item.ActualEndTime = DateTime.Now;
                            Console.WriteLine($"DEBUG APPLY: Auto-set actual end time to {DateTime.Now:MM/dd HH:mm}");
                        }
                    }
                }
            }

            return updatedSchedule;
        }

        public async Task<ProductionSchedule> RescheduleToStartNowAsync(int scheduleId)
        {
            var schedule = await GetScheduleByIdAsync(scheduleId);

            // Calculate how much time to shift everything
            var earliestCurrentStart = schedule.ScheduleItems.Min(item => item.StartTime);
            var newStartTime = DateTime.Now.AddMinutes(30);
            var timeShift = newStartTime - earliestCurrentStart;

            Console.WriteLine($"DEBUG RESCHEDULE: Shifting schedule by {timeShift.TotalHours:F1} hours to start now");

            // Shift all items by the same amount
            foreach (var item in schedule.ScheduleItems)
            {
                var oldStart = item.StartTime;
                var oldEnd = item.EndTime;

                item.StartTime = item.StartTime + timeShift;
                item.EndTime = item.EndTime + timeShift;

                Console.WriteLine($"DEBUG RESCHEDULE: Item {item.Id} moved from {oldStart:MM/dd HH:mm}-{oldEnd:MM/dd HH:mm} to {item.StartTime:MM/dd HH:mm}-{item.EndTime:MM/dd HH:mm}");
            }

            schedule.Explanation = $"Schedule rescheduled to start immediately at {newStartTime:MM/dd HH:mm}. Ready for production!";
            schedule.CreatedBy += " (Rescheduled)";

            await _scheduleRepository.SaveScheduleAsync(schedule);
            return schedule;
        }
    }
}
