using ManufacturingScheduler.Core.Interfaces;
using ManufacturingScheduler.Core.Models;

namespace ManufacturingScheduler.Application.Services;

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
        var schedule = await ResolveSchedule(scheduleId);
        var item = FindScheduleItem(schedule, itemId);

        item.Status = ScheduleItemStatus.Completed;
        item.ActualEndTime = completionTime ?? DateTime.Now;
        item.ActualQuantity = actualQuantity ?? item.PlannedQuantity;
        item.Notes = notes;

        LogScheduleItemChnage("Fertig", item);

        var explanationPrompt = await RecalculateWithExplanation(schedule, itemId, "abgeschlossen");
        schedule.Explanation = await _chatGptService.GenerateExplanationAsync(explanationPrompt);

        await _scheduleRepository.SaveScheduleAsync(schedule);
        return schedule;
    }

    
        
    public async Task<ProductionSchedule> UpdateScheduleItemStatusAsync(
        int? scheduleId,
        int itemId,
        ScheduleItemStatus status,
        DateTime? actualStartTime = null,
        DateTime? actualEndTime = null,
        int? actualQuantity = null,
        string? notes = null)
    {
        var schedule = await ResolveSchedule(scheduleId);
        var item = FindScheduleItem(schedule, itemId);

        item.Status = status;
        item.ActualStartTime = actualStartTime ?? (status == ScheduleItemStatus.InProgress ? DateTime.Now : item.ActualStartTime);
        item.ActualEndTime = actualEndTime ?? (status == ScheduleItemStatus.Completed ? DateTime.Now : item.ActualEndTime);
        item.ActualQuantity = actualQuantity ?? item.PlannedQuantity;
        if (!string.IsNullOrWhiteSpace(notes)) item.Notes = notes;

        if (status is ScheduleItemStatus.Completed or ScheduleItemStatus.Cancelled or ScheduleItemStatus.InProgress or ScheduleItemStatus.Delayed)
        {
            var explanationPrompt = await RecalculateWithExplanation(schedule, itemId, $"Status wurde zu {status} geändert");
            schedule.Explanation = await _chatGptService.GenerateExplanationAsync(explanationPrompt);
        }

        await _scheduleRepository.SaveScheduleAsync(schedule);
        return schedule;
    }

    
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
                .Where(i => i.Status is ScheduleItemStatus.Planned or ScheduleItemStatus.InProgress)
                .Select(i => i.Order?.ProductName ?? $"Order {i.OrderId}")
                .ToList()
        };
    }

    
    public async Task<ProductionSchedule> GetScheduleByIdAsync(int scheduleId)
    {
        // Vereinfachte Version - ordnungsgemäße ID-Suche möglich
        var currentSchedule = await _scheduleRepository.GetCurrentScheduleAsync();
        if (currentSchedule.Id == scheduleId) return currentSchedule;
            
        throw new ArgumentException($"Zeitplan mit den folgenden ID {scheduleId} wurde nicht gefunden");
    }

    
    public Task<ProductionSchedule> GetCurrentScheduleAsync() =>
        _scheduleRepository.GetCurrentScheduleAsync();

    
    public Task DeleteScheduleAsync(int scheduleId) =>
        _scheduleRepository.DeleteScheduleAsync(scheduleId);

    
    public async Task<ProductionSchedule> ProcessNaturalLanguageRequestAsync(string naturalLanguageRequest, ProductionSchedule currentSchedule)
    {
        var interpretationResult = await _chatGptService.InterpretSchedulingRequestAsync(naturalLanguageRequest, currentSchedule);
        var updatedSchedule = ApplySchedulingChanges(currentSchedule, interpretationResult);
        await _scheduleRepository.SaveScheduleAsync(updatedSchedule);
        return updatedSchedule;
    }

    
    public Task<string> GetScheduleInsightsAsync(ProductionSchedule schedule) =>
        _chatGptService.AnalyzeScheduleAsync(schedule);

    
    public async Task<ProductionSchedule> RescheduleToStartNowAsync(int scheduleId)
    {
        var schedule = await GetScheduleByIdAsync(scheduleId);
        var earliestStart = schedule.ScheduleItems.Min(i => i.StartTime);
        var newStart = DateTime.Now.AddMinutes(30);
        var shift = newStart - earliestStart;

        foreach (var item in schedule.ScheduleItems)
        {
            item.StartTime += shift;
            item.EndTime += shift;
        }

        schedule.Explanation = $"Zeitplan wurde umgeplant um am {newStart:MM/dd HH:mm} zu starten. Bereit für die Produktion!";
        schedule.CreatedBy += " (Neu geplant)";

        await _scheduleRepository.SaveScheduleAsync(schedule);
        return schedule;
    }


    private static ScheduleItem FindScheduleItem(ProductionSchedule schedule, int itemId) =>
        schedule.ScheduleItems.FirstOrDefault(i => i.Id == itemId)
        ?? throw new ArgumentException($"Zeitplan-Element {itemId} nicht gefunden");

    
    private async Task<ProductionSchedule> ResolveSchedule(int? scheduleId) =>
        scheduleId.HasValue
            ? await GetScheduleByIdAsync(scheduleId.Value)
            : await GetCurrentScheduleAsync();

    
    private async Task<string> RecalculateWithExplanation(ProductionSchedule schedule, int itemId, string actionLabel)
    {
        var originalEnd = schedule.EstimatedEndDate;
        schedule.RecalculateSchedule();
        var newEnd = schedule.EstimatedEndDate;
        var diff = originalEnd - newEnd;

        return $"A schedule item (ID: {itemId}) was {actionLabel}. " +
               $"Original end date was {originalEnd:MM/dd HH:mm}, " +
               $"new end date is {newEnd:MM/dd HH:mm}. " +
               $"Time difference: {diff.TotalHours:F1} hours. " +
               $"Write a brief German explanation of this change and its impact.";
    }


    private void LogScheduleItemChange(string action, ScheduleItem item)
    {
        Console.WriteLine($"DEBUG: {action} Artikel {item.Id} — Endzeit: {item.ActualEndTime}, Menge: {item.ActualQuantity}, Status: {item.Status}");
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
                if (item == null) continue;

                if (change.NewStartTime.HasValue) item.StartTime = change.NewStartTime.Value;
                if (change.NewEndTime.HasValue) item.EndTime = change.NewEndTime.Value;
                if (change.NewMachineId.HasValue) item.MachineId = change.NewMachineId.Value;

                if (change.NewStatus.HasValue)
                {
                    item.Status = change.NewStatus.Value;
                    if (!item.ActualStartTime.HasValue && item.Status == ScheduleItemStatus.InProgress)
                        item.ActualStartTime = DateTime.Now;
                    if (!item.ActualEndTime.HasValue && item.Status == ScheduleItemStatus.Completed)
                        item.ActualEndTime = DateTime.Now;
                }  
            }

            return updatedSchedule;
    }
}

