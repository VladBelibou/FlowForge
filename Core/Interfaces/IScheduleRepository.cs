using ManufacturingScheduler.Core.Models;

namespace ManufacturingScheduler.Core.Interfaces
{
    public interface IScheduleRepository
    {
        Task<ProductionSchedule> GetCurrentScheduleAsync();
        Task SaveScheduleAsync(ProductionSchedule schedule);
        Task DeleteScheduleAsync(int scheduleId);
        Task<List<ProductionSchedule>> GetAllSchedulesAsync();
        Task<int> BatchDeleteSchedulesAsync(List<int> scheduleIds);
        Task<List<ProductionSchedule>> GetLastNSchedulesAsync(int count, string? createdByFilter = null, DateTime? createdBeforeDate = null); 
    }
}
