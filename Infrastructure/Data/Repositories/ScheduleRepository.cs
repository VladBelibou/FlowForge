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
    }
}
