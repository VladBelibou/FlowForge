using ManufacturingScheduler.Core.Interfaces;
using ManufacturingScheduler.Core.Models;
using ManufacturingScheduler.Core.Models.Requests;


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


    public async Task DeleteScheduleAsync(int scheduleId)
    {
        await _scheduleRepository.DeleteScheduleAsync(scheduleId);
    }


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
        var changedItem = FindScheduleItem(schedule, itemId);
        var originalItemEnd = changedItem.EndTime;
        var actualItemEnd = changedItem.ActualEndTime;

        var originalScheduleEnd = schedule.EstimatedEndDate;
        schedule.RecalculateSchedule();
        var newScheduleEnd = schedule.EstimatedEndDate;

        var scheduleTimeDiff = originalScheduleEnd - newScheduleEnd;
        var itemTimeSaved = actualItemEnd.HasValue ? originalItemEnd - actualItemEnd.Value : TimeSpan.Zero;

        return $"A schedule item (ID: {itemId}) was {actionLabel}. " +
               $"Item's original end date was {originalItemEnd:MM/dd HH:mm}, " +
               $"actual completion was {actualItemEnd:MM/dd HH:mm}. " +
               $"Time saved on this tiem: {itemTimeSaved.TotalHours:F1} hours. " +
               $"Overall schedule impact: {scheduleTimeDiff.TotalHours:F1} hours. " +
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

    public async Task<BatchDeleteResponse> BatchDeleteSchedulesAsync(BatchDeleteScheduleRequest request)
    {
        var response = new BatchDeleteResponse();

        if (!request.ConfirmDeletion)
        {
            response.Errors.Add("ConfirmDeletion must be true to proceed with batch deletion");
            return response;
        }

        try
        {
            List<int> idsToDelete = new();

            // Handle specific IDs
            if (request.ScheduleIds?.Any() == true)
            {
                idsToDelete.AddRange(request.ScheduleIds);
            }

            // Handle "last N" deletion
            if (request.LastCount.HasValue)
            {
                var lastNSchedules = await _scheduleRepository.GetLastNSchedulesAsync(
                    request.LastCount.Value,
                    request.CreatedByFilter,
                    request.CreatedBeforeDate);

                idsToDelete.AddRange(lastNSchedules.Select(s => s.Id));
            }

            // Remove duplicates
            idsToDelete = idsToDelete.Distinct().ToList();

            if (!idsToDelete.Any())
            {
                response.Message = "No schedules found matching the deletion criteria";
                return response;
            }

            response.DeletedCount = await _scheduleRepository.BatchDeleteSchedulesAsync(idsToDelete);
            response.DeletedScheduleIds = idsToDelete;
            response.Message = $"Successfully deleted {response.DeletedCount} schedule(s)";
        }
        catch (Exception ex)
        {
            response.Errors.Add($"Error during batch deletion: {ex.Message}");
        }

        return response;
    }

    public async Task<List<ProductionSchedule>> GetAllSchedulesAsync()
    {
        return await _scheduleRepository.GetAllSchedulesAsync();
    }

    public async Task<List<ProductionSchedule>> GetSchedulePreviewForDeletionAsync(BatchDeleteScheduleRequest request)
    {
        var schedulesToDelete = new List<ProductionSchedule>();

        if (request.ScheduleIds?.Any() == true)
        {
            var allSchedules = await _scheduleRepository.GetAllSchedulesAsync();
            schedulesToDelete.AddRange(allSchedules.Where(s => request.ScheduleIds.Contains(s.Id)));
        }

        if (request.LastCount.HasValue)
        {
            var lastNSchedules = await _scheduleRepository.GetLastNSchedulesAsync(
                request.LastCount.Value,
                request.CreatedByFilter,
                request.CreatedBeforeDate);
            schedulesToDelete.AddRange(lastNSchedules);
        }

        return schedulesToDelete.DistinctBy(s => s.Id).ToList();
    }
}

