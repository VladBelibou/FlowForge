using ManufacturingScheduler.Application.Services;
using ManufacturingScheduler.Core.Models;
using ManufacturingScheduler.Core.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace ManufacturingScheduler.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SchedulingController : ControllerBase
    {
        private readonly SchedulingService _schedulingService;

        public SchedulingController(SchedulingService schedulingService) => _schedulingService = schedulingService;


        [HttpPost("create")]
        public async Task<ActionResult<ProductionSchedule>> CreateSchedule([FromBody] CreateScheduleRequest request)
        {
            var startDate = request.StartDate ?? DateTime.Now.AddMinutes(request.DelayMinutes ?? 60);

            var schedule = await _schedulingService.CreateOptimizedScheduleAsync(startDate, request.SchedulerName);
            return Ok(schedule);
        }


        [HttpPost("status")]
        public async Task<ActionResult<object>> GetStatus([FromBody] GetStatusRequest request)
        {
            var schedule = request.ScheduleId.HasValue
                ? await _schedulingService.GetScheduleByIdAsync(request.ScheduleId.Value)
                : await _schedulingService.GetCurrentScheduleAsync();

            if (request.IncludeSummary)
            {
                var summary = await _schedulingService.GetScheduleSummaryAsync(schedule.Id);
                return Ok(summary);
            }

            return Ok(schedule);
        }


        [HttpPut("status")]
        public async Task<ActionResult<ProductionSchedule>> UpdateStatus([FromBody] UpdateStatusRequest request)
        {
            if (!request.ItemId.HasValue)
                return BadRequest("ItemId wird benötigt");

            var schedule = await _schedulingService.UpdateScheduleItemStatusAsync(
                    request.ScheduleId,
                    request.ItemId.Value,
                    request.Status,
                    request.ActualStartTime,
                    request.ActualEndTime ?? (request.Status == ScheduleItemStatus.Completed ? DateTime.Now : null),
                    request.ActualQuantity,
                    request.Notes);

            return Ok(schedule);
        }


        [HttpDelete("{scheduleId}")]
        public async Task<ActionResult> DeleteSchedule(int scheduleId)
        {
            await _schedulingService.DeleteScheduleAsync(scheduleId);
            return NoContent();
        }


        [HttpPost("optimize")]
        public async Task<ActionResult<ProductionSchedule>> OptimizeSchedule([FromBody] OptimizeRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.NaturalLanguageRequest))
            {
                var currentSchedule = await _schedulingService.GetCurrentScheduleAsync();
                var updatedSchedule = await _schedulingService.ProcessNaturalLanguageRequestAsync(request.NaturalLanguageRequest, currentSchedule);
                return Ok(updatedSchedule);
            }

            return BadRequest("Ungültige Eingabe.");
        }


        [HttpGet("insights")]
        public async Task<ActionResult<string>> GetInsights(int? scheduleId = null)
        {
            var schedule = scheduleId.HasValue
                ? await _schedulingService.GetScheduleByIdAsync(scheduleId.Value)
                : await _schedulingService.GetCurrentScheduleAsync();

            var insights = await _schedulingService.GetScheduleInsightsAsync(schedule);
            return Ok(insights);
        }


        [HttpPost("insights")]
        public async Task<ActionResult<string>> GetCustomInsights([FromBody] ProductionSchedule customSchedule)
        {
            var insights = await _schedulingService.GetScheduleInsightsAsync(customSchedule);
            return Ok(insights);
        }


        [HttpPost("batch-delete")]
        public async Task<ActionResult<BatchDeleteResponse>> BatchDeleteSchedules([FromBody] BatchDeleteScheduleRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(kvp => kvp.Value?.Errors.Count > 0)
                    .Select(kvp => new ModelStateError 
                    {
                        Field = kvp.Key,
                        Errors = kvp.Value!.Errors.Select(e => e.ErrorMessage).ToList()
                    })
                    .ToList();

                Console.WriteLine("ModelState Fehlern:");
                foreach (var error in errors)
                {
                    Console.WriteLine($"Feld: {error.Field}");
                    foreach (var msg in error.Errors)
                        Console.WriteLine($"  Fehler: {msg}");
                }
                return BadRequest(ModelState);
            }

            if ((request.ScheduleIds?.Any() != true) && !request.LastCount.HasValue)
            {
                Console.WriteLine("Validierung: Weder ScheduleIds noch LastCount wurden gegeben.");
                return BadRequest("Entweder ScheduleIds oder LastCount muss eingegeben werden");
            }

            var response = await _schedulingService.BatchDeleteSchedulesAsync(request);

            if (response.Errors.Any())
            {
                Console.WriteLine("BatchDelete Anwortfehler:");
                foreach (var err in response.Errors)
                    Console.WriteLine(err);
                return BadRequest(response);
            }

            return Ok(response);
        }


        [HttpPost("batch-delete/preview")]
        public async Task<ActionResult<object>> PreviewBatchDelete([FromBody] BatchDeleteScheduleRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if ((request.ScheduleIds?.Any() != true) && !request.LastCount.HasValue)
            {
                return BadRequest("Entweder ScheduleIds oder LastCount wurden eingegeben");
            }

            var schedulesToDelete = await _schedulingService.GetSchedulePreviewForDeletionAsync(request);

            return Ok(new
            {
                SchedulesToDelete = schedulesToDelete.Select(s => new
                {
                    s.Id,
                    s.CreatedDate,
                    s.CreatedBy,
                    ItemCount = s.ScheduleItems.Count,
                    Status = s.CompletionPercentage > 0 ? "In Progress" : "Planned"
                }).ToList(),
                TotalCount = schedulesToDelete.Count,
                Message = $"Vorschau: Folgende Menge an Zeitplänen werden gelöscht: {schedulesToDelete.Count}"
            });
        }


        [HttpGet("all")]
        public async Task<ActionResult<List<ProductionSchedule>>> GetAllSchedules()
        {
            var schedules = await _schedulingService.GetAllSchedulesAsync();

            return Ok(new
            {
                AllSchedules = schedules.Select(s => new
                {
                    s.Id,
                    s.CreatedDate,
                    s.CreatedBy,
                    ItemCount = s.ScheduleItems.Count,
                    Status = s.CompletionPercentage > 0 ? "In Progress" : "Planned"
                }).ToList(),
                TotalCount = schedules.Count,
            });
        }
    }
}
