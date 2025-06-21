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
            int? scheduleId,
            int itemId,
            int? actualQuantity,
            DateTime? completionTime,
            string? notes = null)
        {
            var schedule = scheduleId.HasValue
                ? await GetScheduleByIdAsync(scheduleId.Value)
                : await GetCurrentScheduleAsync();
            
            var item = schedule.ScheduleItems.FirstOrDefault(i => i.Id == itemId);

            if (item == null)
                throw new ArgumentException($"Zeitplan-Element {itemId} nicht gefunden");

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

            Console.WriteLine($"DEBUG SERVICE: Schließe Element {itemId} um {completionTime:MM/dd HH:mm} ab");
            Console.WriteLine($"DEBUG SERVICE: Ursprüngliche Endzeit war {item.EndTime:MM/dd HH:mm}");

            // Elementstatus aktualisieren
            item.Status = ScheduleItemStatus.Completed;
            item.ActualEndTime = completionTime;
            item.ActualQuantity = actualQuantity ?? item.PlannedQuantity;
            item.Notes = notes;

            Console.WriteLine($"DEBUG SERVICE: Element-Status aktualisiert auf {item.Status}");

            // Store original end date for comparison
            var originalEndDate = schedule.EstimatedEndDate;
            Console.WriteLine($"DEBUG SERVICE: Ursprünglich geschätztes Enddatum: {originalEndDate:MM/dd HH:mm}");

            // Recalculate the entire schedule
            schedule.RecalculateSchedule();

            // Get new end date after recalculation
            var newEndDate = schedule.EstimatedEndDate;
            Console.WriteLine($"DEBUG SERVICE: Neues geschätztes Enddatum: {newEndDate:MM/dd HH:mm}");

            var timeDifference = originalEndDate - newEndDate;
            
            var explanationPrompt = $"A schedule item (ID: {itemId}) was completed. " +
                         $"Original end date was {originalEndDate:MM/dd HH:mm}, " +
                         $"new end date is {newEndDate:MM/dd HH:mm}. " +
                         $"Time difference: {timeDifference.TotalHours:F1} hours. " +
                         $"Write a brief German explanation of this completion and its impact.";

            schedule.Explanation = await _chatGptService.GenerateExplanationAsync(explanationPrompt);

            Console.WriteLine($"DEBUG SERVICE: Neue Erklärung: {schedule.Explanation}");

            await _scheduleRepository.SaveScheduleAsync(schedule);
            return schedule;
        }

        // Update schedule item status
        public async Task<ProductionSchedule> UpdateScheduleItemStatusAsync(
            int? scheduleId,
            int itemId,
            ScheduleItemStatus status,
            DateTime? actualStartTime = null,
            DateTime? actualEndTime = null,
            int? actualQuantity = null,
            string? notes = null)
        {
            var schedule = scheduleId.HasValue
                ? await GetScheduleByIdAsync(scheduleId.Value)
                : await GetCurrentScheduleAsync();
            
            var item = schedule.ScheduleItems.FirstOrDefault(i => i.Id == itemId);
            var originalEndDate = schedule.EstimatedEndDate;

            if (item == null)
                throw new ArgumentException($"Zeitplan-Element {itemId} nicht gefunden");

            // Update the item
            item.Status = status;
            if (actualStartTime.HasValue) item.ActualStartTime = actualStartTime;

             if (status == ScheduleItemStatus.InProgress && !actualStartTime.HasValue)
             {
                item.ActualStartTime = DateTime.Now;
                Console.WriteLine($"DEBUG: Automatisches Setzen der Startzeit auf {DateTime.Now:MM/dd HH:mm:ss}");
            }
            // AUTO-SETZEN auf aktuelle Zeit, falls nicht angegeben und Status abgeschlossen
            if (status == ScheduleItemStatus.Completed && !actualEndTime.HasValue)
            {
                item.ActualEndTime = DateTime.Now;
                Console.WriteLine($"DEBUG: Automatisches Setzen der Abschlusszeit auf {DateTime.Now:MM/dd HH:mm:ss}");
            }
            else if (actualEndTime.HasValue)
            {
                item.ActualEndTime = actualEndTime;
            }
            if (actualQuantity.HasValue)
                item.ActualQuantity = actualQuantity.Value;
            else
                item.ActualQuantity = item.PlannedQuantity;
            
            if (!string.IsNullOrEmpty(notes)) item.Notes = notes;

            // Recalculate schedule if item was completed or cancelled
            if (status == ScheduleItemStatus.Completed || status == ScheduleItemStatus.Cancelled || status == ScheduleItemStatus.InProgress || status == ScheduleItemStatus.Delayed)
            {
                schedule.RecalculateSchedule();

                var newEndDate = schedule.EstimatedEndDate;
                var timeDifference = originalEndDate - newEndDate;
                                
                // KI-getsützte Erklärung
                var explanationPrompt = $"A schedule item (ID: {itemId}) status was changed to {status}. " +
                                         $"Original end date was {originalEndDate:MM/dd HH:mm}, " +
                                         $"new end date is {newEndDate:MM/dd HH:mm}. " +
                                         $"Time difference: {timeDifference.TotalHours:F1} hours. " +
                                         $"Write a brief German explanation of this change and its impact.";

                schedule.Explanation = await _chatGptService.GenerateExplanationAsync(explanationPrompt);
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
                    Console.WriteLine($"DEBUG APPLY: Wende Änderungen auf Bestellung {change.OrderId} an");

                    if (change.NewStartTime.HasValue)
                    {
                        Console.WriteLine($"DEBUG APPLY: Ändere Startzeit von {item.StartTime:MM/dd HH:mm} auf {change.NewStartTime.Value:MM/dd HH:mm}");
                        item.StartTime = change.NewStartTime.Value;
                    }

                    if (change.NewEndTime.HasValue)
                    {
                        Console.WriteLine($"DEBUG APPLY: Ändere Endzeit von {item.EndTime:MM/dd HH:mm} auf {change.NewEndTime.Value:MM/dd HH:mm}");
                        item.EndTime = change.NewEndTime.Value;
                    }

                    if (change.NewMachineId.HasValue)
                    {
                        Console.WriteLine($"DEBUG APPLY: Ändere Maschine von {item.MachineId} auf {change.NewMachineId.Value}");
                        item.MachineId = change.NewMachineId.Value;
                    }

                    // Apply status changes
                    if (change.NewStatus.HasValue)
                    {
                        Console.WriteLine($"DEBUG APPLY: Ändere Status von {item.Status} auf {change.NewStatus.Value}");
                        item.Status = change.NewStatus.Value;

                        // Auto-set timestamps based on status
                        if (change.NewStatus.Value == ScheduleItemStatus.InProgress && !item.ActualStartTime.HasValue)
                        {
                            item.ActualStartTime = DateTime.Now;
                            Console.WriteLine($"DEBUG APPLY: Automatisches Setzen der tatsächlichen Startzeit auf {DateTime.Now:MM/dd HH:mm}");
                        }

                        if (change.NewStatus.Value == ScheduleItemStatus.Completed && !item.ActualEndTime.HasValue)
                        {
                            item.ActualEndTime = DateTime.Now;
                            Console.WriteLine($"DEBUG APPLY: Automatisches Setzen der tatsächlichen Endzeit auf {DateTime.Now:MM/dd HH:mm}");
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

            Console.WriteLine($"DEBUG RESCHEDULE: Verschiebe Zeitplan um {timeShift.TotalHours:F1} Stunden für sofortigen Start");

            // Shift all items by the same amount
            foreach (var item in schedule.ScheduleItems)
            {
                var oldStart = item.StartTime;
                var oldEnd = item.EndTime;

                item.StartTime = item.StartTime + timeShift;
                item.EndTime = item.EndTime + timeShift;

                Console.WriteLine($"DEBUG RESCHEDULE: Element {item.Id} verschoben von {oldStart:MM/dd HH:mm}-{oldEnd:MM/dd HH:mm} auf {item.StartTime:MM/dd HH:mm}-{item.EndTime:MM/dd HH:mm}");
            }

            schedule.Explanation = $"Schedule rescheduled to start immediately at {newStartTime:MM/dd HH:mm}. Ready for production!";
            schedule.CreatedBy += " (Neu geplant)";

            await _scheduleRepository.SaveScheduleAsync(schedule);
            return schedule;
        }
    }
}
