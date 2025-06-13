namespace ManufacturingScheduler.Core.Interfaces
{
    using ManufacturingScheduler.Core.Interfaces;
    using ManufacturingScheduler.Core.Models;
    public interface IMachineRepository
    {
        Task<List<Machine>> GetOperationalMachinesAsync();
        Task<Machine?> GetMachineByIdAsync(int machineId);
    }
}
