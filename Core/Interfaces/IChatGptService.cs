using ManufacturingScheduler.Core.Models;

namespace ManufacturingScheduler.Core.Interfaces
{
    public interface IChatGptService
    {
        Task<SchedulingInterpretation> InterpretSchedulingRequestAsync(string request, ProductionSchedule currentSchedule);
        Task<string> AnalyzeScheduleAsync(ProductionSchedule schedule);
    }
}
