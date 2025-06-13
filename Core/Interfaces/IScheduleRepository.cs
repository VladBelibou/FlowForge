using ManufacturingScheduler.Core.Models;

namespace ManufacturingScheduler.Core.Interfaces
{
    public interface IScheduleRepository
    {
        Task<ProductionSchedule> GetCurrentScheduleAsync();
        Task SaveScheduleAsync(ProductionSchedule schedule);
        Task DeleteScheduleAsync(int scheduleId);
    }
}
