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

        public SchedulingController(SchedulingService schedulingService)
        {
            _schedulingService = schedulingService;
        }

        [HttpPost("create")]
        public async Task<ActionResult<ProductionSchedule>> CreateSchedule([FromBody] CreateScheduleRequest request)
        {
            DateTime startDate;
            if (request.StartDate.HasValue)
            {
                startDate = request.StartDate.Value;
            }
            else if (request.DelayMinutes.HasValue)
            {
                startDate = DateTime.Now.AddMinutes(request.DelayMinutes.Value);
            }
            else
            {
                startDate = DateTime.Now.AddHours(1);
            }

            var schedule = await _schedulingService.CreateOptimizedScheduleAsync(startDate, request.SchedulerName);
            return Ok(schedule);
        }

        [HttpPost("status")]
        public async Task<ActionResult<object>> GetStatus([FromBody] GetStatusRequest request)
        {
            if (request.ScheduleId.HasValue)
            {
                if (request.IncludeSummary)
                {
                    var summary = await _schedulingService.GetScheduleSummaryAsync(request.ScheduleId.Value);
                    return Ok(summary);
                }
                else
                {
                    var schedule = await _schedulingService.GetScheduleByIdAsync(request.ScheduleId.Value);
                    return Ok(schedule);
                }
            }
            else
            {
                var currentSchedule = await _schedulingService.GetCurrentScheduleAsync();
                if (request.IncludeSummary)
                {
                    var summary = await _schedulingService.GetScheduleSummaryAsync(currentSchedule.Id);
                    return Ok(summary);
                }
                else
                {
                    return Ok(currentSchedule);
                }
            }
        }

        [HttpPut("status")]
        public async Task<ActionResult<ProductionSchedule>> UpdateStatus([FromBody] UpdateStatusRequest request)
        {
            if (request.ItemId.HasValue)
            {
                if (request.Status == ScheduleItemStatus.Completed)
                {
                    var updatedSchedule = await _schedulingService.CompleteScheduleItemAsync(
                        request.ScheduleId,
                        request.ItemId.Value,
                        request.ActualQuantity,
                        request.ActualEndTime ?? DateTime.Now,
                        request.Notes);
                    return Ok(updatedSchedule);
                }
                else
                {
                    var updatedSchedule = await _schedulingService.UpdateScheduleItemStatusAsync(
                        request.ScheduleId,
                        request.ItemId.Value,
                        request.Status,
                        request.ActualStartTime,
                        request.ActualEndTime,
                        request.ActualQuantity,
                        request.Notes);
                    return Ok(updatedSchedule);
                }
            }
            return BadRequest("ItemId is required for status updates");
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
            if (!string.IsNullOrEmpty(request.NaturalLanguageRequest))
            {
                var currentSchedule = await _schedulingService.GetCurrentScheduleAsync();
                var updatedSchedule = await _schedulingService.ProcessNaturalLanguageRequestAsync(
                    request.NaturalLanguageRequest, currentSchedule);
                return Ok(updatedSchedule);
            }
            else if (request.ScheduleId.HasValue && request.StartNow)
            {
                var updatedSchedule = await _schedulingService.RescheduleToStartNowAsync(request.ScheduleId.Value);
                return Ok(updatedSchedule);
            }
            return BadRequest("Either NaturalLanguageRequest or ScheduleId with StartNow must be provided");
        }

        [HttpGet("insights")]
        public async Task<ActionResult<string>> GetInsights(int? scheduleId = null)
        {
            if (scheduleId.HasValue)
            {
                var schedule = await _schedulingService.GetScheduleByIdAsync(scheduleId.Value);
                var insights = await _schedulingService.GetScheduleInsightsAsync(schedule);
                return Ok(insights);
            }
            else
            {
                var currentSchedule = await _schedulingService.GetCurrentScheduleAsync();
                var insights = await _schedulingService.GetScheduleInsightsAsync(currentSchedule);
                return Ok(insights);
            }
        }

        [HttpPost("insights")]
        public async Task<ActionResult<string>> GetCustomInsights([FromBody] ProductionSchedule customSchedule)
        {
            var insights = await _schedulingService.GetScheduleInsightsAsync(customSchedule);
            return Ok(insights);
        }

    }
}
