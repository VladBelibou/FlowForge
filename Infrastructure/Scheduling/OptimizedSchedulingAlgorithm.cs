using ManufacturingScheduler.Core.Interfaces;
using ManufacturingScheduler.Core.Models;

namespace ManufacturingScheduler.Infrastructure.Scheduling
{
    using ManufacturingScheduler.Core.Interfaces;
    using ManufacturingScheduler.Core.Models;
    public class OptimizedSchedulingAlgorithm : ISchedulingAlgorithm
    {
        private readonly int _startDelayMinutes;
        private readonly int _bufferMinutes;

        public OptimizedSchedulingAlgorithm(IConfiguration configuration)
        {
            _startDelayMinutes = configuration.GetValue<int>("SchedulingOptions:DefaultStartDelayMinutes", 10);
            _bufferMinutes = configuration.GetValue<int>("SchedulingOptions:BufferBetweenJobsMinutes", 10);
        }

        public ProductionSchedule CreateSchedule(List<ProductionOrder> orders, List<Machine> machines, DateTime startDate)
        {
            Console.WriteLine($"DEBUG SCHEDULING: Starting with {orders.Count} orders and {machines.Count} machines");

            var schedule = new ProductionSchedule
            {
                Id = new Random().Next(1000, 9999),
                CreatedDate = DateTime.Now,
                CreatedBy = "Algorithm",
                ScheduleItems = new List<ScheduleItem>()
            };

            // DEBUG: Show what start date was requested
            Console.WriteLine($"DEBUG SCHEDULING: Requested start date: {startDate:MM/dd HH:mm}");

            var actualStartTime = DateTime.Now.AddMinutes(10); // Start in 10 minutes
            Console.WriteLine($"DEBUG SCHEDULING: Using immediate start time: {actualStartTime:MM/dd HH:mm}");

            // Debug machines
            Console.WriteLine("DEBUG SCHEDULING: Available machines:");
            foreach (var machine in machines)
            {
                Console.WriteLine($"  Machine {machine.Id} - {machine.Name} - Operational: {machine.IsOperational}");
                foreach (var capability in machine.ProductCapabilities)
                {
                    Console.WriteLine($"    Can make: '{capability.ProductName}' (Setup: {capability.SetupTimeMinutes}min, Rate: {capability.ProductionRatePerHour}/hr)");
                }
            }

            // Sort orders by priority (higher priority first) and due date
            var sortedOrders = orders.OrderByDescending(o => o.CustomerPriority)
                                   .ThenBy(o => o.DueDate)
                                   .ToList();

            Console.WriteLine("DEBUG SCHEDULING: Processing orders:");
            foreach (var order in sortedOrders)
            {
                Console.WriteLine($"  Order {order.Id} - '{order.ProductName}' - Qty: {order.Quantity} - Priority: {order.CustomerPriority}");
            }

            var currentTime = actualStartTime; // Use immediate start time

            foreach (var order in sortedOrders)
            {
                Console.WriteLine($"\nDEBUG SCHEDULING: Processing Order {order.Id} - '{order.ProductName}'");

                // Find suitable machine
                var suitableMachine = machines.FirstOrDefault(m =>
                    m.IsOperational &&
                    m.ProductCapabilities.Any(pc =>
                        string.Equals(pc.ProductName, order.ProductName, StringComparison.OrdinalIgnoreCase)));

                if (suitableMachine != null)
                {
                    Console.WriteLine($"DEBUG SCHEDULING: Found suitable machine: {suitableMachine.Name}");

                    var capability = suitableMachine.ProductCapabilities
                        .First(pc => string.Equals(pc.ProductName, order.ProductName, StringComparison.OrdinalIgnoreCase));

                    Console.WriteLine($"DEBUG SCHEDULING: Using capability - Setup: {capability.SetupTimeMinutes}min, Rate: {capability.ProductionRatePerHour}/hr");

                    // Calculate production time
                    var setupTime = TimeSpan.FromMinutes(capability.SetupTimeMinutes);
                    var productionHours = (double)order.Quantity / capability.ProductionRatePerHour;
                    var productionTime = TimeSpan.FromHours(productionHours);
                    var totalTime = setupTime + productionTime;
                    var endTime = currentTime + totalTime;

                    Console.WriteLine($"DEBUG SCHEDULING: Time calculation - Setup: {setupTime}, Production: {productionTime}, Total: {totalTime}");
                    Console.WriteLine($"DEBUG SCHEDULING: Schedule slot: {currentTime:MM/dd HH:mm} - {endTime:MM/dd HH:mm}");

                    var scheduleItem = new ScheduleItem
                    {
                        Id = schedule.ScheduleItems.Count + 1,
                        OrderId = order.Id,
                        MachineId = suitableMachine.Id,
                        StartTime = currentTime,
                        EndTime = endTime,
                        PlannedQuantity = order.Quantity
                    };

                    schedule.ScheduleItems.Add(scheduleItem);
                    Console.WriteLine($"DEBUG SCHEDULING: Added schedule item {scheduleItem.Id}");

                    currentTime = endTime.AddMinutes(10); // Buffer time between jobs
                    Console.WriteLine($"DEBUG SCHEDULING: Next available time: {currentTime:MM/dd HH:mm}");
                }
                else
                {
                    Console.WriteLine($"DEBUG SCHEDULING: No suitable machine found for product '{order.ProductName}'");
                }
            }

            Console.WriteLine($"DEBUG SCHEDULING: Final schedule has {schedule.ScheduleItems.Count} items");

            // Set explanation with actual timeline
            if (schedule.ScheduleItems.Any())
            {
                var lastEndTime = schedule.ScheduleItems.Max(item => item.EndTime);
                schedule.Explanation = $"Schedule starts immediately at {actualStartTime:MM/dd HH:mm} and completes at {lastEndTime:MM/dd HH:mm}. Ready to begin production!";
            }

            return schedule;
        }
    }
}
