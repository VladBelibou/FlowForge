using ManufacturingScheduler.Core.Models;

namespace ManufacturingScheduler.Core.Interfaces
{
    public interface ISchedulingAlgorithm
    {
        ProductionSchedule CreateSchedule(List<ProductionOrder> orders, List<Machine> machines, DateTime startDate);
    }
}
