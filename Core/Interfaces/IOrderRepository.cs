namespace ManufacturingScheduler.Core.Interfaces
{
    public interface IOrderRepository
    {
        Task<List<ProductionOrder>> GetActiveOrdersAsync();
        Task<ProductionOrder?> GetOrderByIdAsync(int orderId);
        Task SaveOrderAsync(ProductionOrder order);
    }
}
