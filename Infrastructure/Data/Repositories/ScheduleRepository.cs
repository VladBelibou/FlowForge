using ManufacturingScheduler.Core.Interfaces;
using ManufacturingScheduler.Core.Models;

namespace ManufacturingScheduler.Infrastructure.Data.Repositories
{
    public class ScheduleRepository : IScheduleRepository
    {
        private readonly MockFileRepository<ProductionSchedule> _mockRepository;

        public ScheduleRepository()
        {
            _mockRepository = new MockFileRepository<ProductionSchedule>("schedules.txt");
        }

        public async Task<ProductionSchedule> GetCurrentScheduleAsync()
        {
            var allSchedules = await _mockRepository.GetAllAsync();
            return allSchedules.OrderByDescending(s => s.CreatedDate).FirstOrDefault() ??
                new ProductionSchedule
                {
                    Id = 1,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System",
                    ScheduleItems = new List<ScheduleItem>()
                };
        }

        public async Task SaveScheduleAsync(ProductionSchedule schedule)
        {
            await _mockRepository.SaveItemAsync(schedule, s => s.Id);
        }

        public async Task DeleteScheduleAsync(int scheduleId)
        {
            await _mockRepository.DeleteItemAsync(scheduleId);
        }

        public async Task<List<ProductionSchedule>> GetAllSchedulesAsync()
        {
            return await _mockRepository.GetAllAsync();
        }

        public async Task<int> BatchDeleteSchedulesAsync(List<int> scheduleIds)
        {
            var allSchedules = await _mockRepository.GetAllAsync();
            var initialCount = allSchedules.Count;

            var schedulesToKeep = allSchedules.Where(s => !scheduleIds.Contains(s.Id)).ToList();
            await _mockRepository.SaveAsync(schedulesToKeep);

            return initialCount - schedulesToKeep.Count;
        }

        public async Task<List<ProductionSchedule>> GetLastNSchedulesAsync(int count, string? createdByFilter = null, DateTime? createdBeforeDate = null)
        {
            var allSchedules = await _mockRepository.GetAllAsync();

            var filteredSchedules = allSchedules.AsQueryable();

            if (!string.IsNullOrWhiteSpace(createdByFilter))
            {
                filteredSchedules = filteredSchedules.Where(s => s.CreatedBy.Contains(createdByFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (createdBeforeDate.HasValue)
            {
                filteredSchedules = filteredSchedules.Where(s => s.CreatedDate < createdBeforeDate.Value);
            }

            return filteredSchedules
                .OrderByDescending(s => s.CreatedDate)
                .Take(count)
                .ToList();
        }
    }
}
