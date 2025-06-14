using ManufacturingScheduler.Core.Interfaces;

namespace ManufacturingScheduler.Infrastructure.Data.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly MockFileRepository<ProductionOrder> _mockRepository;

        public OrderRepository()
        {
            _mockRepository = new MockFileRepository<ProductionOrder>("orders.txt");
        }

        public async Task<List<ProductionOrder>> GetActiveOrdersAsync()
        {
            var allOrders = await _mockRepository.GetAllAsync();

            // Debug-Protokollierung
            Console.WriteLine($"DEBUG: Found {allOrders.Count} total orders");
            foreach (var order in allOrders)
            {
                Console.WriteLine($"DEBUG: Order {order.Id} - {order.ProductName} - Status: {order.Status}");
            }

            var activeOrders = allOrders.Where(o => o.Status == OrderStatus.Planned || o.Status == OrderStatus.InProgress).ToList();
            Console.WriteLine($"DEBUG: Found {activeOrders.Count} active orders");

            return activeOrders;
        }

        public async Task<ProductionOrder?> GetOrderByIdAsync(int orderId)
        {
            return await _mockRepository.GetByIdAsync(orderId, (order, id) => order.Id == id);
        }

        public async Task SaveOrderAsync(ProductionOrder order)
        {
            await _mockRepository.SaveItemAsync(order, o => o.Id);
        }
    }
}
