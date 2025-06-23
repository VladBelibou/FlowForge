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
            Console.WriteLine($"DEBUG SCHEDULING: Starte mit {orders.Count} Bestellungen und {machines.Count} Maschinen");

            var schedule = new ProductionSchedule
            {
                Id = new Random().Next(1000, 9999),
                CreatedDate = DateTime.Now,
                CreatedBy = "Algorithm",
                ScheduleItems = new List<ScheduleItem>()
            };

            // DEBUG: Zeigt welches Startdatum angefordert wurde
            Console.WriteLine($"DEBUG SCHEDULING: Angefordertes Startdatum: {startDate:MM/dd HH:mm}");

            var actualStartTime = DateTime.Now.AddMinutes(10); // Start in 10 minutes
            Console.WriteLine($"DEBUG SCHEDULING: Verwende sofortige Startzeit: {actualStartTime:MM/dd HH:mm}");

            // Debug-Maschinen
            Console.WriteLine("DEBUG SCHEDULING: Verfügbare Maschinen:");
            foreach (var machine in machines)
            {
                Console.WriteLine($"Maschine {machine.Id} - {machine.Name} - Betriebsbereit: {machine.IsOperational}");
                foreach (var capability in machine.ProductCapabilities)
                {
                    Console.WriteLine($"Kann folgendes produzieren: '{capability.ProductName}' (Setup: {capability.SetupTimeMinutes}min, Rate: {capability.ProductionRatePerHour}/Std)");
                }
            }

            // Bestellungen nach Priorität (höhere zuerst) und Fälligkeitsdatum sortieren
            var sortedOrders = orders.OrderByDescending(o => o.CustomerPriority)
                                   .ThenBy(o => o.DueDate)
                                   .ToList();

            Console.WriteLine("DEBUG SCHEDULING: Bestellungen werden verarbeitet:");
            foreach (var order in sortedOrders)
            {
                Console.WriteLine($"Bestellung {order.Id} - '{order.ProductName}' - Menge: {order.Quantity} - Priorität: {order.CustomerPriority}");
            }

            var currentTime = actualStartTime; // Use immediate start time

            foreach (var order in sortedOrders)
            {
                Console.WriteLine($"DEBUG SCHEDULING: Bestellung wird verarbeitet: {order.Id} - '{order.ProductName}'");

                // Passende Maschine finden
                var suitableMachine = machines.FirstOrDefault(m =>
                    m.IsOperational &&
                    m.ProductCapabilities.Any(pc =>
                        string.Equals(pc.ProductName, order.ProductName, StringComparison.OrdinalIgnoreCase)));

                if (suitableMachine != null)
                {
                    Console.WriteLine($"DEBUG SCHEDULING: Passende Maschine gefunden: {suitableMachine.Name}");

                    var capability = suitableMachine.ProductCapabilities
                        .First(pc => string.Equals(pc.ProductName, order.ProductName, StringComparison.OrdinalIgnoreCase));

                    Console.WriteLine($"DEBUG SCHEDULING: Verwende Höchstkapazität - Setup: {capability.SetupTimeMinutes}min, Rate: {capability.ProductionRatePerHour}/hr");

                    // Produktionszeit berechnen
                    var setupTime = TimeSpan.FromMinutes(capability.SetupTimeMinutes);
                    var productionHours = (double)order.Quantity / capability.ProductionRatePerHour;
                    var productionTime = TimeSpan.FromHours(productionHours);
                    var totalTime = setupTime + productionTime;
                    var endTime = currentTime + totalTime;

                    Console.WriteLine($"DEBUG SCHEDULING: Zeitberechnung - Setup: {setupTime}, Produktion: {productionTime}, Total: {totalTime}");
                    Console.WriteLine($"DEBUG SCHEDULING: Zeitslot: {currentTime:MM/dd HH:mm} - {endTime:MM/dd HH:mm}");

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
                    Console.WriteLine($"DEBUG SCHEDULING: Zeitplan-Element {scheduleItem.Id} hinzugefügt");

                    currentTime = endTime.AddMinutes(10); // Buffer time between jobs
                    Console.WriteLine($"DEBUG SCHEDULING: Nächste verfügbare Zeit: {currentTime:MM/dd HH:mm}");
                }
                else
                {
                    Console.WriteLine($"DEBUG SCHEDULING: Keine passende Maschine für Produkt '{order.ProductName}' gefunden");
                }
            }

            Console.WriteLine($"DEBUG SCHEDULING: Finaler Zeitplan hat {schedule.ScheduleItems.Count} Elemente");

            // Set explanation with actual timeline
            if (schedule.ScheduleItems.Any())
            {
                var lastEndTime = schedule.ScheduleItems.Max(item => item.EndTime);
                schedule.Explanation = $"Zeitplan startet sofort um {actualStartTime:MM/dd HH:mm} und wird am {lastEndTime:MM/dd HH:mm} abgeschlossen. Bereit für die Produktion!";
            }

            return schedule;
        }
    }
}
