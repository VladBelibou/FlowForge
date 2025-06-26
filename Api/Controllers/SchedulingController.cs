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

        public SchedulingController(SchedulingService schedulingService) =>
            _schedulingService = schedulingService;

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

                var schedule = request.Status == ScheduleItemStatus.Completed
                    ? await _schedulingService.CompleteScheduleItemAsync(
                        request.ScheduleId,
                        request.ItemId.Value,
                        request.ActualQuantity,
                        /* Add ActualStartTime and let it become nullable 
                        just like the method below, to avoid TimeSaved calculations
                        from coming up wrong.                               
                        */
                        request.ActualEndTime ?? DateTime.Now,
                        request.Notes)
                    : await _schedulingService.UpdateScheduleItemStatusAsync(
                        request.ScheduleId,
                        request.ItemId.Value,
                        request.Status,
                        request.ActualQuantity,
                        request.ActualStartTime,
                        request.ActualEndTime,
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
            
            if (request.ScheduleId.HasValue && request.StartNow)
            {
                var updatedSchedule = await _schedulingService.RescheduleToStartNowAsync(request.ScheduleId.Value);
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

    }
}
